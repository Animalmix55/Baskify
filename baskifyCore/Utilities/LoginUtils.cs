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
    public static class TokenTypes
    {
        public const int PASSWORDRESET = 1;
        public const int NORMAL = 0;
    }

    public static class LoginUtils
    {
        public static Guid getAnonymousId(ApplicationDbContext _context, HttpRequest request, IHttpContextAccessor _accessor, HttpResponse response)
        {
            AnonymousClientModel model;
            var ip = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            if (request.Cookies["SessionId"] != null)
            {
                Guid guid;
                if (Guid.TryParse(request.Cookies["SessionId"], out guid))
                {
                    if((model = _context.AnonymousClientModel.Find(guid)) != null && model.IPAddress == ip)
                        return guid;
                }
            }

            //look for a model with ip
            model = _context.AnonymousClientModel.Where(m => m.IPAddress == ip).FirstOrDefault();
            if (model == null) //make a new model
            {
                model = new AnonymousClientModel()
                {
                    ClientId = Guid.NewGuid(),
                    Created = DateTime.UtcNow,
                    IPAddress = ip
                };
                _context.AnonymousClientModel.Add(model);
                _context.SaveChanges();
            }

            response.Cookies.Append("SessionId", model.ClientId.ToString()); //add session cookie
            return model.ClientId;
        }

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

            if(user == null)
                throw new InvalidUsernameException();

            _context.Entry(user).Collection(u => u.UserAlerts).Load(); //bring in any alerts as well
            var hash = hashPassword(password);
            if (user.PasswordHash != hash)
            {
                throw new InvalidPasswordException();
            }
            else
            {
                if (user.Locked)
                {
                    throw new UserLockedException(user.LockDetails, user.LockReason.GetValueOrDefault()); //lock reason should never be null if locked
                }

                return user;
            }
        }

        /// <summary>
        /// Embeds the token into the user's cookies and returns the token
        /// </summary>
        /// <param name="user"></param>
        /// <param name="Response"></param>
        /// <param name="_context"></param>
        /// <returns></returns>
        public static string GrantToken(UserModel user, HttpResponse Response, ApplicationDbContext _context)
        {
            var tokenPromise = buildToken(user, _context);

            var cookieOptions = new CookieOptions()
            {//session token for now
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = true
            };

            var token = tokenPromise.Result;
            //add cookie to response
            Response.Cookies.Append("BearerToken", token, cookieOptions);

            return token;
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
            var userNameClaim = decodedToken.Claims.Where(c => c.Type == "nameid").FirstOrDefault();
            if (userNameClaim != null) {
                var userName = userNameClaim.Value;
                var user = _context.UserModel.Find(userName); //should already be in context from user loading
                if (user != null && user.BearerHash == HashToken(token))
                {
                    user.BearerHash = null; //remove hash from server
                    _context.SaveChanges();
                }
            }
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
                var decodedToken = new JwtSecurityToken(token);
                SecurityToken validatedToken;
                IPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                //now check to see if token is in database
                if (!validateAgainstDB)
                    return true;
                var tokenHash = _context.UserModel.Find(validatedToken.Id);
                var userName = decodedToken.Claims.Where(c => c.Type == "nameid").FirstOrDefault().Value;

                var user = _context.UserModel.Find(userName);
                if (user != null && user.BearerHash == HashToken(token)) //make sure it's the same token
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
        /// Hashes the bearer token with a special salt
        /// </summary>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        private static string HashToken(string bearerToken)
        {
            var Salt = "2961065D2512D914A337185A16DEA806D7CC616F829169D9E85D853765886D72"; //just for added security
            var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(bearerToken + Salt));
            StringBuilder Sb = new StringBuilder();
            foreach (var b in hash)
            {
                Sb.Append(b.ToString("x2"));
            }
            return Sb.ToString();
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
                var decodedToken = new JwtSecurityToken(token);
                SecurityToken validatedToken;
                IPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                //now get the token from the DB
                var userName = decodedToken.Claims.Where(c => c.Type == "nameid").FirstOrDefault().Value;

                var user = _context.UserModel.Find(userName);
                if (user != null && user.BearerHash == HashToken(token)) //make sure it's the same token
                    return user;
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

            user.BearerHash = HashToken(returnToken); //add token hash to user

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
        public InvalidPasswordException()
        : base("Invalid password")
        {
        }
    }
    public class InvalidUsernameException : Exception
    {
        public InvalidUsernameException()
        : base("Invalid username")
        {
        }
    }

    public class UserLockedException : Exception
    {
        public UserLockedException(string LockDescription, LockReason _Reason)
        : base(LockDescription)
        {
            Reason = _Reason;
        }

        public LockReason Reason { get; set; }
    }

}
