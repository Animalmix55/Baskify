using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.DTOs;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc;

namespace baskifyCore.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MFAController : ControllerBase
    {
        ApplicationDbContext _context;
        IHttpContextAccessor _accessor;
        public MFAController(IHttpContextAccessor accessor)
        {
            _context = new ApplicationDbContext();
            _accessor = accessor; //gives ip access
        }

        [HttpPost]
        [Route("create")]
        public ActionResult CreateValidation([FromForm] string payload, [FromForm] VerificationType type, [FromHeader] string authorization, [FromForm] string username, [FromForm] string password)
        {
            if (payload == null)
                return BadRequest("Empty payload");

            //blacklist of types that can be created
            if (type == VerificationType.EnableMFA)
                return BadRequest("Invalid type"); //MFA needs email confirmation, so it cannot be created with this endpoint, only by the email confirmation

            Guid? anonId = null;
            UserModel user = null;

            if (authorization != null) //USE AUTH (BEARER)
            {
                user = LoginUtils.getUserFromToken(authorization.Replace("Bearer", string.Empty), _context);
                if (user == null)
                    return Unauthorized("Invalid login");
            }
            else //USE ANON ID
            {
                anonId = LoginUtils.getAnonymousId(_context, Request, _accessor, Response); //never null
            }

            //now we have a user
            string phoneNum = "";
            if (type == VerificationType.NewPhone) //ensures number is a properly formatted long
            {
                phoneNum = "+" + new string(payload.Where(c => char.IsNumber(c)).ToArray()); //only numbers and +
            }

            var validation = VerificationCodeUtils.CreateVerification(payload, type, anonId, user != null? user.Username : null, 
                long.Parse((type != VerificationType.NewPhone ? user.PhoneNumber : phoneNum).Where(c => char.IsNumber(c)).ToArray()), _context); //gotta make number a long for the model

            //text
            try
            {
                PhoneUtils.Message(type != VerificationType.NewPhone ? "+" + user.PhoneNumber : phoneNum, $"Your baskify verification code is: {validation.Secret}", PhoneUtils.MessageTypes.Text); //sends message
            }
            catch (Exception)
            {
                return BadRequest("Error sending message (invalid phone number?)");
            }
            return Ok(validation.Id);
        }

        [HttpPost]
        [Route("verify/{id}")]
        public ActionResult Verify([FromRoute] int id, [FromForm] string payload, [FromForm] VerificationType type, [FromForm] string secret, [FromHeader] string authorization, [FromForm] string username, [FromForm] string password)
        {
            Guid? anonId = null;
            UserModel user = null;
            if (authorization != null)
            {
                user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);
                if (user == null)
                    return Unauthorized("Invalid login");
            }
            else
            {
                if (!string.IsNullOrEmpty(username))
                    try
                    {
                        user = LoginUtils.getUserAsync(username, password, _context).Result;
                    }
                    catch (Exception) { }
                else
                {
                    //use accessor w/o other auth
                    anonId = LoginUtils.getAnonymousId(_context, Request, _accessor, Response); //never null
                }
            }
            //now we have user OR anon ALWAYS...
            try
            {
                var success = VerificationCodeUtils.VerifyCode(id, type, secret, payload, user != null ? user.Username : anonId.ToString(), _context); //validate

                if(type == VerificationType.LoginMFA && success)
                {
                    user = _context.UserModel.Find(payload); //if successful, an MFA model carries the username as the payload, can only be placed there with the right password
                    var token = LoginUtils.GrantToken(user, Response, _context); //puts token into user cookies

                    var response = new LoginDto
                    {
                        Username = user.Username,
                        Role = user.UserRole == Roles.USER ? "User" : "Organization",
                        Icon = user.iconUrl,
                        DisplayName = user.UserRole == Roles.COMPANY ? user.OrganizationName : $"{user.FirstName} {user.LastName}",
                        Token = token
                    };

                    return Ok(response);
                }//for login
                if(type == VerificationType.EnableMFA && success)
                {
                    var verModel = _context.VerificationCodeModel.Find(id);
                    verModel.Used = true; //mark used so it is never again usable
                    user.PhoneNumber = verModel.PhoneNumber.ToString();
                    user.isMFA = true; //MFA is now enabled for the phone number provided
                    var emailVerif = _context.EmailVerification.Where(v => v.ChangeType == ChangeTypes.MFA && v.Payload == verModel.PhoneNumber.ToString()).OrderByDescending(v => v.ChangeTime).FirstOrDefault(); //retrieves the most recent email MFA ver with this payload (should only ever be one MFA ver at a time)
                    emailVerif.Committed = true; //marks the verification as committed
                    _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception e) //not valid
            {
                return BadRequest(e.Message);
            }
        }
    }
}