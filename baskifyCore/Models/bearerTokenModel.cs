using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class BearerTokenModel
    {
        /// <summary>
        /// Must match the time inside of the serialized token
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Required]
        public string TokenIdentifier { get; set; }
        public DateTime TimeWritten { get; set; }
        public DateTime TimeExpire { get; set; }

        [StringLength(30)]
        [ForeignKey("UserModel")]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel UserModel { get; set; }
        public string Token { get; set; }

    }
}
