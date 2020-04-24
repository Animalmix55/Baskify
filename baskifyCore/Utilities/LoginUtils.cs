using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using baskifyCore.Models;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Tokens;
using System.Security.Principal;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http;

namespace baskifyCore.Utilities
{
    public static class LoginUtils
    {
        private static ApplicationDbContext _context;
        static LoginUtils() //construct the context only once
        {
            _context = new ApplicationDbContext();
        }
        /// <summary>
        /// Gets a logged in user if they exist, otherwise throws an exception. Automatically builds a new bearer token into the UserModel.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<UserModel> getUserAsync(string username, string password)
        {
            var user = _context.UserModel.Find(username);
            var hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder Sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                Sb.Append(b.ToString("x2"));
            }
            var hash = Sb.ToString();
            if (user.PasswordHash != hash)
            {
                throw new Exception("Invalid password");
            }
            else
            {
                user.bearerToken = await buildToken(user); //give the user a token
                return user;
            }
        }

        /// <summary>
        /// Ensures that a given token string is valid, optionally makes sure it was actually issued by the server.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="validateAgainstDB"></param>
        /// <returns></returns>
        public static bool CheckToken(string token, bool validateAgainstDB = true)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetValidationParameters();

            try
            {
                SecurityToken validatedToken;
                IPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                //now check to see if token is in database
                if (!validateAgainstDB)
                    return true;
                var bearerTokenModel = _context.BearerTokenModel.Find(validatedToken.Id);
                if (bearerTokenModel != null && bearerTokenModel.Token == token) //make sure it's the same token
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

        }

        /// <summary>
        /// Gets the UserModel for a user from a given token. Returns null if there's an issue.
        /// If given a response object, will assume it's getting a cookie and delete the cookie if there's an issue.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static UserModel getUserFromToken(string token, HttpResponse response=null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetValidationParameters();

            try
            {
                SecurityToken validatedToken;
                IPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                //now get the token from the DB
                var bearerTokenModel = _context.BearerTokenModel.Include("UserModel").Where(b => b.TokenIdentifier == validatedToken.Id).FirstOrDefault();
                if (bearerTokenModel != null && bearerTokenModel.Token == token) //make sure it's the same token
                    return bearerTokenModel.UserModel;
                else
                {
                    if (response != null)
                        deleteTokenFromCookie(response);
                    return null; //returns null if there's an issue or the token isn't valid
                }
            }
            catch (Exception)
            {
                if (response != null)
                    deleteTokenFromCookie(response);
                return null;
            }

        }

        private static TokenValidationParameters GetValidationParameters()
        {
            return new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidateIssuer = true,   
                ValidIssuer = "https://www.baskify.com",
                ValidAudience = "https://www.baskify.com",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.Default.GetBytes("RZTNvUYeGEv36qxkqNqW8uJNQ9KNrkHdDWCMmBHEnKXunajCjbq6L9JdgffQKSf8qTgaSNayshn5qsZMJFCwgP25hHeJugFwQTdBUhtPzMhnHDDpStXwRnjExE9EFq9nY7X4XW4ksujhUuzYkpzhXWYtqYZsSTHFuzutxLH2UJ6jUjSjge9MTSPwwdWRktqSnZaGSFxd3WnA5hNsRswfVYqn4J9uC87eURMfut6n7TuNawLRrXL84GLmCZCuFLr4")) // The same key as the one that generate the token
            };
        }

        /// <summary>
        /// Gets a new token if the most recent token for that user on the database has expired. Otherwise just uses that token.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<string> buildToken(UserModel user)
        {
            //Check to see if the user already has a valid token...
            BearerTokenModel bearerToken;
            if ((bearerToken = _context.BearerTokenModel.Where(b => b.Username == user.Username).OrderByDescending(b => b.TimeWritten).FirstOrDefault()) != null)
            {
                if (CheckToken(bearerToken.Token, false))
                    return bearerToken.Token;
            }

            var numDaysValid = 1;
            //256-bit encryption key
            var encryptionPrivate = "RZTNvUYeGEv36qxkqNqW8uJNQ9KNrkHdDWCMmBHEnKXunajCjbq6L9JdgffQKSf8qTgaSNayshn5qsZMJFCwgP25hHeJugFwQTdBUhtPzMhnHDDpStXwRnjExE9EFq9nY7X4XW4ksujhUuzYkpzhXWYtqYZsSTHFuzutxLH2UJ6jUjSjge9MTSPwwdWRktqSnZaGSFxd3WnA5hNsRswfVYqn4J9uC87eURMfut6n7TuNawLRrXL84GLmCZCuFLr4";
            var issuer = "https://www.baskify.com";
            var authority = "https://www.baskify.com";
            var currentTime = DateTime.UtcNow;
            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = await CreateClaimsIdentitiesAsync(user);
            var token = tokenHandler.CreateJwtSecurityToken(issuer: issuer,
            audience: authority,
            subject: claims,
            notBefore: currentTime,
            expires: currentTime.AddDays(numDaysValid), //valid for 1 day at a time
            signingCredentials:
            new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.Default.GetBytes(encryptionPrivate)),
                    SecurityAlgorithms.HmacSha256Signature));
            var id = token.Id;
            //send the token to the server for safekeeping
            var returnToken = tokenHandler.WriteToken(token);
            bearerToken = new BearerTokenModel { TokenIdentifier = id, TimeExpire = currentTime.AddDays(numDaysValid), TimeWritten = currentTime, UserModel = user, Username = user.Username, Token = returnToken  };
            _context.BearerTokenModel.Add(bearerToken);
            _context.SaveChanges();
            return returnToken;
        }

        public static async Task<ClaimsIdentity> CreateClaimsIdentitiesAsync(UserModel user)
        {
            ClaimsIdentity claimsIdentity = new ClaimsIdentity();
            //Create a unique JTI by hashing username with time
            StringBuilder Sb = new StringBuilder();
            var hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(user.Username + DateTime.Now));
            foreach (var b in hashBytes)
            {
                Sb.Append(b.ToString("x2"));
            }
            var hash = Sb.ToString();

            claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, hash));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Username));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"));
            string role = null;
            switch (user.UserRole)
            {
                case Roles.COMPANY:
                    role = "Company";
                    break;
                case Roles.USER:
                    role = "User";
                    break;
            }
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));

            return await Task.FromResult(claimsIdentity);
        }
        /*
        protected static string GetIp(HttpContext context)
        {
            return context.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress.ToString();
        }
        */

        public static void deleteTokenFromCookie(HttpResponse response)
        {
            CookieOptions options = new CookieOptions() { Expires = DateTime.Now.AddDays(-1) };
            response.Cookies.Append("BearerToken", "", options); //removes token
        }
    }
    
}
