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
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName
        { get; set; }

        public string Email { get; set; }
        public string LastName
        { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZIP { get; set; }
        
        public int UserRole
        { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }
        public string iconUrl { get; set; }
        public DateTime lastLogin { get; set; }

        [NotMapped]
        public string bearerToken { get; set; }

    }
}
