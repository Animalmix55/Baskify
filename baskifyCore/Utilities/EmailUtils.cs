using Microsoft.AspNetCore.Http;
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
        public static bool sendVerificationEmail(string originalEmail, Guid revertId, string newEmail, Guid commitId, HttpRequest request)
        {
            var OriginalEmailText = "<h1>Thank you for using Baskify!</h1>" +
                "We have received your request to change your email to <b>" + newEmail +"</b>\n";

            var NewEmailText = OriginalEmailText + 
                "<br><br>To verify your new email address, please <a href=\"" + LoginUtils.getAbsoluteUrl("/account/verifyemailchange?emailVerifyId=" + commitId, request) + "\">CLICK HERE</a>.\n" +
                "<h1>Thank you!</h1>";

            OriginalEmailText +=
                "<br><br>If you did not request this change, please <a href=\"" + LoginUtils.getAbsoluteUrl("/account/verifyemailchange?emailVerifyId=" + revertId, request) + "\">CLICK HERE</a>.\n" +
                "<h1>Have a great day!</h1>";

            var subject = "Baskify - Your Email Change Request";
            return SendEmail(originalEmail, subject, OriginalEmailText) && SendEmail(newEmail, subject, NewEmailText);
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
    }
}
