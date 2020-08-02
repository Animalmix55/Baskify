using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Helpers;
using AutoMapper;
using BaskifyClient.DTOs;
using BaskifyClient.Models;
using BaskifyClient.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using static BaskifyClient.DTOs.IncomingSearchDto;

namespace BaskifyClient.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : Controller
    {
        ApplicationDbContext _context;
        IWebHostEnvironment _env;
        IHttpContextAccessor _accessor;
        public AuctionsController(IWebHostEnvironment env, IHttpContextAccessor accessor)
        {
            _context = new ApplicationDbContext();
            _env = env;
            _accessor = accessor;
        }

        /// <summary>
        /// Lists all the auctions that the user owns
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [Authorize(Policy = "LocalOnly")]
        public ActionResult Index([FromHeader] string authorization)
        {
            var auctions = _context.AuctionModel.ToList(); //get auctions

            List<AuctionDto> auctionDto = Mapper.Map<List<AuctionDto>>(auctions);

            return Ok(auctionDto); //return auction list
        }

        /// <summary>
        /// The route for organizations to get detailed information via the orgBasketDto
        /// </summary>
        /// <param name="id"></param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}/detailedBaskets")]
        [Authorize(Policy = "LocalOnly")]
        public ActionResult detailedBaskets(int id)
        {
            var auction = _context.AuctionModel.Find(id);
            if (auction == null)
                return NotFound("Auction does not exist");

            List<BasketModel> permBasketModel;
            if (auction.EndTime < DateTime.UtcNow) //only show accepted baskets if auction has ended
                permBasketModel = _context.BasketModel.Include(b => b.photos).Include(b => b.SubmittingUser).Include(b => b.Tickets).Include(b => b.Winner).Where(b => b.AuctionId == id).Where(b => !b.Draft && b.AcceptedByOrg).ToList();
            else
                permBasketModel = _context.BasketModel.Include(b => b.photos).Include(b => b.SubmittingUser).Include(b => b.Tickets).Where(b => b.AuctionId == id).Where(b => !b.Draft).ToList();

            permBasketModel.ForEach(basket => {
                if (basket.WinnerUsername == null) //set the status accurately
                    basket.Status = "Not Drawn";
                else
                {
                    if (basket.DisputedShipment)
                        basket.Status = "Disputed";
                    else if (basket.Delivered)
                        basket.Status = "Delivered";
                    else if (!string.IsNullOrWhiteSpace(basket.TrackingNumber))
                    {
                        var dto = TrackingUtils.TrackBasket(basket, _accessor.HttpContext.Connection.RemoteIpAddress.ToString(), _context);
                        if (dto != null && dto.Delivered)
                        {
                            basket.Status = "Delivered";
                        }
                        else
                            basket.Status = "In Transit";
                    }
                    else
                        basket.Status = "Drawn, Not Delivered";
                }
            }); //set status

            var OrgBasketDto = Mapper.Map<List<PrivBasketDto>>(permBasketModel); //includes donor address and email

            //cleanse info
            OrgBasketDto.ForEach(b => b.Cleanse(auction.DeliveryType == DeliveryTypes.Pickup, auction.BasketRetrieval != BasketRetrieval.OrgPickup, false));

            return Ok(OrgBasketDto);

        }


        /// <summary>
        /// Returns all NON-draft/Accepted baskets for an auction with images and ticket counts for a given user (if applicable)
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

            List<BasketDto> baskets;

            if (!string.IsNullOrWhiteSpace(authorization)) { //add the ticket count
                var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
                if (user == null)
                    return Unauthorized("Invalid Authorization");

                baskets = _context.AuctionModel.Where(a => a.AuctionId == id).Join(
                    _context.BasketModel.Include(b => b.photos).Where(b => !b.Draft && b.AcceptedByOrg), //only non-draft, accepted baskets
                    auction => auction.AuctionId,
                    basket => basket.AuctionId,
                    (auction, basket) => new
                    {
                        photos = basket.photos,
                        BasketId = basket.BasketId,
                        BasketTitle = basket.BasketTitle,
                        BasketDescription = basket.BasketDescription,
                        SubmissionDate = basket.SubmissionDate,
                        AuctionId = basket.AuctionId,
                        BasketContentString = basket.BasketContentString
                    }) //get all the baskets for that auction
                    .GroupJoin(
                    _context.TicketModel.Where(t => t.Username == user.Username).DefaultIfEmpty(), //gets associated ticket for user
                    basket => basket.BasketId,
                    ticket => ticket.BasketId,
                    (basket, ticket) => new
                    {
                        photos = basket.photos.Select(p => new BasketPhotoDto { PhotoDesc = p.PhotoDesc, PhotoId = p.PhotoId, Url = p.Url }).ToList(), //map photos to DTO
                        BasketId = basket.BasketId,
                        BasketTitle = basket.BasketTitle,
                        BasketDescription = basket.BasketDescription,
                        SubmissionDate = basket.SubmissionDate,
                        AuctionId = basket.AuctionId,
                        BasketContents = basket.BasketContentString,
                        NumTickets = ticket.FirstOrDefault() == null ? 0 : ticket.FirstOrDefault().NumTickets //defaults to 0 if empty
                    }).ToList()
                    .Select(b => new BasketDto
                    {
                        AuctionId = b.AuctionId,
                        BasketContents = (b.BasketContents == null) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(b.BasketContents),
                        BasketDescription = b.BasketDescription,
                        BasketId = b.BasketId,
                        BasketTitle = b.BasketTitle,
                        NumTickets = b.NumTickets,
                        photos = b.photos,
                        SubmissionDate = b.SubmissionDate
                    }).ToList();

            }
            else {
                baskets = _context.AuctionModel.Where(a => a.AuctionId == id).Join(
                        _context.BasketModel.Include(b => b.photos).Where(b => !b.Draft && b.AcceptedByOrg), //only non-draft, accepted baskets
                        auction => auction.AuctionId,
                        basket => basket.AuctionId,
                        (auction, basket) => new
                        {
                            photos = basket.photos.Select(p => new BasketPhotoDto { PhotoDesc = p.PhotoDesc, PhotoId = p.PhotoId, Url = p.Url }).ToList(),
                            BasketId = basket.BasketId,
                            BasketTitle = basket.BasketTitle,
                            BasketDescription = basket.BasketDescription,
                            SubmissionDate = basket.SubmissionDate,
                            AuctionId = basket.AuctionId,
                            BasketContents = basket.BasketContentString,
                            NumTickets = 0 //no tickets
                        }).ToList().ToList()
                    .Select(b => new BasketDto
                    {
                        AuctionId = b.AuctionId,
                        BasketContents = (b.BasketContents == null) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(b.BasketContents),
                        BasketDescription = b.BasketDescription,
                        BasketId = b.BasketId,
                        BasketTitle = b.BasketTitle,
                        NumTickets = b.NumTickets,
                        photos = b.photos,
                        SubmissionDate = b.SubmissionDate
                    }).ToList(); //get all the baskets for that auction
            }
            return Ok(baskets); //return JSON array of baskets
        }

        /// <summary>
        /// Draws all of the baskets for the given auction, maybe can someday be scheduled
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("{id}/draw")]
        [Authorize(Policy = "LocalOnly")]
        [HttpPost]
        public ActionResult Draw(int id)
        {
            var auction = _context.AuctionModel.Find(id);
            if (auction == null)
                return BadRequest("Invalid auction");

            if (auction.isDrawn)
                return BadRequest("Auction already drawn!");

            if (auction.EndTime > DateTime.UtcNow)
                return BadRequest("Auction has not yet ended!");

            //now, they own the auction
            if (AuctionUtilities.draw(id, _context, LoginUtils.getAbsoluteUrl("/", Request), _env))
            {
                _context.SaveChanges();
                return Ok("Baskets Drawn");
            }
            else
                return BadRequest("An error occurred");
        }
    }
}