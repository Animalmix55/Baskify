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
using Stripe;
using Microsoft.Extensions.Options;

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

        /// <summary>
        /// Returns all the saved payment method dtos for reuse in DB
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [Route("methods")]
        [HttpGet]
        public ActionResult PaymentMethods([FromHeader] string authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                return Unauthorized("Invalid authorization");

            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Bad login");

            var paymentMethodListOpts = new PaymentMethodListOptions() { 
                Customer = user.StripeCustomerId,
                Type = "card"
            };

            List<PaymentMethodDto> paymentMethodDtos;

            var paymentMethodService = new PaymentMethodService();
            paymentMethodDtos = Mapper.Map<List<PaymentMethodDto>>(paymentMethodService.List(paymentMethodListOpts));

            return Ok(paymentMethodDtos);
        }
        /// <summary>
        /// Deletes a payment method from user
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [Route("methods")]
        [HttpDelete]
        public ActionResult PaymentMethods([FromHeader] string authorization, [FromForm] string paymentMethodId)
        {
            if (string.IsNullOrEmpty(authorization))
                return Unauthorized("Invalid authorization");

            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Bad login");

            var paymentMethodService = new PaymentMethodService();
            paymentMethodService.Detach(paymentMethodId);

            return Ok();
            
        }
    }
}