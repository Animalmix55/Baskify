﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using baskifyCore.DTOs;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Data.Entity;

namespace baskifyCore.Controllers.api
{
    [Route("api/[controller]")]
    public class StripeWebHook : Controller
    {
        const string endpointSecret = "whsec_DaiWoTMdSQrYTAl5WJB8Jh8vUjRdKLFr"; //TEST SECRET

        ApplicationDbContext _context;
        public StripeWebHook()
        {
            _context = new ApplicationDbContext();
        }

        [HttpPost]
        //ONLY RECIEVES STRIPE PAYMENT SUCCESS EVENTS
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            PaymentModel paymentModel;

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], endpointSecret);

                // Handle the event
                if (stripeEvent.Type == Events.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                    paymentModel = _context.PaymentModel.Include(p => p.AuctionModel.HostUser).Include(p => p.UserModel).Where(p => p.PaymentIntentId == paymentIntent.Id).FirstOrDefault();
                    if (paymentModel == null)
                        return BadRequest("Invalid Payment Model Id");

                    _context.Entry(paymentModel).Reload(); //make sure locked flag is up to date

                    if (paymentModel.Locked)
                        return Ok("Payment Already Updated"); //tells server to stop trying

                    paymentModel.Locked = true; //lock payment PERMANENTLY
                    _context.SaveChanges(); //make sure locked is set

                    paymentModel.Success = true;
                    paymentModel.Complete = true;

                    var wallet = _context.UserAuctionWallet.Find(paymentModel.UserModel.Username, paymentModel.AuctionId);
                    if (wallet == null) //no wallet? Make a new one
                    {
                        wallet = new UserAuctionWalletModel() { AuctionId = paymentModel.AuctionId, Username = paymentModel.Username, WalletBalance = 0 };
                        _context.UserAuctionWallet.Add(wallet);
                    }

                    var numTickets = (int)(((decimal)paymentIntent.AmountReceived/100) / paymentModel.AuctionModel.TicketCost);

                    wallet.WalletBalance += numTickets; //add tickets to wallet
                    _context.SaveChanges();

                    var recieptDto = Mapper.Map<ReceiptDto>(paymentModel);
                    var service = new PaymentMethodService();
                    var paymentMethod = service.Get(paymentIntent.PaymentMethodId); //get card

                    recieptDto.CardLastFour = paymentMethod.Card.Last4;
                    recieptDto.CardType = paymentMethod.Card.Brand.ToUpper();
                    recieptDto.CardExp = $"{paymentMethod.Card.ExpMonth}/{paymentMethod.Card.ExpYear}";

                    EmailUtils.SendReceiptEmail(paymentModel.UserModel, recieptDto);


                    return Ok();
                }
                else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                    paymentModel = _context.PaymentModel.Find(paymentIntent.Id);
                    if (paymentModel == null)
                        return BadRequest("Invalid Payment Model Id");

                    if (paymentModel.Complete == true)
                        return Ok("Payment Already Updated"); //tells server to stop trying



                    /* HIDE ALERTS FOR NOW, A BIT OVERWHELMING
                    _context.Entry(paymentModel).Reference(pm => pm.AuctionModel).Load();
                    var newAlert = new UserAlertModel()
                    {
                        AlertBody = String.Format("Your payment of ${0} on {1} failed! Dismiss this error and try again!", decimal.Round((decimal)paymentModel.Amount/100, 2), paymentModel.AuctionModel.Title),
                        AlertHeader = "Payment Failed",
                        AlertType = "PaymentAlert",
                        Username = paymentModel.Username,
                        Dismissable = true
                    };

                    _context.UserAlert.Add(newAlert);
                    */

                    paymentModel.Complete = true; //complete but NOT succeeded

                    _context.SaveChanges();
                    return Ok();
                }
                else
                    return BadRequest("Invalid Event");

            }
            catch (StripeException e)
            {
                return BadRequest();
            }
        }
    }
}