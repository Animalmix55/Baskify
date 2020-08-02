using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.Models
{
    /// <summary>
    /// Primary key is AlertType AND User
    /// </summary>
    public class UserAlertModel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string AlertType { get; set; }

        [MaxLength(20)]
        public string AlertHeader { get; set; }

        [MaxLength(200)]
        [Required]
        public string AlertBody { get; set; }

        [MaxLength(30)]
        [ForeignKey("UserModel")]
        [Required]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel UserModel { get; set; }

        public bool Dismissable { get; set; } //indicates if the alert can be silenced by x-ing it out.

    }
}
