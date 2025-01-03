﻿using baskifyCore.DTOs;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Configuration;

namespace baskifyCore.Controllers.api
{
    public static class StripeConsts
    {
        public static string publicKey;
        public static string secretKey;
        public static string clientId;
        static StripeConsts()
        {
#if DEBUG
            publicKey = ConfigurationManager.AppSettings["StripePublicKeyTEST"];
            secretKey = ConfigurationManager.AppSettings["StripeSecretTEST"];
            clientId = ConfigurationManager.AppSettings["StripeClientIdTEST"];

#else
            publicKey = ConfigurationManager.AppSettings["StripePublicKeyPROD"];
            secretKey = ConfigurationManager.AppSettings["StripeSecretPROD"];
            clientId = ConfigurationManager.AppSettings["StripeClientIdPROD"];
#endif
        }
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

        // /api/wallets/{id}/purchase
        [HttpPost]
        [Route("{auctionId}/getsecret")]
        public ActionResult GetSecret([FromHeader] string authorization, [FromForm] TicketPurchaseDto ticketPurchaseDto, int auctionId)
        {
            if (authorization == null)
                return Unauthorized("No Authorization");

            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Invalid Authorization");

            if (ticketPurchaseDto.NumTickets == 0)
                return BadRequest("Must purchase at least 1 ticket");
            if (user == null)
                return BadRequest("Invalid credentials");
            if (user.UserRole == Roles.COMPANY)
                return BadRequest("Organizations cannot buy tickets");
            var auction = _context.AuctionModel.Include(a => a.HostUser).Where(a => a.AuctionId == auctionId).FirstOrDefault(); //gets auction AND host
            if (auction == null)
                return BadRequest("Invalid Auction");
            if (!auction.isLive)
                return BadRequest("Auction is not live");
            if (ticketPurchaseDto.NumTickets * auction.TicketCost < auction.MinPurchase) //don't allow users to purchase below the min
                return BadRequest(string.Format("Purchase must exceed ${0}", auction.MinPurchase));
            //if county never loaded properly, ignore it
            if (!auction.CanParticipate(user, _context))
                return BadRequest("You are outside of the auction's jurisdiction");

            try
            {
                if (!ticketPurchaseDto.UseAccountAddress) //get address from form
                {
                    var addressDto = accountUtils.validateAddress(ticketPurchaseDto.BillingAddress, ticketPurchaseDto.BillingCity, ticketPurchaseDto.BillingState, ticketPurchaseDto.BillingState);
                    if (addressDto.Status == "ADDRESS NOT FOUND")
                        return BadRequest("Invalid Address");
                    else if (string.IsNullOrEmpty(ticketPurchaseDto.CardholderName))
                        return BadRequest("Invalid Cardholder Name");

                    ticketPurchaseDto.BillingAddress = addressDto.Address; //set address
                    ticketPurchaseDto.BillingCity = addressDto.City;
                    ticketPurchaseDto.BillingState = addressDto.State;
                    ticketPurchaseDto.BillingZIP = addressDto.ZIP;
                }
                else if (!string.IsNullOrEmpty(ticketPurchaseDto.PaymentMethodId)) //get address from paymentModel
                {
                    var PaymentMethodService = new PaymentMethodService();
                    var paymentMethod = PaymentMethodService.Get(ticketPurchaseDto.PaymentMethodId);
                    ticketPurchaseDto.BillingAddress = paymentMethod.BillingDetails.Address.Line1;
                    ticketPurchaseDto.BillingCity = paymentMethod.BillingDetails.Address.City;
                    ticketPurchaseDto.BillingState = paymentMethod.BillingDetails.Address.State;
                    ticketPurchaseDto.BillingZIP = paymentMethod.BillingDetails.Address.PostalCode;
                    ticketPurchaseDto.CardholderName = paymentMethod.BillingDetails.Name;
                }
                else //get address from account
                {
                    ticketPurchaseDto.BillingAddress = user.Address;
                    ticketPurchaseDto.BillingCity = user.City;
                    ticketPurchaseDto.BillingState = user.State;
                    ticketPurchaseDto.BillingZIP = user.ZIP;
                    ticketPurchaseDto.CardholderName = user.FirstName + " " + user.LastName;
                }

                if (!ModelState.IsValid)
                    return BadRequest("Invalid Input");

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
                        Name = string.Format(ticketPurchaseDto.CardholderName),
                        Address = new AddressOptions()
                        {
                            Line1 = user.Address,
                            City = user.City,
                            State = user.State,
                            Country = "USA",
                            PostalCode = user.ZIP
                        },
                        Email = user.Email
                    };

                    var custService = new CustomerService();
                    var customer = custService.Create(custOptions);

                    user.StripeCustomerId = customer.Id; //add new customer
                }

                var amount = (int)(ticketPurchaseDto.NumTickets * auction.TicketCost * 100); //amount in cents
                var fee = FundraisingTotalsDto.CalculateFee((int)Math.Round((decimal)amount), 1, auction.FeePerTrans, auction.FeePercentage); //cents

                var options = new PaymentIntentCreateOptions
                {
                    ReceiptEmail = user.Email,
                    Customer = user.StripeCustomerId,
                    CaptureMethod = "automatic",
                    ConfirmationMethod = "automatic",
                    Amount = amount, //in cents
                    Currency = "usd",
                    ApplicationFeeAmount = fee, //transfer sans fee to user
                    TransferData = new PaymentIntentTransferDataOptions()
                    {
                        Destination = auction.HostUser.StripeCustomerId
                    }
                };
                if (ticketPurchaseDto.SaveCard) //save if requested
                    options.SetupFutureUsage = "on_session";
                if (!string.IsNullOrEmpty(ticketPurchaseDto.PaymentMethodId))
                    options.PaymentMethod = ticketPurchaseDto.PaymentMethodId;


                var service = new PaymentIntentService();
                var paymentIntent = service.Create(options);

                var PaymentModel = new PaymentModel()
                {
                    Amount = amount, //cents
                    PaymentIntentId = paymentIntent.Id,
                    Time = paymentIntent.Created,
                    Username = user.Username,
                    AuctionId = auction.AuctionId,
                    BillingAddress = ticketPurchaseDto.BillingAddress,
                    BillingCity = ticketPurchaseDto.BillingCity,
                    BillingState = ticketPurchaseDto.BillingState,
                    BillingZIP = ticketPurchaseDto.BillingZIP,
                    CardholderName = ticketPurchaseDto.CardholderName,
                    Fee = fee //in cents
                };

                //create payment in database
                _context.PaymentModel.Add(PaymentModel);
                _context.SaveChanges();

                return Ok(new { ClientSecret = paymentIntent.ClientSecret, PaymentMethod = ticketPurchaseDto.PaymentMethodId }); //method will be null if not provided
            }
            catch (Exception e)
            {
                return BadRequest("There was an error processing this payment");
            }
        }

        // /api/wallets/checkpayment/{id}
        [HttpGet]
        [Route("CheckPayment/{paymentId}")]
        public ActionResult CheckPayment([FromHeader] string authorization, string paymentId)
        {
            if (authorization == null)
                return Unauthorized("No Authorization");

            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Invalid Authorization");

            var payment = _context.PaymentModel.Find(paymentId);
            if (payment.Username != user.Username)
                return BadRequest("Invalid User");

            if (payment.Complete)
            {
                if (payment.Success) {
                    var wallet = _context.UserAuctionWallet.Find(payment.Username, payment.AuctionId);
                    return Ok( new { Result = "Success", NumTickets = wallet.WalletBalance });
                }
                else
                    return Ok(new { Result = "Failure" });
            }
            else
                return Ok(new { Result = "Pending" });

        }

    }
}
