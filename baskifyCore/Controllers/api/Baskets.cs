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
using Microsoft.AspNetCore.Http;
using System.Web;
using Microsoft.AspNetCore.Hosting;

namespace baskifyCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class Baskets : Controller
    {
        ApplicationDbContext _context;
        IHttpContextAccessor _accessor;
        IWebHostEnvironment _env;
        public Baskets(IHttpContextAccessor accessor, IWebHostEnvironment env)
        {
            _context = new ApplicationDbContext();
            _accessor = accessor;
            _env = env;
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
        [Route("{id}/ticket/add/{numTickets}")]
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
                .Where(b => b.SubmittingUsername == user.Username).Where(b => !b.Draft).ToList();

            baskets.ForEach(basket => {
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
                        if (dto != null? false : dto.Delivered)
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

            List<PrivBasketDto> basketDto = Mapper.Map<List<PrivBasketDto>>(baskets);
            basketDto.ForEach(b => {
                b.Cleanse(b.AuctionModel.DeliveryType != (int)DeliveryTypes.DeliveryBySubmitter, true, true);
            }); //cleanse

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
            foreach (var resultGroup in results)
            {
                var sendAddress = false;
                var auction = resultGroup.First().auction;
                auction.HostUser = resultGroup.First().host;

                var dtoGroup = new RaffleDtoGroup()
                {
                    auction = Mapper.Map<LocationAuctionDto>(auction), //get group auction, includes location information
                    raffleResults = new List<RaffleResultDto>()
                };

                foreach (var result in resultGroup.ToList())
                {
                    var basket = result.basket;
                    basket.photos = result.photos.ToList();
                    var basketDto = Mapper.Map<BasketDto>(basket);
                    basketDto.NumTickets = result.ticket.NumTickets;
                    
                    var resultDto = new RaffleResultDto()
                    {
                        Basket = basketDto,
                    };

                    if(basket.WinnerUsername != user.Username) //set the status accurately
                        resultDto.Status = "Lost";
                    else
                    {
                        if (basket.DisputedShipment)
                            resultDto.Status = "Disputed";
                        else if (basket.Delivered)
                            resultDto.Status = "Delivered";
                        else if (!string.IsNullOrWhiteSpace(basket.TrackingNumber))
                        {
                            var dto = TrackingUtils.TrackBasket(basket, _accessor.HttpContext.Connection.RemoteIpAddress.ToString(), _context);
                            if (dto != null ? dto.Delivered : false)
                            {
                                resultDto.Status = "Delivered";
                            }
                            else
                                resultDto.Status = "In Transit";
                        }
                        else
                            resultDto.Status = "Won, Not Delivered";
                    }

                    dtoGroup.raffleResults.Add(resultDto);
                    if (basket.WinnerUsername == user.Username && auction.DeliveryType == DeliveryTypes.Pickup) //if any won and pickup, show address
                        sendAddress = true;
                }

                if (!sendAddress) //cleanse the address if the user shouldn't be privy to it (no winners, not pickup)
                {
                    dtoGroup.auction.Address = null;
                    dtoGroup.auction.City = null;
                    dtoGroup.auction.State = null;
                    dtoGroup.auction.ZIP = null;
                }

                returnList.Add(dtoGroup);

            }

            return Ok(returnList);

        }

        [HttpGet]
        [Route("track/{id}")]
        public ActionResult track([FromHeader] string authorization, [FromRoute] int id)
        {
            if (authorization == null)
                return Unauthorized("No authorization");
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Bad authorization");

            var basket = _context.BasketModel.Include(b => b.AuctionModel).Where(b => b.BasketId == id).FirstOrDefault();
            if (basket == null)
                return BadRequest("Invalid Basket");


            //if the user is nto the auction host, winner, or the submitter (if submitter delivers), don't allow
            if (!(basket.WinnerUsername == user.Username || basket.AuctionModel.HostUsername == user.Username || (basket.SubmittingUsername == user.Username && basket.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter)))
                return BadRequest("Not permitted to view this information");

            TrackingDto dto;

            if (string.IsNullOrWhiteSpace(basket.TrackingNumber))
            {
                if (basket.Delivered) //marked delivered without shippping info
                {
                    _context.Entry(basket).Reference(b => b.Winner).Load();
                    dto = new TrackingDto() { Delivered = true };
                    dto.Updates = new List<TrackingItem>();
                    dto.Updates.Add(new TrackingItem() { Location = $"{basket.Winner.City}, {basket.Winner.State} {basket.Winner.ZIP}", Message = "Marked Delivered by Sender", Time = basket.DeliveryTime.Value });
                    dto.Disputeable = user.Username == basket.WinnerUsername && !basket.DisputedShipment && basket.Delivered && basket.DisputeTime == null && basket.DeliveryTime.Value.AddDays(7) > DateTime.UtcNow; ; //cant already be disputed
                    dto.Disputed = basket.DisputedShipment;
                    dto.DisputeText = basket.DisputeReason;
                    dto.CanCancelDispute = basket.WinnerUsername == user.Username;

                    return Ok(dto);
                }
                return NotFound(new { Error = "No tracking number provided", Editable = !basket.Delivered && (basket.AuctionModel.HostUsername == user.Username || (basket.SubmittingUsername == user.Username && basket.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter)) });
            }
            
            try
            {
                switch (basket.Carrier)
                {
                    case PostalCarrier.FedEx:
                        dto = TrackingUtils.trackFedEx(basket.TrackingNumber);
                        break;
                    case PostalCarrier.UPS:
                        dto = TrackingUtils.trackUPS(basket.TrackingNumber);
                        break;
                    case PostalCarrier.USPS:
                        dto = TrackingUtils.trackUSPS(basket.TrackingNumber, _accessor.HttpContext.Connection.RemoteIpAddress.ToString());
                        break;
                    default:
                        throw new Exception("Invalid carrier");
                }

                if (dto.Delivered && !basket.Delivered) //update basket if need be
                {
                    basket.DeliveryTime = dto.Updates[0].Time;
                    basket.Delivered = true;
                    _context.SaveChangesAsync();
                }

                //can edit if have permissions and not delivered
                dto.Editable = !dto.Delivered && (basket.AuctionModel.HostUsername == user.Username || (basket.SubmittingUsername == user.Username && basket.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter));
                //can dispute if are recipient and has been delivered and was never before disputed and it was delivered within 7 days
                dto.Disputeable = user.Username == basket.WinnerUsername && dto.Delivered && !basket.DisputedShipment && basket.DisputeTime == null && dto.Updates[0].Time.AddDays(7) > DateTime.UtcNow;
                dto.Disputed = basket.DisputedShipment;
                dto.DisputeText = basket.DisputeReason;
                dto.CanCancelDispute = basket.WinnerUsername == user.Username;

                return Ok(dto);
            }
            catch (Exception e)
            {
                return StatusCode(426,
                    new
                    {
                        Error = e.Message,
                        Editable = !basket.Delivered && (basket.AuctionModel.HostUsername == user.Username || (basket.SubmittingUsername == user.Username && basket.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter))
                    });
            }
        }

        [HttpPost]
        [Route("track/{id}")]
        public ActionResult addTracking([FromRoute] int id, [FromHeader] string authorization, [FromForm] PostalCarrier? Carrier, [FromForm] string TrackingNumber)
        {
            if (authorization == null)
                return Unauthorized("No authorization");
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Bad authorization");

            var basket = _context.BasketModel.Include(b => b.AuctionModel).Where(b => b.BasketId == id).FirstOrDefault();
            if (basket == null)
                return BadRequest("Invalid Basket");


            //if the user is nto the auction host, winner, or the submitter (if submitter delivers), don't allow
            if (!(basket.AuctionModel.HostUsername == user.Username || (basket.SubmittingUsername == user.Username && basket.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter)))
                return BadRequest("Not permitted to edit this information");

            if (basket.Delivered) //cant edit once delivered
                return BadRequest("Basket already marked as delivered");

            if (string.IsNullOrWhiteSpace(TrackingNumber)) //deleting tracking info
            {
                basket.TrackingNumber = null;
                basket.Carrier = null;
                _context.SaveChanges();
                return Ok();
            }

            if (Carrier == null)
                return BadRequest("Invalid Carrier");

            TrackingDto dto;
            try
            {
                switch (Carrier)
                {
                    case PostalCarrier.FedEx:
                        dto = TrackingUtils.trackFedEx(TrackingNumber);
                        break;
                    case PostalCarrier.UPS:
                        dto = TrackingUtils.trackUPS(TrackingNumber);
                        break;
                    case PostalCarrier.USPS:
                        dto = TrackingUtils.trackUSPS(TrackingNumber, _accessor.HttpContext.Connection.RemoteIpAddress.ToString());
                        break;
                    default:
                        return BadRequest("Invalid carrier");
                }
                //valid tracking number

                basket.TrackingNumber = TrackingNumber;
                basket.Carrier = Carrier;
                dto.CanCancelDispute = basket.WinnerUsername == user.Username;

                if (dto.Delivered)
                {
                    basket.Delivered = true; //set delivered
                    basket.DeliveryTime = dto.Updates.Last().Time;
                }

                _context.SaveChanges();
                return Ok();
            }
            catch (Exception)
            {
                return BadRequest("Invalid Tracking Number");
            }
        }

        [HttpPost]
        [Route("delivered/{id}")]
        public ActionResult setDelivered([FromRoute] int id, [FromHeader] string authorization)
        {
            if (authorization == null)
                return Unauthorized("No authorization");
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Bad authorization");

            var basket = _context.BasketModel.Include(b => b.AuctionModel).Where(b => b.BasketId == id).FirstOrDefault();
            if (basket == null)
                return BadRequest("Invalid Basket");


            //if the user is nto the auction host, winner, or the submitter (if submitter delivers), don't allow
            if (!(basket.AuctionModel.HostUsername == user.Username || (basket.SubmittingUsername == user.Username && basket.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter)))
                return BadRequest("Not permitted to edit this information");

            if (!string.IsNullOrWhiteSpace(basket.TrackingNumber))
                return BadRequest("Tracking number already provided, delivery will be set when the shipment arrives");

            basket.Delivered = true;
            basket.DeliveryTime = DateTime.UtcNow;
            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Adds a delivery dispute to the basket
        /// </summary>
        /// <param name="id"></param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("dispute/{id}")]
        public ActionResult addDispute([FromRoute] int id, [FromHeader] string authorization, [FromForm] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return BadRequest("Enter a message");
            if (authorization == null)
                return Unauthorized("No authorization");
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Bad authorization");

            var basket = _context.BasketModel.Include(b => b.AuctionModel.HostUser).Include(b => b.SubmittingUser).Where(b => b.BasketId == id).FirstOrDefault();
            if (basket == null)
                return BadRequest("Invalid Basket");

            if (!basket.Delivered)
                return BadRequest("Basket not yet marked as delivered");

            if (basket.DisputedShipment)
                return BadRequest("Basket already disputed");

            if (basket.DisputeTime != null) //if the basket is not marked but still has a dispute time, then it will be considered ex-disputed
                return BadRequest("Basket can only be disputed once!");

            //if the user is nto the auction host, winner, or the submitter (if submitter delivers), don't allow
            if (user.Username != basket.WinnerUsername)
                return BadRequest("Not permitted to dispute a basket you did not win");

            basket.DisputedShipment = true;
            basket.DisputeTime = DateTime.UtcNow;
            basket.DisputeReason = HttpUtility.HtmlEncode(message); //avoid injection
            _context.SaveChanges();

            EmailUtils.SendStyledEmail(basket.AuctionModel.HostUser, "Basket Delivery Disputed", $"The user {basket.WinnerUsername} disputed the delivery of your basket entitled \"{basket.BasketTitle}\" which was marked as delivered on {basket.DeliveryTime.Value} (UTC). <br><br> The user's dispute states: \"{basket.DisputeReason}\". <br><br> You will not be able to request a payout or create other auctions until this dispute is resolved. <br><br> Please reach out to the user at {basket.Winner.Email} to resolve the issue and have them close the dispute. {(basket.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter? "The basket submitter has also been notified.": "")}", _env, basket.AuctionModel.HostUser.ContactEmail);
            if(basket.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter && basket.SubmittingUsername != basket.AuctionModel.HostUsername)
            {
                EmailUtils.SendStyledEmail(basket.SubmittingUser, "Basket Delivery Disputed", $"The user {basket.WinnerUsername} disputed the delivery of your basket entitled \"{basket.BasketTitle}\" which was marked as delivered on {basket.DeliveryTime.Value} (UTC). <br><br> The user's dispute states: \"{basket.DisputeReason}\". <br><br> You will not be able to submit any baskets to other auctions until this issue is resolved. <br><br> Please reach out to the user at {basket.Winner.Email} to resolve the issue and have them close the dispute. The auction host has also been notified.", 
                    _env, basket.SubmittingUser.Email);
            }
            return Ok();
        }

        [HttpDelete]
        [Route("dispute/{id}")]
        public ActionResult removeDispute([FromRoute] int id, [FromHeader] string authorization, [FromForm] string message)
        {
            if (authorization == null)
                return Unauthorized("No authorization");
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Bad authorization");

            var basket = _context.BasketModel.Include(b => b.AuctionModel.HostUser).Include(b => b.SubmittingUser).Where(b => b.BasketId == id).FirstOrDefault();
            if (basket == null)
                return BadRequest("Invalid Basket");

            if (!basket.DisputedShipment)
                return BadRequest("Basket not disputed");

            if (user.Username != basket.WinnerUsername)
                return BadRequest("Not permitted to recind a dispute on a basket you did not win");

            basket.DisputedShipment = false;
            //leave the dispute time to show that is was once disputed
            basket.DisputeReason = null;
            _context.SaveChanges();

            EmailUtils.SendStyledEmail(basket.AuctionModel.HostUser, "Basket Delivery Dispute Released", $"The user {basket.WinnerUsername} released their dispute on the delivery of your basket entitled \"{basket.BasketTitle}\" in the auction \"{basket.AuctionModel.Title}\" which was marked as delivered on {basket.DeliveryTime.Value} (UTC).<br><br> You will now be able to request a payout and create other auctions again! <br><br> {(basket.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter ? "The basket submitter has also been notified." : "")}", _env, basket.AuctionModel.HostUser.ContactEmail);
            if (basket.AuctionModel.DeliveryType == DeliveryTypes.DeliveryBySubmitter && basket.SubmittingUsername != basket.AuctionModel.HostUsername)
            {
                EmailUtils.SendStyledEmail(basket.SubmittingUser, "Basket Delivery Dispute Released", $"The user {basket.WinnerUsername} released their dispute on the delivery of your basket entitled \"{basket.BasketTitle}\" in the auction \"{basket.AuctionModel.Title}\" which was marked as delivered on {basket.DeliveryTime.Value} (UTC). <br><br> You will now be able to submit baskets to other auctions again! <br><br> The auction host has also been notified.", _env, basket.SubmittingUser.Email);
            }

            return Ok();
        }
    }
}
