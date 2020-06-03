using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers
{
    public class SearchController : Controller
    {

        Models.ApplicationDbContext _context;
        public SearchController()
        {
            _context = new Models.ApplicationDbContext();
        }

        [HttpGet]
        public IActionResult Index(string searchQuery)
        {
            var user = Utilities.LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);

            ViewData["NavBarOverride"] = user ?? new Models.UserModel();

            return View((object)searchQuery);
        }
    }
}