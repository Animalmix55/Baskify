using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Controllers
{
    public class BasketController : Controller
    {
        ApplicationDbContext _context;
        public BasketController()
        {
            _context = new ApplicationDbContext();
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

            AuctionModel auction;
            _context.Entry(basket).Reference(b => b.AuctionModel).Load(); //get the auction from the basket

            if (basket.AuctionModel.HostUsername != user.Username) //check the auction's owner
                return Content("ERROR: INVALID BASKET OWNER");

            //at this point, the basket belongs to the auction that belongs to the user! Now we can do what we must!

            return PartialView("BasketModalPartialView", basket);
        }
    }
}
