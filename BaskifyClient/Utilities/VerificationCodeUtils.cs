using BaskifyClient.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.Utilities
{
    public static class VerificationCodeUtils
    {
        private static string newVerificationCode()
        {
            return Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
        }

        /// <summary>
        /// returns a TRACKED verification model DOES NOT SEND THE VERIFICATION CODE TO THE PROVIDED PHONE NUMBER
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="Type"></param>
        /// <param name="_context"></param>
        /// <returns></returns>
        public static VerificationCodeModel CreateVerification(string payload, VerificationType Type, Guid? anonUserId, string username, long PhoneNum, ApplicationDbContext _context)
        {
            //remove old MFA requests OF THAT TYPE for that user
            _context.VerificationCodeModel.RemoveRange(_context.VerificationCodeModel
                .Where(m => m.VerificationType == Type)
                .Where(m => m.Username == username || m.AnonymousClientId == anonUserId)); //only one at a time

            var verificationModel = new VerificationCodeModel()
            {
                Payload = payload,
                TimeCreated = DateTime.UtcNow,
                VerificationType = Type,
                Secret = newVerificationCode(), //always uppercase
                AnonymousClientId = anonUserId,
                Username = username,
                PhoneNumber = PhoneNum
            };

            _context.VerificationCodeModel.Add(verificationModel);
            _context.SaveChanges();

            return verificationModel;
        }

        /// <summary>
        /// Verifies the code and payload inputted against the db, throws an error if the inputs are invalid.
        /// Returns true if the verification model is now a valid grant.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="secret"></param>
        /// <param name="payload"></param>
        /// <param name="_context"></param>
        /// <param name="UseCode">Marks the verification as complete and unusable again</param>
        /// <param name="creatorId">The username/anon id of the creator</param>
        /// <returns></returns>
        public static bool VerifyCode(int id, VerificationType type, string secret, string payload, string creatorId, ApplicationDbContext _context, bool UseCode = false)
        {
            var model = _context.VerificationCodeModel.Find(id);
            if (model == null)
                throw new Exception("Verification request not found");

            if (model.VerificationType != type) //ensure types match
                throw new Exception("Invalid verification type");

            if (model.AnonymousClientId.ToString() != creatorId && model.Username != creatorId) //make sure the user that created it is requesting validation
                throw new Exception("You did not request this validation");

            //the model is less than 15 minutes old
            if (secret.ToLower() != model.Secret.ToLower())
                throw new Exception("Invalid verification code");

            if (model.Payload != null && model.Payload.ToLower() != payload.ToLower()) //tolerate null payloads for MFA 
            { //payload does not match, use changed EIN, etc. and tried to use same code
                switch (model.VerificationType)
                {
                    case VerificationType.EIN:
                        throw new Exception("The selected company has changed since the request was made");
                    case VerificationType.NewPhone:
                        throw new Exception("The phone number has changed since the request was made");
                    default:
                        throw new Exception("The selected payload has changed since the request was made");
                }
            }

            if (model.Used)
                throw new Exception("This code was already used");

            //all matches now! Validate the model...
            if (UseCode)
                model.Used = true;

            if (model.Validated) //if the code has already been checked once
            {
                if (model.ValidatedOn?.AddMinutes(30) > DateTime.UtcNow) //if it's been validated once, it's valid for another 30 mins
                    return true;
                else
                {
                    _context.VerificationCodeModel.Remove(model);
                    _context.SaveChanges(); //DELETE THE OUTDATED MODEL
                    throw new Exception("This code was validated too long ago");
                }
            }
            else //first time it's been checked
            {
                if (model.TimeCreated.AddMinutes(15) < DateTime.UtcNow) //if it's over 15 minutes old or used
                {
                    _context.VerificationCodeModel.Remove(model);
                    _context.SaveChanges(); //DELETE THE OUTDATED MODEL
                    throw new Exception("Expired code");
                }

                model.Validated = true;
                model.ValidatedOn = DateTime.UtcNow;
                _context.SaveChanges(); //model is now a valid grant
                return true;
            }
        }

        /// <summary>
        /// Refreshes the verification model's creation time and nulls current validation, generates a new secret.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="payload"></param>
        /// <param name="_context"></param>
        /// <returns>The overwritten model's id</returns>
        public static int Refresh(this VerificationCodeModel model, ApplicationDbContext _context)
        {
            model.TimeCreated = DateTime.UtcNow;
            model.Validated = false;
            model.ValidatedOn = null;
            model.Secret = newVerificationCode();
            model.Used = false;
            //has the same type

            _context.SaveChanges();

            return model.Id;
        }

        public static void SetMFA(UserModel user, bool enabled, long? NewPhoneNum, ApplicationDbContext _context, HttpRequest Request, IWebHostEnvironment _env)
        {
            if (enabled && !user.isMFA) //we are turning on MFA
            {
                var emailVerification = new EmailVerificationModel()
                {
                    CommitId = Guid.NewGuid(),
                    CanRevert = false,
                    ChangeTime = DateTime.UtcNow,
                    ChangeType = ChangeTypes.MFA,
                    Payload = NewPhoneNum?.ToString(), //payload is verification id
                    Username = user.Username,
                };
                _context.EmailVerification.RemoveRange(_context.EmailVerification.Where(v => v.ChangeType == ChangeTypes.MFA)); //only one MFA change type at a time
                _context.EmailVerification.Add(emailVerification);
                _context.SaveChanges(); //updates change id

                //DONT SEND MESSAGE UNTIL LINK CLICKED

                EmailUtils.sendVerificationEmail(user, emailVerification, Request, _env);
            }
            else //disable MFA
            {
                var emailVerification = new EmailVerificationModel()
                {
                    CommitId = Guid.NewGuid(),
                    CanRevert = false,
                    ChangeTime = DateTime.UtcNow,
                    ChangeType = ChangeTypes.MFA,
                    Payload = null, //payload is null for disabling MFA
                    Username = user.Username,
                };

                _context.EmailVerification.Add(emailVerification);
                _context.SaveChanges(); //updates change id

                EmailUtils.sendVerificationEmail(user, emailVerification, Request, _env);
            }
        }

        /// <summary>
        /// Verifies that the email verification has been satisfied with the given GUID and disables a user's MFA
        /// </summary>
        /// <param name="VerifyId"></param>
        /// <param name="verification"></param>
        /// <param name="user"></param>
        /// <param name="_context"></param>
        /// <returns></returns>
        public static string VerifyDisable(Guid VerifyId, EmailVerificationModel verification, UserModel user, ApplicationDbContext _context)
        {
            if(VerifyId == verification.CommitId)
            {
                user.isMFA = false; //disable MFA
                return "MFA has successfully been disabled, you can reenable any time in your account settings";
            }
            else
            {
                return "Failed to disable MFA due to a mysterious error";
            }
        }
    }
}
