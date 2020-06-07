using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Helpers;
using AutoMapper;
using baskifyCore.DTOs;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static baskifyCore.DTOs.IncomingSearchDto;

namespace baskifyCore.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : Controller
    {
        ApplicationDbContext _context;
        IWebHostEnvironment _env;
        public AuctionsController(IWebHostEnvironment env)
        {
            _context = new ApplicationDbContext();
            _env = env;
        }

        /// <summary>
        /// Lists all the auctions that the user owns
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        public ActionResult Index([FromHeader] string authorization)
        {
            if (authorization == null)
                return Unauthorized("Invalid Authorization");
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", String.Empty), _context);
            if (user == null)
                return Unauthorized("Invalid Login");
            if (user.UserRole != Roles.COMPANY)
                return BadRequest("Only organizations can access this resource");
            //now the user exists and is a company

            _context.Entry(user).Collection(u => u.Auctions).Load(); //get auctions

            List<AuctionDto> auctionDto = Mapper.Map<List<AuctionDto>>(user.Auctions);

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
        public ActionResult detailedBaskets(int id, [FromHeader] string authorization)
        {
            var auction = _context.AuctionModel.Find(id);
            if (auction == null)
                return NotFound("Auction does not exist");

            if (string.IsNullOrWhiteSpace(authorization))
            {
                return Unauthorized("Invalid Authorization");
            }

            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Invalid Authorization");


            if (user.UserRole != Roles.COMPANY) //companies get a more detailed Dto
                return BadRequest("You do not have sufficient priviledge");

            if (auction.HostUsername != user.Username)
                return Unauthorized("You do not own this auction");

            //Now, they own the auction

            List<BasketModel> permBasketModel;
            if (auction.EndTime < DateTime.UtcNow) //only show accepted baskets if auction has ended
                permBasketModel = _context.BasketModel.Include(b => b.photos).Include(b => b.SubmittingUser).Include(b => b.Tickets).Include(b => b.Winner).Where(b => b.AuctionId == id).Where(b => !b.Draft && b.AcceptedByOrg).ToList();
            else
                permBasketModel = _context.BasketModel.Include(b => b.photos).Include(b => b.SubmittingUser).Include(b => b.Tickets).Where(b => b.AuctionId == id).Where(b => !b.Draft).ToList();

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
        [HttpPost]
        public ActionResult Draw(int id, [FromHeader] string authorization)
        {
            if (authorization == null)
                return Unauthorized("Invalid authorization");
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Invalid login");

            var auction = _context.AuctionModel.Find(id);
            if (auction == null)
                return BadRequest("Invalid auction");

            if (auction.HostUsername != user.Username)
                return BadRequest("You do not own this auction");

            if (auction.isDrawn)
                return BadRequest("Auction already drawn!");

            //now, they own the auction
            if (AuctionUtilities.draw(id, _context, LoginUtils.getAbsoluteUrl("/", Request)))
            {
                _context.SaveChanges();
                return Ok("Baskets Drawn");
            }
            else
                return BadRequest("An error occurred");
        }

        [HttpPost]
        [Route("search")]
        public ActionResult Search([FromBody] IncomingSearchDto search, [FromHeader] string authorization)
        {
            try
            {
                int firstResult = search.start;
                int maxRecords = search.length;

                var query = _context.AuctionModel.Where(a => a.EndTime > DateTime.UtcNow).Where(a => a.StartTime < DateTime.UtcNow)
                    .Join(_context.BasketModel.Where(b => !b.Draft && b.AcceptedByOrg),
                    auction => auction.AuctionId,
                    basket => basket.AuctionId,
                    (auction, basket) => auction //this will effectively remove all non-basket-weilding auctions
                    );

                var totalNum = query.Count();

                query = query.Include(a => a.HostUser); //seems to get clobbered if in first statement by join

                if (!string.IsNullOrWhiteSpace(search.search.value) && !search.search.regex) //avoid regex
                {
                    query = query.Where(
                        a => DbFunctions.Like(a.HostUser.OrganizationName.ToLower(), "%" + search.search.value.ToLower() + "%") ||
                        DbFunctions.Like(a.Title.ToLower(), "%" + search.search.value.ToLower() + "%")); //global search
                }

                foreach (var column in search.columns)
                {
                    if (column.searchable && !string.IsNullOrWhiteSpace(column.search.value) && !column.search.regex)
                    {
                        switch (column.name)
                        {
                            case "OrgName":
                                query = query.Where(a => DbFunctions.Like(a.HostUser.OrganizationName.ToLower(), "%" + column.search.value.ToLower() + "%"));
                                break;
                            case "Title":
                                query = query.Where(a => DbFunctions.Like(a.Title.ToLower(), "%" + column.search.value.ToLower() + "%"));
                                break;
                        }
                    }
                }

                //ordering round 1
                int i = 0;
                OrderItem orderitem = search.order[i];
                IOrderedQueryable<AuctionModel> orderedQuery;
                switch (search.columns[orderitem.column].name)
                {
                    case "Title":
                        if (orderitem.dir == "asc")
                            orderedQuery = query.OrderBy(a => a.Title);
                        else
                            orderedQuery = query.OrderByDescending(a => a.Title);
                        break;
                    case "EndTime":
                        if (orderitem.dir == "asc")
                            orderedQuery = query.OrderBy(a => a.EndTime);
                        else
                            orderedQuery = query.OrderByDescending(a => a.EndTime);
                        break;
                    case "OrgName":
                        if (orderitem.dir == "asc")
                            orderedQuery = query.OrderBy(a => a.HostUser.OrganizationName);
                        else
                            orderedQuery = query.OrderByDescending(a => a.HostUser.OrganizationName);
                        break;
                    default:
                        orderedQuery = (IOrderedQueryable<AuctionModel>)query; //do nothing
                        break;
                }


                //now thenby
                for (i = 1; i < search.order.Count; i++)
                {
                    orderitem = search.order[i];
                    switch (search.columns[orderitem.column].name)
                    {

                        case "OrgName":
                            if (orderitem.dir == "asc")
                                orderedQuery = orderedQuery.ThenBy(a => a.HostUser.OrganizationName);
                            else
                                orderedQuery = orderedQuery.ThenByDescending(a => a.HostUser.OrganizationName);
                            break;
                        case "Title":
                            if (orderitem.dir == "asc")
                                orderedQuery = orderedQuery.ThenBy(a => a.Title);
                            else
                                orderedQuery = orderedQuery.ThenByDescending(a => a.Title);
                            break;
                        case "EndTime":
                            if (orderitem.dir == "asc")
                                orderedQuery = orderedQuery.ThenBy(a => a.EndTime);
                            else
                                orderedQuery = orderedQuery.ThenByDescending(a => a.EndTime);
                            break;
                    }
                }
                //now query is ordered and filtered by everything but distance

                UserModel user = null;
                if (!string.IsNullOrWhiteSpace(authorization))
                    user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);

                var resultSet = orderedQuery.ToList();

                resultSet = resultSet.Where(a => SearchUtils.getMiles(user.Latitude, user.Longitude, a.Latitude, a.Longitude) < a.MaxRange).ToList(); //remove results too far away

                //order by distance
                //DISTANCE MUST BE OUTSIDE OF LINQ QUERY
                foreach (var oi in search.order)
                {
                    if (search.columns[oi.column].name == "Distance")
                    {
                        if (user == null)
                            break; //dont bother if user is invalid
                        else if (oi.dir == "asc")
                            resultSet = resultSet.OrderBy(a => SearchUtils.getMiles(user.Latitude, user.Longitude, a.Latitude, a.Longitude)).ToList();
                        else
                            resultSet = resultSet.OrderByDescending(a => SearchUtils.getMiles(user.Latitude, user.Longitude, a.Latitude, a.Longitude)).ToList();
                    }
                }

                var recordsFiltered = resultSet.Count;

                if (search.start + search.length < resultSet.Count)
                    resultSet = resultSet.GetRange(search.start, search.length); //trim down result set
                else if (search.start + 1 <= resultSet.Count)
                {
                    var numRemaining = resultSet.Count - search.start;
                    resultSet = resultSet.GetRange(search.start, numRemaining);
                }
                else
                    resultSet = new List<AuctionModel>(); //empty

                List<AuctionDto> dtoResult = Mapper.Map<List<AuctionDto>>(resultSet);

                if (user != null)
                    for (int j = 0; j < dtoResult.Count; j++)
                    {
                        dtoResult[j].DistanceFromUser = (float)SearchUtils.getMiles(user.Latitude, user.Longitude, resultSet[j].Latitude, resultSet[j].Longitude); //populate distances
                    }
                //now we're all filtered and sorted
                var returnObject = new
                {
                    draw = search.draw,
                    recordsTotal = totalNum,
                    recordsFiltered = recordsFiltered,
                    data = dtoResult
                };
                return Ok(returnObject);
            }
            catch (Exception e)
            {
                var returnObject = new
                {
                    draw = search.draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<AuctionDto>(),
                    error = "An error was encountered, try again"
                };
                return BadRequest(returnObject);
            }
        }
    }
}