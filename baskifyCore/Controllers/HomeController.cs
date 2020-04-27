
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using baskifyCore.Models;
using baskifyCore.Utilities;
using System.Linq;

namespace baskifyCore.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext _context;

        public HomeController()
        {
            _context = new ApplicationDbContext();
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        public IActionResult Index()
        {
            UserModel userModel = LoginUtils.getUserFromToken(Request.Cookies["BearerToken"], _context, Response);
            if (userModel == null)
                userModel = new UserModel();
            return View(userModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
