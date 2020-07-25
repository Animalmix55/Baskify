using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers
{
    public class ErrorController : Controller
    {

        IWebHostEnvironment _env;
        public ErrorController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index(int? statusCode=null)
        {
            if (statusCode.HasValue)
            {
                // here is the trick
                this.HttpContext.Response.StatusCode = statusCode.Value;
            }

            switch (statusCode)
            {
                case 404:
                    return PhysicalFile(_env.ContentRootPath + "\\ErrorPages\\404Error.html", "text/html");
                default:
                    return PhysicalFile(_env.ContentRootPath + "\\ErrorPages\\Error.html", "text/html");
            }
            
        }
    }
}