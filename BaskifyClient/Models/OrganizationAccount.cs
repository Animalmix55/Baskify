using Stripe;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace BaskifyClient.Models
{
    public static class OrganizationAccount
    {
        public static string OrgName {
            get { return OrgName; }
            set
            {
                ConfigurationManager.AppSettings["OrgName"] = value;
                OrgName = value;
            }
        }
        public static int? OrgPhone
        {
            get { return OrgPhone; }
            set
            {
                ConfigurationManager.AppSettings["OrgPhone"] = value.ToString();
                OrgPhone = value;
            }
        }
        public static string Address
        {
            get { return Address; }
            set
            {
                ConfigurationManager.AppSettings["Address"] = value;
                Address = value;
            }
        }
        public static string City
        {
            get { return City; }
            set
            {
                ConfigurationManager.AppSettings["City"] = value;
                City = value;
            }
        }
        public static string State
        {
            get { return State; }
            set
            {
                ConfigurationManager.AppSettings["State"] = value;
                State = value;
            }
        }
        public static string ZIP
        {
            get { return ZIP; }
            set
            {
                ConfigurationManager.AppSettings["ZIP"] = value;
                ZIP = value;
            }
        }
        public static string TaxCode
        {
            get { return TaxCode; }
            set
            {
                ConfigurationManager.AppSettings["TaxCode"] = value;
                TaxCode = value;
            }
        }
        public static string ContactEmail
        {
            get { return ContactEmail; }
            set
            {
                ConfigurationManager.AppSettings["ContactEmail"] = value;
                ContactEmail = value;
            }
        }

        public static string IconUrl {
            get { return IconUrl; }
            set
            {
                ConfigurationManager.AppSettings["IconUrl"] = value;
                IconUrl = value;
            } 
        }
        public static string StripePublic
        {
            get { return StripePublic; }
            set
            {
                ConfigurationManager.AppSettings["StripePublic"] = value;
                StripePublic = value;
            }
        }

        public static string StripePrivate
        {
            get { return ConfigurationManager.AppSettings["StripePrivate"]; }
            set
            {
                ConfigurationManager.AppSettings["StripePrivate"] = value;
                StripeConfiguration.ApiKey = value;
            }
        }


        static OrganizationAccount()
        {
            OrgName = ConfigurationManager.AppSettings["OrgName"];
            int phone;
            if (int.TryParse(ConfigurationManager.AppSettings["OrgPhone"], out phone))
                OrgPhone = phone;
            Address = ConfigurationManager.AppSettings["Address"];
            City = ConfigurationManager.AppSettings["City"];
            State = ConfigurationManager.AppSettings["State"];
            ZIP = ConfigurationManager.AppSettings["ZIP"];
            TaxCode = ConfigurationManager.AppSettings["TaxCode"];
            ContactEmail = ConfigurationManager.AppSettings["ContactEmail"];
        }
    }
}
