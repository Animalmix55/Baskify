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
using Microsoft.AspNetCore.Http.Extensions;

namespace baskifyCore.Utilities
{
    public static class LoginUtils
    {
        /// <summary>
        /// Looks at claims to see if this is a valid reset token... DOES NOT CHECK AGAINST DB, MUST DO THAT BEFOREHAND
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool isValidPasswordResetToken(string token)
        {
            if (token == null)
                return false;
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetValidationParameters();

            try
            {
                SecurityToken validatedToken;
                IPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken); //still check to make sure it's a valid token
                var jwtToken = new JwtSecurityToken(token);
                var claims = jwtToken.Claims;
                foreach(var claim in claims)
                {
                    if (claim.Type == "typ" && claim.Value == TokenTypes.PASSWORDRESET.ToString())
                        return true; //if any type is isResetToken
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Gets a logged in user if they exist, otherwise throws an exception. Automatically builds a new bearer token into the UserModel.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<UserModel> getUserAsync(string username, string password, ApplicationDbContext _context)
        {
            var user = _context.UserModel.Find(username);
            _context.Entry(user).Collection("UserAlerts").Load(); //bring in any alerts as well
            var hash = hashPassword(password);
            if (user.PasswordHash != hash)
            {
                throw new InvalidPasswordException("Invalid password");
            }
            else
            {
                user.bearerToken = await buildToken(user, _context); //give the user a token
                return user;
            }
        }

        public static string hashPassword(string password)
        {
            if (password == null)
                return string.Empty;
            var hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder Sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                Sb.Append(b.ToString("x2"));
            }
            return Sb.ToString();
        }

        /// <summary>
        /// Takes the given token and wipes it from the cookies and database.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="_context"></param>
        /// <param name="Response"></param>
        public static void destroyToken(string token, ApplicationDbContext _context, HttpResponse Response=null)
        {
            if(Response != null)
                deleteTokenFromCookie(Response);
            var decodedToken = new JwtSecurityToken(token);
            var tokenEntry = _context.BearerTokenModel.Find(decodedToken.Id); //should already be in context from user loading
            _context.BearerTokenModel.Remove(tokenEntry);
            _context.SaveChanges();
        }

        /// <summary>
        /// Ensures that a given token string is valid, optionally makes sure it was actually issued by the server.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="validateAgainstDB"></param>
        /// <returns></returns>
        public static bool CheckToken(string token, ApplicationDbContext _context, bool validateAgainstDB = true)
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
        public static UserModel getUserFromToken(string token, ApplicationDbContext _context, HttpResponse response=null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetValidationParameters();

            try
            {
                SecurityToken validatedToken;
                IPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                //now get the token from the DB
                var bearerTokenModel = _context.BearerTokenModel.Include("UserModel.UserAlerts").Where(b => b.TokenIdentifier == validatedToken.Id).FirstOrDefault();
                if (bearerTokenModel != null && bearerTokenModel.Token == token) //make sure it's the same token
                    return bearerTokenModel.UserModel;
                else
                {
                    if (response != null)
                        deleteTokenFromCookie(response);
                    return null; //returns null if there's an issue or the token isn't valid
                }
            }
            catch (Exception e)
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
        /// Each time a password reset is called, all tokens for that user will be destroyed. 
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<string> buildToken(UserModel user, ApplicationDbContext _context, int tokenType=TokenTypes.NORMAL)
        {
            //Check to see if the user already has a valid token...
            BearerTokenModel bearerToken;
            if (tokenType == TokenTypes.NORMAL && (bearerToken = _context.BearerTokenModel
                .Where(b => b.Username == user.Username)
                .Where(b => b.Type != TokenTypes.PASSWORDRESET) //make sure a user doesn't load in a reset token
                .OrderByDescending(b => b.TimeWritten)
                .FirstOrDefault()) != null)
            {
                if (CheckToken(bearerToken.Token, _context, false))
                    return bearerToken.Token;
            }

            if(tokenType == TokenTypes.PASSWORDRESET)
                _context.BearerTokenModel.RemoveRange(_context.BearerTokenModel
                    .Where(b => b.Username == user.Username)); //removes ALL TOKENS from the user, kicks off other sessions and keeps things tidy

            var currentTime = DateTime.UtcNow;
            DateTime expiry;
            switch (tokenType)
            {
                case TokenTypes.PASSWORDRESET:
                    expiry = currentTime.AddMinutes(10); //reset lasts 10 minutes
                    break;
                default:
                    expiry = currentTime.AddDays(1);
                    break;
            }
            
            //256-bit encryption key
            var encryptionPrivate = "RZTNvUYeGEv36qxkqNqW8uJNQ9KNrkHdDWCMmBHEnKXunajCjbq6L9JdgffQKSf8qTgaSNayshn5qsZMJFCwgP25hHeJugFwQTdBUhtPzMhnHDDpStXwRnjExE9EFq9nY7X4XW4ksujhUuzYkpzhXWYtqYZsSTHFuzutxLH2UJ6jUjSjge9MTSPwwdWRktqSnZaGSFxd3WnA5hNsRswfVYqn4J9uC87eURMfut6n7TuNawLRrXL84GLmCZCuFLr4";
            var issuer = "https://www.baskify.com";
            var authority = "https://www.baskify.com";
            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = await CreateClaimsIdentitiesAsync(user);
            claims.AddClaim(new Claim(JwtRegisteredClaimNames.Typ, tokenType.ToString()));//add type to typ
            var token = tokenHandler.CreateJwtSecurityToken(issuer: issuer,
            audience: authority,
            subject: claims,
            notBefore: currentTime,
            expires: expiry, //valid for 1 day at a time
            signingCredentials:
            new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.Default.GetBytes(encryptionPrivate)),
                    SecurityAlgorithms.HmacSha256Signature));
            var id = token.Id;
            //send the token to the server for safekeeping
            var returnToken = tokenHandler.WriteToken(token);
            bearerToken = new BearerTokenModel { Type = tokenType, TokenIdentifier = id, TimeExpire = expiry, TimeWritten = currentTime, UserModel = user, Username = user.Username, Token = returnToken  };
            _context.BearerTokenModel.Add(bearerToken);
            _context.SaveChanges();
            return returnToken;
        }

        public static async Task<ClaimsIdentity> CreateClaimsIdentitiesAsync(UserModel user)
        {
            ClaimsIdentity claimsIdentity = new ClaimsIdentity();
            //Create a unique JTI by hashing username with time
            StringBuilder Sb = new StringBuilder();
            var hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(user.Username + DateTime.UtcNow));
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
                    role = "Organization";
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
            CookieOptions options = new CookieOptions() { Expires = DateTime.UtcNow.AddDays(-1) };
            response.Cookies.Append("BearerToken", "", options); //removes token
        }
        
        /// <summary>
        /// Returns the Url if it's safe, otherwise returns the root url of the request.
        /// </summary>
        /// <param name="redirectUrl"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string checkRedirectLocation(string redirectUrl, HttpRequest request)
        {
            var uri = new Uri(request.GetDisplayUrl());
            var rootUrl = uri.Scheme + @"://" + uri.Authority;
            if (redirectUrl == null || !redirectUrl.StartsWith(rootUrl)) //to avoid open redirect, route them back to home...
                return rootUrl;
            else
                return redirectUrl;
        }

        /// <summary>
        /// Gets a relative URL like /account and translates it into an absolute url.
        /// </summary>
        /// <param name="relUrl"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string getAbsoluteUrl(string relUrl, HttpRequest request)
        {
            var reqUri = new Uri(request.GetDisplayUrl());
            var absUriBuilder = new UriBuilder();
            relUrl = relUrl.Trim('/');

            absUriBuilder.Scheme = reqUri.Scheme;
            absUriBuilder.Host = reqUri.Host;
            absUriBuilder.Port = reqUri.Port;
            if (relUrl.Contains('?'))
            {
                absUriBuilder.Path = relUrl.Split('?')[0];
                absUriBuilder.Query = relUrl.Split('?')[1]; //now copies query strings
            }
            else
            {
                absUriBuilder.Path = relUrl;
            }
            

            return absUriBuilder.Uri.ToString();

        }
    }
    public class InvalidPasswordException : Exception
    {
        public InvalidPasswordException(string name)
        : base(String.Format("Invalid Student Name: {0}", name))
        {
        }
    }
    
}
