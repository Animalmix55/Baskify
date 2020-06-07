using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using Microsoft.AspNetCore.Mvc;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Extensions;
using baskifyCore.ViewModels;

namespace baskifyCore.Controllers
{
    public class LoginController : Controller
    {
        ApplicationDbContext _context;
        public LoginController()
        {
            _context = new ApplicationDbContext();
        }
        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }


        /// <summary>
        /// This is for the login page that users can access
        /// </summary>
        /// <param name="redirectBack"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Index(string redirectBack) //redirect will be in query string
        {
            if (Request.Cookies["BearerToken"] != null)
            {
                if (LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response) != null)
                    return Redirect(LoginUtils.checkRedirectLocation(redirectBack, Request)); //redirects if already logged in
            }
            var loginModel = new LoginViewModel() { redirectUrl = redirectBack };
            return View(loginModel);
        }

        /// <summary>
        /// Returns a navbar, used by any mid-application login
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            try
            {
                var user = LoginUtils.getUserAsync(username, password, _context).Result;
                //var ip = GetIp();

                var cookieOptions = new CookieOptions();
                //cookieOptions.Expires = new DateTimeOffset(DateTime.UtcNow.AddDays(1)); //creates expiration... right now only for session
                Response.Cookies.Append("BearerToken", user.bearerToken, cookieOptions);

                return PartialView("NavBar", user);
            }
            catch(Exception e)
            {
                return Content("ERROR: Invalid Password");
            }
            
        }

        /// <summary>
        /// This is strictly for the login page or any page that has a redirect. Produces the bearer token cookie and returns redirect url
        /// Gets redirect url from the query string of the login page...
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="redirectLocation"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult LoginRedirect(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }
            try
            {
                var user = LoginUtils.getUserAsync(model.Username, model.Password, _context).Result;
                //var ip = GetIp();

                var cookieOptions = new CookieOptions();
                //cookieOptions.Expires = new DateTimeOffset(DateTime.UtcNow.AddDays(1)); //creates expiration... right now only for session
                Response.Cookies.Append("BearerToken", user.bearerToken, cookieOptions);

                return Redirect(LoginUtils.checkRedirectLocation(model.redirectUrl, Request)); //returns redirect url for browser from query string
            }
            catch (InvalidPasswordException)
            {
                ModelState.AddModelError("Password", "Invalid Password");
                return View("Index", model);
            }
            catch (Exception)
            {
                ViewData["Alert"] = "User could not be located.";
                return View("Index", model);
            }


        }

        public IActionResult signout()
        {
            LoginUtils.deleteTokenFromCookie(Response);

            return Redirect("/");
        }
    }
}