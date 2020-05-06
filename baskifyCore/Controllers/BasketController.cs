using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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

        [HttpPost]
        public IActionResult viewModal(int basketID, int auctionID)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if(user == null)
            {
                return Content("ERROR: INVALID LOGIN");
            }

            BasketModel basket;
            if ((basket = _context.BasketModel.Find(basketID)) == null)
                return Content("ERROR: BASKET NOT FOUND");

            _context.Entry(basket).Reference(b => b.AuctionModel).Load(); //get the auction from the basket
            _context.Entry(basket).Collection(b => b.photos).Load(); //load in any pictures

            if (basket.AuctionModel.HostUsername != user.Username) //check the auction's owner
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
            BasketModel basket;
            if ((basket = _context.BasketModel.Find(basketId)) == null)
                return Content("ERROR: BASKET NOT FOUND");

            _context.Entry(basket).Reference(b => b.AuctionModel).Load(); //get the auction from the basket
            if (basket.AuctionModel.HostUsername != user.Username)
                return Content("ERROR: INVALID BASKET OWNER");

            if (basket.AcceptedByOrg)
                return Content("ERROR: BASKET ALREADY VALIDATED");

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
            _context.Entry(basket).Reference(b => b.AuctionModel).Load();

            if (basket.AuctionModel.HostUsername != user.Username)
                return Content("ERROR: INVALID USER");

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
        public IActionResult updateBasket(BasketModel updatedBasket)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?redirectBack=" + Request.GetDisplayUrl());

            ViewData["NavBarOverride"] = user;
            var dbBasket = _context.BasketModel.Find(updatedBasket.BasketId);
            if (dbBasket == null) //basket does not exist...
            {
                ViewData["Alert"] = "The basket " + HttpUtility.HtmlEncode(updatedBasket.BasketTitle) + " was not found";
                AuctionModel userInputtedAuction = null;
                if ((userInputtedAuction = _context.AuctionModel.Find(updatedBasket.AuctionId)) != null && userInputtedAuction.HostUsername == user.Username)
                {
                    _context.Entry(userInputtedAuction).Collection(a => a.Baskets).Load();
                    return View("~/Views/Auctions/EditAuction.cshtml", userInputtedAuction); //if the auction is the users' we can send them back
                }

                return View("~/Views/Home/Index.cshtml", user);
            }

            _context.Entry(dbBasket).Reference(b => b.AuctionModel).Load();
            if(dbBasket.AuctionModel.HostUsername != user.Username)
            {
                ViewData["Alert"] = "You do not have access to this auction";
                return View("~/Views/Home/Index.cshtml", user);
            }

            //at this point the auction and basket are safe to edit
            _context.Entry(dbBasket).Collection(b => b.photos).Load(); //load photos in
            updatedBasket.photos = dbBasket.photos; //to satisfy the galleria viewer
            updatedBasket.AcceptedByOrg = dbBasket.AcceptedByOrg; //to keep things from getting weird
            _context.Entry(dbBasket.AuctionModel).Collection(a => a.Baskets).Load();

            if (!ModelState.IsValid)
            {
                ViewData["basket"] = updatedBasket;
                return View("~/Views/Auctions/EditAuction.cshtml", dbBasket.AuctionModel); //bad inputs, send them back!
            }

            var imageSet = new SortedSet<string>();
            if (updatedBasket.addImages != null)
            {
                foreach (var imageUrl in updatedBasket.addImages)
                {
                    var pendingModel = _context.PendingImageModel.Find(imageUrl);
                    if (pendingModel != null) //only let the user gain delete control of the image if they added it in the first place
                    {
                        if (updatedBasket.removeImages != null && updatedBasket.removeImages.Contains(imageUrl)) //don't add any images that the user removed, just delete them from DB
                        {
                            _context.PendingImageModel.Remove(pendingModel);
                            imageUtilities.deleteFile(imageUrl, _env.WebRootPath);
                        }
                        else
                        {
                            _context.PendingImageModel.Remove(pendingModel);
                            var basketImageModel = new BasketPhotoModel() { BasketId = dbBasket.BasketId, Url = imageUrl }; //add image to basket officially
                            _context.BasketPhotoModel.Add(basketImageModel);
                        }
                    }
                }
            }

            if (updatedBasket.removeImages != null)
            {
                foreach (var photo in dbBasket.photos.ToList())
                {
                    if (updatedBasket.removeImages.Contains(photo.Url))
                    {
                        _context.BasketPhotoModel.Remove(photo); //remove any photos attributed to the basket that were requested
                        imageUtilities.deleteFile(photo.Url, _env.WebRootPath);
                    }
                }
            }

            //now any desired photos have been added and removed, lets update other stuff
            updatedBasket.BasketContents.RemoveAll(m => string.IsNullOrWhiteSpace(m));
            updatedBasket.BasketContents.ForEach(e => HttpUtility.HtmlEncode(e)); //clean the contents of bad chars and whitespace

            dbBasket.BasketContents = updatedBasket.BasketContents;
            dbBasket.BasketDescription =  updatedBasket.BasketDescription.Replace(">", String.Empty)
                    .Replace("<", String.Empty)
                    .Replace("\'", String.Empty)
                    .Replace("\"", String.Empty)
                    .Replace("&", "and"); //avoid injection
            dbBasket.BasketTitle = updatedBasket.BasketTitle;

            _context.SaveChanges();

            ViewData["Alert"] = "Basket updated successfully!";
            return View("~/Views/Auctions/EditAuction.cshtml", dbBasket.AuctionModel);
        }
    }
}
