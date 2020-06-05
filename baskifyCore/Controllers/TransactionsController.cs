using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers
{
    public class TransactionsController : Controller
    {
        ApplicationDbContext _context;
        public TransactionsController()
        {
            _context = new ApplicationDbContext();
        }

        public IActionResult Index()
        {
            var user = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (user == null)
                return Redirect("/login?redirectBack=" + LoginUtils.getAbsoluteUrl("/transactions", Request));

            return View(user);
        }
    }
}