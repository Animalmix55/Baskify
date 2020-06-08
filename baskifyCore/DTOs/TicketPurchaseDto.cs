using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    public class TicketPurchaseDto
    {
        public int NumTickets { get; set; }

        //Billing address info
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$", ErrorMessage = "Name can only contain alphabetical letters, hyphens, and spaces")]
        public string CardholderName { get; set; }
        [RegularExpression(@"^[0-9]+(\s[-\.A-Za-z0-9]+)*$", ErrorMessage = "Invalid Address string")]
        public string BillingAddress { get; set; }
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$")]
        public string BillingCity { get; set; }
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$")]
        public string BillingState { get; set; }
        [RegularExpression(@"^([0-9]{5}\-[0-9]{4})|([0-9]{5})$", ErrorMessage = "ZIPs should be in the format ##### or #####-####)")]
        public string BillingZIP { get; set; }
        public bool UseAccountAddress { get; set; }
        public string PaymentMethodId { get; set; }

        public bool SaveCard { get; set; }
    }
}
