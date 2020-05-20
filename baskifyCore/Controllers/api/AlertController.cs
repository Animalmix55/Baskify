using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Issuing;

namespace baskifyCore.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertController : ControllerBase
    {
        ApplicationDbContext _context;
        public AlertController()
        {
            _context = new ApplicationDbContext();
        }

        [HttpDelete]
        public ActionResult Delete([FromHeader] string authorization, [FromForm] Guid id) 
        {
            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return BadRequest("Invalid Credentials");

            var alert = _context.UserAlert.Find(id);
            if (alert == null)
                return BadRequest("Invalid Alert ID");

            if (alert.Username != user.Username)
                return BadRequest("You do not have sufficient permissions");

            //now the alert exists and belongs to the user!
            if (!alert.Dismissable)
                return BadRequest("This alert is not dismissable");

            //now we can remove it
            _context.UserAlert.Remove(alert);
            _context.SaveChangesAsync();
            return Ok();
        } 
    }
}