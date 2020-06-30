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

        public IActionResult signout()
        {
            LoginUtils.deleteTokenFromCookie(Response);

            return Redirect("/");
        }
    }
}