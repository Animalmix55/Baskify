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

namespace baskifyCore.Controllers
{
    public class LoginController : Controller
    {
        ApplicationDbContext _context;
        public LoginController()
        {
            _context = new ApplicationDbContext();
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

            return View((object)redirectBack);
        }

        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            try
            {
                var user = LoginUtils.getUserAsync(username, password, _context).Result;
                //var ip = GetIp();

                var cookieOptions = new CookieOptions();
                //cookieOptions.Expires = new DateTimeOffset(DateTime.Now.AddDays(1)); //creates expiration... right now only for session
                Response.Cookies.Append("BearerToken", user.bearerToken, cookieOptions);

                return PartialView("NavBar", user);
            }
            catch(Exception)
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
        public IActionResult LoginRedirect(string username, string password, string redirectUrl)
        {
            try
            {
                var user = LoginUtils.getUserAsync(username, password, _context).Result;
                //var ip = GetIp();

                var cookieOptions = new CookieOptions();
                //cookieOptions.Expires = new DateTimeOffset(DateTime.Now.AddDays(1)); //creates expiration... right now only for session
                Response.Cookies.Append("BearerToken", user.bearerToken, cookieOptions);

                return Content(LoginUtils.checkRedirectLocation(redirectUrl, Request)); //returns redirect url for browser from query string
            }
            catch (Exception e)
            {
                return Content("ERROR: Invalid Password");
            }

        }

        public IActionResult signout()
        {
            LoginUtils.deleteTokenFromCookie(Response);

            return Redirect("/");
        }

        /// <summary>
        /// Will load the signon modal designed (a) specifically to reload an iFrame that has failed to load
        /// or (b) to silently renew the token to redo an update/add/delete action.
        /// </summary>
        /// <param name="targetIframe"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult loadSignOnModal(string targetElement, string actionUrl, string jsonParams)
        {
            //if target element is "reloadPage", then reloads the entire page...
            ViewData["jsonParams"] = jsonParams;
            ViewData["actionUrl"] = actionUrl; //this is the url of the action we want the signon to complete into the element.
            return PartialView("SignInPartialView", targetElement);
        }
    }
}