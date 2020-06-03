using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    public class UserDto
    {
        public string Username { get; set; }

        public string FirstName
        { get; set; }

        public string Email { get; set; }

        public string LastName
        { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string ZIP { get; set; }
    }
}
