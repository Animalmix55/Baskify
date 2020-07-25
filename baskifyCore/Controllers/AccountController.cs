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
using System.IO;
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

            //format user phone number correctly...
            user.PhoneNumber = user.PhoneNumber != null? new string(user.PhoneNumber.Where(c => char.IsNumber(c)).ToArray()) : null; //removes errant chars, only numbers

            if (oldUser != null) {

                if (oldUser.Email != user.Email)//dont allow the email to be changed more than once in a ten-day range
                {
                    var lastEmailChange = _context.EmailVerification.Where(ev => ev.Username == oldUser.Username).Where(ev => ev.ChangeType == ChangeTypes.EMAIL).OrderByDescending(ev => ev.ChangeTime).FirstOrDefault();

                    if (lastEmailChange != null && lastEmailChange.ChangeTime > DateTime.UtcNow.AddDays(-10)) 
                        ModelState.AddModelError("Email", "Email cannot be changed more than once in 10 days!");
                }

                if(oldUser.isMFA != user.isMFA || oldUser.PhoneNumber != user.PhoneNumber) //don't allow hackers to change email to change MFA until 10 days have passed
                {
                    var lastEmailChange = _context.EmailVerification.Where(ev => ev.Username == oldUser.Username).Where(ev => ev.ChangeType == ChangeTypes.EMAIL).OrderByDescending(ev => ev.ChangeTime).FirstOrDefault();

                    if (lastEmailChange != null && lastEmailChange.ChangeTime > DateTime.UtcNow.AddDays(-10))
                        ModelState.AddModelError("PhoneNumber", "MFA settings cannot be tweaked within 10 days of an email change");
                }

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
                    accountUtils.updateUser(oldUser, user, _context, Request, ModelState, this, _env);
                }
                catch(Exception)//only the email change throws error
                {
                    ViewData["Alert"] = "Verification Emails Failed to Send, Please Try Again Later.";
                    ViewData["NavBarOverride"] = oldUser;
                    return View("Index", user);
                }
                _context.SaveChanges(); //save olduser changes
                ViewData["Alert"] = "Account updated!";
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
                var sent = accountUtils.sendRecoveryEmail(model, _context, Request, this, _env);
                if (sent)
                    ViewData["Alert"] = "Recovery email sent to your inbox!";
                else
                {
                    ModelState.AddModelError("Username", "Unable to locate account");
                    ModelState.AddModelError("Email", "Unable to locate account");
                }
                return View("ForgotPassword", model);
            }
            catch (Exception e) //something went wrong
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
                    Response.Cookies.Append("BearerToken", bearerToken, new CookieOptions() { Expires = new DateTimeOffset(DateTime.UtcNow.AddMinutes(10)) }); //add cookie for ten minutes
                }
            }
            else //retrieve token from user cookies
            {
                user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response); //gets logged in user's password change screen
                if(LoginUtils.isValidPasswordResetToken(Request.Cookies["BearerToken"])) //if the current cookie token is a reset token
                    passwordChange.reset = true;
            }


            if(user == null)
                return Redirect("/login?redirectBack="+ LoginUtils.getAbsoluteUrl("/account/changepassword", Request));
            
            ViewData["NavBarOverride"] = user; //this allows the navbar to render correctly
            return View("ChangePassword", passwordChange);
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
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Content("ERROR: INVALID LOGIN"); //require people be logged in to avoid abuse...

            var response = accountUtils.validateAddress(Address, City, State, ZIP); //this response will already contain lat and lng
            if (response.ContainsKey("addressLine1"))
            {
                response.Add("GoogleMapUrl", accountUtils.getMapLink(response["addressLine1"], response["city"], response["state"], response["zip"]));
            }

            return Content(JsonConvert.SerializeObject(response));
        }
    }
}