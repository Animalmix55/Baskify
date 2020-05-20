using AutoMapper;
using baskifyCore.DTOs;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace baskifyCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class Baskets: Controller
    {
        ApplicationDbContext _context;
        public Baskets()
        {
            _context = new ApplicationDbContext();
        }

        // /api/baskets/{id}/images
        [HttpGet]
        [Route("{id}/images")]
        public ActionResult GetBasketImages(int id)
        {
            var basket = _context.BasketModel.Find(id);
            if (basket == null)
                return NotFound();

            _context.Entry(basket).Collection(b => b.photos).Load();

            return Ok(basket.photos.Select(p => p.Url));

        }

        [HttpGet]
        [Route("{id}")]
        public ActionResult GetBasket([FromHeader] string authorization, int id)
        {
            var basket = _context.BasketModel.Find(id);
            if (basket == null)
                return NotFound();

            TicketModel tickets = null;
            UserModel user;
            if (authorization != null)
            {
                user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", String.Empty), _context);
                if (user == null)
                    return BadRequest("Invalid Authorization");

                tickets = _context.TicketModel.Find(user.Username, basket.BasketId);
            }

            _context.Entry(basket).Collection(b => b.photos).Load();

            var BasketDto = Mapper.Map<BasketModel, BasketDto>(basket); //map the basket into the DTO...
            BasketDto.NumTickets = tickets == null ? 0 : tickets.NumTickets; //add the number of tickets
            

            return Ok(BasketDto);
        }

        [HttpPost]
        [Route("{id}/ticket/add")]
        public ActionResult AddTicket(int id, [FromHeader] string authorization)
        {
            if (authorization == null)
                return BadRequest("No Authorization");

            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", String.Empty), _context);

            if (user == null)
                return BadRequest("Invalid Authorization");

            var basket = _context.BasketModel.Find(id);
            if (basket == null)
                return BadRequest("Invalid Basket");

            _context.Entry(basket).Reference(b => b.AuctionModel).Load();
            if (!basket.AuctionModel.isLive)
                return BadRequest("Auction is not live");

            //now the auction is live and the basket exists

            var userWallet = _context.UserAuctionWallet.Find(user.Username, basket.AuctionId);
            if (userWallet == null || userWallet.WalletBalance < 1)
                return BadRequest("Insufficient Balance");

            userWallet.WalletBalance--; //remove a ticket from wallet

            var tickets = _context.TicketModel.Find(user.Username, id);
            if (tickets == null)
            {
                tickets = new TicketModel() { BasketId = id, NumTickets = 1, Username = user.Username };
                _context.TicketModel.Add(tickets);
            }
            else
                tickets.NumTickets++; //add ticket to basket

            _context.SaveChanges();

            return Ok(tickets.NumTickets);


        }
    }
}
