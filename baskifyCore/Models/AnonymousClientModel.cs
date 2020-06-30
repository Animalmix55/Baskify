using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class AnonymousClientModel
    {
        public AnonymousClientModel()
        {
            ClientId = Guid.NewGuid();
            Created = DateTime.UtcNow;
        }

        [Key]
        public Guid ClientId { get; set; }
        public DateTime Created { get; set; }
        [Required]
        public string IPAddress { get; set; }
    }
}
