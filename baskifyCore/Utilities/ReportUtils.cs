using baskifyCore.Models;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Utilities
{
    public static class ReportUtils
    {
        private class BasketReportRow
        {
            [Name("Identifier")]
            public int BasketId { get; set; }
            [Name("Title")]
            public string BasketTitle { get; set; }
            [Name("Description")]
            public string BasketDescription { get; set; }
            [Name("Contents")]
            public string BasketContentString { get; set; }
            [Name("Submission Date")]
            public DateTime SubmissionDate { get; set; }
            [Name("Number of Tickets")]
            public int NumTickets { get; set; }
            [Name("Submitting Username")]
            [Optional]
            public string SubmittingUsername { get; set; }
            [Name("Submitting User Address")]
            [Optional]
            public string SubmittingAddress { get; set; }
            [Name("Submitting User City")]
            [Optional]
            public string SubmittingCity { get; set; }
            [Name("Submitting User State")]
            [Optional]
            public string SubmittingState { get; set; }
            [Name("Submitting User ZIP Code")]
            [Optional]
            public string SubmittingZIP { get; set; }
            [Name("Submitting User Email")]
            [Optional]
            public string SubmittingEmail { get; set; }
            [Name("Winner Username")]
            [Optional]
            public string WinnerUsername { get; set; }
            [Name("Winner Address")]
            [Optional]
            public string WinnerAddress { get; set; }
            [Name("Winner City")]
            [Optional]
            public string WinnerCity { get; set; }
            [Name("Winner State")]
            [Optional]
            public string WinnerState { get; set; }
            [Name("Winner ZIP Code")]
            [Optional]
            public string WinnerZIP { get; set; }
            [Name("Winner Email")]
            [Optional]
            public string WinnerEmail { get; set; }
            [Name("Distance to Winner")]
            public float DistanceFromAuction { get; set; }

            [Name("Accepted By Organization")]
            [BooleanTrueValues("Yes")]
            [BooleanFalseValues("No")]
            public bool AcceptedByOrg { get; set; }
        }

        /// <summary>
        /// Writes report to inputted stream
        /// </summary>
        /// <param name="auctionId"></param>
        /// <param name="_context"></param>
        /// <param name="ms"></param>
        public static void getAuctionReport(int auctionId, ApplicationDbContext _context, out MemoryStream ms)
        {
            var auction = _context.AuctionModel.Find(auctionId);

            List<BasketModel> completeRows;
            completeRows = _context.BasketModel.Where(b => b.AuctionId == auctionId).Include(b => b.Winner).Include(b => b.SubmittingUser).Include(b => b.Tickets).ToList();
            

            List<BasketReportRow> censoredRows = completeRows.Select(r => new BasketReportRow()
            {
                BasketDescription = r.BasketDescription,
                BasketId = r.BasketId,
                BasketTitle = r.BasketTitle,
                NumTickets = r.Tickets.Sum(t => t.NumTickets),
                SubmissionDate = r.SubmissionDate,
                SubmittingAddress = r.SubmittingUser != null ? r.SubmittingUser.Address : null,
                SubmittingCity = r.SubmittingUser != null ? r.SubmittingUser.City : null,
                SubmittingEmail = r.SubmittingUser != null ? r.SubmittingUser.Email : null,
                SubmittingState = r.SubmittingUser != null ? r.SubmittingUser.State : null,
                SubmittingUsername = r.SubmittingUsername,
                SubmittingZIP = r.SubmittingUser != null ? r.SubmittingUser.ZIP : null,
                WinnerAddress = r.Winner != null? r.Winner.Address : null,
                WinnerCity = r.Winner != null ? r.Winner.City : null,
                WinnerEmail = r.Winner != null ? r.Winner.Email : null,
                WinnerState = r.Winner != null ? r.Winner.State : null,
                WinnerUsername = r.WinnerUsername,
                WinnerZIP = r.Winner != null ? r.Winner.ZIP : null,
                BasketContentString = r.BasketContentString.Trim('[').Trim(']'),
                DistanceFromAuction = r.Winner != null? (float)SearchUtils.getMiles(auction.Latitude, auction.Longitude, r.Winner.Latitude, r.Winner.Longitude) : 0,
                AcceptedByOrg = r.AcceptedByOrg
              
            }).ToList();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture);
            config.SanitizeForInjection = true;
            config.HasHeaderRecord = true;

            ms = new MemoryStream();
            var ts = new StreamWriter(ms);
            var csv = new CsvWriter(ts, config);
            csv.WriteHeader<BasketReportRow>();
            csv.NextRecord();
            foreach(var record in censoredRows)
            {
                csv.WriteRecord(record);
                csv.NextRecord();
            }
            ts.Flush();
            return;
        }
    }
}
