using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers.api
{

    /// <summary>
    /// This API is responsible for producing data for reports and forms.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        ApplicationDbContext _context;
        public DataController()
        {
            _context = new ApplicationDbContext();
        }
        /// <summary>
        /// Returns auction sales stats for the entire duration of the auction, provided the user has permissions
        /// If a given day has transactions, breaks the day into times and amounts
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="hourly"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("auction/{auctionId}")]
        public ActionResult Auction(int auctionId, [FromHeader] string authorization)
        {
            if (authorization == null)
                return Unauthorized("Invalid authorization");
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);

            if (user == null)
                return Unauthorized("Invalid login");

            var auction = _context.AuctionModel.Find(auctionId);
            if (auction == null)
                return BadRequest("Invalid auction");

            if (auction.HostUsername != user.Username)
                return BadRequest("You do not own this auction");

            //at this point, the user owns the auction

            var salesPerDay = _context.PaymentModel.Where(p => p.AuctionId == auctionId).Where(p => p.Complete).Select(p => new { p.Time, p.Amount, p.Fee, p.Username }).GroupBy(p => DbFunctions.TruncateTime(p.Time))
                .Select(g => new {
                    Date = g.Key,
                    Amount = g.Sum(dt => dt.Amount),
                    Breakdown = g.Select(g => new { g.Time, g.Username, g.Amount, g.Fee })})
                .ToList();

            var resultDict = salesPerDay.ToDictionary(pair => pair.Date.Value, pair => new { pair.Amount, pair.Breakdown });

            return Ok(resultDict);
        }
    }
}