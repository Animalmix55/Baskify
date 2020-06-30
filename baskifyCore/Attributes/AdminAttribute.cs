using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace baskifyCore.Attributes
{
    public class AdminHandler : AuthorizationHandler<AdminRequirement>
    {
        IHttpContextAccessor _httpContextAccessor = null;
        ApplicationDbContext _context;
        public AdminHandler(IHttpContextAccessor httpContextAccessor)
        {
            _context = new ApplicationDbContext();
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Authorizes the admin user with the appropriate secret (on localhost) to access admin controls
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requirement"></param>
        /// <returns></returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;

            var authorization = httpContext.Request.Cookies["Admin"]; //get secret cookie
            var user = LoginUtils.getUserFromToken(httpContext.Request.Cookies["BearerToken"], _context, httpContext.Response);
            if(user == null)
            {
                var authToken = httpContext.Request.Headers["authorization"].ToString();
                if(authToken != null)
                    user = LoginUtils.getUserFromToken(authToken.Replace("Bearer ", string.Empty), _context);
            }
            
            var adminUsername = ConfigurationManager.AppSettings.Get("AdminUser");

            if (user == null || user.Username != adminUsername) //must be declared admin login
            {
                context.Fail();
            }
            else {
                var appPassword = ConfigurationManager.AppSettings.Get("AdminSecret"); //must have secret passcode as cookie
                if (!httpContext.Connection.RemoteIpAddress.Equals(httpContext.Connection.LocalIpAddress) && !IPAddress.IsLoopback(httpContext.Connection.RemoteIpAddress) || !(authorization == appPassword))
                    context.Fail();
                else
                    context.Succeed(requirement);
            }

            return Task.FromResult(0);
        }
    }

    public class AdminRequirement: IAuthorizationRequirement
    {
        //this is a stub! No need for more!
    }
}
