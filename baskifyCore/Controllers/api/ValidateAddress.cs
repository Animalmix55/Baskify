using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValidateAddress: Controller
    {
        ApplicationDbContext _context;
        public ValidateAddress()
        {
            _context = new ApplicationDbContext();
        }

        /// <summary>
        /// Returns the address in a JSON format if is valid, or just a dictionary of resultStatus: "ADDRESS NOT FOUND" otherwise...
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="City"></param>
        /// <param name="State"></param>
        /// <param name="ZIP"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Index([FromForm]string Address, [FromForm] string City, [FromForm] string State, [FromForm] string ZIP, [FromHeader] string authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization))
                return Unauthorized("No authorization");

            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
            if (user == null)
                return Unauthorized("Invalid auuthorization"); //require people be logged in to avoid abuse...

            var response = accountUtils.validateAddress(Address, City, State, ZIP); //this response will already contain lat and lng
            if (response.ContainsKey("addressLine1"))
            {
                response.Add("GoogleMapUrl", accountUtils.getMapLink(response["addressLine1"], response["city"], response["state"], response["zip"]));
            }

            return Ok(response);
        }
    }
}
