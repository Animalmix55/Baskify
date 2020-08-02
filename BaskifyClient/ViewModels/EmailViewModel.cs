using BaskifyClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BaskifyClient.ViewModels
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
        public string Title { get; set; }
    }
}
