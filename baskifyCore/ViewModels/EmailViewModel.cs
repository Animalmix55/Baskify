using baskifyCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.ViewModels
{
    public class EmailViewModel
    {
        public EmailViewModel()
        {
            rootUrl = "https://baskify.com/";
        }
        public BasketModel Basket { get; set; }
        public UserModel User { get; set; }
        public string Contents { get; set; }
        public string rootUrl { set; get; }
    }
}
