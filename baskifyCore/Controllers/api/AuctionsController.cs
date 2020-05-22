using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using baskifyCore.DTOs;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : ControllerBase
    {
        ApplicationDbContext _context;
        public AuctionsController()
        {
            _context = new ApplicationDbContext();
        }

        /// <summary>
        /// Returns all NON-draft baskets for an auction with images
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}/baskets")]
        public ActionResult getBaskets(int id, [FromHeader] string authorization)
        {
            var auction = _context.AuctionModel.Find(id);
            if (auction == null)
                return NotFound("Auction does not exist");
            _context.Entry(auction).Collection(a => a.Baskets).Query().Include(b => b.photos).Include(b => b.Tickets).Load();

            var baskets = new List<BasketDto>();
            Mapper.Map(auction.Baskets.Where(b => !b.Draft).ToList(), baskets);

            if (!string.IsNullOrWhiteSpace(authorization)) { //add the ticket count
                var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
                if (user == null)
                    return Unauthorized("Invalid Authorization");
                for(int i =0; i < baskets.Count; i++)
                {
                    var ticketModel = auction.Baskets[i].Tickets.Where(t => t.Username == user.Username).FirstOrDefault(); //go through basketModel tickets, find any that belong to user
                    baskets[i].NumTickets = ticketModel == null ? 0 : ticketModel.NumTickets;
                }
            }

            return Ok(baskets); //return JSON array of baskets
        }
    }
}