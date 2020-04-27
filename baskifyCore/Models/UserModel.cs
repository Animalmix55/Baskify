using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public static class Roles 
    {
        public const int USER = 1;
        public const int COMPANY = 2;
        public const int NONE = 0;
        
    }
    public class UserModel
    {
        public UserModel() 
        {
            //defaults for empty user
             FirstName = String.Empty;
             LastName = String.Empty;
             UserRole = Roles.NONE;
             iconUrl = "/Content/unknownUser.png";
        }
        [StringLength(30)]
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage="Username must only be alphanumerical")] //any number of alphanumericals
        public string Username { get; set; }
        public string PasswordHash { get; set; }

        [Required]
        [Display(Name = "First Name")]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "Name can only contain alphabetical letters, hyphens, and spaces")]
        public string FirstName
        { get; set; }

        [Required]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "Name can only contain alphabetical letters, hyphens, and spaces")]
        public string LastName
        { get; set; }

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
        public int UserRole
        { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string iconUrl { get; set; }
        public DateTime lastLogin { get; set; }


        public ICollection<UserAlertModel> UserAlerts { get; set; }

        //---------------------------------------------------------UNMAPPED PROPERTIES FOR APPLICATION WORKFLOW----------------------------------------------------------
        [NotMapped]
        public string bearerToken { get; set; }

    }
}
