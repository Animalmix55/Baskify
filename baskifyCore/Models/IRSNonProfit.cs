using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class IRSNonProfit
    {
        [Key]
        public int EIN { get; set; }

        [Required]
        [MaxLength(100)]
        public string OrganizationName { get; set; }

        public List<IRSNonProfitDocument> Documents { get; set; }

        [MaxLength(60)]
        public string City { get; set; }

        [MaxLength(2)]
        public string State { get; set; }

        [MaxLength(100)]
        public string Country { get; set; }
    }
}
