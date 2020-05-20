using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            if (VerifyId == null)//if accidentally navigated here, go home
                return Redirect("/");

            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)//invalid login state, redirect to login
                return Redirect("/login?redirectBack=" + Request.GetDisplayUrl());

            //now for the magic
            var verification = _context.EmailVerification.Find(ChangeId);


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
            
            //the change is legit

            switch (verification.ChangeType)
            {
                case ChangeTypes.EMAIL:
                    ViewData["Alert"] = EmailUtils.VerifyEmailChange(VerifyId, verification, user, _context);
                    break;

                case ChangeTypes.AUCTIONDELETION:
                    ViewData["Alert"] = AuctionUtilities.VerifyDeletion(VerifyId, verification, user, _context, _env.WebRootPath);
                    break;
            }

            _context.SaveChanges();
            _context.Entry(user).Reload(); //update everything

            return View("~/Views/Home/Index.cshtml", user); //redirect home
        }
    }
}