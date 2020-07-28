using baskifyCore.Utilities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Models
{
    public enum DeliveryTypes
    {
        [Display(Name = "Pickup")]
        Pickup,
        [Display(Name = "Delivered by Organization")]
        DeliveryByOrg,
        [Display(Name = "Delivery by Submitting Users")]
        DeliveryBySubmitter
    }

    public enum BasketRetrieval //if organization wants to deliver/pickup, they need to coallesce the baskets
    {
        [Display(Name = "Organization Picks Up")]
        OrgPickup,
        [Display(Name = "Donors Deliver to Auction Address")]
        UserDeliver
    }

    public class AuctionModel : IValidatableObject
    {
        public const decimal PurchaseFloor = 0.50M;
        public const decimal PurchaseCeil = 20.00M;
        public const int MaxDistance = 2001;
        public const int MinDistance = 5; //5 miles minimum
        public const decimal MaxPrice = 2.00M;

        public AuctionModel()
        {
            isDrawn = false;
            MinPurchase = PurchaseFloor;
            StartTime = DateTime.UtcNow.AddDays(1);
            EndTime = DateTime.UtcNow.AddDays(5);
            MaxRange = 30;
            TicketCost = 0.40M; //each ticket, by default, costs $.40
            FeePerTrans = DTOs.Fees.FeePerTrans;
            FeePercentage = DTOs.Fees.FeePercent;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuctionId { get; set; }

        [Required]
        [ForeignKey("HostUser")]
        public string HostUsername { get; set; }

        [ForeignKey("HostUsername")]
        public UserModel HostUser { get; set; }

        public List<BasketModel> Baskets { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]+(\s[-\.A-Za-z0-9]+)*$", ErrorMessage = "Invalid Address string")]
        public string Address { get; set; }

        [Required]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$")]
        public string City { get; set; }

        [Required]
        [RegularExpression(@"^[-\.A-Za-z]+(\s[-\.A-Za-z]+)*$")]
        public string State { get; set; }

        [Required]
        [RegularExpression(@"^([0-9]{5}\-[0-9]{4})|([0-9]{5})$", ErrorMessage = "ZIPs should be in the format ##### or #####-####)")]
        public string ZIP { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        [Required]
        [RegularExpression(@"^[-A-Za-z0-9]+(\s[-A-Za-z0-9]+)*$", ErrorMessage = "Title can only contain alphabetical letters, hyphens, and spaces")]
        public string Title { get; set; }

        [Required(AllowEmptyStrings=false, ErrorMessage = "Description cannot be empty")]
        public string Description { get; set; }

        public string BannerImageUrl { get; set; }

        [Display(Name = "Maximum Auction Range (miles)")]
        public int MaxRange { get; set; }

        [Display(Name = "Cost Per Ticket (USD)")]
        [Required]
        [RegularExpression(@"\d+(\.\d{1,2})?", ErrorMessage = "Invalid cost format")]
        public decimal TicketCost { get; set; }

        [Required]
        [Display(Name = "Basket Delivery (to winner)")]
        public DeliveryTypes DeliveryType { get; set; }

        [Display(Name = "Basket Retrieval (from donor)")]
        public BasketRetrieval? BasketRetrieval { get; set; }

        /// <summary>
        /// Minimum purchase IN DOLLARS
        /// </summary>
        [Required]
        [Display(Name = "Minimum Purchase")]
        [RegularExpression(@"\d+(\.\d{1,2})?", ErrorMessage = "Invalid cost format")]
        public decimal MinPurchase { get; set; }

        public virtual AuctionLinkModel Link { get; set; } //lazy loads without inclusion, add basket link

        public bool isDrawn { get; set; }

        /// <summary>
        /// When the auction was drawn
        /// </summary>
        public DateTime? DrawDate { get; set; }

        /// <summary>
        /// If the auction host has been paid yet
        /// </summary>
        public bool PaidOut { get; set; }

        public List<PaymentModel> Payments { get; set; }

        /// <summary>
        /// IN CENTS
        /// </summary>
        public int FeePerTrans { get; set; }

        /// <summary>
        /// IN PERCENT NOT DECIMAL
        /// </summary>
        public float FeePercentage { get; set; }

        [NotMapped]
        public IFormFile BannerImage { get; set; }

        [NotMapped]
        public bool isLive 
        { 
            get { return DateTime.UtcNow < EndTime && DateTime.UtcNow >= StartTime; } 
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (isDrawn && EndTime > DateTime.UtcNow) //if it's somehow drawn and the end date hasn't arrived yet
                yield return new ValidationResult("An auction cannot be drawn until it is over!");
            if(StartTime > EndTime)
            {
                yield return new ValidationResult("The auction cannot start after it ends!", new[] { "StartTime" });
            }
            if((EndTime - StartTime).TotalDays > 31)
            {
                yield return new ValidationResult("The auction cannot last longer than a month!", new[] { "EndTime" });
            }
            if ((EndTime - StartTime).TotalHours < 1)
                yield return new ValidationResult("The auction must be at least an hour long!", new[] { "EndTime" });

            if (MaxRange < MinDistance && MaxRange != -1)
                yield return new ValidationResult("Range is too small", new[] { "MaxRange" });

            if(MinPurchase < PurchaseFloor)
                yield return new ValidationResult(String.Format("The minimum purchase must exceed ${0}", PurchaseFloor), new[] {"MinPurchase"});
            else if(MinPurchase > PurchaseCeil)
                yield return new ValidationResult(String.Format("The minimum purchase must not exceed ${0}", PurchaseCeil), new[] { "MinPurchase" });

            if (TicketCost > MaxPrice)
                yield return new ValidationResult(String.Format("Ticket price must not exceed ${0}", MaxPrice), new[] { "TicketCost" });

            if (DeliveryType != DeliveryTypes.DeliveryBySubmitter && BasketRetrieval == null)
                yield return new ValidationResult("Must specify how baskets arrive at auction location");

            if (DeliveryType == DeliveryTypes.DeliveryBySubmitter)
                BasketRetrieval = null; //make sure no retrieval is specified for a submitter delivery
        }
    }
}
