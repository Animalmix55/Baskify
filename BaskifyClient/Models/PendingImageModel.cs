using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.Models
{
    public class PendingImageModel
    {
        public PendingImageModel()
        {
            UploadTime = DateTime.UtcNow;
        }

        public DateTime UploadTime { get; set; }

        [Key]
        [Required]
        public string ImageUrl { get; set; }

        [ForeignKey("User")]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel User { get; set; }
    }
}
