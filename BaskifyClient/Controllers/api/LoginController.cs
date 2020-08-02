using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaskifyClient.DTOs;
using BaskifyClient.Models;
using BaskifyClient.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BaskifyClient.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        ApplicationDbContext _context;
        private IHttpContextAccessor _accessor;
        public LoginController(IHttpContextAccessor accessor)
        {
            _context = new ApplicationDbContext();
            _accessor = accessor;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index([FromForm] string Username, [FromForm] string Password, [FromForm] string redirectUrl, [FromForm] string Token)
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                return BadRequest("Input fields mustn't be empty");
            try
            {
                if (!CaptchaUtils.verifyToken(Token))
                    return BadRequest("Invalid reCAPTCHA!");

                var user = LoginUtils.getUserAsync(Username, Password, _context).Result; //get user model
                if (user.isMFA)
                {
                    var anonId = LoginUtils.getAnonymousId(_context, Request, _accessor, Response); //never null, used for MFA
                    var MFAModel = VerificationCodeUtils.CreateVerification(user.Username, VerificationType.LoginMFA, anonId, null, long.Parse(user.PhoneNumber), _context);

                    PhoneUtils.SendValidationMessage("+" + user.PhoneNumber, MFAModel.Secret); //sends message

                    var returnModel = new
                    {
                        Status = "MFA Needed",
                        Redirect = redirectUrl != null ? LoginUtils.checkRedirectLocation(redirectUrl, Request) : null,
                        VerificationId = MFAModel.Id,
                        Last4 = user.PhoneNumber.Substring(user.PhoneNumber.Length - 5, 4)
                    };

                    return StatusCode(449, returnModel);
                }

                var token = LoginUtils.GrantToken(user, Response, _context);

                var response = new LoginDto
                {
                    Username = user.Username,
                    Role = user.UserRole == Roles.USER ? "User" : "Organization",
                    Icon = user.iconUrl,
                    DisplayName = user.UserRole == Roles.COMPANY ? user.OrganizationName : $"{user.FirstName} {user.LastName}",
                    Token = token,
                    Redirect = redirectUrl != null ? LoginUtils.checkRedirectLocation(redirectUrl, Request) : null
                };

                return Ok(response);

            }
            catch (AggregateException ex) when (ex.InnerException is InvalidPasswordException e) //invalid password is 401
            {
                return Unauthorized(e.Message);
            }
            catch (AggregateException ex) when (ex.InnerException is InvalidUsernameException e) //invalid user is 404
            {
                return NotFound(e.Message);
            }
            catch (AggregateException ex) when (ex.InnerException is UserLockedException e) //otherwise 400
            {
                var error = "";
                switch (e.Reason)
                {
                    case LockReason.Fraud:
                        error = "This account was locked due to suspected fraudulent activity";
                        break;
                    case LockReason.InvalidDocuments:
                        error = "This account was locked due to insufficient proof of organization status";
                        break;
                    case LockReason.OrgPending:
                        error = "This organization is still pending approval";
                        break;
                    case LockReason.Requested:
                        error = "The account owner requested a lock be placed on their account";
                        break;
                    case LockReason.PendingEmail:
                        error = "Please verify your account using the link emailed to you.";
                        break;
                    case LockReason.StripePending:
                        error = "Please add Stripe to your account using the link provided in your email.";
                        break;
                    case LockReason.Other:
                        error = "Your account is locked. " + e.Message;
                        break;
                    default:
                        error = "Your account has been locked";
                        break;
                }
                return BadRequest(error);
            }
            catch (Exception e) //otherwise 400
            {
                return BadRequest(e.Message);
            }
            
        }
    }
}