using BaskifyClient.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaskifyClient.ViewModels
{
    public class UserAuctionViewModel : IValidatableObject
    {
        public UserModel UserModel { get; set; }
        
        [Required]
        public AuctionModel AuctionModel { get; set; } //it's important that the auction have all the baskets and the user's tickets should be in UserTickets for each basket
        public UserAuctionWalletModel Wallet { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Wallet != null && UserModel != null && Wallet.Username != UserModel.Username) //forces the wallet to be the correct one
                yield return new ValidationResult("Invalid Wallet for User", new[] { "UserModel" });
        }
    }
}
