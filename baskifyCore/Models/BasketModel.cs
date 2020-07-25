using baskifyCore.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;


namespace baskifyCore.Models
{
    public enum PostalCarrier
    {
        UPS,
        USPS,
        FedEx
    }

    public class BasketModel : IValidatableObject
    {
        public List<BasketPhotoModel> photos { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BasketId { get; set; }

        [ForeignKey("SubmittingUser")]
        public string SubmittingUsername { get; set; }

        [ForeignKey("SubmittingUsername")]
        public UserModel SubmittingUser { get; set; }

        //Can get host through auction model

        [Required]
        [RegularExpression(@"^[-'\!A-Za-z0-9]+(\s[-'\!A-Za-z0-9]+)*$", ErrorMessage = "Title contains illegal characters")]
        [Display(Name = "Basket Title")]
        public string BasketTitle { get; set; }

        [Display(Name = "Basket Description")]
        [Required(AllowEmptyStrings = false)]
        public string BasketDescription { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Basket Content is required!")]
        public string BasketContentString
        {
            get { return (BasketContents == null || BasketContents.Count == 0) ? null : JsonSerializer.Serialize(BasketContents); }
            set { BasketContents = (value == null) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(value); }
        }

        public bool AcceptedByOrg { get; set; }

        public List<TicketModel> Tickets { get; set; }

        public DateTime SubmissionDate { get; set; }

        [Required]
        [ForeignKey("AuctionModel")]
        public int AuctionId { get; set; }

        [ForeignKey("AuctionId")]
        public AuctionModel AuctionModel { get; set; }

        [Required]
        [Display(Name = "Basket Contents")]
        public List<string> BasketContents { get; set; }

        [ForeignKey("Winner")]
        public string WinnerUsername {get; set;}

        [ForeignKey("WinnerUsername")]
        public UserModel Winner { get; set; }

        /// <summary>
        /// Indicates if the basket was ever completed by the user
        /// </summary>
        [Required]
        public bool Draft { get; set; }

        [Required]
        public bool Delivered { get; set; }

        public string TrackingNumber { get; set; }

        public PostalCarrier? Carrier { get; set; }

        public DateTime? DeliveryTime { get; set; }

        public bool DisputedShipment { get; set; }

        public string DisputeReason { get; set; }

        public DateTime? DisputeTime { get; set; }

        //--------------------------------------------UNMAPPED ATTRIBUTES---------------------------------

        [NotMapped]
        public List<string> addImages { get; set; }


        /// <summary>
        /// Used to set privbasketdto status
        /// </summary>
        [NotMapped]
        public string Status { get; set; }

        [NotMapped]
        public List<string> removeImages { get; set; }

        [NotMapped]
        public TicketModel UserTickets { get; set; } //the tickets for a SPECIFIC user, as needed

        [NotMapped]
        public int? NumTickets { get { return (Tickets == null ? (int?)null : (int?)Tickets.Sum(t => t.NumTickets)); } }

            /*
        {
            get { return string.IsNullOrWhiteSpace(BasketContentString) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(BasketContentString); }
            set { BasketContentString = (value == null || value.Count == 0)? null : JsonSerializer.Serialize(value); }
        }
        */

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BasketContents.Any(b => string.IsNullOrWhiteSpace(b)))
                yield return new ValidationResult("No content rows can be empty!", new[] { "BasketContentString" }); //no empty indeces allowed

            if (!string.IsNullOrWhiteSpace(TrackingNumber) && Carrier == null)
                yield return new ValidationResult("A postal carrier must be specified!");

            if(Delivered && DeliveryTime == null)
                yield return new ValidationResult("A delivered basket must have a delivery time!");
        }
    }
}
