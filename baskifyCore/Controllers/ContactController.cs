using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using baskifyCore.Models;
using baskifyCore.Utilities;
using baskifyCore.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers
{
    public class ContactController : Controller
    {
        ApplicationDbContext _context;
        IHttpContextAccessor _accessor;
        IWebHostEnvironment _env;

        public ContactController(IHttpContextAccessor accessor, IWebHostEnvironment env)
        {
            _context = new ApplicationDbContext();
            _accessor = accessor;
            _env = env;
        }
        public IActionResult Index()
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user != null)
                ViewData["NavBarOverride"] = user;
            else
                ViewData["NavBarOverride"] = new UserModel();

            var model = new ContactModel()
            {
                Email = user?.Email
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Submit(ContactModel model)
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user != null)
                ViewData["NavBarOverride"] = user;
            else
                ViewData["NavBarOverride"] = new UserModel();

            if (!ModelState.IsValid)
            {
                ViewData["Alert"] = "There was an error with your form content";
                return View("~/Views/Contact/Index.cshtml", model);
            }

            if(!CaptchaUtils.verifyToken(model.Token, _accessor.HttpContext.Connection.RemoteIpAddress.ToString()))
            {
                ViewData["Alert"] = "Your captcha was invalid!";
                return View("~/Views/Contact/Index.cshtml", model);
            }

            model.SubmissionTime = DateTime.UtcNow;
            model.Message = HttpUtility.HtmlEncode(model.Message);

            _context.ContactModel.Add(model);
            _context.SaveChanges();

            //Send email to admin
            var admin = _context.UserModel.Find(ConfigurationManager.AppSettings["AdminUser"]);
            if (admin != null)
                EmailUtils.SendStyledEmail(admin, "Baskify Contact Submission", $"A user submitted a contact request form to Baskify.com with the following content: <br><br> {model.Message} <br><br> The form was submitted from IP {_accessor.HttpContext.Connection.RemoteIpAddress.ToString()} with the contact email of {model.Email}", _env);

            ViewData["Alert"] = "Submission sent, we will respond within 24-48 hours.";
            ModelState.Clear();

            return View("~/Views/Contact/Index.cshtml", new ContactModel() { Email = model.Email });
        }
    }
}