using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using baskifyCore.Controllers.api;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers
{
    public class VerifyController : Controller
    {
        private ApplicationDbContext _context;
        private IWebHostEnvironment _env;
        public VerifyController(IWebHostEnvironment env)
        {
            _context = new ApplicationDbContext();
            _env = env;
        }

        // /verify
        [HttpGet]
        public IActionResult Index(int ChangeId, Guid VerifyId)
        {
            var userIsTemp = false;

            if (VerifyId == null)//if accidentally navigated here, go home
                return Redirect("/");

            var verification = _context.EmailVerification.Find(ChangeId);

            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);

            if(verification?.ChangeType == ChangeTypes.ADDSTRIPE) //unlock the user
            {
                var tempUser = _context.UserModel.Find(verification.Username);
                if (tempUser.LockReason == LockReason.StripePending)
                {
                    tempUser.Locked = false;
                    tempUser.LockReason = null;
                }
                _context.SaveChanges();
            }

            if (user == null)//invalid login state, redirect to login if not verifying email or reverting email change
            {
                if ((verification?.ChangeType == ChangeTypes.EMAIL && VerifyId == verification?.RevertId) || verification?.ChangeType == ChangeTypes.VERIFYEMAIL)
                {
                    user = _context.UserModel.Find(verification.Username);
                    userIsTemp = true;
                }
                else
                    return Redirect("/login?redirectBack=" + HttpUtility.UrlEncode(Request.GetDisplayUrl()));
            }

            if (verification == null) //reverted changes are deleted or...
            { //some odd issue occured?
                ViewData["Alert"] = "The requested change was reverted/not found!";
                return View("~/Views/Home/Index.cshtml", user);
            }
            else if (verification.Committed && VerifyId == verification.CommitId) //can't commit twice, but can revert still, if reverted, you'll never get here
            {
                ViewData["Alert"] = "The requested change was already executed";
                return View("~/Views/Home/Index.cshtml", user);
            }
            else if (verification.Username != user.Username) //don't allow the change to affect other users
            {
                ViewData["Alert"] = "This requested change does not belong to the current account!";
                return View("~/Views/Home/Index.cshtml", user);
            }

            if (verification.ChangeType != ChangeTypes.MFA)
            {
                //the change is legit

                switch (verification.ChangeType)
                {
                    case ChangeTypes.EMAIL:
                        ViewData["Alert"] = EmailUtils.VerifyEmailChange(VerifyId, verification, user, _context);
                        break;
                    case ChangeTypes.AUCTIONDELETION:
                        ViewData["Alert"] = AuctionUtilities.VerifyDeletion(VerifyId, verification, user, _context, _env.WebRootPath);
                        break;
                    case ChangeTypes.VERIFYEMAIL:
                        ViewData["Alert"] = EmailUtils.VerifyEmail(VerifyId, verification, user);
                        break;
                    case ChangeTypes.ADDSTRIPE:
                        var stateKey = accountUtils.GetStripeRegistrationState(VerifyId, verification, user, _context);
                        var baskifyLandingPage = HttpUtility.UrlEncode(LoginUtils.getAbsoluteUrl("/Stripe/completeSignup", Request));
                        var redirectLink = $"https://connect.stripe.com/express/oauth/authorize?redirect_uri={baskifyLandingPage}&client_id={StripeConsts.clientId}&state={stateKey}";
                        return Redirect(redirectLink);
                    case ChangeTypes.CONTACTEMAIL:
                        ViewData["Alert"] = EmailUtils.VerifyContactEmailChange(VerifyId, verification, user);
                        break;
                }

                _context.SaveChanges();
                return View("~/Views/Home/Index.cshtml", !userIsTemp? user : new UserModel()); //redirect home, only provide user if they are logged in initially
            }
            else //MFA change
            {
                if (verification.Payload != null) { //this is an MFA enable script
                    //the payload of an MFA enable email verification is the phone number, send the verification code
                    var verificationMFAModel = VerificationCodeUtils.CreateVerification(null, VerificationType.EnableMFA, null, user.Username, long.Parse(verification.Payload), _context);
                    PhoneUtils.SendValidationMessage("+" + long.Parse(verification.Payload), verificationMFAModel.Secret); //send text
                    ViewData["NavBarOverride"] = user;
                    return View("~/Views/VerificationCode/Index.cshtml", verificationMFAModel); //return view of page where user can validate MFA
                }
                else {
                    ViewData["Alert"] = VerificationCodeUtils.VerifyDisable(VerifyId, verification, user, _context);
                    _context.SaveChanges();
                    return View("~/Views/Home/Index.cshtml", user); //redirect home
                }
            }
        }
    }
}