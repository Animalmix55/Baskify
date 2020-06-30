using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Controllers
{
    public class SignUpController : Controller
    {
        private IHttpContextAccessor _accessor; //injects ip
        ApplicationDbContext _context;
        public SignUpController(IHttpContextAccessor accessor)
        {
            _context = new ApplicationDbContext();
            _accessor = accessor;
        }
        public IActionResult Index()
        {
            LoginUtils.getAnonymousId(_context, Request, _accessor, Response); //gets a session id
            return View();
        }
    }
}
