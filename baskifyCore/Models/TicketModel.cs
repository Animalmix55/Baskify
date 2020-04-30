using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    //Indicates NOT individual tickets, but the investment a certain user has into a basket
    public class TicketModel : IValidatableObject
    {
        [Required]
        public int NumTickets { get; set; }

        [Key]
        [Column(Order = 1)]
        [Required]
        [ForeignKey("User")]
        public string Username { get; set; }

        [ForeignKey("Username")]
        public UserModel User { get; set; }

        //must be nullable to avoid cycle when user that creates it dissapears... 
        //this should never really be an issue since there's no way to delete an account.

        //This is probably bad practice, but EF won't allow circular references since
        //organizations are the same a users... So, I can't call basketId a foreign ID. 
        //It still will be treated as one.

        [Key]
        [Column(Order = 2)]
        [Required]
        [ForeignKey("Basket")]
        public int BasketId { get; set; }

        [ForeignKey("BasketId")]
        public BasketModel Basket {get; set;}

        /// <summary>
        /// This is shoddy but forces BasketId to follow foreign key constraints
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var _context = new ApplicationDbContext(); //also bad practice, but gotta keep the restraints
            BasketModel basket;
            var validBasket = true;
            try
            {
                basket = _context.BasketModel.Find(BasketId);
            }
            catch (Exception)
            {
                validBasket = false;
            }
            if(!validBasket)
                yield return new ValidationResult("Failed basket foreign key constraint", new[] { "BasketId" });
        }
    }
}
