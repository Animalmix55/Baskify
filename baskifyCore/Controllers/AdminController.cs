using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace baskifyCore.Controllers
{

    /// <summary>
    /// The controller used for administrating Baskify
    /// </summary>
    public class AdminController: Controller
    {
        [Authorize(Policy = "Admin")]
        [HttpGet]
        public IActionResult Index()
        {
            //now we're validated
            return View();
            
        }
    }
}
