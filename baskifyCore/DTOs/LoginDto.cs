using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    public class LoginDto
    {
        public string Username {get; set;}
        public string Role { get; set; }

        /// <summary>
        /// Icon URL
        /// </summary>
        public string Icon { get; set; }
        public string DisplayName { get; set; }

        public string Token { get; set; }

        /// <summary>
        /// The redirect after login
        /// </summary>
        public string Redirect { get; set; }
    }
}
