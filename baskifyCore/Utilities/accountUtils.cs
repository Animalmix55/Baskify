﻿using baskifyCore.Migrations;
using baskifyCore.Models;
using baskifyCore.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;

namespace baskifyCore.Utilities
{
    public static class accountUtils
    {

        public static bool saveIcon(UserModel user, IFormFile file, string webroot, int height, int width)
        {
            var inputStream = file.OpenReadStream();
            try
            {
                var bitmap = new Bitmap(inputStream);
                bitmap = imageUtilities.ResizePhoto(bitmap, height, width); //resize to small square
                //var extension = Path.GetExtension(file.FileName);
                var filename = webroot + "/Content/userIcons/" + user.Username + "-icon" + ".jpeg"; //should be safe characters
                using (var fs = new FileStream(Path.GetFullPath(filename), FileMode.Create))
                {
                    bitmap.Save(fs, ImageFormat.Jpeg);
                }
                user.iconUrl = "/Content/userIcons/" + user.Username + "-icon" + ".jpeg";
                inputStream.Close();
                return true;
            }
            catch (Exception)
            {
                inputStream.Close();
                return false;
            }
                

        }

        /// <summary>
        /// Validates the given address against USPS, returns what the user passed in if there's an error...
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="City"></param>
        /// <param name="State"></param>
        /// <param name="ZIP"></param>
        /// <returns>
        /// A dictionary of the form: {
        ///                            resultStatus: status,
        ///                            addressLine1: address,
        ///                            city: city,
        ///                            state: state,
        ///                            zip: zip
        ///                            }        
        /// </returns>
        public static Dictionary<string, string> validateAddress(string Address, string City, string State, string ZIP)
        {
            //ADDRESS VALIDATION VIA USPS
            var values = new Dictionary<string, string>
            {
                {"address1", Address},
                {"city", City },
                {"state", State },
                {"zip", ZIP }
            };
            Dictionary<string, string> returnDict;
            try
            {
                //assume true so that any error with API is not an issue
                var client = new HttpClient();
                var content = new FormUrlEncodedContent(values);
                var response = client.PostAsync("https://tools.usps.com/tools/app/ziplookup/zipByAddress", content).Result;

                var requestBodyString = response.Content.ReadAsStringAsync().Result;

                JObject parent = JObject.Parse(requestBodyString);
                //this contains resultstatus and addresslist
                var resultStatus = parent.Value<string>("resultStatus");
                if (resultStatus != "ADDRESS NOT FOUND")
                {
                    JArray addressValues = (JArray)parent["addressList"];
                    var addressObject = addressValues[0].Value<JObject>();

                    returnDict = new Dictionary<string, string>() {
                        { "resultStatus", resultStatus },
                        {"addressLine1", addressObject.Value<string>("addressLine1") },
                        {"city", addressObject.Value<string>("city") },
                        {"state", addressObject.Value<string>("state") },
                        {"zip", addressObject.Value<string>("zip5") + "-" + addressObject.Value<string>("zip4") }
                    };
                    return returnDict;
                }
                else
                    return new Dictionary<string, string>() { { "resultStatus", "ADDRESS NOT FOUND" } };
            }
            catch (Exception)
            {
                return new Dictionary<string, string>() { { "resultStatus", "DEFAULTED" }, { "addressLine1", Address },{ "city", City },{ "state", State },{ "zip", ZIP } };
            }

        }

        /// <summary>
        /// Updates all the modifiable user components.
        /// </summary>
        /// <param name="oldUser"></param>
        /// <param name="newUser"></param>
        /// <param name="_context">Uses context to update the user, alert, and email change entries</param>
        /// <param name="req">Needs the request to produce an email verification link without hardcoding</param>
        public static void updateUser(UserModel oldUser, UserModel newUser, ApplicationDbContext _context, HttpRequest req, ModelStateDictionary ModelState)
        {
            Dictionary<string, string> addressDict = validateAddress(newUser.Address, newUser.City, newUser.State, newUser.ZIP);
            if (addressDict["resultStatus"] == "ADDRESS NOT FOUND")
            {
                ModelState.AddModelError("Address", "Invalid Address");
                return;
            }
            else
            {
                newUser.Address = addressDict["addressLine1"];
                newUser.City = addressDict["city"];
                newUser.State = addressDict["state"];
                newUser.ZIP = addressDict["zip"];
            }

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
            
            //To get here, address must be valid and emails must have sent
            oldUser.Address = newUser.Address;
            oldUser.City = newUser.City;
            oldUser.State = newUser.State;
            oldUser.ZIP = newUser.ZIP;
            oldUser.iconUrl = newUser.iconUrl;

            oldUser.FirstName = newUser.FirstName;
            oldUser.LastName = newUser.LastName;
            ModelState.Clear();
        }

        /// <summary>
        /// Returns a map link, or an empty map link waiting to be appended if not provided an address
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="City"></param>
        /// <param name="State"></param>
        /// <param name="ZIP"></param>
        /// <returns></returns>
        public static string getMapLink(string Address, string City, string State, string ZIP)
        {
            var address = "";
            string APIKey = ConfigurationManager.AppSettings["GoogleAPIKey"].ToString();
            address = String.Format("{0}, {1}, {2} {3}", Address, City, State, ZIP);
            return String.Format("https://www.google.com/maps/embed/v1/place?key={0}&q={1}", APIKey, HttpUtility.UrlEncode(address));
        }


        public static EmailChangeModel getEmailChangeModel(UserModel user, ApplicationDbContext _context)
        {
            return _context.EmailChange.Find(user.Username);
        }


        /// <summary>
        /// Set's the user's password. NOTE: User must already be tracked by DB...
        /// </summary>
        /// <param name="user"></param>
        /// <param name="newPassword"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool setPassword(UserModel user, string newPassword, ApplicationDbContext _context)
        {
            try
            {
                var hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(newPassword));
                StringBuilder Sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    Sb.Append(b.ToString("x2"));
                }
                var hash = Sb.ToString();

                user.PasswordHash = hash;
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }



        public static bool sendRecoveryEmail(ForgotPasswordViewModel model, ApplicationDbContext _context, HttpRequest request)
        {
            UserModel user;
            if (model.isEmailValidation)
            {
                user = _context.UserModel.Where(um => um.Email == model.Email).FirstOrDefault();
                if (user == null)
                    user = _context.UserModel.Find(model.Username);
            }
            else
            {
                user = _context.UserModel.Find(model.Username);
                if (user == null)
                    user = _context.UserModel.Where(um => um.Email == model.Email).FirstOrDefault();
            }

            if (user == null) //this user does not exist, for sure
                return false;

            var RecoveryBearerToken = LoginUtils.buildToken(user, _context, TokenTypes.PASSWORDRESET).Result;

            var sent = EmailUtils.sendRecoveryEmail(user.Email, RecoveryBearerToken, request);

            if (!sent) //if for some reason there's an error...
                throw new Exception("Email failed to send");

            return true;

        }
    }

}