using baskifyCore.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.DTOs
{
    public static class Fees
    {
        /// <summary>
        /// FEE IN PERCENT (NOT DECIMAL)
        /// </summary>
        public static readonly float FeePercent;
        /// <summary>
        /// Fee IN CENTS
        /// </summary>
        public static readonly int FeePerTrans;
        static Fees()
        {
            FeePercent = float.Parse(ConfigurationManager.AppSettings["FeePercent"]);
            FeePerTrans = int.Parse(ConfigurationManager.AppSettings["FeePerTransaction"]);
        }
    }

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

            //PAYMENTS MUST BE COMPLETED AND SUCCESSFUL!
            PercentDeliveryReq = percentDeliveredReq;
            TotalFundraised = Decimal.Round((Decimal)Auction.Payments.Where(p => p.Complete && p.Success).Sum(p => p.Amount) / 100, 2);
            Fees = Decimal.Round(Auction.Payments.Where(p => p.Complete && p.Success).Sum(p => p.Fee) / 100);
            MeetsDeliveryReq = Auction.Baskets.Where(b => b.AcceptedByOrg && !b.Draft).Count(b => b.Delivered && b.DeliveryTime.Value.AddDays(3) <= DateTime.UtcNow) >= percentDeliveredReq * Auction.Baskets.Where(b => b.AcceptedByOrg && !b.Draft).Count();
            MeetsDisputeReq = Auction.Baskets.Count(b => b.DisputedShipment) == 0;
            MeetsAuctionDrawnReq = Auction.isDrawn;
            MeetsAuctionNotPaidReq = !Auction.PaidOut;
        }

        /// <summary>
        /// Outputs total fee IN CENTS, INPUTS IN CENTS
        /// </summary>
        /// <param name="total">IN CENTS</param>
        /// <param name="numTransactions"></param>
        /// <returns></returns>
        public static int CalculateFee(int total, int numTransactions, int FeePerTrans, float FeePercent)
        {
            var percentage = FeePercent/100.00; //decimal
            var perTrans = FeePerTrans; //cents
            return (int)Math.Round(total * percentage + numTransactions * perTrans);
        } 

        /// <summary>
        /// IN DOLLARS
        /// </summary>
        public Decimal TotalFundraised { get; set; }
        /// <summary>
        /// IN DOLLARS
        /// </summary>
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
