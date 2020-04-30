using baskifyCore.Models;
using baskifyCore.Utilities;
using baskifyCore.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;

namespace baskifyCore.Controllers
{
    public class AccountController : Controller
    {
        private readonly IWebHostEnvironment _env;
        ApplicationDbContext _context;
        public AccountController(IWebHostEnvironment env)
        {
            _env = env;
            _context = new ApplicationDbContext();
        }
        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        public IActionResult Index()
        {
            ViewData["GoogleAPIKey"] = ConfigurationManager.AppSettings["GoogleAPIKey"].ToString();
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?redirectBack="+LoginUtils.getAbsoluteUrl("/account", Request));

            return View(user);
        }

        [HttpPost]
        public IActionResult updateValues(UserModel user, IFormFile Icon)
        {
        var oldUser = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (oldUser != null) {
                if (!ModelState.IsValid)
                {
                    ViewData["NavBarOverride"] = oldUser; //this allows the navbar to render correctly
                    ViewData["Alert"] = "The change could not be completed due to a formatting issue.";
                    return View("Index", user);
                }
                if (Icon != null)
                    if (!accountUtils.saveIcon(user, Icon, _env.WebRootPath, 500, 500)) //this will add the file url to the new user object as well
                        ViewData["Alert"] = "Image upload failed, try another?";
                try
                {
                    accountUtils.updateUser(oldUser, user, _context, Request, ModelState);
                }
                catch(Exception)//only the email change throws error
                {
                    ViewData["Alert"] = "Verification Emails Failed to Send, Please Try Again Later.";
                    ViewData["NavBarOverride"] = oldUser;
                    return View("Index", user);
                }
                _context.SaveChanges(); //save olduser changes
                return View("Index", oldUser);
            }
            else
            {
                ViewData["LoginAgain"] = true;
                return View("Index", user);
            }
            
        }


        [HttpPost]
        public IActionResult updatePassword(ChangePasswordViewModel model)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            model.reset = LoginUtils.isValidPasswordResetToken(Request.Cookies["BearerToken"]);
            if (user == null)
            {
                ViewData["Alert"] = "Invalid Login, please login again";
                return View("~/Views/Login/Index.cshtml", LoginUtils.getAbsoluteUrl("/account/changepassword", Request));
            }
            else
            {
                if (!ModelState.IsValid)
                {
                    //model passed failed constraints
                }
                else if (LoginUtils.hashPassword(model.CurrentPassword) == user.PasswordHash)//password matches
                {
                    if (accountUtils.setPassword(user, model.NewPassword, _context))
                    {
                        ViewData["Alert"] = "Password Changed Successfully";
                        ModelState.Clear();
                        model.clear();
                    }
                    else //something botches password change
                        ViewData["Alert"] = "An unknown error occurred";
                }
                else if (model.reset)
                {
                    if (accountUtils.setPassword(user, model.NewPassword, _context))
                    {
                        ViewData["Alert"] = "Password Changed Successfully, please log back in";
                        LoginUtils.destroyToken(Request.Cookies["BearerToken"], _context, Response); //destroys reset token in DB and cookie
                        return View("~/Views/Home/Index.cshtml",new UserModel());
                    }
                    else //something botches password change
                        ViewData["Alert"] = "An unknown error occurred";
                }
                else //password doesn't match
                    ModelState.AddModelError("CurrentPassword", "Current password does not match");

                ViewData["NavBarOverride"] = user; //this allows the navbar to render correctly
                return View("ChangePassword", model);
            }
        }

        [HttpPost]
        public IActionResult sendRecoveryEmail(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View("ForgotPassword", model);
            try
            {
                var sent = accountUtils.sendRecoveryEmail(model, _context, Request);
                if (sent)
                    ViewData["Alert"] = "Recovery email sent to your inbox!";
                else
                {
                    ModelState.AddModelError("Username", "Unable to locate account");
                    ModelState.AddModelError("Email", "Unable to locate account");
                }
                return View("ForgotPassword", model);
            }
            catch (Exception) //something went wrong
            {
                ViewData["Alert"] = "Error sending recovery email";
                return View("ForgotPassword", model);
            }
        }

        [HttpGet]
        public IActionResult forgotPassword()
        {
            if (LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response) != null)
            {//user is logged in
                return Redirect("/"); //go home
            }

            var model = new ForgotPasswordViewModel();
            return View("ForgotPassword", model);
        }

        [HttpGet]
        public IActionResult changePassword(string bearerToken)
        {
            var passwordChange = new ChangePasswordViewModel();
            UserModel user;
            if (LoginUtils.isValidPasswordResetToken(bearerToken)) //valid reset token
            {
                user = LoginUtils.getUserFromToken(bearerToken, _context);
                if (user != null)//token issued by server
                {
                    passwordChange.reset = true; //this is a reset
                    Response.Cookies.Append("BearerToken", bearerToken, new CookieOptions() { Expires = new DateTimeOffset(DateTime.Now.AddMinutes(10)) }); //add cookie for ten minutes
                }
            }
            else //retrieve token from user cookies
            {
                user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response); //gets logged in user's password change screen
                if(LoginUtils.isValidPasswordResetToken(Request.Cookies["BearerToken"])) //if the current cookie token is a reset token
                    passwordChange.reset = true;
            }


            if(user == null)
            {
                ViewData["NavBarOverride"] = new UserModel();
                ViewData["Alert"] = "Your login expired";
                return View("~/Views/Login/Index.cshtml", LoginUtils.getAbsoluteUrl("/account/changepassword", Request));
            }
            
            ViewData["NavBarOverride"] = user; //this allows the navbar to render correctly
            return View("ChangePassword", passwordChange);
        }

        [HttpGet]
        public IActionResult verifyEmailChange(Guid emailVerifyId)
        {
            if (emailVerifyId == null)//if accidentally navigated here, go home
                return Redirect("/");

            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)//invalid login state, redirect to login
                return Redirect("/login?redirectBack=" + Request.GetDisplayUrl());

            //now for the magic
            var emailChange = _context.EmailChange.Where(ec => ec.Username == user.Username).OrderByDescending(ec => ec.ChangeTime).FirstOrDefault(); //gets the most recent change object

            if (emailChange == null | (emailVerifyId != emailChange.RevertId && emailVerifyId != emailChange.CommitId))
            { //some odd issue occured?
                ViewData["Alert"] = "The requested email change could not be found for your user";
            }
            else if (emailChange.Committed)
            {
                ViewData["Alert"] = "The requested email change was already executed";
            }
            else if (emailChange.Reverted)
            {
                ViewData["Alert"] = "The requested email change was already rolled back";
            }
            //the email change is legit

            else if (emailVerifyId == emailChange.CommitId)
            { //commit change
                user.Email = emailChange.NewEmail;
                emailChange.Committed = true;
                var emailAlert = _context.UserAlert.Find(user.Username, "EmailAlert");
                //remove email alert, should already be attached
                _context.UserAlert.Remove(emailAlert);
                ViewData["Alert"] = "The requested email change was executed";
            }
            else if(emailVerifyId == emailChange.RevertId)
            {
                emailChange.Reverted = true; //yes, this is strange, but committed just means it was somehow verified/canceled
                var emailAlert = _context.UserAlert.Find(user.Username, "EmailAlert");
                //remove email alert
                _context.UserAlert.Remove(emailAlert);
                ViewData["Alert"] = "The requested email change was rolled back";
            }
            else //the change exists, hasn't been executed, but our GUID doesn't match for some reason...
                ViewData["Alert"] = "An unknown error occurred";

            _context.SaveChanges();
            _context.Entry(user).Reload(); //update everything

            return View("Index", user); //redirect to account

        }

        /// <summary>
        /// Returns the address in a JSON format if is valid, or just a dictionary of resultStatus: "ADDRESS NOT FOUND" otherwise...
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="City"></param>
        /// <param name="State"></param>
        /// <param name="ZIP"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult validateAddress(string Address, string City, string State, string ZIP)
        {
            var response = accountUtils.validateAddress(Address, City, State, ZIP);
            if (response.ContainsKey("addressLine1"))
            {
                response.Add("GoogleMapUrl", accountUtils.getMapLink(response["addressLine1"], response["city"], response["state"], response["zip"]));
            }

            return Content(JsonConvert.SerializeObject(response));
        }
    }
}