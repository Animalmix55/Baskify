using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
            if (!ModelState.IsValid)
            {
                return View("Index", user);
            }
            else
            {
                var oldUser = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
                if (oldUser != null) {
                    if (Icon != null)
                        accountUtils.saveFile(user, Icon, _env.WebRootPath); //this will add the file url to the user object as well
                    try
                    {
                        accountUtils.updateUser(oldUser, user, _context, Request);
                    }
                    catch(Exception e)
                    {
                        ViewData["Alert"] = "Verification Emails Failed to Send, Please Try Again Later.";
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
    }
}