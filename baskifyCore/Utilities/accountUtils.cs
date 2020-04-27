using baskifyCore.Migrations;
using baskifyCore.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;

namespace baskifyCore.Utilities
{
    public static class accountUtils
    {

        public static void saveFile(UserModel user, IFormFile file, string webroot)
        {
            var extension = Path.GetExtension(file.FileName);
            var filename = webroot + "/Content/userIcons/" + user.Username + "-icon" + extension; //should be safe characters
            using (var fs = new FileStream(Path.GetFullPath(filename), FileMode.Create))
            {
                file.CopyTo(fs);
            }
            user.iconUrl = "/Content/userIcons/" + user.Username + "-icon" + extension;
        }

        /// <summary>
        /// Updates all the modifiable user components.
        /// </summary>
        /// <param name="oldUser"></param>
        /// <param name="newUser"></param>
        /// <param name="_context">Uses context to update the user, alert, and email change entries</param>
        /// <param name="env">Needs the environment to produce an email verification link without hardcoding</param>
        public static void updateUser(UserModel oldUser, UserModel newUser, ApplicationDbContext _context, HttpRequest req)
        {
            oldUser.FirstName = newUser.FirstName;
            oldUser.LastName = newUser.LastName;
            if (oldUser.Email != newUser.Email)
            {
                Guid commitId = Guid.NewGuid();
                Guid revokeId = Guid.NewGuid();

                var emailsSent = EmailUtils.sendVerificationEmail(oldUser.Email, revokeId, newUser.Email, commitId, req);

                if (!emailsSent)
                    throw new Exception("Verification Emails Failed to Send");

                var emailChange = new EmailChangeModel() //this is the email change sent to server
                { ChangeTime = DateTime.Now,
                    CommitId = commitId,
                    Committed = false,
                    NewEmail = newUser.Email,
                    OriginalEmail = oldUser.Email,
                    RevertId = revokeId,
                    Reverted = false,
                    Username = oldUser.Username
                };

                var EmailAlert = new UserAlertModel()//Add email change alert
                {
                    AlertHeader = "Email Change Pending",
                    AlertType = "EmailAlert",
                    AlertBody = string.Format("You have a pending email change to {0}, please accept the revision in your new account's inbox", newUser.Email),
                    Username = oldUser.Username
                };

                _context.EmailChange.Add(emailChange);
                _context.UserAlert.AddOrUpdate(EmailAlert);
                _context.SaveChanges();
                _context.Entry(oldUser).Reload();

            }

            oldUser.Address = newUser.Address; //TODO implement verification
            oldUser.City = newUser.City;
            oldUser.State = newUser.State;
            oldUser.ZIP = newUser.ZIP;
            oldUser.iconUrl = newUser.iconUrl;
        }
        public static string getMapLink(UserModel user)
        {
            string APIKey = ConfigurationManager.AppSettings["GoogleAPIKey"].ToString();
            var address = String.Format("{0}, {1}, {2} {3}", user.Address, user.City, user.State, user.ZIP);
            return String.Format("https://www.google.com/maps/embed/v1/place?key={0}&q={1}", APIKey, HttpUtility.UrlEncode(address));
        }

        public static EmailChangeModel getEmailChangeModel(UserModel user, ApplicationDbContext _context)
        {
            return _context.EmailChange.Find(user.Username);
        }
    }
}
