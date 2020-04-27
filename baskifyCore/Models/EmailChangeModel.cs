using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class EmailChangeModel
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        [Key]
        public int EmailChangeId { get; set; }

        [Required]
        [ForeignKey("UserModel")]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel UserModel { get; set; }

        [Required]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Invalid email format")]
        public string OriginalEmail { get; set; }

        [Required]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Invalid email format")]
        public string NewEmail { get; set; }

        [Required]
        public DateTime ChangeTime { get; set; }

        //The revert and committ IDs allow for reversing and committing of email changes via change URLs.
        [Required]
        public Guid RevertId { get; set; }

        [Required]
        public Guid CommitId { get; set; }

        [Required]
        public bool Committed { get; set; }

        [Required]
        public bool Reverted { get; set; }
    }
}
