using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public class IRSNonProfitDocument
    {
        [Required]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [Required]
        public string DLN { get; set; }

        [Required]
        [ForeignKey("IRSNonProfit")]
        public int EIN { get; set; }

        [ForeignKey("EIN")]
        public IRSNonProfit IRSNonProfit {get; set;}

        [Required]
        public string URL { get; set; }
        public string TaxPeriod { get; set; }

        public string FormType { get; set; }
    }
}
