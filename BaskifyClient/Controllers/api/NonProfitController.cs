using BaskifyClient.Models;
using BaskifyClient.Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class NonProfitController : ControllerBase
    {
        ApplicationDbContext _context;
        public NonProfitController()
        {
            _context = new ApplicationDbContext();
        }

        [HttpGet]
        [Route("search")]
        public ActionResult Search(string query, string state)
        {
            var orgs = NonProfitUtils.Search(query, _context, state);
            return Ok(orgs);
        }

        [HttpGet]
        [Route("select/{ein}")]
        public ActionResult Select(int ein)
        {
            var org = NonProfitUtils.Select(ein, _context);
            if (org == null)
                return BadRequest("Invalid Ein");
            else
                return Ok(org);
        }
    }
}
