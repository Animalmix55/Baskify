using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.ViewModels
{
    public class UpdateOrgViewModel
    {
        [Required(AllowEmptyStrings = false)]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "Organization name can only contain alphabetical letters, hyphens, and spaces")]
        public string OrgName
        {
            get; set;
        }

        [Required(AllowEmptyStrings = false)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]+(\s[-\.A-Za-z0-9]+)*$", ErrorMessage = "Invalid Address string")]
        public string Address { get; set; }

        [Required]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$")]
        public string City { get; set; }

        [Required]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$")]
        public string State { get; set; }

        [Required]
        [RegularExpression(@"^([0-9]{5}\-[0-9]{4})|([0-9]{5})$", ErrorMessage = "ZIPs should be in the format ##### or #####-####)")]
        public string ZIP { get; set; }

        [Required]
        public string iconUrl { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string TaxCode
        {
            get; set;
        }

        [RegularExpression(@"^sk\_live\_", ErrorMessage = "Invalid private key format")]
        public string StripePrivate { get; set; }

        [RegularExpression(@"^pk\_live\_", ErrorMessage = "Invalid public key format")]
        public string StripePublic { get; set; }

        public IFormFile Icon { get; set; }
    }
}
