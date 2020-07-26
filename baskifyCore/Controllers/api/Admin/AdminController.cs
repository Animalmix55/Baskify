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
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Hosting;

namespace baskifyCore.Controllers.api.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class AdminController : ControllerBase
    {
        ApplicationDbContext _context;
        IWebHostEnvironment _env;
        public AdminController(IWebHostEnvironment env)
        {
            _context = new ApplicationDbContext();
            _env = env;
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

            if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
            {
                var emailVer = new EmailVerificationModel()
                {
                    ChangeTime = DateTime.UtcNow,
                    ChangeType = ChangeTypes.ADDSTRIPE,
                    CommitId = Guid.NewGuid(),
                    Username = user.Username,
                    Payload = ""
                };
                
                user.Locked = true;
                user.LockReason = LockReason.StripePending; //lock account for stripe to ensure emial validation

                _context.EmailVerification.Add(emailVer);
                _context.SaveChanges();

                EmailUtils.sendVerificationEmail(user, emailVer, Request, _env);
            }

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

        [HttpPost]
        [Route("PinMessage/{id}")]
        public ActionResult PinMessage([FromRoute] int id, [FromForm] bool Pinned)
        {
            var message = _context.ContactModel.Find(id);
            if (message == null)
                return NotFound();

            message.Pinned = Pinned;
            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        [Route("ReadMessage/{id}")]
        public ActionResult ReadMessage([FromRoute] int id)
        {
            var message = _context.ContactModel.Find(id);
            if (message == null)
                return NotFound();

            message.Read = true;
            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        [Route("DeleteMessage/{id}")]
        public ActionResult DeleteMessage([FromRoute] int id)
        {
            var message = _context.ContactModel.Find(id);
            if (message == null)
                return NotFound();

            _context.ContactModel.Remove(message);
            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        [Route("GetMessages")]
        public ActionResult GetMessages(IncomingSearchDto search)
        {
            try
            {
                int firstResult = search.start;
                int maxRecords = search.length;

                var query = _context.ContactModel.AsQueryable();

                var totalNum = query.Count();

                if (!string.IsNullOrWhiteSpace(search.search.value) && !search.search.regex) //avoid regex
                {
                    query = query.Where(
                        a => DbFunctions.Like(a.Email.ToLower(), "%" + search.search.value.ToLower() + "%") ||
                        DbFunctions.Like(a.Subject.ToLower(), "%" + search.search.value.ToLower() + "%")); //global search
                }

                foreach (var column in search.columns)
                {
                    if (column.searchable && !string.IsNullOrWhiteSpace(column.search.value) && !column.search.regex)
                    {
                        switch (column.name)
                        {
                            case "Email":
                                query = query.Where(a => DbFunctions.Like(a.Email.ToLower(), "%" + column.search.value.ToLower() + "%"));
                                break;
                            case "Subject":
                                query = query.Where(a => DbFunctions.Like(a.Subject.ToLower(), "%" + column.search.value.ToLower() + "%"));
                                break;
                        }
                    }
                }

                //ordering round 1
                int i = 0;
                IncomingSearchDto.OrderItem orderitem = search.order[i];
                IOrderedQueryable<ContactModel> orderedQuery;
                switch (search.columns[orderitem.column].name)
                {
                    case "Email":
                        if (orderitem.dir == "asc")
                            orderedQuery = query.OrderBy(a => a.Email);
                        else
                            orderedQuery = query.OrderByDescending(a => a.Email);
                        break;
                    case "Subject":
                        if (orderitem.dir == "asc")
                            orderedQuery = query.OrderBy(a => a.Subject);
                        else
                            orderedQuery = query.OrderByDescending(a => a.Subject);
                        break;
                    case "SubmissionTime":
                        if (orderitem.dir == "asc")
                            orderedQuery = query.OrderBy(a => a.SubmissionTime);
                        else
                            orderedQuery = query.OrderByDescending(a => a.SubmissionTime);
                        break;
                    default:
                        orderedQuery = (IOrderedQueryable<ContactModel>)query; //do nothing
                        break;
                }


                //now thenby
                for (i = 1; i < search.order.Count; i++)
                {
                    orderitem = search.order[i];
                    switch (search.columns[orderitem.column].name)
                    {

                        case "Email":
                            if (orderitem.dir == "asc")
                                orderedQuery = query.OrderBy(a => a.Email);
                            else
                                orderedQuery = query.OrderByDescending(a => a.Email);
                            break;
                        case "Subject":
                            if (orderitem.dir == "asc")
                                orderedQuery = query.OrderBy(a => a.Subject);
                            else
                                orderedQuery = query.OrderByDescending(a => a.Subject);
                            break;
                        case "SubmissionTime":
                            if (orderitem.dir == "asc")
                                orderedQuery = query.OrderBy(a => a.SubmissionTime);
                            else
                                orderedQuery = query.OrderByDescending(a => a.SubmissionTime);
                            break;
                    }
                }
                //now query is ordered and filtered

                orderedQuery = orderedQuery.OrderBy(c => c.Read).OrderByDescending(c => c.Pinned); //put read at bottom, pinned at top

                var resultSet = orderedQuery.ToList();
                var recordsFiltered = resultSet.Count;

                if (search.start + search.length < resultSet.Count)
                    resultSet = resultSet.GetRange(search.start, search.length); //trim down result set
                else if (search.start + 1 <= resultSet.Count)
                {
                    var numRemaining = resultSet.Count - search.start;
                    resultSet = resultSet.GetRange(search.start, numRemaining);
                }
                else
                    resultSet = new List<ContactModel>(); //empty

                var returnObject = new
                {
                    draw = search.draw,
                    recordsTotal = totalNum,
                    recordsFiltered = recordsFiltered,
                    data = resultSet
                };
                return Ok(returnObject);
            }
            catch (Exception e)
            {
                var returnObject = new
                {
                    draw = search.draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<ContactModel>(),
                    error = "An error was encountered, try again"
                };
                return BadRequest(returnObject);
            }
        }
    }
}