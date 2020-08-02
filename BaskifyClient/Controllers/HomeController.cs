
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BaskifyClient.Models;
using BaskifyClient.Utilities;
using System.Linq;

namespace BaskifyClient.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Redirect("/account");
        }
    }
}
