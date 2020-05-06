using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using baskifyCore.Utilities;
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

        public IActionResult Index()
        {
            return View();
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

            //Validate address
            /*
            Dictionary<string, string> addressValidation;
            if ((addressValidation = accountUtils.validateAddress(auction.Address, auction.City, auction.State, auction.ZIP))["resultStatus"] == "ADDRESS NOT FOUND")
                ModelState.AddModelError("Address", "Address not valid");
            */

            if (auction.StartTime < DateTime.Now)
                ModelState.AddModelError("StartTime", "Auction cannot have already started!");

            if (!ModelState.IsValid)
            {
                ViewData["Alert"] = "Invalid input! Check your form values and readd any banner!";
                ViewData["NavBarOverride"] = user;
                return View(auction);
            }

            try {
                /* addresses are now validated in model
                //update address with validated version
                auction.Address = addressValidation["addressLine1"];
                auction.City = addressValidation["city"];
                auction.State = addressValidation["state"];
                auction.ZIP = addressValidation["zip"];
                */

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
                return View(auction);
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
                if(auction.EndTime < DateTime.Now)
                {
                    ViewData["Alert"] = "You cannot edit a completed auction!";
                    return View("~/Auctions/ListAuctions"); //send them back to the auction list!
                }
                _context.Entry(auction).Collection(a => a.Baskets).Load();
                ViewData["NavBarOverride"] = user;
                return View(auction);
            }
            else
            {
                return Redirect("/"); //send them home!
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
                return View("~/Views/Home/Index.cshtml", user);
            }

            if(dbAuction.HostUsername != user.Username) //not their auction
            {
                ViewData["Alert"] = "You do not have access to this auction.";
                return View("~/Views/Home/Index.cshtml", user);
            }
            if(dbAuction.EndTime < DateTime.UtcNow)
            {
                ViewData["Alert"] = "You cannot edit an ended auction.";
                return View("~/Views/Home/Index.cshtml", user);
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
            }
            _context.SaveChanges();

            ViewData["Alert"] = "Auction successsfully updated!";
            ModelState.Clear();
            return View(dbAuction); //navbar is already overridden
        }
    }
}