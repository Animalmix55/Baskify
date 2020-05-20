using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Controllers.api
{
    public static class StripeConsts
    {
        public const string publicKey = "pk_test_GBB2UKgj1tlGwykGFvX7xypd00G0GIaBW6";
        public const string secretKey = "sk_test_dNuiju1uhjBr4AhpuZGzfQPd00glKbAVYw";
    }

    [ApiController]
    [Route("api/[controller]")]
    public class Wallets : Controller
    {
        ApplicationDbContext _context;
        public Wallets()
        {
            _context = new ApplicationDbContext();
        }

        [HttpPost]
        [Route("{auctionId}/purchase")]
        public ActionResult GetSecret([FromHeader] string authorization, [FromForm] int tickets, [FromForm] PaymentMethodCard card, int auctionId)
        {
            return Ok();
        }

        // /api/wallets/{id}/purchase
        [HttpPost]
        [Route("{auctionId}/getsecret")]
        public ActionResult GetSecret([FromHeader] string authorization, [FromForm] int tickets, int auctionId)
        {
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (tickets == 0)
                return BadRequest("Must purchase at least 1 ticket");
            if (user == null)
                return BadRequest("Invalid credentials");
            if (user.UserRole == Roles.COMPANY)
                return BadRequest("Organizations cannot buy tickets");
            var auction = _context.AuctionModel.Find(auctionId);
            if (auction == null)
                return BadRequest("Invalid Auction");

            var wallet = _context.UserAuctionWallet.Find(user.Username, auctionId);
            if (wallet == null) //make new wallet
            {
                wallet = new UserAuctionWalletModel() { AuctionId = auctionId, Username = user.Username, WalletBalance = 0 };
                _context.UserAuctionWallet.Add(wallet);
            }


            if (user.StripeCustomerId == null)
            {
                var custOptions = new CustomerCreateOptions
                {
                    Name = string.Format("{0} {1}", user.FirstName, user.LastName),
                };

                var custService = new CustomerService();
                var customer = custService.Create(custOptions);

                user.StripeCustomerId = customer.Id; //add new customer
            }

            var amount = (long?)(tickets * auction.TicketCost * 100); //amount in cents

            var options = new PaymentIntentCreateOptions
            {
                ReceiptEmail = user.Email,
                Customer = user.StripeCustomerId,
                CaptureMethod = "automatic",
                ConfirmationMethod = "automatic",
                Amount = amount, //in cents
                Currency = "usd",
                // Verify your integration in this guide by including this parameter
                Metadata = new Dictionary<string, string>
                {
                    { "auctionId", wallet.AuctionId.ToString() },
                    {"userId", user.Username }
                },
                SetupFutureUsage = "on_session"
            };
            var service = new PaymentIntentService();
            var paymentIntent = service.Create(options);

            var PaymentModel = new PaymentModel() { Amount = (float)amount, PaymentIntentId = paymentIntent.Id, Time = paymentIntent.Created, Username = user.Username, AuctionId = auction.AuctionId };
            //create payment in database
            _context.PaymentModel.Add(PaymentModel);
            _context.SaveChanges();

            return Ok(paymentIntent.ClientSecret);


        }

        // /api/wallets/checkpayment/{id}
        [HttpGet]
        [Route("CheckPayment/{paymentId}")]
        public ActionResult CheckPayment([FromHeader] string authorization, string paymentId)
        {
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return BadRequest("Invalid credentials");

            var payment = _context.PaymentModel.Find(paymentId);
            if (payment.Username != user.Username)
                return BadRequest("Invalid User");

            if (payment.Complete)
            {
                if (payment.Success)
                    return Ok("Success");
                else
                    return Ok("Failure");
            }
            else
                return Ok("Pending");

        }

    }
}
