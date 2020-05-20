using baskifyCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Policy;
using System.Threading.Tasks;

namespace baskifyCore.Utilities
{
    public static class EmailUtils
    {
        public static bool sendVerificationEmail(string email, EmailVerificationModel verificationModel, HttpRequest request)
        {
            var OriginalEmailText = "<h1>Thank you for using Baskify!</h1>" +
                "We have received your request to";
            string subject;
            switch (verificationModel.ChangeType)
            {
                case ChangeTypes.EMAIL:
                    OriginalEmailText += " change your email to <b>" + verificationModel.Payload + "</b>\n";
                    var NewEmailText = OriginalEmailText +
                        "<br><br>To verify this change, please <a href=\"" + LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.CommitId, verificationModel.ChangeId), request) + "\">CLICK HERE</a>.\n" +
                        "<h1>Thank you!</h1>";

                    OriginalEmailText +=
                        "<br><br>If you did not request this change, please <a href=\"" + LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.RevertId, verificationModel.ChangeId), request) + "\">CLICK HERE</a>.\n" +
                        "<h1>Have a great day!</h1>";

                    subject = "Baskify - Your Email Change Request";
                    return SendEmail(email, subject, OriginalEmailText) && SendEmail(verificationModel.Payload, subject, NewEmailText);

                case ChangeTypes.AUCTIONDELETION:
                    OriginalEmailText += " delete your auction entitled: <b>" + verificationModel.Payload + "</b>\n";
                    OriginalEmailText += "If you did not request this change, just ignore this email; your account is not at any risk.</b>\n";
                    OriginalEmailText += "<br><br>If you did request this change, verify it by clicking <a href=\"" + LoginUtils.getAbsoluteUrl(String.Format("/verify?VerifyId={0}&ChangeId={1}", verificationModel.CommitId, verificationModel.ChangeId), request) + "\">HERE</a>.\n" +
                        "<h1>Thank you!</h1>";
                    subject = "Baskify - Your Auction Deletion Request";
                    return SendEmail(email, subject, OriginalEmailText);
            }
            return false;
            

            
        }

        public static bool sendRecoveryEmail(string email, string bearerToken, HttpRequest request)
        {
            var EmailText = "<h1>Thank you for using Baskify!</h1>" +
                "We have received your request to reset your passcode! If you did not request this change, just ignore this email; your account is not at any risk.</b>\n";

            EmailText +=
                "<br><br>If you did request this change, please <a href=\"" + LoginUtils.getAbsoluteUrl("/account/changepassword?bearerToken=" + bearerToken, request) + "\">CLICK HERE</a>.\n" +
                "<h1>Have a great day!</h1>";

            var subject = "Baskify - Your Password Change Request";
            return SendEmail(email, subject, EmailText);
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
                if (emailAlert != null )
                    _context.UserAlert.Remove(emailAlert);

                _context.EmailVerification.Remove(emailChange); //get rid of change if reverted!
                return "The requested email change was rolled back";
            }
            else //the change exists, hasn't been executed, but our GUID doesn't match for some reason...
                return "An unknown error occurred";
        }
    }
}
