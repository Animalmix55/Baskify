using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class FAQController : ControllerBase
    {
        ApplicationDbContext _context;
        public FAQController()
        {
            _context = new ApplicationDbContext();
        }

        [Route("search")]
        [HttpGet]
        public ActionResult Search([FromQuery] string searchQuery, [FromQuery] string Token)
        {
            if (!CaptchaUtils.verifyToken(Token))
                return BadRequest("Invalid captcha");

            List<FAQModel> FAQs;
            if (string.IsNullOrWhiteSpace(searchQuery))
                FAQs = _context.FAQModel.ToList();
            else
                FAQs = _context.FAQModel.Where(f => DbFunctions.Like(searchQuery, f.Question) || DbFunctions.Like(searchQuery, f.Response)).ToList();

            FAQs.ForEach(f => { //replace any macros
                f.Response = f.Response.Replace("{FeePerTransaction}", DTOs.Fees.FeePerTrans.ToString());
                f.Response = f.Response.Replace("{FeePercent}", DTOs.Fees.FeePercent.ToString());
            });

            return Ok(FAQs);
        }
    }
}