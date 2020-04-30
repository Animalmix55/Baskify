using baskifyCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Utilities
{
    public class AuctionException : Exception
    {
        public AuctionException(string text) : base(text) { }
    }
    public static class AuctionUtilities
    {
        /// <summary>
        /// Adds the auction provided the user has the right to
        /// </summary>
        /// <param name="auction"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static int addAuction(AuctionModel auction, UserModel user, ApplicationDbContext _context)
        {
            if (user.UserRole != Roles.COMPANY)
                throw new AuctionException("Invalid Role");

            _context.AuctionModel.Add(auction);
            _context.SaveChanges();
            return auction.AuctionId;

        }
    }
}
