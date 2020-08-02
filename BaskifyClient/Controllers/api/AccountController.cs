using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Helpers;
using AutoMapper;
using BaskifyClient.DTOs;
using BaskifyClient.Models;
using BaskifyClient.Utilities;
using BaskifyClient.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace BaskifyClient.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        ApplicationDbContext _context;
        IWebHostEnvironment _env;
        IHttpContextAccessor _accessor;
        public AccountController(IWebHostEnvironment env, IHttpContextAccessor accessor)
        {
            _context = new ApplicationDbContext();
            _env = env;
            _accessor = accessor;
        }

        [HttpPost]
        public IActionResult updateValues([FromForm] UserModel user,[FromForm] IFormFile Icon, [FromHeader] string authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                return Unauthorized("Bad authorization");

            var oldUser = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);

            //format user phone number correctly...
            user.PhoneNumber = user.PhoneNumber != null ? new string(user.PhoneNumber.Where(c => char.IsNumber(c)).ToArray()) : null; //removes errant chars, only numbers

            if (oldUser != null)
            {

                if (oldUser.Email != user.Email)//dont allow the email to be changed more than once in a ten-day range
                {
                    var lastEmailChange = _context.EmailVerification.Where(ev => ev.Username == oldUser.Username).Where(ev => ev.ChangeType == ChangeTypes.EMAIL).OrderByDescending(ev => ev.ChangeTime).FirstOrDefault();

                    if (lastEmailChange != null && lastEmailChange.ChangeTime > DateTime.UtcNow.AddDays(-10))
                        ModelState.AddModelError("Email", "Email cannot be changed more than once in 10 days!");
                }

                if (oldUser.isMFA != user.isMFA || oldUser.PhoneNumber != user.PhoneNumber) //don't allow hackers to change email to change MFA until 10 days have passed
                {
                    var lastEmailChange = _context.EmailVerification.Where(ev => ev.Username == oldUser.Username).Where(ev => ev.ChangeType == ChangeTypes.EMAIL).OrderByDescending(ev => ev.ChangeTime).FirstOrDefault();

                    if (lastEmailChange != null && lastEmailChange.ChangeTime > DateTime.UtcNow.AddDays(-10))
                        ModelState.AddModelError("PhoneNumber", "MFA settings cannot be tweaked within 10 days of an email change");
                }

                if (!ModelState.IsValid)
                {
                    ViewData["NavBarOverride"] = oldUser; //this allows the navbar to render correctly
                    ViewData["Alert"] = "The change could not be completed due to a formatting issue.";
                    return BadRequest(ModelState);
                }
                if (Icon != null)
                    if (!accountUtils.saveIcon(user, Icon, _env.WebRootPath, 500, 500)) //this will add the file url to the new user object as well
                        BadRequest("Image upload failed, try another?");

                string updateMessages = "";
                try
                {
                    updateMessages = accountUtils.updateUser(oldUser, user, _context, Request, ModelState, this, _env);
                }
                catch (Exception)//only the email change throws error
                {
                    return BadRequest("Verification Emails Failed to Send, Please Try Again Later.");
                }
                _context.SaveChanges(); //save olduser changes

                return Ok("Account updated!");
            }
            else
            {
                return Unauthorized("Login not valid");
            }

        }


        [HttpPost]
        public IActionResult updatePassword([FromForm] ChangePasswordViewModel model, [FromHeader] string authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                return Unauthorized("Bad authorization");

            var user = LoginUtils.getUserFromToken(authorization.Replace("Bearer ", string.Empty), _context);

            model.reset = LoginUtils.isValidPasswordResetToken(authorization.Replace("Bearer ", string.Empty));
            if (user == null)
            {
                return Unauthorized("Bad authorization code");
            }
            else
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                else if (LoginUtils.hashPassword(model.CurrentPassword) == user.PasswordHash)//password matches
                {
                    if (accountUtils.setPassword(user, model.NewPassword, _context))
                    {
                        return Ok("Password Changed Successfully");
                    }
                    else //something botches password change
                        return BadRequest("An unknown error occured");
                }
                else if (model.reset)
                {
                    if (accountUtils.setPassword(user, model.NewPassword, _context))
                    {
                        LoginUtils.destroyToken(Request.Cookies["BearerToken"], _context, Response); //destroys reset token in DB and cookie
                        return Ok();
                    }
                    else //something botches password change
                        return BadRequest("An unknown error occured");
                }
                else //password doesn't match
                    ModelState.AddModelError("CurrentPassword", "Current password does not match");

                return Ok();
            }
        }

        [HttpPost]
        public IActionResult sendRecoveryEmail([FromForm] ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var sent = accountUtils.sendRecoveryEmail(model, _context, Request, this, _env);
                if (sent)
                    return Ok();
                else
                {
                    return NotFound();
                }
            }
            catch (Exception) //something went wrong
            {
                return BadRequest("Error sending recovery email");
            }
        }
    }
}