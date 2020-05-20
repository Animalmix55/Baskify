using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.ViewModels
{
    public class CreditCardViewModel
    {
        [Required]
        [StringLength(16, MinimumLength = 16)]
        public string CardNumber { get; set; }
        
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CVV { get; set; }

        [Required]
        [StringLength(2,MinimumLength = 2)]
        public string ExpMonth { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 4)]
        public string ExpYear { get; set; }
    }
}
