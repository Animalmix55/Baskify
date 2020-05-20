using baskifyCore.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        public static string getGoogleAPI()
        {
            return ConfigurationManager.AppSettings["GoogleAPIKey"].ToString();
        }

        public static bool getCoordinates(string Address, string City, string State, string ZIP, out string Latitude, out string Longitude)
        {
            var address = String.Format("{0} {1}, {2} {3}", Address, City, State, ZIP);
            var requestUri = string.Format("https://maps.googleapis.com/maps/api/geocode/xml?key={1}&address={0}&sensor=false", Uri.EscapeDataString(address), getGoogleAPI());

            WebRequest request = WebRequest.Create(requestUri);
            WebResponse response = request.GetResponse();
            XDocument xdoc = XDocument.Load(response.GetResponseStream());

            XElement result = xdoc.Element("GeocodeResponse").Element("result");
            XElement locationElement = result.Element("geometry").Element("location");
            XElement lat = locationElement.Element("lat");
            XElement lng = locationElement.Element("lng");

            Latitude = lat.Value;
            Longitude = lng.Value;

            if (Latitude == null || Longitude == null)
                return false;

            return true;
        }

        /// <summary>
        /// This function verifies that a deletion is to be executed and then executes it!
        /// DOES NOT check to see if anything is permissioned, check beforehand. At this point,
        /// the verification model should belong to the user.
        /// </summary>
        /// <param name="VerifyId"></param>
        /// <param name="verification"></param>
        /// <param name="user"></param>
        /// <param name="_context"></param>
        public static string VerifyDeletion(Guid VerifyId, EmailVerificationModel verification, UserModel user, ApplicationDbContext _context, string WebRootPath)
        {
            if(VerifyId != verification.CommitId) 
                return "Invalid Deletion ID!";
            var auction = _context.AuctionModel.Find(int.Parse(verification.Payload)); //newValue stores the auction id as a string
            if (auction == null)
                return "Auction Not Found!";
            else if (auction.HostUsername != user.Username)
                return "You do not have access rights to this auction!";
            else if (auction.StartTime < DateTime.UtcNow)
                return "Auctions that have already begun/ended cannot be deleted!";

            //now the user owns the auction!
            _context.Entry(auction).Collection(a => a.Baskets).Load(); //get baskets to delete them
            foreach (var basket in auction.Baskets.ToList())
            {
                basketUtils.deleteBasket(basket, _context, WebRootPath); //delete all the baskets (and their associated images)
            }
            //now delete the auction
            verification.Committed = true;
            _context.AuctionModel.Remove(auction);
            _context.SaveChanges();
            return "Auction deleted successfully!";
        }
    }
}
