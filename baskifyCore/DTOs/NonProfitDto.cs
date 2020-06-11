using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    public class NonProfitDto
    {
        public int EIN { get; set; }
        public string OrganizationName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZIP { get; set; }

        public string Country { get; set; }
        public int SubSecId { get; set; } //501(c)(##) OR 92 for 4947(a)(1)
        public List<string> Members { get; set; } //a list of people mentioned in 990s
    }
}
