using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Controllers
{
    public class FAQController : Controller
    {
        ApplicationDbContext _context;
        public FAQController()
        {
            _context = new ApplicationDbContext();
        }
        public IActionResult Index()
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                user = new Models.UserModel();

            return View(user);
        }
    }
}
