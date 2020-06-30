using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using baskifyCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;
using baskifyCore.DTOs;

namespace baskifyCore.Controllers.api.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class AdminController : ControllerBase
    {
        ApplicationDbContext _context;
        public AdminController()
        {
            _context = new ApplicationDbContext();
        }

        /// <summary>
        /// Update the given user model
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("user")]
        public ActionResult UpdateUser([FromForm] UserModel user)
        {
            if (user == null)
                return BadRequest();
            var destUser = _context.UserModel.Find(user.Username);
            if (destUser == null)
                return NotFound();



            if (destUser.Address != user.Address)
            {
                //check address
                var address = Utilities.accountUtils.validateAddress(user.Address, user.City, user.State, user.ZIP);

                if (address["resultStatus"] == "ADDRESS NOT FOUND")
                    return BadRequest("Invalid address");
                //now set everything!
                destUser.Address = address["addressLine1"];
                destUser.City = address["city"];
                destUser.State = address["state"];
                destUser.ZIP = address["zip"];
                destUser.Latitude = float.Parse(address["lat"]);
                destUser.Longitude = float.Parse(address["lng"]);
            }

            destUser.UserRole = user.UserRole;

            if(destUser.UserRole == Roles.COMPANY)
            {
                destUser.OrganizationName = user.OrganizationName;
                destUser.ContactEmail = user.ContactEmail;
            }
            else
            {
                destUser.EIN = null;
                destUser.OrganizationName = null;
                destUser.ContactEmail = null;
            }

            destUser.Email = user.Email;
            destUser.isMFA = user.isMFA;

            if (destUser.isMFA)
            {
                destUser.PhoneNumber = new String(user.PhoneNumber.Where(c => char.IsNumber(c)).ToArray()); //only numbers
            }

            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        [Route("lock")]
        public ActionResult Lock([FromForm] string Username, [FromForm] LockReason LockReason, [FromForm] string LockDetails)
        {
            var user = _context.UserModel.Find(Username);
            if (user == null)
                return NotFound();

            if ((LockReason == LockReason.OrgPending || LockReason == LockReason.InvalidDocuments) && user.UserRole != Roles.COMPANY)
                return BadRequest("A user cannot be locked with an organization-specific lock"); //keep org locks off users

            user.Locked = true;
            user.LockReason = LockReason;
            user.LockDetails = HttpUtility.HtmlEncode(LockDetails);

            _context.SaveChanges();

            return Ok();
        }

        [HttpDelete]
        [Route("unlock")]
        public ActionResult Unlock([FromForm] string Username)
        {
            var user = _context.UserModel.Find(Username);
            if (user == null)
                return NotFound();

            if(user.LockReason == LockReason.OrgPending && user.Locked)
            {
                return BadRequest("Use /api/admin/verifyOrg for organization verification");
            }

            user.Locked = false;
            user.LockReason = null;
            user.LockDetails = null;

            _context.SaveChanges();

            return Ok();
        }

        [HttpPut]
        [Route("verifyOrg")]
        public ActionResult VeryifyOrg([FromForm] string Username, [FromForm] NonProfitDto Org)
        {
            var user = _context.UserModel.Find(Username);
            if (user == null)
                return NotFound();

            if (user.LockReason != LockReason.OrgPending || !user.Locked)
            {
                return BadRequest("Organization not pending");
            }

            if (user.UserRole != Roles.COMPANY)
                return BadRequest("User is not an organization");

            var IRSNonProfit = _context.IRSNonProfit.Find(Org.EIN); //use inputted EIN ALWAYS
            if (IRSNonProfit == null)
            {
                IRSNonProfit = new IRSNonProfit()
                {
                    City = Org.City,
                    State = Org.State,
                    Country = Org.Country,
                    EIN = Org.EIN,
                    OrganizationName = Org.OrganizationName
                };

                _context.IRSNonProfit.Add(IRSNonProfit);
                _context.SaveChanges();
            }

            user.EIN = IRSNonProfit.EIN;
            user.OrganizationName = IRSNonProfit.OrganizationName; //set user EIN and orgname

            user.Locked = false;
            user.LockReason = null;
            user.LockDetails = null;

            _context.SaveChanges();

            //TODO: SEND EMAIL TO CONTINUE STRIPE REGISTRATION

            return Ok();
        }

        [HttpGet]
        [Route("getDocumentLinks")]
        public ActionResult GetDocLinks([FromQuery] string Username)
        {
            var user = _context.UserModel.Include(u => u.Documents).Where(u => u.Username == Username).FirstOrDefault();
            if (user == null)
                return NotFound();

            var linkList = new List<string>();

            foreach(var doc in user.Documents)
            {
                linkList.Add("/api/admin/viewDocument/" + doc.Id);
            }

            return Ok(linkList);
        }

        [HttpGet]
        [Route("viewDocument/{id}")]
        public ActionResult ViewDocument([FromRoute] int id)
        {
            var file = _context.AccountDocumentsModel.Find(id);
            return File(file.Document, file.ContentType);

        }

        [HttpGet]
        [Route("GetOrg/{ein}")]
        public ActionResult GetOrg([FromRoute] long ein){
            var org = _context.IRSNonProfit.Find(ein);
            if (org == null)
                return NotFound();
            else
                return Ok(org);
        }
    }
}