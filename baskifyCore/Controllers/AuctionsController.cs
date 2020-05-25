using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Controllers.api;
using baskifyCore.Migrations;
using baskifyCore.Models;
using baskifyCore.Utilities;
using baskifyCore.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace baskifyCore.Controllers
{
    public class AuctionsController : Controller
    {
        private readonly IWebHostEnvironment _env;
        ApplicationDbContext _context;
        public AuctionsController(IWebHostEnvironment env)
        {
            _env = env;
            _context = new ApplicationDbContext();
        }

        /// <summary>
        /// Lists the organization's auctions
        /// </summary>
        /// <returns></returns>
        public IActionResult Index(int? page)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?redirectBack=" + LoginUtils.getAbsoluteUrl("/auctions", Request));
            else if (user.UserRole != Roles.COMPANY)
                Redirect("/"); //send home if a user

            ViewData["page"] = page;
            _context.Entry(user).Collection(u => u.Auctions).Load(); //get auctions

            return View(user);
        }

        [HttpGet]
        public IActionResult createAuction()
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?=" + LoginUtils.getAbsoluteUrl("/auctions/createauction", Request));
            else if (user.UserRole != Roles.COMPANY)
            {
                ViewData["Alert"] = "You do not have access to this resource";
                return View("~/Home/Index.cshtml", user);
            }
            ViewData["NavBarOverride"] = user;
            var auctionModel = new AuctionModel() { HostUsername = user.Username };
            return View(auctionModel);
        }

        [HttpPost]
        public IActionResult createAuction(AuctionModel auction)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null) {
                ViewData["LoginAgain"] = true; //require log back in
                ViewData["NavBarOverride"] = user;
                return View(auction);
            }
            else if (user.UserRole != Roles.COMPANY)
            {
                ViewData["Alert"] = "You do not have access to this resource";
                return View("~/Home/Index.cshtml", user);
            }

            if (auction.StartTime < DateTime.UtcNow)
                ModelState.AddModelError("StartTime", "Auction cannot have already started!");

            var addressDict = accountUtils.validateAddress(auction.Address, auction.City, auction.State, auction.ZIP);
            if (addressDict["resultStatus"] == "ADDRESS NOT FOUND") //now, addresses are validated in the model
                ModelState.AddModelError("Address", "Address not found");
            else
            {
                auction.Address = addressDict["addressLine1"];
                auction.City = addressDict["city"];
                auction.State = addressDict["state"];
                auction.ZIP = addressDict["zip"];
                auction.Latitude = float.Parse(addressDict["lat"]);
                auction.Longitude = float.Parse(addressDict["lng"]);
            }

            if (!ModelState.IsValid)
            {
                ViewData["Alert"] = "Invalid input! Check your form values and readd any banner!";
                ViewData["NavBarOverride"] = user;
                return View(auction);
            }

            try {
                auction.HostUsername = user.Username;
                auction.Description = auction.Description.Replace(">", String.Empty)
                    .Replace("<", String.Empty)
                    .Replace("\'", String.Empty)
                    .Replace("\"", String.Empty)
                    .Replace("&", "and"); //avoid injection
                var auctionId = AuctionUtilities.addAuction(auction, user, _context);
                if (auction.BannerImage != null)
                    auction.BannerImageUrl = imageUtilities.uploadFile(auction.BannerImage, _env.WebRootPath, "/Content/Auctions/BannerImages/", 300, 1000, "auctionBanner" + auctionId.ToString()); //save image
                _context.SaveChanges();
                ViewData["Alert"] = "Auction added successfully!";
                ViewData["NavBarOverride"] = user;
                ModelState.Clear();

                _context.Entry(user).Collection(u => u.Auctions).Load(); //load auctions for auction list...
                return View("~/Views/Auctions/Index.cshtml", user);
            }
            catch (Exception)
            {
                ViewData["NavBarOverride"] = user;
                ViewData["Alert"] = "An unknown error occurred, please try again...";
                return View(auction);
            }

        }

        [HttpGet]
        public IActionResult EditAuction(int id)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["bearerToken"], _context, Response);

            if(user != null)
                _context.Entry(user).Collection(u => u.Auctions).Load(); //load in auctions

            if (user == null)//not logged in
            {
                return Redirect("/login?redirectBack=" + LoginUtils.getAbsoluteUrl("auctions/editauction/"+id, Request));
            }
            else if (user.Auctions != null && user.Auctions.Any(a => a.AuctionId == id)) //user hosts this auction?
            {
                var auction = _context.AuctionModel.Find(id);
                if(auction.EndTime < DateTime.UtcNow)
                {
                    ViewData["Alert"] = "You cannot edit a completed auction!";
                    return View("~/Views/Auctions/Index.cshtml", user); //send them back to the auction list!
                }

                _context.Entry(auction).Collection(a => a.Baskets).Load();

                ViewData["NavBarOverride"] = user;
                return View(auction);
            }
            else
            {
                ViewData["Alert"] = "Auction not found!";
                return View("~/Views/Auctions/Index.cshtml", user);
            }
        }

        [HttpPost]
        public IActionResult editAuction(AuctionModel auction)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if(user == null)//if the user isn't logged in, give them a chance...
            {
                return Redirect("/login?redirectBack=" + LoginUtils.getAbsoluteUrl("/auctions/editAuction/" + auction.AuctionId, Request));
            }

            ViewData["NavBarOverride"] = user;
            var dbAuction = _context.AuctionModel.Find(auction.AuctionId);

            if(dbAuction == null)
            {
                ViewData["Alert"] = "Auction not found... try again.";
                return View("~/Views/Auctions/Index.cshtml", user);
            }

            if(dbAuction.HostUsername != user.Username) //not their auction
            {
                ViewData["Alert"] = "You do not have access to this auction.";
                return View("~/Views/Auctions/Index.cshtml", user);
            }
            if(dbAuction.EndTime < DateTime.UtcNow)
            {
                ViewData["Alert"] = "You cannot edit an ended auction.";
                return View("~/Views/Auctions/Index.cshtml", user);
            }

            var addressDict = accountUtils.validateAddress(auction.Address, auction.City, auction.State, auction.ZIP);
            if (addressDict["resultStatus"] == "ADDRESS NOT FOUND") //now, addresses are validated in the model
                ModelState.AddModelError("Address", "Address not found");
            else
            {
                auction.Address = addressDict["addressLine1"];
                auction.City = addressDict["city"];
                auction.State = addressDict["state"];
                auction.ZIP = addressDict["zip"];
                auction.Latitude = float.Parse(addressDict["lat"]);
                auction.Longitude = float.Parse(addressDict["lng"]);
            }


            //at this point, it's their auction, they can edit
            //isLive limits what they can edit...
            var isLive = dbAuction.StartTime < DateTime.UtcNow;

            if (isLive && dbAuction.StartTime != auction.StartTime)
            {//make sure they are not trying to change any times...
                auction.StartTime = dbAuction.StartTime; //reset times to avoid weird window behavior
                auction.EndTime = dbAuction.EndTime;
                ModelState.AddModelError("StartTime", "You cannot change the start time of an in-progress auction");
            }
            else if (!isLive && (auction.StartTime - DateTime.UtcNow).TotalHours < 1)
            {
                auction.StartTime = dbAuction.StartTime;
                auction.EndTime = dbAuction.EndTime;
                ModelState.AddModelError("StartTime", "The start time must be at least an hour in the future (UTC)");
            }
            else if (!isLive && (auction.EndTime - DateTime.UtcNow).TotalHours < 1)
            {
                auction.StartTime = dbAuction.StartTime;
                auction.EndTime = dbAuction.EndTime;
                ModelState.AddModelError("EndTime", "The end time must be at least an hour in the future (UTC)");
            }
            _context.Entry(dbAuction).Collection(a => a.Baskets).Load(); //get baskets...
            auction.Baskets = dbAuction.Baskets; //give auction baskets
            auction.BannerImageUrl = dbAuction.BannerImageUrl; //so banner appears on error
            if (!ModelState.IsValid)
            {
                ViewData["Alert"] = "Invalid input, please update and try again!";
                return View(auction);
            }

            if (auction.BannerImage != null)
            { //update banner
                string BannerUrl;
                BannerUrl = imageUtilities.uploadFile(auction.BannerImage, _env.WebRootPath, "/Content/Auctions/BannerImages/", 300, 1000, "auctionBanner" + dbAuction.AuctionId.ToString() + Guid.NewGuid().ToString().Substring(0,2)); //save image
                if (BannerUrl == null)
                {
                    ViewData["Alert"] = "Banner update failed, reupload and try again...";
                    return View(auction);
                }
                else
                {
                    if (!String.IsNullOrWhiteSpace(dbAuction.BannerImageUrl))
                        imageUtilities.deleteFile(dbAuction.BannerImageUrl, _env.WebRootPath); //delete old banner to save space!
                    dbAuction.BannerImageUrl = BannerUrl;
                }
            }

            dbAuction.Description = auction.Description.Replace(">", String.Empty)
                    .Replace("<", String.Empty)
                    .Replace("\'", String.Empty)
                    .Replace("\"", String.Empty)
                    .Replace("&", "and"); //avoid injection

            dbAuction.Title = auction.Title;
            if (isLive)
            {
                dbAuction.EndTime = auction.EndTime; //at this point since start must match the server version, this will be within 31 days
            }
            else //no restrictions on what can be changed
            {
                dbAuction.Address = auction.Address;
                dbAuction.City = auction.City;
                dbAuction.State = auction.State;
                dbAuction.ZIP = auction.ZIP;
                dbAuction.StartTime = auction.StartTime;
                dbAuction.EndTime = auction.EndTime;
                dbAuction.Latitude = auction.Latitude;
                dbAuction.Longitude = auction.Longitude;
                dbAuction.MaxRange = auction.MaxRange;
            }
            _context.SaveChanges();

            ViewData["Alert"] = "Auction successsfully updated!";
            ModelState.Clear();
            return View(dbAuction); //navbar is already overridden
        }

        [HttpPost]
        public IActionResult getBasketShareLink(int auctionID)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Content("ERROR: INVALID LOGIN");
            if (user.UserRole != Roles.COMPANY)
                return Content("ERROR: ONLY ORGANIZATIONS CAN ADD LINKS TO AUCTIONS");

            var auction = _context.AuctionModel.Find(auctionID);
            if (auction == null)
                return Content("ERROR: AUCTION NOT FOUND");

            if (auction.HostUsername != user.Username)
                return Content("ERROR: YOU DO NOT OWN THIS AUCTION");

            //now, the user owns the auction
            var link = new AuctionLinkModel() { AuctionId = auctionID };
            _context.AuctionLinkModel.Add(link);
            _context.SaveChanges();

            _context.Entry(link).Reload(); //to get GUID

            return Content(LoginUtils.getAbsoluteUrl("/auctions/addBasket/" + link.Link.ToString(), Request));
        }

        [HttpGet]
        [Route("/auctions/addBasket/{AuctionLink}")]
        public IActionResult addBasket(Guid AuctionLink)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?redirectBack=" + LoginUtils.getAbsoluteUrl("/auctions/addBasket/" + AuctionLink.ToString(), Request)); //redirect to login

            var model = _context.AuctionLinkModel.Where(am => am.Link == AuctionLink).FirstOrDefault(); //link is invalid
            if (model == null)
            {
                ViewData["Alert"] = "Invalid link!";
                return View("~/Views/Home/Index.cshtml", user);
            }

            _context.Entry(model).Reference(l => l.Auction).Query().Include(a => a.Baskets).Include(a => a.HostUser).Load(); //get auction, it's baskets, and host

            if(model.Auction.EndTime < DateTime.UtcNow) //auction has already ended
            {
                ViewData["Alert"] = "Auction has already ended!";
                return View("~/Views/Home/Index.cshtml", user);
            }

            var baskets = model.Auction.Baskets.Where(b => b.SubmittingUsername == user.Username); //get user's baskets
            var userAddModel = new userAddBasketViewModel() { Auction = model.Auction, AuctionAddLink = AuctionLink, Baskets = new List<BasketModel>(baskets) };

            //all is well
            ViewData["NavBarOverride"] = user;
            return View("~/Views/Basket/userAddBasket.cshtml", userAddModel);
        }

        [HttpPost]
        public IActionResult deleteAuction(int AuctionId)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?redirectBack=" + LoginUtils.getAbsoluteUrl("/auctions/editAuction/" + AuctionId, Request)); //redirect to login

            var auction = _context.AuctionModel.Find(AuctionId);
            if (auction == null) //auction doesn't exist
            {
                _context.Entry(user).Collection(u => u.Auctions).Load(); //get auctions
                ViewData["Alert"] = "Invalid auction!";
                return View("~/Views/Auctions/Index.cshtml", user); //redirect to auction list
            }
            if (auction.StartTime < DateTime.UtcNow) //cannot delete any auction that has executed
            {
                _context.Entry(user).Collection(u => u.Auctions).Load(); //get auctions
                ViewData["Alert"] = "You cannot delete auctions that have already begun/ended!";
                return View("~/Views/Auctions/Index.cshtml", user); //redirect to auction list
            }

            if (auction.HostUsername != user.Username)
            {
                _context.Entry(user).Collection(u => u.Auctions).Load(); //get auctions
                ViewData["Alert"] = "You do not own this auction!";
                return View("~/Views/Auctions/Index.cshtml", user); //redirect to auction list
            }

            //At this point, the auction has not yet started and the user owns it
            _context.Entry(auction).Collection(a => a.Baskets).Load(); //get baskets for deletion

            //Remove any previous deletion entries
            _context.EmailVerification.RemoveRange(_context.EmailVerification.Where(ev => ev.Username == user.Username).Where(ev => ev.Payload == auction.AuctionId.ToString()));

            var changeId = Guid.NewGuid(); //gets new change GUID
            var deletionValidation = new EmailVerificationModel()
            {
                ChangeTime = DateTime.UtcNow,
                ChangeType = ChangeTypes.AUCTIONDELETION,
                CommitId = changeId,
                CanRevert = false,
                Payload = AuctionId.ToString(), //the value stored is the Auction ID in these models
                Username = user.Username
            };

            _context.EmailVerification.Add(deletionValidation); //adds the pending verification to the queue.
            _context.SaveChanges();
            _context.Entry(deletionValidation).Reload();

            if (EmailUtils.sendVerificationEmail(user.Email, deletionValidation, Request)) //send verification email
            {
                _context.SaveChanges();
                ViewData["Alert"] = "Success, verify the deletion via email to complete the process!";
                ViewData["NavBarOverride"] = user;
                return View("EditAuction", auction);
            }
            else
            {
                ViewData["Alert"] = "Failed to send verification email, try again!";
                ViewData["NavBarOverride"] = user;
                return View("EditAuction", auction);
            }
        }

        [HttpGet]
        public IActionResult View(int id, int? pageNum, int? itemsPerPage)
        {
            UserAuctionWalletModel wallet = null;
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            
            var auction = _context.AuctionModel.Find(id);
            if (auction == null)
            {
                ViewData["Alert"] = "Auction not found!";
                if (user == null)
                    return View("~/Views/Home/Index.cshtml", new UserModel()); //no login, send back home
                else
                    return View("~/Views/Home/Index.cshtml", user);
            }
            _context.Entry(auction).Collection(a => a.Baskets).Query().Include(b => b.photos).Load(); //load in baskets with photos

            if (user != null)
            {
                ViewData["NavBarOverride"] = user;
                foreach (var basket in auction.Baskets)
                    basket.UserTickets = _context.TicketModel.Find(user.Username, basket.BasketId); //get the user's investment in each basket

                wallet = _context.UserAuctionWallet.Find(user.Username, auction.AuctionId);
                if (wallet == null)
                {
                    wallet = new UserAuctionWalletModel() { AuctionId = auction.AuctionId, Username = user.Username, WalletBalance = 0 };
                    _context.UserAuctionWallet.Add(wallet); //add a new wallet if the user doesn't yet have one.
                    _context.SaveChanges();
                }
            }
            else
            {
                ViewData["NavBarOverride"] = new UserModel();
                ViewData["RefreshAfterLogin"] = true; //to reload buttons
            }

            ViewData["itemsPerPage"] = itemsPerPage;
            ViewData["page"] = pageNum;
            ViewData["StripePublicKey"] = StripeConsts.publicKey;

            var userViewAuctionModel = new UserAuctionViewModel() { UserModel = user ?? new UserModel(), AuctionModel = auction, Wallet = wallet ?? new UserAuctionWalletModel() };
            
            return View("ViewAuction", userViewAuctionModel);
        }
    }
}