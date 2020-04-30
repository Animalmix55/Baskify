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
            Dictionary<string, string> addressValidation;
            if ((addressValidation = accountUtils.validateAddress(auction.Address, auction.City, auction.State, auction.ZIP))["resultStatus"] == "ADDRESS NOT FOUND")
                ModelState.AddModelError("Address", "Address not valid");

            if (auction.StartTime < DateTime.Now)
                ModelState.AddModelError("StartTime", "Auction cannot have already started!");

            if (!ModelState.IsValid)
            {
                ViewData["Alert"] = "Invalid input! Check your form values and readd any banner!";
                ViewData["NavBarOverride"] = user;
                return View(auction);
            }

            try {
                //update address with validated version
                auction.Address = addressValidation["addressLine1"];
                auction.City = addressValidation["city"];
                auction.State = addressValidation["state"];
                auction.ZIP = addressValidation["zip"];

                auction.HostUsername = user.Username;
                auction.Description = auction.Description.Replace(">", String.Empty)
                    .Replace("<", String.Empty)
                    .Replace("\'", String.Empty)
                    .Replace("\"", String.Empty)
                    .Replace("&", "and"); //avoid injection
                var auctionId = AuctionUtilities.addAuction(auction, user, _context);
                if (auction.BannerImage != null)
                    auction.BannerImageUrl = imageUtilities.uploadFile(auction.BannerImage, _env.WebRootPath, "/Content/Auctions/BannerImages/", 500, 1000, "auctionBanner" + auctionId.ToString()); //save image
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
    }
}