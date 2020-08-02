using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.Models
{
    public class ContactModel
    {
        public ContactModel()
        {
            SubmissionTime = DateTime.UtcNow;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Required(AllowEmptyStrings = false)]
        public string Email { get; set; }
        public DateTime SubmissionTime { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Subject cannot be empty!")]
        public string Subject { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Message cannot be empty!")]
        public string Message { get; set; }
        public bool Read { get; set; }
        public bool Pinned { get; set; }
        public string ipAddress { get; set; }
        [NotMapped]
        public string Token { get; set; }
    }
}
