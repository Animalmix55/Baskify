using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using baskifyCore.DTOs;
using baskifyCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace baskifyCore.Controllers.api.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountDetailsController : ControllerBase
    {
        ApplicationDbContext _context;
        public AccountDetailsController()
        {
            _context = new ApplicationDbContext();
        }

        [Authorize(Policy = "Admin")]
        [HttpPost]
        public ActionResult Index([FromBody] IncomingSearchDto search)
        {
            var start = search.start;
            var length = search.length;

            var totalNumber = _context.UserModel.Count();

            var users =!string.IsNullOrWhiteSpace(search.search.value)? _context.UserModel.Where(u => DbFunctions.Like(u.Username, "%" + search.search.value + "%") || DbFunctions.Like(u.Email, "%" + search.search.value + "%") || DbFunctions.Like((u.FirstName + " " + u.LastName), "%" + search.search.value + "%") || DbFunctions.Like(u.OrganizationName, "%" + search.search.value + "%")).AsQueryable() : _context.UserModel;

            var filteredNumber = users.Count();
            var orderUsers = users.OrderByDescending(u => u.Username);

            foreach(var column in search.order)
            {
                switch (search.columns[column.column].name)
                {
                    case "Username":
                        orderUsers = column.dir == "asc" ? orderUsers.OrderBy(u => u.Username) : orderUsers.OrderByDescending(u => u.Username);
                        break;
                    case "Email":
                        orderUsers = column.dir == "asc" ? orderUsers.OrderBy(u => u.Email) : orderUsers.OrderByDescending(u => u.Email);
                        break;
                    case "UserType":
                        orderUsers = column.dir == "asc" ? orderUsers.OrderBy(u => u.UserRole) : orderUsers.OrderByDescending(u => u.UserRole);
                        break;
                    case "DisplayName":
                        orderUsers = column.dir == "asc" ? orderUsers.OrderBy(u => u.FirstName == null? u.OrganizationName : u.FirstName + " " + u.LastName) : orderUsers.OrderByDescending(u => u.FirstName == null ? u.OrganizationName : u.FirstName + " " + u.LastName);
                        break;
                    case "CreationDate":
                        orderUsers = column.dir == "asc" ? orderUsers.OrderBy(u => u.CreationDate) : orderUsers.OrderByDescending(u => u.CreationDate);
                        break;
                    case "LastLogin":
                        orderUsers = column.dir == "asc" ? orderUsers.OrderBy(u => u.lastLogin) : orderUsers.OrderByDescending(u => u.lastLogin);
                        break;
                    case "Locked":
                        orderUsers = column.dir == "asc" ? orderUsers.OrderBy(u => u.Locked) : orderUsers.OrderByDescending(u => u.Locked);
                        break;
                    case "PendingOrg":
                        orderUsers = column.dir == "asc" ? orderUsers.OrderBy(u => u.Locked && u.LockReason == LockReason.OrgPending) : orderUsers.OrderByDescending(u => u.Locked && u.LockReason == LockReason.OrgPending);
                        break;
                }
            }


            var returnObject = new
            {
                draw = search.draw,
                recordsTotal = totalNumber,
                recordsFiltered = filteredNumber,
                data = orderUsers.ToList().Where((value, index) => index >= start && index < start + length).ToList()
            };

            return Ok(returnObject);
        }
    }
}