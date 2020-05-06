using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class AuctionModel : IValidatableObject
    {
        public AuctionModel()
        {
            StartTime = DateTime.Now.AddDays(1);
            EndTime = DateTime.Now.AddDays(5);
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuctionId { get; set; }

        [Required]
        [ForeignKey("HostUser")]
        public string HostUsername { get; set; }

        [ForeignKey("HostUsername")]
        public UserModel HostUser { get; set; }

        public List<BasketModel> Baskets { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

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
        [RegularExpression(@"^[-A-Za-z0-9]+(\s[-A-Za-z0-9]+)*$", ErrorMessage = "Title can only contain alphabetical letters, hyphens, and spaces")]
        public string Title { get; set; }

        [Required(AllowEmptyStrings=false, ErrorMessage = "Description cannot be empty")]
        public string Description { get; set; }

        public string BannerImageUrl { get; set; }

        [NotMapped]
        public IFormFile BannerImage { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(StartTime > EndTime)
            {
                yield return new ValidationResult("The auction cannot start after it ends!", new[] { "StartTime" });
            }
            if((EndTime - StartTime).TotalDays > 31)
            {
                yield return new ValidationResult("The auction cannot last longer than a month!", new[] { "EndTime" });
            }
            if ((EndTime - StartTime).TotalHours < 1)
                yield return new ValidationResult("The auction must be at least an hour long!", new[] { "EndTime" });

            var addressDict = accountUtils.validateAddress(Address, City, State, ZIP);
            if (addressDict["resultStatus"] == "ADDRESS NOT FOUND") //now, addresses are validated in the model
                yield return new ValidationResult("Address not found", new[] { "Address" });
            else
            {
                Address = addressDict["addressLine1"];
                City = addressDict["city"];
                State = addressDict["state"];
                ZIP = addressDict["zip"];
            }
        }
    }
}
