using AutoMapper;
using baskifyCore.DTOs;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Loads the uneditable basket view window - DOES NOT REQUIRE AUTHENTIFICATION
        /// </summary>
        /// <param name="authorization"></param>
        /// <param name="id"></param>
        /// <returns></returns>
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
                    return Unauthorized("Invalid Authorization");
                tickets = _context.TicketModel.Find(user.Username, basket.BasketId);
            }

            _context.Entry(basket).Collection(b => b.photos).Load();

            var BasketDto = Mapper.Map<BasketModel, BasketDto>(basket); //map the basket into the DTO...
            BasketDto.NumTickets = tickets == null ? 0 : tickets.NumTickets; //add the number of tickets
            

            return Ok(BasketDto);
        }

        [HttpPost]
        [Route("{id}/ticket/add/{numTickets}")] //numTickets is optional
        public ActionResult AddTicket(int id, [FromHeader] string authorization, int numTickets)
        {
            if (authorization == null)
                return Unauthorized("No Authorization");

            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", String.Empty), _context);

            if (user == null)
                return Unauthorized("Invalid Authorization");

            if (user.UserRole != Roles.USER)
                return BadRequest("Only users can purchase tickets");

            var basket = _context.BasketModel.Find(id);
            if (basket == null)
                return BadRequest("Invalid Basket");

            _context.Entry(basket).Reference(b => b.AuctionModel).Load();
            if (!basket.AuctionModel.isLive)
                return BadRequest("Auction is not live");

            //now the auction is live and the basket exists

            var userWallet = _context.UserAuctionWallet.Find(user.Username, basket.AuctionId);
            if (userWallet == null || userWallet.WalletBalance < numTickets)
                return BadRequest("Insufficient Balance");

            userWallet.WalletBalance -= numTickets; //remove tickets from wallet
            _context.SaveChanges(); //avoid any weird behavior

            var tickets = _context.TicketModel.Find(user.Username, id);
            if (tickets == null)
            {
                tickets = new TicketModel() { BasketId = id, NumTickets = numTickets, Username = user.Username };
                _context.TicketModel.Add(tickets);
            }
            else
                tickets.NumTickets += numTickets; //add tickets to basket

            _context.SaveChanges();

            return Ok(new { NumTickets = tickets.NumTickets, BasketTitle = basket.BasketTitle });


        }


        /// <summary>
        /// Gets the baskets that a user submitted
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [Route("submitted")]
        [HttpGet]
        public ActionResult Submitted([FromHeader] string authorization)
        {
            if (authorization == null)
                return Unauthorized("No authorization");
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Bad authorization");

            var baskets = _context.BasketModel
                .Include(b => b.AuctionModel.HostUser)
                .Include(b => b.Winner)
                .Include(b => b.photos)
                .Where(b => b.SubmittingUsername == user.Username).Where(b => !b.Draft);

            List<PrivBasketDto> basketDto = Mapper.Map<List<PrivBasketDto>>(baskets);
            basketDto.ForEach(b => b.Cleanse(b.AuctionModel.DeliveryType != (int)DeliveryTypes.DeliveryBySubmitter, true, true)); //cleanse

            return Ok(basketDto);
        }


        /// <summary>
        /// Returns JSON containing the status of all of the tickets the user has invested, grouped by auction
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [Route("results")]
        [HttpGet]
        public ActionResult results([FromHeader] string authorization)
        {
            if (authorization == null)
                return Unauthorized("No authorization");
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Bad authorization");

            //now we have a user

            var results = _context.BasketModel.Include(b => b.photos).Include(b => b.AuctionModel.HostUser).Join(
                _context.TicketModel.Where(t => t.Username == user.Username),
                basket => basket.BasketId,
                ticket => ticket.BasketId,
                (basket, ticket) => new
                {
                    basket,
                    ticket
                })
                .GroupJoin(
                _context.BasketPhotoModel,
                r => r.basket.BasketId,
                photo => photo.BasketId,
                (r, photos) => new
                {
                    r.basket,
                    r.ticket,
                    photos
                })
                .Join(
                _context.AuctionModel,
                r => r.basket.AuctionId,
                auction => auction.AuctionId,
                (r, auction) => new
                {
                    r.basket,
                    r.ticket,
                    r.photos,
                    auction
                })
                .Join(
                _context.UserModel,
                r => r.auction.HostUsername,
                user => user.Username,
                (r, host) => new
                {
                    r.basket,
                    r.ticket,
                    r.photos,
                    r.auction,
                    host
                }).GroupBy(r => r.auction.AuctionId)
                .ToList(); //grouped by auctions


            var returnList = new List<RaffleDtoGroup>();

            //package everything up by auction
            foreach(var resultGroup in results)
            {
                var auction = resultGroup.First().auction;
                auction.HostUser = resultGroup.First().host;

                var dtoGroup = new RaffleDtoGroup()
                {
                    auction = Mapper.Map<LocationAuctionDto>(auction), //get group auction, includes location information
                    raffleResults = new List<RaffleResultDto>()
                };
                
                foreach(var result in resultGroup.ToList())
                {
                    var basket = result.basket;
                    basket.photos = result.photos.ToList();
                    var basketDto = Mapper.Map<BasketDto>(basket);
                    basketDto.NumTickets = result.ticket.NumTickets;

                    var resultDto = new RaffleResultDto()
                    {
                        Basket = basketDto,
                        Status = !auction.isDrawn? Results.PENDING : (basket.WinnerUsername == user.Username? Results.WON : Results.LOST)
                        
                    };
                    dtoGroup.raffleResults.Add(resultDto);
                }

                returnList.Add(dtoGroup);
                              
            }

            return Ok(returnList);

        }
    }
}
