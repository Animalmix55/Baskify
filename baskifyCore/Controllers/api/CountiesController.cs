using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountiesController : ControllerBase
    {
        ApplicationDbContext _context;
        public CountiesController()
        {
            _context = new ApplicationDbContext();
        }

        [Route("{state}")]
        public ActionResult CountiesByState([FromRoute] string state)
        {
            return Ok(_context.CountyModel.ToList().Where(a => a.StateModel.Equals(state)).Select(c => new { Name = c.CountyName, Id = c.Id }).ToList());
        }
    }
}