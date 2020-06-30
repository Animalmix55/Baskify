using baskifyCore.DTOs;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.AspNetCore.Hosting;
using System.Dynamic;

namespace baskifyCore.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignUpController : ControllerBase
    {
        HttpClient _client;
        ApplicationDbContext _context;
        private IHttpContextAccessor _accessor;
        IWebHostEnvironment _env;
        public SignUpController(IHttpContextAccessor accessor, IWebHostEnvironment env)
        {
            _accessor = accessor;
            _context = new ApplicationDbContext();
            _client = new HttpClient();
            _env = env;
        }

        [HttpPost]
        [Route("validateAddress")]
        public ActionResult ValidateAddress(AddressDto address)
        {
            var values = new Dictionary<string, string>
            {
                {"address1", address.Line1},
                {"address2", address.Line2},
                {"city", address.City },
                {"state", address.State },
                {"zip", address.PostalCode }
            };
            var client = new HttpClient();
            var content = new FormUrlEncodedContent(values);

            try
            {
                var response = client.PostAsync("https://tools.usps.com/tools/app/ziplookup/zipByAddress", content).Result;

                var requestBodyString = response.Content.ReadAsStringAsync().Result;

                JObject parent = JObject.Parse(requestBodyString);
                //this contains resultstatus and addresslist
                var resultStatus = parent.Value<string>("resultStatus");
                if (resultStatus == "SUCCESS")
                {
                    JArray addressValues = (JArray)parent["addressList"];
                    var addressObject = addressValues[0].Value<JObject>();

                    var returnModel = new AddressDto()
                    {
                        Line1 = addressObject.Value<string>("addressLine1"),
                        City = addressObject.Value<string>("city"),
                        State = addressObject.Value<string>("state"),
                        PostalCode = addressObject.Value<string>("zip5") + (string.IsNullOrEmpty(addressObject.Value<string>("zip4")) ? "" : "-" + addressObject.Value<string>("zip4"))
                    };

                    return Ok(returnModel);
                }
                else
                    return BadRequest();
            }
            catch (Exception)
            {
                string lat, lng;
                AuctionUtilities.getCoordinates(address.Line1, address.City, address.State, address.PostalCode, out lat, out lng); //if USPS fails for some reason, we can still try google...

                if (lat == null || lng == null)
                    return BadRequest();
                else
                    return Ok(address); //if google works, they're good
            }
        }

        [HttpGet]
        [Route("checkUsername/{username}")]
        public ActionResult CheckUsername(string username)
        {
            if (_context.UserModel.Count(u => u.Username == username) > 0)
                return BadRequest();
            else
                return Ok();
        }

        [HttpPost]
        [Route("verifyOrg/{ein}")]
        public ActionResult VerifyOrg([FromRoute] int ein, [FromForm] string name, [FromForm] long phoneNumber, [FromForm] int? currentId, [FromForm] int countryCode, [FromForm] bool isCall = true)
        {
            var sessionId = LoginUtils.getAnonymousId(_context, Request, _accessor, Response);
            var latestRecent = DateTime.UtcNow.AddSeconds(-30); //30 seconds ago
            if (_context.VerificationCodeModel.Where(m => m.TimeCreated > latestRecent).Count() > 0) //make sure at least 30 seconds passes between requests
                return BadRequest("Please wait 30 seconds between requests");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Invalid phone number owner");

            if (phoneNumber.ToString().Length < 10) //invalid phone number
                return BadRequest("Invalid phone number!");

            var org = NonProfitUtils.Select(ein, _context);
            if (org == null)
                return NotFound("The specified EIN was not found in our database");

            //now the org exists, check number-owner validity
            if (!org.Members.Any(m => m.Name == name && m.PhoneNumber == phoneNumber))
                return BadRequest($"Not member-number combo exists with these values in {org.OrganizationName}");

            if (countryCode > 999 || countryCode < 1)
                return BadRequest("Invalid country code");


            var fullNumber = $"+{countryCode}{phoneNumber}";
            VerificationCodeModel verificationModel;
            PhoneUtils.MessageTypes type;
            if (isCall)
                type = PhoneUtils.MessageTypes.Call;
            else
                type = PhoneUtils.MessageTypes.Text;

            //now the number is safe for authentification!
            if (currentId != null)
            {
                verificationModel = _context.VerificationCodeModel.Find(currentId);
                if (verificationModel != null 
                    && verificationModel.Payload == ein.ToString() 
                    && verificationModel.AnonymousClientId == sessionId) //refresh if for same ein and creating user, more secure
                {
                    verificationModel.Refresh(_context);
                    PhoneUtils.Message(fullNumber, $"Your authorization pin for Baskify is: {verificationModel.Secret}, again, your authorization pin for Baskify is {verificationModel.Secret}", type);
                    return Ok(verificationModel.Id); //one already existed, overwrite it!
                }
            }

            //make a new model
             verificationModel = VerificationCodeUtils.CreateVerification(ein.ToString(), VerificationType.EIN, sessionId, null, long.Parse(fullNumber.Where(c => char.IsNumber(c)).ToArray()), _context);
            PhoneUtils.Message(fullNumber, $"Your authorization pin for Baskify is: {verificationModel.Secret}, again, your authorization pin for Baskify is {verificationModel.Secret}", type);

            return Ok(verificationModel.Id);
        }

        [HttpPost]
        [RequestSizeLimit(1073741824)]//1 gig max
        [Route("createAccount")]
        public ActionResult CreateAccount([FromForm] SignUpDto signUpDto)
        {
            IRSNonProfit nonProfit = null;
            bool ValidatedAuthority = false;
            var sessionId = LoginUtils.getAnonymousId(_context, Request, _accessor, Response);
            //check model
            if (signUpDto.EIN != null)
            {
                nonProfit = _context.IRSNonProfit.Include(np => np.Documents).Where(np => np.EIN == signUpDto.EIN).FirstOrDefault(); //loads non profit with docs
                if (nonProfit == null)
                    ModelState.AddModelError("EIN", "Invalid EIN");

                var validationCode = signUpDto.ValidationCode;
                if (signUpDto.VerifyId != null && validationCode.Length == 5) {
                    //check verification code
                    try
                    {
                        ValidatedAuthority = VerificationCodeUtils.VerifyCode(signUpDto.VerifyId.Value, VerificationType.EIN, validationCode, signUpDto.EIN.ToString(), sessionId.ToString(), _context);
                    }
                    catch (Exception e)
                    {
                        ModelState.AddModelError("ValidationCode", e.Message);
                    }
                }
            }

            float lat = 0;
            float lng = 0;
            var addressDict = accountUtils.validateAddress(signUpDto.Address, signUpDto.City, signUpDto.State, signUpDto.ZIP);
            if(addressDict["resultStatus"] != "ADDRESS NOT FOUND")
            {
                signUpDto.Address = addressDict["addressLine1"];
                signUpDto.City = addressDict["city"];
                signUpDto.State = addressDict["state"];
                signUpDto.ZIP = addressDict["zip"];
                lat = float.Parse(addressDict["lat"]);
                lng = float.Parse(addressDict["lng"]);
            }
            else
                ModelState.AddModelError("Address", "Invalid Address");

            if(_context.UserModel.Find(signUpDto.Username) != null)
                ModelState.AddModelError("Username", "Username already taken");

            if(signUpDto.Password != signUpDto.ConfirmPassword)
                ModelState.AddModelError("Password", "Passwords do not match");

            var enableMFA = false;
            //validate phone
            if (signUpDto.MFAValId != null && !string.IsNullOrWhiteSpace(signUpDto.MFASecret)) //try adding phone
            {
                try
                {
                    enableMFA = VerificationCodeUtils.VerifyCode(signUpDto.MFAValId.Value, VerificationType.NewPhone, signUpDto.MFASecret, signUpDto.MFAPhone, sessionId.ToString(), _context, true);
                }
                catch (Exception e) {
                    ModelState.AddModelError("MFASecret", e.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                var dictionary = new Dictionary<string, dynamic>();
                ModelState.Keys
                    .Select(k => new { Input = k, Errors = ModelState[k].Errors })
                    .Where(m => m.Errors.Count > 0)
                    .ToList()
                    .ForEach(m => { dictionary[m.Input] = m.Errors.Select(e => e.ErrorMessage); });

                return BadRequest(new { errors = dictionary });
                /*
                return BadRequest(ModelState.Keys
                    .Select(k => new { Input = k, Errors= ModelState[k].Errors })
                    .Where(m => m.Errors.Count > 0)
                    );
                    */
            }

            //now the address is legit, passwords match, username is available, MFA is valid

            var user = new UserModel() {
                PasswordHash = LoginUtils.hashPassword(signUpDto.Password),
                Address = signUpDto.Address,
                City = signUpDto.City,
                State = signUpDto.State,
                ZIP = signUpDto.ZIP,
                Longitude = lng,
                Latitude = lat,
                Email = signUpDto.Email,
                Username = signUpDto.Username,
                lastLogin = DateTime.UtcNow
            };

            if (enableMFA)
            {
                user.PhoneNumber = new String(signUpDto.MFAPhone.Where(c => char.IsNumber(c)).ToArray()); //remove errant nonnumerical chars
                user.isMFA = true;
            }

            if (signUpDto.Icon != null)
                accountUtils.saveIcon(user, signUpDto.Icon, _env.WebRootPath, 500, 500); //sets icon url

            if (signUpDto.Type == "organization")
            {
                user.UserRole = Roles.COMPANY;
                if (nonProfit != null) {
                    user.OrganizationName = nonProfit.OrganizationName;
                    if (!ValidatedAuthority) //not validated by text
                    {
                        user.Locked = true;
                        user.LockReason = LockReason.OrgPending;
                    }
                }
                else {
                    user.OrganizationName = "PENDING";
                    user.Locked = true;
                    user.LockReason = LockReason.OrgPending;
                }

                user.ContactEmail = signUpDto.Email;
                user.EIN = signUpDto.EIN; //could be null
            }
            else
            {
                user.Locked = true;
                user.LockReason = LockReason.PendingEmail;
                user.UserRole = Roles.USER;
                user.FirstName = signUpDto.FirstName;
                user.LastName = signUpDto.LastName;
            }

            if (ValidatedAuthority)
            {
                var verifModel = _context.VerificationCodeModel.Find(signUpDto.VerifyId);
                if (verifModel != null) //should always be good if validated
                    verifModel.Used = true;
            }

            _context.UserModel.Add(user);
            _context.SaveChanges();

            //upload documents
            if (user.Locked && user.LockReason == LockReason.OrgPending)
            {
                byte[] bytes;
                if (signUpDto.IdFront != null)
                {
                    using (var stream = signUpDto.IdFront.OpenReadStream())
                    {
                        bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, (int)stream.Length);
                        _context.AccountDocumentsModel.Add(new AccountDocumentsModel() { Document = bytes, Type = DocumentType.Identification, UploadDate = DateTime.UtcNow, Username = user.Username , ContentType = signUpDto.IdFront.ContentType });
                    }
                }
                if (signUpDto.IdBack != null)
                {
                    using (var stream = signUpDto.IdBack.OpenReadStream())
                    {
                        bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, (int)stream.Length);
                        _context.AccountDocumentsModel.Add(new AccountDocumentsModel() { Document = bytes, Type = DocumentType.Identification, UploadDate = DateTime.UtcNow, Username = user.Username, ContentType = signUpDto.IdBack.ContentType });
                    }
                }
                if (signUpDto.MembershipForm != null && signUpDto.MembershipForm.Count != 0)
                {
                    signUpDto.MembershipForm.ForEach(f =>
                    {
                        using (var stream = f.OpenReadStream())
                        {
                            bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, (int)stream.Length);
                            _context.AccountDocumentsModel.Add(new AccountDocumentsModel() { Document = bytes, Type = DocumentType.ProofOfAuthority, UploadDate = DateTime.UtcNow, Username = user.Username, ContentType = f.ContentType });
                        }
                    });
                }
                if (signUpDto.ProofOfNonProfit != null && signUpDto.ProofOfNonProfit.Count != 0)
                {
                    signUpDto.ProofOfNonProfit.ForEach(f =>
                    {
                        using (var stream = f.OpenReadStream())
                        {
                            bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, (int)stream.Length);
                            _context.AccountDocumentsModel.Add(new AccountDocumentsModel() { Document = bytes, Type = DocumentType.ProofOfNonProfit, UploadDate = DateTime.UtcNow, Username = user.Username, ContentType = f.ContentType });
                        }
                    });
                }
            }//save nonprofit documents

            _context.SaveChanges();

            //sending verification email to users
            if(user.UserRole != Roles.COMPANY && user.Locked && user.LockReason == LockReason.PendingEmail)
            {
                var emailModel = new EmailVerificationModel()
                {
                    ChangeTime = DateTime.UtcNow,
                    ChangeType = ChangeTypes.VERIFYEMAIL,
                    CommitId = Guid.NewGuid(),
                    Payload = user.Email,
                    Username = user.Username
                };

                _context.EmailVerification.Add(emailModel);
                _context.SaveChanges(); //get id
                
                EmailUtils.sendVerificationEmail(user, emailModel, Request); //send verification email
            }

            return Ok();
        }
    }
}
