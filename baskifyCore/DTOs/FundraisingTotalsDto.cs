using baskifyCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    /// <summary>
    /// Designed to ensure that there is a standard across the board for fees
    /// </summary>
    public class FundraisingTotalsDto
    {
        public FundraisingTotalsDto(AuctionModel Auction)
        {
            if (Auction.Payments == null)
                throw new Exception("Error retrieving payments, not tracked");

            double percentDeliveredReq = 0;
            switch (Auction.DeliveryType)
            {
                case DeliveryTypes.DeliveryByOrg:
                    percentDeliveredReq = 0.80;
                    break;
                case DeliveryTypes.DeliveryBySubmitter:
                    percentDeliveredReq = 0.50;
                    break;
                case DeliveryTypes.Pickup:
                    percentDeliveredReq = 0.10;
                    break;
            }

            PercentDeliveryReq = percentDeliveredReq;
            TotalFundraised = Decimal.Round((Decimal)Auction.Payments.Sum(p => p.Amount) / 100, 2);
            Fees = Decimal.Round(Auction.Payments.Sum(p => p.Fee) / 100);
            MeetsDeliveryReq = Auction.Baskets.Where(b => b.AcceptedByOrg && !b.Draft).Count(b => b.Delivered && b.DeliveryTime.Value.AddDays(3) <= DateTime.UtcNow) >= percentDeliveredReq * Auction.Baskets.Where(b => b.AcceptedByOrg && !b.Draft).Count();
            MeetsDisputeReq = Auction.Baskets.Count(b => b.DisputedShipment) == 0;
            MeetsAuctionDrawnReq = Auction.isDrawn;
            MeetsAuctionNotPaidReq = !Auction.PaidOut;
        }

        public static int CalculateFee(int total, int numTransactions)
        {
            var percentage = .049; 
            var perTrans = 30; //cents
            return (int)Math.Round(total * percentage + numTransactions * perTrans);
        } 

        public Decimal TotalFundraised { get; set; }
        public Decimal Fees { get; set; }
        public Decimal NetFundraised { get { return (TotalFundraised - Fees); } }

        public bool MeetsDisputeReq { get; set; }
        public bool MeetsDeliveryReq { get; set; }
        public double PercentDeliveryReq { get; set; }
        public bool MeetsAuctionDrawnReq { get; set; }
        public bool MeetsAuctionNotPaidReq { get; set; }

        public bool isPayable { get { return MeetsDeliveryReq && MeetsDisputeReq && MeetsAuctionDrawnReq && MeetsAuctionNotPaidReq; } }
    }
}
