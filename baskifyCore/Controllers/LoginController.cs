using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using Microsoft.AspNetCore.Mvc;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

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

        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            try
            {
                var user = LoginUtils.getUserAsync(username, password).Result;
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

        public IActionResult signout()
        {
            LoginUtils.deleteTokenFromCookie(Response);

            return Redirect("/");
        }
    }
}