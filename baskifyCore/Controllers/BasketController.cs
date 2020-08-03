using baskifyCore.Models;
using baskifyCore.Utilities;
using baskifyCore.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace baskifyCore.Controllers
{
    public class BasketController : Controller
    {
        private readonly IWebHostEnvironment _env;
        ApplicationDbContext _context;
        public BasketController(IWebHostEnvironment env)
        {
            _context = new ApplicationDbContext();
            _env = env;
        }


        /// <summary>
        /// In this procedure, ANY user can view a basket. ONLY companies see the verification warning 
        /// and ONLY companies can make new baskets here. If a basket ID of -1 is passed and the user is a company, a new basket
        /// will be minted.
        /// 
        /// A user can only view a basket they submitted IF it has not yet been verified by the organization.
        /// </summary>
        /// <param name="basketID"></param>
        /// <param name="auctionID"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult viewModal(int basketID, int auctionID)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if(user == null)
            {
                return Content("ERROR: INVALID LOGIN");
            }

            if (user.UserRole != Roles.COMPANY)
                ViewData["HideVerification"] = true; //dont show verification for users

            //------------------------------------------------START BUILD NEW ORGANIZATION BASKET----------------------------------------------------
            BasketModel basket;
            if (basketID == -1 && user.UserRole == Roles.COMPANY) //means a new basket is being built, DRAFT IS TRUE
            {
                var auction = _context.AuctionModel.Find(auctionID);
                if (auction == null)
                    return Content("ERROR: AUCTION NOT FOUND");

                if (auction.HostUsername != user.Username)
                    return Content("ERROR: THIS AUCTION IS NOT YOURS");

                basket = basketUtils.getDraftBasket(user, _context, auctionID);
                ModelState.Clear();

                //this doesn't get passed to the DB, just makes the basket cleaner
                basket.BasketTitle = String.Empty;
                basket.BasketContents = null;
                basket.BasketDescription = String.Empty;

                return PartialView("BasketModalPartialView", basket);
            }
            //------------------------------------------------END BUILD NEW ORGANIZATION BASKET----------------------------------------------------

            if ((basket = _context.BasketModel.Find(basketID)) == null)
                return Content("ERROR: BASKET NOT FOUND");

            _context.Entry(basket).Reference(b => b.AuctionModel).Load(); //get the auction from the basket
            _context.Entry(basket).Collection(b => b.photos).Load(); //load in any pictures

            if (!(basket.AuctionModel.HostUsername == user.Username || (user.Username == basket.SubmittingUsername && !basket.AcceptedByOrg))) //check the auction's owner or basket's owner
                return Content("ERROR: INVALID BASKET OWNER");

            //at this point, the basket belongs to the auction that belongs to the user! Now we can do what we must!

            return PartialView("BasketModalPartialView", basket);
        }

        [HttpPost]
        public IActionResult verifyBasket(int basketId)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Content("ERROR: INVALID LOGIN");

            if (user.UserRole != Roles.COMPANY)
                return Content("ERROR: ONLY ORGANIZATIONS CAN VERIFY BASKETS");

            BasketModel basket;
            if ((basket = _context.BasketModel.Find(basketId)) == null)
                return Content("ERROR: BASKET NOT FOUND");

            _context.Entry(basket).Reference(b => b.AuctionModel).Load(); //get the auction from the basket
            if (basket.AuctionModel.HostUsername != user.Username)
                return Content("ERROR: INVALID BASKET OWNER");

            if (basket.AcceptedByOrg)
                return Content("ERROR: BASKET ALREADY VALIDATED");

            _context.Entry(basket).Collection(b => b.photos).Load();
            if (basket.photos.Count == 0)
                return Content("ERROR: ADD A PHOTO FIRST");

            //now we have the correct owner
            basket.AcceptedByOrg = true;
            _context.SaveChanges();
            return Content("SUCCESS");
        }

        [HttpPost]
        public IActionResult uploadBasketImages(IFormFile[] files, int basketId)
        {
            foreach(var file in files) //don't allow bad filetypes
            {
                if (!file.ContentType.StartsWith("image"))
                    return Content("ERROR: INVALID FILETYPES");
            }

            if (files == null)
                return Content("ERROR: NO FILES");
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Content("ERROR: INVALID LOGIN");

            var basket = _context.BasketModel.Find(basketId); //get auction
            if (basket == null)
                return Content("ERROR: INVALID BASKET");
            _context.Entry(basket).Reference(b => b.AuctionModel).Load();

            if (!(basket.AuctionModel.HostUsername == user.Username || (basket.SubmittingUsername == user.Username && !basket.AcceptedByOrg)))
                return Content("ERROR: INVALID USER");

            if (basket.AuctionModel.EndTime < DateTime.UtcNow)
                return Content("ERROR: BASKET'S AUCTION HAS ENDED");

            //now we have a valid user...
            _context.Entry(basket).Collection(b => b.photos).Load(); //get photos
            if (basket.photos.Count + files.Length > 10)
                return Content("ERROR: MAXIMUM OF 10 PHOTOS PER BASKET"); //no more than 10 photos per basket

            List<string> newUrls = new List<string>();
            foreach(var imageFile in files)
            {
                var url = imageUtilities.uploadFile(imageFile, _env.WebRootPath, "Content/basketImages", -1, -1, "basket"+basketId+"image"+Guid.NewGuid().ToString().Substring(0,5)); //unique name with GUID
                if (url != null) //don't add a file if it couldn't save
                {
                    /* DONT ADD FILES TO THE MODEL HERE, THIS OCCURS AT SAVE CHANGES
                    var photoModel = new BasketPhotoModel() { BasketId = basket.BasketId, Url = url }; //depreciate photo descriptions
                    basket.photos.Add(photoModel);
                    */
                    var pendingModel = new PendingImageModel() { ImageUrl = url, Username = user.Username };
                    _context.PendingImageModel.Add(pendingModel); //add the pending model

                    newUrls.Add(url);
                }
            }
            _context.SaveChanges();

            return Content(JsonSerializer.Serialize(newUrls)); //sends json of the urls

        }

        [HttpPost]
        public IActionResult orgUpdateBasket(BasketModel updatedBasket)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?redirectBack=" + Request.GetDisplayUrl());

            if (user.UserRole != Roles.COMPANY)
            {
                ViewData["Alert"] = "Only organizations can update baskets here...";
                return View("~/Views/Home/Index.cshtml", user);
            }

            ViewData["NavBarOverride"] = user;
            var dbBasket = _context.BasketModel.Find(updatedBasket.BasketId);
            if (dbBasket == null) //basket does not exist
            {
                ViewData["Alert"] = "The basket " + HttpUtility.HtmlEncode(updatedBasket.BasketTitle) + " was not found";
                AuctionModel userInputtedAuction = null;
                if ((userInputtedAuction = _context.AuctionModel.Include(a => a.Baskets).Include(a => a.Payments).Where(a => a.AuctionId == updatedBasket.AuctionId).FirstOrDefault()) != null && userInputtedAuction.HostUsername == user.Username)
                {
                    return View("~/Views/Auctions/EditAuction.cshtml", userInputtedAuction); //if the auction is the users' we can send them back
                }

                return View("~/Views/Home/Index.cshtml", user);
            }

            var auction = _context.AuctionModel.Include(a => a.Payments).Include(a => a.Baskets).Where(a => a.AuctionId == dbBasket.AuctionId).First();
            if(dbBasket.AuctionModel.HostUsername != user.Username) //if the user doesn't own the auction OR the org has authed, reject
            {
                ViewData["Alert"] = "You do not have access to this basket";
                return View("~/Views/Home/Index.cshtml", user);
            }

            if (!basketUtils.updateBasket(dbBasket, updatedBasket, _context, ModelState, _env.WebRootPath))
            { //returns false for model errors
                ViewData["basket"] = updatedBasket; //send the basket with errors to be fixed
                return View("~/Views/Auctions/EditAuction.cshtml", dbBasket.AuctionModel);
            }

            //all is well!
            ViewData["Alert"] = "Basket updated successfully!";
            return View("~/Views/Auctions/EditAuction.cshtml", dbBasket.AuctionModel);
        }


        /// <summary>
        /// This is where users can update baskets that they own in /basket/userBaskets...
        /// Takes in an updated basket, redirects to /basket/userBaskets...
        /// </summary>
        /// <param name="updatedBasket"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult userUpdateBasket(BasketModel updatedBasket)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?redirectBack=" + Request.GetDisplayUrl());

            ViewData["NavBarOverride"] = user;
            var dbBasket = _context.BasketModel.Find(updatedBasket.BasketId);
            if (dbBasket == null) //basket does not exist
            {
                ViewData["Alert"] = "The basket " + HttpUtility.HtmlEncode(updatedBasket.BasketTitle) + " was not found";
                return View("UserBasketsView", user);
            }

            _context.Entry(dbBasket).Reference(b => b.AuctionModel).Load();
            if (dbBasket.AcceptedByOrg || dbBasket.SubmittingUsername != user.Username) //if the user doesn't own the auction OR the org has authed, reject
            {
                ViewData["Alert"] = "You do not have access to this basket";
                return View("UserBasketsView", user);
            }

            if(dbBasket.AuctionModel.EndTime < DateTime.UtcNow) //users cannot edit complete auctions' baskets
            {
                ViewData["Alert"] = "This auction has already ended, the basket cannot be changed";
                return View("UserBasketsView", user);
            }


            if (!basketUtils.updateBasket(dbBasket, updatedBasket, _context, ModelState, _env.WebRootPath))
            { //returns false for model errors
                ViewData["basket"] = updatedBasket; //send the basket with errors to be fixed
                return View("UserBasketsView", user);
            }

            //all is well!
            ViewData["Alert"] = "Basket updated successfully!";
            return View("UserBasketsView", user);
        }

        /// <summary>
        /// This is where users can update baskets that they own in /basket/userBaskets...
        /// Takes in an updated basket, redirects to /auctions/addBasket...
        /// </summary>
        /// <param name="updatedBasket"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult userUpdateAuctionBasket(BasketModel updatedBasket)
        {
            
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?redirectBack=" + Request.GetDisplayUrl());

            Guid linkGUID;
            try //ensure the GUID portion of the url exists
            {
                linkGUID = Guid.Parse(Request.Headers["Referer"].ToString().ToLower().Split("/addbasket/")[1]); //referer omits anything after # in url
            }
            catch (Exception)
            {
                ViewData["Alert"] = "Invalid link";
                return View("~/Views/Home/Index.cshtml", user);
            }

            var LinkModel = _context.AuctionLinkModel.Where(lm => (lm.Link == linkGUID)).FirstOrDefault();
            if (LinkModel == null)
            {
                ViewData["Alert"] = "Invalid link";
                return View("~/Views/Home/Index.cshtml", user);
            }

            _context.Entry(LinkModel).Reference(lm => lm.Auction).Query().Include(a => a.Baskets).Include(a => a.HostUser).Load(); //load auction, baskets, and host

            if(LinkModel.Auction.EndTime < DateTime.UtcNow)
            {
                ViewData["Alert"] = "You cannot change baskets in completed auctions";
                return View("~/Views/Home/Index.cshtml", user);
            }

            //generate the model
            var baskets = LinkModel.Auction.Baskets.Where(b => (b.SubmittingUsername == user.Username));
            var model = new userAddBasketViewModel() { Auction = LinkModel.Auction, AuctionAddLink = LinkModel.Link, Baskets = new List<BasketModel>(baskets), User = user };

            ViewData["NavBarOverride"] = user;
            var dbBasket = _context.BasketModel.Find(updatedBasket.BasketId);
            if (dbBasket == null) //basket does not exist
            {
                ViewData["Alert"] = "The basket " + HttpUtility.HtmlEncode(updatedBasket.BasketTitle) + " was not found";
                return View("UserAddBasket", model);
            }

            if(dbBasket.AuctionId != LinkModel.AuctionId)
            {
                ViewData["Alert"] = "Link auction does not match basket auction";
                return View("~/Views/Home/Index.cshtml", model);
            }

            _context.Entry(dbBasket).Reference(b => b.AuctionModel).Load();
            if (dbBasket.AcceptedByOrg || dbBasket.SubmittingUsername != user.Username) //if the user doesn't own the auction OR the org has authed, reject
            {
                ViewData["Alert"] = "You do not have access to this basket";
                return View("UserAddBasket", model);
            }

            if (!basketUtils.updateBasket(dbBasket, updatedBasket, _context, ModelState, _env.WebRootPath))
            { //returns false for model errors
                ViewData["basket"] = updatedBasket; //send the basket with errors to be fixed
                return View("UserAddBasket", model);
            }

            //all is well!
            ViewData["Alert"] = "Basket updated successfully!";
            return View("UserAddBasket", model);
        }



        [HttpPost]
        public IActionResult delete(int auctionID, int basketID)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Content("ERROR: INVALID LOGIN");

            var basket = _context.BasketModel.Find(basketID);
            if (basket == null)
                return Content("ERROR: BASKET NOT FOUND");

            if (basket.AuctionId != auctionID)
                return Content("ERROR: BASKET DOES NOT BELONG TO THIS AUCTION");

            _context.Entry(basket).Reference(b => b.AuctionModel).Load();
            if (!(basket.AuctionModel.HostUsername == user.Username | user.Username == basket.SubmittingUsername && !basket.AcceptedByOrg)) //can be deleted by org OR a user if the org hasn't yet verified
                return Content("ERROR: USER DOES NOT HAVE ACCESS TO THIS BASKET");

            if (basket.AuctionModel.StartTime < DateTime.UtcNow && basket.AcceptedByOrg) //you cannot delete a basket once the auction has started IF it is validated
                return Content("ERROR: THIS BASKET'S AUCTION IS ALREADY LIVE");

            //at this point the basket belongs to an auction that the user owns

            if (basketUtils.deleteBasket(basket, _context, _env.WebRootPath))
            {
                try
                {
                    _context.SaveChanges();
                    return Content("SUCCESS");
                }
                catch (Exception)
                {
                    return Content("ERROR: DELETION FAILED");
                }
                
            }
            else
                return Content("ERROR: DELETION FAILED");


        }

        [HttpGet]
        public IActionResult userBaskets()
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if(user == null) //redirect to login
            {
                return Redirect("/login?=" + LoginUtils.getAbsoluteUrl("basket/userBaskets", Request));
            }

            if (user.UserRole == Roles.COMPANY)
                return Redirect("/auctions"); //send organizations elsewhere

            return View("UserBasketsView", user);
        }

        [HttpPost]
        public IActionResult userCreate(Guid Link)
        {
            var disputes = new List<BasketModel>();
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Content("ERROR: INVALID LOGIN");
            var AuctionLink = _context.AuctionLinkModel.Include(a => a.Auction).Where(al => (al.Link == Link)).FirstOrDefault();
            if (AuctionLink == null)
                return Content("ERROR: INVALID AUCTION LINK");

            //Oragnizations are allowed to use this, but why?
            if(user.UserRole == Roles.COMPANY && AuctionLink.Auction.HostUsername != user.Username) //keep an org from owning baskets
                return Content("ERROR: ORGANIZATIONS CANNOT CREATE BASKETS FOR OTHER ORGANIZATIONS");

            if (AuctionLink.Auction.EndTime < DateTime.UtcNow)
                return Content("ERROR: AUCTION HAS ENDED"); //can't put a new basket in a terminated auction

            //ONLY require matching state, since solicitation rights are statewide
            if (!AuctionLink.Auction.CanParticipate(user, _context, true))
                return Content("ERROR: YOU ARE NOT WITHIN THIS AUCTION'S JURISDICTION");

            if ((disputes = _context.BasketModel.Include(b => b.AuctionModel).Where(b => b.SubmittingUsername == user.Username && b.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter && b.DisputedShipment).ToList()).Count > 0)
            {
                var error = $"You cannot create any baskets when you have pending disputes on basket(s): {string.Join(',', disputes.Select(d => d.BasketTitle + $" (in '{d.AuctionModel.Title}')" ))}";
                return Content(error);
            }

            var basket = basketUtils.getDraftBasket(user, _context, AuctionLink.AuctionId); //get draft, destroy old drafts

            ModelState.Clear();
            return PartialView("BasketModalPartialView", basket);
        }
    }
}
