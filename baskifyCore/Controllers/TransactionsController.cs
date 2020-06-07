using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;
using AutoMapper;
using baskifyCore.DTOs;
using Stripe;

namespace baskifyCore.Controllers
{
    public class TransactionsController : Controller
    {
        ApplicationDbContext _context;
        public TransactionsController()
        {
            _context = new ApplicationDbContext();
        }

        public IActionResult Index()
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?redirectBack=" + LoginUtils.getAbsoluteUrl("/transactions", Request));
            if (user.UserRole != Roles.USER)
                Redirect("/"); //send home

            return View(user);
        }

        public IActionResult Receipt(string id)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);

            if (user == null)
                Redirect("/login?redirectBack=" + LoginUtils.getAbsoluteUrl("/transactions/receipt/" + id, Request));

            var payment = _context.PaymentModel.Include(p => p.AuctionModel.HostUser).Include(p => p.UserModel).Where(p => p.PaymentIntentId == id).FirstOrDefault();
            if(payment == null)
            {
                ViewData["Alert"] = "Invalid transaction";
                return View("Index", user);
            }

            if(payment.Username != user.Username)
            {
                ViewData["Alert"] = "You do not have access to this transaction";
                return View("Index", user);
            }

            if (!payment.Success)
            {
                ViewData["Alert"] = "This transaction failed to complete";
                return View("Index", user);
            }

            //now the user exists and owns the transaction
            var intentService = new PaymentIntentService();
            var paymentIntent = intentService.Get(payment.PaymentIntentId);
            var recieptDto = Mapper.Map<ReceiptDto>(payment);
            var service = new PaymentMethodService();
            var paymentMethod = service.Get(paymentIntent.PaymentMethodId); //get card

            recieptDto.CardLastFour = paymentMethod.Card.Last4;
            recieptDto.CardType = paymentMethod.Card.Brand.ToUpper();
            recieptDto.CardExp = $"{paymentMethod.Card.ExpMonth}/{paymentMethod.Card.ExpYear}";

            ViewData["NavBarOverride"] = user;
            return View(recieptDto);
        }
    }
}