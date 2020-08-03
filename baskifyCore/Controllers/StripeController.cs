using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using baskifyCore.Utilities;
using System.Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace baskifyCore.Controllers
{

    [Route("stripe")]
    public class Stripe : Controller
    {
        ApplicationDbContext _context;
        public Stripe()
        {
            _context = new ApplicationDbContext();
        }

        [HttpGet]
        [Route("completeSignup")]
        public ActionResult completeSignup(string code, string state)
        {
            var stateGuid = Guid.Parse(state);
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context);
            if(user == null)
            {
                return Redirect($"/login?redirectBack={System.Web.HttpUtility.UrlEncode(LoginUtils.getAbsoluteUrl($"/stripe/completeSignup?code={code}&state={state}", Request))}");
            }

            var registrationModel = _context.StripeRegistrationModel.Include(m => m.UserModel).Where(m => m.State == stateGuid).FirstOrDefault();
            if (registrationModel == null)
            {
                ViewData["Alert"] = "Stripe registration failed, please try again";
                return View("~/Views/Home/Index.cshtml", user ?? new UserModel());
            }
            else if(registrationModel.Username != user.Username) //only allow state values to be valid for an hour to avoid fraud
            {
                ViewData["Alert"] = "Error! Stripe registration intended for another account";
                return View("~/Views/Home/Index.cshtml", user);
            }

            //get client id
            var options = new OAuthTokenCreateOptions
            {
                GrantType = "authorization_code",
                Code = code
            };

            var service = new OAuthTokenService();
            var response = service.Create(options);

            registrationModel.Complete = true;
            registrationModel.UserModel.StripeCustomerId = response.StripeUserId;
            if(registrationModel.EmailVerificationId != null)
                registrationModel.EmailVerification.Committed = true; //mark email verification as complete

            if (string.IsNullOrWhiteSpace(response.StripeUserId))
            {
                ViewData["Alert"] = "Stripe registration failed, please try again";
                return View("~/Views/Home/Index.cshtml", user);
            }

            _context.SaveChanges();

            ViewData["Alert"] = "Stripe registration succeeded!";
            return View("~/Views/Home/Index.cshtml", user);
        }
    }
}