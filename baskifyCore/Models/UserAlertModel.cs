using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    /// <summary>
    /// Primary key is AlertType AND User
    /// </summary>
    public class UserAlertModel
    {
        [Required]
        [MaxLength(20)]
        [Key]
        [Column(Order = 1)]
        public string AlertType { get; set; }

        [MaxLength(20)]
        public string AlertHeader { get; set; }

        [MaxLength(200)]
        [Required]
        public string AlertBody { get; set; }

        [MaxLength(30)]
        [ForeignKey("UserModel")]
        [Column(Order = 0)]
        [Required]
        [Key]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel UserModel { get; set; }

    }
}
