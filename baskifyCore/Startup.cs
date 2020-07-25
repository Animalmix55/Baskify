using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AutoMapper;
using baskifyCore.Attributes;
using baskifyCore.Controllers.api;
using baskifyCore.Models;
using baskifyCore.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Stripe;
using Twilio;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace baskifyCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(o => o.AddPolicy("AnyOrigin", builder =>
                builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
            ));

            services.AddControllersWithViews();

            services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            );

            var issuer = "https://www.baskify.com";
            var authority = "https://www.baskify.com";
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.Default.GetBytes("RZTNvUYeGEv36qxkqNqW8uJNQ9KNrkHdDWCMmBHEnKXunajCjbq6L9JdgffQKSf8qTgaSNayshn5qsZMJFCwgP25hHeJugFwQTdBUhtPzMhnHDDpStXwRnjExE9EFq9nY7X4XW4ksujhUuzYkpzhXWYtqYZsSTHFuzutxLH2UJ6jUjSjge9MTSPwwdWRktqSnZaGSFxd3WnA5hNsRswfVYqn4J9uC87eURMfut6n7TuNawLRrXL84GLmCZCuFLr4")),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = issuer,
                    ValidAudience = authority
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin",
                                  policy => policy.Requirements.Add(new AdminRequirement()));
            }); //creates admin policy

            services.AddTransient<IAuthorizationHandler, AdminHandler>(); //adds handler, transient allows for updating context

            services.AddHttpContextAccessor(); //injects ips into app
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
#if !DEBUG
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["LogDir"]))
                Console.SetOut(new System.IO.StreamWriter(ConfigurationManager.AppSettings["LogDir"]));
#endif

            TwilioClient.Init(ConfigurationManager.AppSettings["TwilioAccountSID"], ConfigurationManager.AppSettings["TwilioAuth"]);

            Mapper.Initialize(c => c.AddProfile<MappingProfile>()); //add mapper

            StripeConfiguration.ApiKey = StripeConsts.secretKey;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
