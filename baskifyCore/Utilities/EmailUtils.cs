using baskifyCore.Controllers;
using baskifyCore.DTOs;
using baskifyCore.Models;
using baskifyCore.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Policy;
using System.Threading.Tasks;

namespace baskifyCore.Utilities
{
    public static class EmailUtils
    {
        /// <summary>
        /// Sends the winner an email, should attach the auction submitting user to the auction
        /// </summary>
        /// <param name="user"></param>
        /// <param name="basket"></param>
        /// <param name="auction"></param>
        /// <returns></returns>
        public static bool sendBasketWinnerEmail(UserModel user, BasketModel basket, AuctionModel auction, string webrootUrl)
        {
            var viewModel = new EmailViewModel() { Basket = basket, rootUrl = webrootUrl };
            string contents = "";
            contents += $"Congratulations, you won the basket {basket.BasketTitle} from the auction {auction.Title}! <br><br>";

            switch (auction.DeliveryType)
            {
                case DeliveryTypes.DeliveryByOrg:
                case DeliveryTypes.DeliveryBySubmitter:
                    contents += $"The auction's host specified that they will be delivering the basket to your account's address: <br>";
                    contents += $"{user.Address}, {user.City}, {user.State} {user.ZIP}";
                    contents += $"<br>If this address is not valid, it is important that you update your address immediately and/or reach out to the organization responsible at {auction.HostUser.ContactEmail}.";
                    contents += $"<br>You can track the status of this delivery in your account, under the 'Baskets Won' tab.";
                    break;

                case DeliveryTypes.Pickup:
                    contents += $"The auction's host specified that winners are responsible for picking up baskets from the address: <br>";
                    contents += $"{auction.Address}, {auction.City}, {auction.State} {auction.ZIP} <br><br>";
                    contents += $"If there is an issue with this arrangement, kindly contact the host at {auction.HostUser.ContactEmail}";
                    break;
            }
            viewModel.Contents = contents;

            return SendEmail(user.Email, $"Baskify - Congrats!", RenderEmail(viewModel));
        }

        public static bool sendVerificationEmail(UserModel user, EmailVerificationModel verificationModel, HttpRequest request)
        {
            
            var OriginalEmailText = "We have received your request to";
            string subject;
            switch (verificationModel.ChangeType)
            {
                case ChangeTypes.EMAIL:
                    var origEmailModel = new EmailViewModel() { User = user };
                    var newEmailModel = new EmailViewModel() { User = user };
                    OriginalEmailText += " change your email to <b>" + verificationModel.Payload + "</b>\n";
                    var NewEmailText = OriginalEmailText +
                        "<br><br>To verify this change, please <a href=\"" + LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.CommitId, verificationModel.ChangeId), request) + "\">CLICK HERE</a>.\n";

                    OriginalEmailText +=
                        "<br><br>If you did not request this change, please <a href=\"" + LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.RevertId, verificationModel.ChangeId), request) + "\">CLICK HERE</a>.\n";

                    subject = "Baskify - Your Email Change Request";
                    origEmailModel.Contents = OriginalEmailText;
                    newEmailModel.Contents = NewEmailText;

                    return SendEmail(user.Email, subject, RenderEmail(origEmailModel)) && SendEmail(verificationModel.Payload, subject, RenderEmail(newEmailModel));

                case ChangeTypes.AUCTIONDELETION:
                    var viewModel = new EmailViewModel() { User = user };
                    OriginalEmailText += " delete your auction entitled: <b>" + verificationModel.Payload + "</b>\n";
                    OriginalEmailText += "If you did not request this change, just ignore this email; your account is not at any risk.</b>\n";
                    OriginalEmailText += "<br><br>If you did request this change, verify it by clicking <a href=\"" + LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.CommitId, verificationModel.ChangeId), request) + "\">HERE</a>.\n" +
                        "<h1>Thank you!</h1>";
                    subject = "Baskify - Your Auction Deletion Request";
                    viewModel.Contents = OriginalEmailText;

                    return SendEmail(user.Email, subject, RenderEmail(viewModel));
            }
            return false;
            

            
        }

        public static bool sendRecoveryEmail(UserModel user, string bearerToken, HttpRequest request)
        {
            var EmailText = "We have received your request to reset your passcode! If you did not request this change, just ignore this email; your account is not at any risk.</b>\n";

            EmailText +=
                "<br><br>If you did request this change, please <a href=\"" + LoginUtils.getAbsoluteUrl("/account/changepassword?bearerToken=" + bearerToken, request) + "\">CLICK HERE</a>.\n" +
                "<h1>Have a great day!</h1>";

            var subject = "Baskify - Your Password Change Request";
            var viewModel = new EmailViewModel() { Contents = EmailText, User = user };

            return SendEmail(user.Email, subject, RenderEmail(viewModel));
        }
        public static bool SendEmail(string receiver, string subject, string message) {
            try
            {
                var senderEmail = new MailAddress("corycherven@hotmail.com", "Cory");
                var receiverEmail = new MailAddress(receiver, "Receiver");
                var password = "animalmix55"; //to be changed
                var smtp = new SmtpClient
                {
                    Host = "smtp.office365.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, password)
                };
                using (var mess = new MailMessage(senderEmail, receiverEmail)
                {
                    Subject = subject,
                    Body = message

                })
                {
                    mess.IsBodyHtml = true;
                    smtp.Send(mess);
                }
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        /// <summary>
        /// Will commit or roll back any changes to the given email change verification object
        /// </summary>
        /// <param name="VerifyId"></param>
        /// <param name="emailChange"></param>
        /// <param name="user"></param>
        /// <param name="_context"></param>
        /// <returns></returns>
        public static string VerifyEmailChange(Guid VerifyId, EmailVerificationModel emailChange, UserModel user, ApplicationDbContext _context)
        {
            if (VerifyId == emailChange.CommitId)
            { //commit change
                var oldEmail = user.Email;
                user.Email = emailChange.Payload;
                emailChange.Payload = oldEmail; //after commiting, the emailChange object holds the OLD email!
                emailChange.Committed = true;
                var emailAlert = _context.UserAlert.Find(emailChange.AlertId);
                //remove email alert, should already be attached
                _context.UserAlert.Remove(emailAlert);
                return "The requested email change was executed";
            }
            else if (VerifyId == emailChange.RevertId) //roll back
            {
                if (DateTime.UtcNow > emailChange.ChangeTime.AddDays(10)) //don't let any rollbacks occur after 10 days!
                    return "The requested change expired after 10 days, try again!";

                if (emailChange.Committed)
                    user.Email = emailChange.Payload; //after commiting, the emailChange object holds the OLD email!

                var emailAlert = _context.UserAlert.Find(emailChange.AlertId);
                //remove email alert

                _context.EmailVerification.Remove(emailChange); //get rid of change if reverted!

                if (emailAlert != null)
                    _context.UserAlert.Remove(emailAlert);
                return "The requested email change was rolled back";
            }
            else //the change exists, hasn't been executed, but our GUID doesn't match for some reason...
                return "An unknown error occurred";
        }
        

        /// <summary>
        /// Renders the email frame at ~/Views/Email/EmailContainer.cshtml
        /// </summary>
        /// <param name="model"></param>
        /// <param name="partial"></param>
        /// <returns></returns>
        public static string RenderEmail(EmailViewModel Model)
        {
            var html = @"
            <html>
            <body>
                <header>
                    <nav style=""display:flex; align-items: center; height: 80px; border-bottom: solid rgba(0,0,0,.2) 2px"">
                        <a style=""text-decoration: none"" href="""+Model.rootUrl+@"""><div style=""font-family: 'Nunito Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol';position: relative;text-transform: uppercase;color: black;font-weight: 600;text-decoration: none;align-items: center;margin-right: auto;margin-left: 10px; font-size: 30pt; align-self: center; text-decoration:none;"">Baskify</div></a>
                    </nav>
                </header>";
                if (Model.Basket != null)
                {
                    var basket = Model.Basket;
                            html += @"<div class=""card mb-3 basketCard"" style=""
                    width: 300px;
                    border-radius: 30px;
                    position: relative;
                    -webkit-box-orient: vertical;
                    -webkit-box-direction: normal;
                    min-width: 0;
                    word-wrap: break-word;
                    background-color: #fff;
                    background-clip: border-box;
                    border: 1px solid rgba(0,0,0,0.125);
                    font-family: 'Nunito Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol';
                    left: 0;
                    right: 0;
                    margin: auto;
                    overflow: hidden;
                    margin-top: 10px;
                "">
                        <h3 class=""card-header"" style=""font-size: 114%;
                        padding: 0.75rem 1.25rem;
                        margin-bottom: 0;
                        background-color: rgba(0,0,0,0.03);
                        border-bottom: 1px solid rgba(0,0,0,0.125);
                        text-transform: uppercase;
                        letter-spacing: 3px;
                        margin:unset;
                         "">";
                            html += basket.BasketTitle + @"
                        </h3>
                        <div>
                            <div style=""height: 300px;width: inherit;overflow: hidden;display: flex;align-items: center; background-image: url(" + Model.rootUrl.Trim('/') + "/" + basket.photos[0].Url + @"); background-size:cover; background-position:center;"">
                        </div>
                            <div>
                                <div style=""font-weight: bold; font-size: 145%; text-transform: uppercase; text-align: center; border-bottom: solid rgba(1,1,1,0.1) 1px; border-top: solid rgba(1,1,1,0.1) 1px;"">
                                    Description:
                                </div>
                                <div class=""basketDesc"" style=""text-align:center;"">" + basket.BasketDescription + @"
                                </div>
                                <div style=""font-weight: bold; font-size: 145%; text-transform: uppercase; text-align: center; border-bottom: solid rgba(1,1,1,0.1) 1px; border-top: solid rgba(1,1,1,0.1) 1px;"">
                                    Contents:
                                </div>
                                <table style=""border-collapse: collapse; width:100%;"">
                                    <tbody style=""display: flex; justify-content: space-evenly; flex-wrap: wrap;"">";
                                        foreach (var item in basket.BasketContents)
                                        {
                                            html += @"<tr style='flex-basis: 50%; display:flex; justify-content:center' class='table-light'>
                                                <td style='padding: unset;'>" + item + @"</td>
                                            </tr>";
                                        }
                                    html += @"</tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                    <hr />";
                }
                html += @"<div style=""margin-bottom: 30px; margin-top:10px; font-family: 'Nunito Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol';"">";
                if (Model.User != null)
                {
                    if (Model.User.UserRole != Roles.COMPANY)
                    {
                        html += $"{Model.User.FirstName} {Model.User.LastName},";
                    }
                    else
                    {
                        html += $"{Model.User.OrganizationName},";
                    }
                }
                else
                {
                    html += "User,";
                }
                        html += @"</div>
                <div style=""padding-left:10px; font-family: 'Nunito Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol';"">"
                    + Model.Contents + @"
                </div>
                <footer style=""margin-top: 20px; font-size: 150%; display: flex; justify-content: center; font-weight: 300; font-family: 'Nunito Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol';"">
                    We thank you again for choosing Baskify!
                </footer>
            </body>
            </html>";
            return html;
        }
        public static void SendReceiptEmail(UserModel user, ReceiptDto receiptData)
        {
            var html = @"<!DOCTYPE html>
                    <html>
                    <head>

                      <meta charset=""utf-8"">
                      <meta http-equiv=""x-ua-compatible"" content=""ie=edge"">
                      <title>Email Receipt</title>
                      <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                      <style type=""text/css"">
                      @media screen {
                        @font-face {
                          font-family: 'Source Sans Pro';
                          font-style: normal;
                          font-weight: 400;
                          src: local('Source Sans Pro Regular'), local('SourceSansPro-Regular'), url(https://fonts.gstatic.com/s/sourcesanspro/v10/ODelI1aHBYDBqgeIAH2zlBM0YzuT7MdOe03otPbuUS0.woff) format('woff');
                        }

                        @font-face {
                          font-family: 'Source Sans Pro';
                          font-style: normal;
                          font-weight: 700;
                          src: local('Source Sans Pro Bold'), local('SourceSansPro-Bold'), url(https://fonts.gstatic.com/s/sourcesanspro/v10/toadOcfmlt9b38dHJxOBGFkQc6VGVFSmCnC_l7QZG60.woff) format('woff');
                        }
                      }
                      body,
                      table,
                      td,
                      a {
                        -ms-text-size-adjust: 100%; /* 1 */
                        -webkit-text-size-adjust: 100%; /* 2 */
                      }
                      table,
                      td {
                        mso-table-rspace: 0pt;
                        mso-table-lspace: 0pt;
                      }
                      img {
                        -ms-interpolation-mode: bicubic;
                      }
                      a[x-apple-data-detectors] {
                        font-family: inherit !important;
                        font-size: inherit !important;
                        font-weight: inherit !important;
                        line-height: inherit !important;
                        color: inherit !important;
                        text-decoration: none !important;
                      }
                      div[style*=""margin: 16px 0;""] {
                        margin: 0 !important;
                      }

                      body {
                        width: 100% !important;
                        height: 100% !important;
                        padding: 0 !important;
                        margin: 0 !important;
                      }
                      table {
                        border-collapse: collapse !important;
                      }
                      a {
                        color: #1a82e2;
                      }
                      img {
                        height: auto;
                        line-height: 100%;
                        text-decoration: none;
                        border: 0;
                        outline: none;
                      }
                      </style>

                    </head>
                    <body style=""background-color: #D2C7BA;"">
                      <div class=""preheader"" style=""display: none; max-width: 0; max-height: 0; overflow: hidden; font-size: 1px; line-height: 1px; color: #fff; opacity: 0;"">
                        Your transaction with Baskify completed successfully! Your account has been credited.
                      </div>
                      <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                        <tr>
                          <td align=""center"" bgcolor="""">
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px;"">
                              <tr>
                                <td align=""center"" valign=""top"" style=""padding: 36px 24px;"">
                                  <div target=""_blank"" style=""display: inline-block;"">
                                    <a class=""navigationBarBrand navBarElement"" style=""font-family: 'Nunito Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol';letter-spacing: 1px;font-size: 30pt;color: black;font-weight: bold;text-decoration: none;text-transform: uppercase;"" href=""https://baskify.com"">Baskify</a>
                                  </div>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td align=""center"" bgcolor=""#D2C7BA"">
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px;"">
                              <tr>
                                <td align=""left"" bgcolor=""#ffffff"" style=""padding: 36px 24px 0; font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; border-top: 3px solid #d4dadf;"">
                                  <h1 style=""margin: 0; font-size: 32px; font-weight: 700; letter-spacing: -1px; line-height: 48px;"">Thank you for your purchase!</h1>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td align=""center"" bgcolor=""#D2C7BA"">
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px;"">
                              <tr>
                                <td align=""left"" bgcolor=""#ffffff"" style=""padding: 24px; font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;"">
                                  <p style=""margin: 0;"">Here is a summary of your recent order. If you have any questions or concerns about your order, please <a href=""https://baskify.com"">contact us</a>.</p>
                                </td>
                              </tr>
                              <tr>
                                <td align=""left"" bgcolor=""#ffffff"" style=""padding: 24px; font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;"">
                                  <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                                    <tr>
                                      <td align=""left"" bgcolor=""#d2ebf5"" width=""33%"" style=""padding: 12px;font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;""><strong>Order #</strong></td>
                                      <td align=""left"" bgcolor=""#d2ebf5"" width=""33%"" style=""padding: 12px;font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;""><strong>Auction Name</strong></td>
                                        <td align=""left"" bgcolor=""#d2ebf5"" width=""33%"" style=""padding: 12px;font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;""><strong>Organization</strong></td>
                                    </tr>
                                  <tr>
                                        <td align=""left"" bgcolor=""#d2ebf5"" width=""33%"" style=""padding: 12px;font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 10px;"">" + receiptData.PaymentIntentId.ToUpper().Substring(15) + @"</td>
                                          <td align=""left"" bgcolor=""#d2ebf5"" width=""33%"" style=""padding: 12px;font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 10px;"">" + receiptData.AuctionModel.Title + @"</td>
                                          <td align=""left"" bgcolor=""#d2ebf5"" width=""33%"" style=""padding: 12px;font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 10px;"">" + receiptData.AuctionModel.HostUser.OrganizationName + @"</td>
                                    </tr>
                                    </table>
                
                                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin-top: 10px"">
                            <tr>
                                <tr>
                                      <td align=""left"" bgcolor=""#d2ebf5"" width=""33%"" style=""padding: 12px;font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;""><strong>Item</strong></td>
                                      <td align=""left"" bgcolor=""#d2ebf5"" width=""33%"" style=""padding: 12px;font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;""><strong>Amount (USD)</strong></td>
                                    </tr>
                                    <tr>
                                      <td align=""left"" width=""75%"" style=""padding: 6px 12px;font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;"">Ticket Purchase (x" + (int)((receiptData.Amount / 100) / receiptData.AuctionModel.TicketCost) + @")</td>
                                      <td align=""left"" width=""25%"" style=""padding: 6px 12px;font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;"">$" + Math.Round((decimal)(receiptData.Amount / 100), 2) + @"</td>
                                    </tr>
                                    <tr>
                                      <td align=""left"" width=""75%"" style=""padding: 12px; font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px; border-top: 2px dashed #D2C7BA; border-bottom: 2px dashed #D2C7BA;""><strong>Total</strong></td>
                                      <td align=""left"" width=""25%"" style=""padding: 12px; font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px; border-top: 2px dashed #D2C7BA; border-bottom: 2px dashed #D2C7BA;""><strong>$" + Math.Round((decimal)(receiptData.Amount / 100), 2) + @"</strong></td>
                                    </tr>
                                  </table>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td align=""center"" bgcolor=""#D2C7BA"" valign=""top"" width=""100%"">
                            <table align=""center"" bgcolor=""#ffffff"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px;"">
                              <tr>
                                <td align=""center"" valign=""top"" style=""font-size: 0; border-bottom: 3px solid #d4dadf"">
                                  <div style=""display: inline-block; width: 100%; max-width: 50%; min-width: 240px; vertical-align: top;"">
                                    <table align=""left"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 300px;"">
                                      <tr>
                                        <td align=""left"" valign=""top"" style=""padding-bottom: 36px; padding-left: 36px; font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;"">
                                          <p><strong>Billing Address</strong></p>
                                          <p>" + receiptData.BillingAddress + @"<br>" + $"{receiptData.BillingCity}, {receiptData.BillingState} {receiptData.BillingZIP}" + @"</p>
                                        </td>
                                      </tr>
                                    </table>
                                  </div>
                                  <div style=""display: inline-block; width: 100%; max-width: 50%; min-width: 240px; vertical-align: top;"">
                                    <table align=""left"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 300px;"">
                                      <tr>
                                        <td align=""left"" valign=""top"" style=""padding-bottom: 36px; padding-left: 36px; font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px;"">
                                          <p><strong>Card Info</strong></p>
                                          <p>" + receiptData.CardholderName + @"<br>" + receiptData.CardType + @" (ends in " + receiptData.CardLastFour + @")<br>Expires: " + receiptData.CardExp + @"</p>
                                        </td>
                                      </tr>
                                    </table>
                                  </div>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td align=""center"" bgcolor=""#D2C7BA"" style=""padding: 24px;"">
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px;"">
                            </table>
                          </td>
                        </tr>
                      </table>
                    </body>
                    </html>";

            SendEmail(user.Email, "Baskify Purchase", html);
        }
    }

    
}
