using BaskifyClient.Controllers.api;
using BaskifyClient.Models;
using BaskifyClient.Utilities;
using BaskifyClient.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace BaskifyClient.Controllers
{
    public class AccountController : Controller
    {
        private readonly IWebHostEnvironment _env;
        ApplicationDbContext _context;
        public AccountController(IWebHostEnvironment env)
        {
            _env = env;
            _context = new ApplicationDbContext();
        }
        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        public IActionResult Index()
        {
            var viewModel = new UpdateOrgViewModel()
            {
                Email = OrganizationAccount.ContactEmail,
                OrgName = OrganizationAccount.OrgName,
                Address = OrganizationAccount.Address,
                City = OrganizationAccount.City,
                State = OrganizationAccount.State,
                ZIP = OrganizationAccount.ZIP,
                iconUrl = OrganizationAccount.IconUrl,
                PhoneNumber = OrganizationAccount.OrgPhone.ToString()
            };
            return View(viewModel);
        }

        public IActionResult updateValues(UpdateOrgViewModel model)
        {
            var addressDict = accountUtils.validateAddress(model.Address, model.City, model.State, model.ZIP);
            if (addressDict["resultStatus"] == "ADDRESS NOT FOUND")
                ModelState.AddModelError("Address", "Invalid address");
            else
            {
                model.Address = addressDict["addressLine1"];
                model.City = addressDict["city"];
                model.State = addressDict["state"];
                model.ZIP = addressDict["zip"];
            }

            if (!ModelState.IsValid)
            {
                ViewData["Alert"] = "Could not update account due to some errors...";
                return View("~/Views/Account/Index.cshtml", model);
            }

            OrganizationAccount.Address = model.Address;
            OrganizationAccount.City = model.City;
            OrganizationAccount.State = model.State;
            OrganizationAccount.ZIP = model.ZIP;
            OrganizationAccount.ContactEmail = model.Email;
            OrganizationAccount.OrgPhone = int.Parse(new string(model.PhoneNumber.Where(c => char.IsNumber(c)).ToArray()));
            OrganizationAccount.TaxCode = model.TaxCode;
            OrganizationAccount.StripePrivate = model.StripePrivate;
            OrganizationAccount.StripePublic = model.StripePublic;

            //ICON STUFFS
            if (model.Icon != null) {
                var inputStream = model.Icon.OpenReadStream();
                try
                {
                    var bitmap = new Bitmap(inputStream);
                    bitmap = imageUtilities.ResizePhoto(bitmap, 500, 500); //resize to small square
                                                                           //var extension = Path.GetExtension(file.FileName);
                    var filename = _env.WebRootPath + "/Content/admin-icon.jpeg"; //should be safe characters
                    using (var fs = new FileStream(Path.GetFullPath(filename), FileMode.Create))
                    {
                        bitmap.Save(fs, ImageFormat.Jpeg);
                    }
                    OrganizationAccount.IconUrl = "/Content/admin-icon.jpeg";
                    inputStream.Close();
                }
                catch (Exception)
                {
                    inputStream.Close();
                }
            }
            //END ICON

            ModelState.Clear();

            return View("~/Views/Account/Index.cshtml", model);
        }
    }
}