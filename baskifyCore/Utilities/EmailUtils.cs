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
using System.Configuration;
using System.Drawing.Text;
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
        private static string applicationEmail;
        private static string emailEndpoint;
        private static int emailPort;

        static EmailUtils()
        {
            applicationEmail = ConfigurationManager.AppSettings["Email"];
            emailEndpoint = ConfigurationManager.AppSettings["EmailEndpoint"];
            emailPort = int.Parse(ConfigurationManager.AppSettings["EmailPort"]);
        }

        public static bool SendStyledEmail(UserModel user, string Subject, string Message, IWebHostEnvironment _env, string EmailOverride=null)
        {
            return SendEmail(EmailOverride == null? user.Email: EmailOverride, Subject, getEmailTemplate(Subject, Message, null, null, _env));
        }

        public static string getEmailTemplate(string title, string body, List<string> buttonTextList, List<string> buttonLinkList, IWebHostEnvironment _env)
        {
            var templateFile = File.ReadAllText(_env.ContentRootPath + @"\EmailTemplates\singleButtonEmailTemplate.html");

            const string buttonDiv = @"<div align=""center"" class=""button-container"" style=""padding-top:15px;padding-right:10px;padding-bottom:0px;padding-left:10px;"">
                <!--[if mso]><table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""border-spacing: 0; border-collapse: collapse; mso-table-lspace:0pt; mso-table-rspace:0pt;""><tr><td style=""padding-top: 15px; padding-right: 10px; padding-bottom: 0px; padding-left: 10px"" align=""center""><v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word"" href="""" style=""height:46.5pt; width:197.25pt; v-text-anchor:middle;"" arcsize=""97%"" stroke=""false"" fillcolor=""#e8c47c""><w:anchorlock/><v:textbox inset=""0,0,0,0""><center style=""color:#ffffff; font-family:Tahoma, sans-serif; font-size:16px""><![endif]-->
                <a href=""{0}""><div style=""text-decoration:none;display:inline-block;color:#ffffff;background-color:#e8c47c;border-radius:60px;-webkit-border-radius:60px;-moz-border-radius:60px;width:auto; width:auto;;border-top:1px solid #e8c47c;border-right:1px solid #e8c47c;border-bottom:1px solid #e8c47c;border-left:1px solid #e8c47c;padding-top:15px;padding-bottom:15px;font-family:Montserrat, Trebuchet MS, Lucida Grande, Lucida Sans Unicode, Lucida Sans, Tahoma, sans-serif;text-align:center;mso-border-alt:none;word-break:keep-all;""><span style=""padding-left:30px;padding-right:30px;font-size:16px;display:inline-block;""><span style=""font-size: 16px; margin: 0; line-height: 2; word-break: break-word; mso-line-height-alt: 32px;""><strong>{1}</strong></span></span></div></a>
                <!--[if mso]></center></v:textbox></v:roundrect></td></tr></table><![endif]-->
                </div>";

            var buttons = "";
            if (buttonTextList != null && buttonLinkList != null)
            {
                for (int i = 0; i < buttonTextList.Count; i++)
                {
                    buttons += String.Format(buttonDiv, buttonLinkList[i], buttonTextList[i]);
                }
            }

            string[] contents = templateFile.Split("</head>");
            templateFile = contents[0] + "</head>" + String.Format(contents[1], title, body, buttons);

            return templateFile;
        }

        /// <summary>
        /// Sends the winner an email, should attach the auction submitting user to the auction
        /// </summary>
        /// <param name="user"></param>
        /// <param name="basket"></param>
        /// <param name="auction"></param>
        /// <returns></returns>
        public static bool sendBasketWinnerEmail(UserModel user, BasketModel basket, AuctionModel auction, IWebHostEnvironment _env)
        {
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

            return SendEmail(user.Email, $"Baskify - Congrats!", getEmailTemplate("Congrats!", contents, null, null, _env));
        }

        public static bool sendVerificationEmail(UserModel user, EmailVerificationModel verificationModel, HttpRequest request, IWebHostEnvironment _env, object additionalInfo=null)
        {
            
            var OrigEmailText = "We have received your request to";
            string subject;
            switch (verificationModel.ChangeType)
            {
                case ChangeTypes.EMAIL:
                    OrigEmailText += " change your email to <b>" + verificationModel.Payload + "</b>.";
                    var newEmailText = OrigEmailText + " If you did not make this request do not worry, just ignore this email! If you did, use the button below to verify!";

                    OrigEmailText += " If you did not make this request, you don't have to worry, just use the button below to undo any changes and be sure to secure your account by changing the password!";

                    subject = "Your Email Change Request";

                    return SendEmail(user.Email, subject, getEmailTemplate(subject, OrigEmailText, new List<string>() { "Undo Change" }, new List<string>() { LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.RevertId, verificationModel.ChangeId), request) }, _env))
                        && SendEmail(verificationModel.Payload, subject, getEmailTemplate(subject, newEmailText, new List<string>() { "Verify Change" }, new List<string>() { LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.CommitId, verificationModel.ChangeId), request) }, _env));

                case ChangeTypes.AUCTIONDELETION:
                    OrigEmailText += " delete your auction entitled: <b>" + (additionalInfo as AuctionModel).Title + "</b>.\n";
                    OrigEmailText += "If you did not request this change, just ignore this email; your account is not at any risk.\n";
                    OrigEmailText += "If you did request this change, verify it by clicking the button below!";

                    subject = "Your Auction Deletion Request";

                    return SendEmail(user.Email, subject,
                        getEmailTemplate(subject, OrigEmailText, new List<string>() { "Verify Deletion" }, new List<string>() { LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.CommitId, verificationModel.ChangeId), request) }, _env));

                case ChangeTypes.MFA:

                    if (verificationModel.Payload == null) //disable MFA
                        OrigEmailText += $" disable multi-factor authentification for your account {user.Username}. ";
                    else
                        OrigEmailText += $" enable multi-factor authentification for your account {user.Username}. ";
                    OrigEmailText += "If you did not request this change, just ignore this email and <b>CHANGE YOUR PASSWORD IMMEDIATELY</b> as this request originates from a logged-in account.\n";
                    OrigEmailText += "<br><br>If you did request this change, verify it by clicking the button below!";
                    subject = "Your MFA Change Request";

                    return SendEmail(user.Email, subject, getEmailTemplate(subject, OrigEmailText, new List<string>() { "Verify Change" }, new List<string>() { LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.CommitId, verificationModel.ChangeId), request) }, _env));
                case ChangeTypes.VERIFYEMAIL:
                    OrigEmailText = "Thank you for creating an account with Baskify! It's a pleasure to meet you! <br><br> In order to verify your account, we ask that you please verify your email address by clicking the button below!";
                    OrigEmailText += "If you did not create this account, do not worry, just ignore this email";

                    subject = "Welcome to Baskify!";

                    return SendEmail(user.Email, subject, getEmailTemplate(subject, OrigEmailText, new List<string>() { "Verify Email" }, new List<string>() { LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.CommitId, verificationModel.ChangeId), request) }, _env));

                case ChangeTypes.ADDSTRIPE:
                    OrigEmailText = "Thank you for creating an account with Baskify! It's a pleasure to meet you! <br><br> In order to complete your organization's registration, we ask that you complete signup through our affiliate, Stripe.";
                    OrigEmailText  += "Stripe is our secure intermediary used to ensure that your organization gets paid when an auction is complete. You can register with Stripe by clicking the button below!";
                    OrigEmailText += "<br><br>If you did not create this account, do not worry, just ignore this email";
                    subject = "Register with Stripe!";

                    return SendEmail(user.Email, subject, getEmailTemplate(subject, OrigEmailText, new List<string>() { "Add Stripe" }, new List<string>() { LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.CommitId, verificationModel.ChangeId), request) }, _env));
            }
            return false;
        }

        public static bool sendRecoveryEmail(UserModel user, string bearerToken, HttpRequest request, IWebHostEnvironment _env)
        {
            var EmailText = "We have received your request to reset your passcode! If you did not request this change, just ignore this email; your account is not at any risk. If you did request this change, please click the button below!";

            var subject = "Your Password Change Request";

            return SendEmail(user.Email, subject, getEmailTemplate(subject, EmailText, new List<string>() { "Change Password "}, new List<string>(){ LoginUtils.getAbsoluteUrl("/account/changepassword?bearerToken=" + bearerToken, request) }, _env));
        }
        public static bool SendEmail(string receiver, string subject, string message) {
            try
            {
                var senderEmail = new MailAddress(applicationEmail, "Baskify");
                var receiverEmail = new MailAddress(receiver, "Recipient");
                var password = ConfigurationManager.AppSettings["EmailPassword"]; //never stored at rest
                var smtp = new SmtpClient
                {
                    Host = emailEndpoint,
                    Port = emailPort,
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

        /// <summary>
        /// Unlocks a user account after account creation
        /// </summary>
        /// <param name="VerifyId"></param>
        /// <param name="verification"></param>
        /// <param name="user"></param>
        /// <param name="_context"></param>
        /// <returns></returns>
        public static string VerifyEmail(Guid VerifyId, EmailVerificationModel verification, UserModel user, ApplicationDbContext _context)
        {
            if (verification.CommitId == VerifyId)
            {
                if (user.Locked && user.LockReason == LockReason.PendingEmail) //unlock user
                {
                    user.Locked = false;
                    user.LockReason = null;
                    return "Account verified! Please log in to begin enjoying Baskify!";
                }
                else
                    return "Verification failed: user is not pending verification.";
            }
            else
                return "An unknown error occured, invalid verification code.";
        }
    }

    
}
