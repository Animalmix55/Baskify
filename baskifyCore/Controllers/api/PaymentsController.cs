using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using baskifyCore.DTOs;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;

namespace baskifyCore.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : Controller
    {
        ApplicationDbContext _context;
        public PaymentsController()
        {
            _context = new ApplicationDbContext();
        }

        /// <summary>
        /// gets a list of payment objects that a user initiated
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Index([FromHeader] string authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                return Unauthorized("Invalid authorization");

            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Bad login");

            //now we have a user!
            var payments = _context.PaymentModel.Include(p => p.AuctionModel.HostUser).Where(p => p.Username == user.Username).Where(p => p.Complete).ToList();
            var paymentDtos = Mapper.Map<List<PaymentDto>>(payments);

            return Ok(paymentDtos);
        }
    }
}