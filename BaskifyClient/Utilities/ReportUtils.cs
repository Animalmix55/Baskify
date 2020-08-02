using AutoMapper;
using BaskifyClient.Models;
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

namespace BaskifyClient.Utilities
{
    public static class ReportUtils
    {
        static ReportUtils() //create map
        {
            Mapper.CreateMap<BasketModel, BasketReportRow>()//flatten the model
                .ForMember(d => d.SubmittingAddress, source => source.MapFrom(src => src.SubmittingUser.Address))
                .ForMember(d => d.SubmittingCity, source => source.MapFrom(src => src.SubmittingUser.City))
                .ForMember(d => d.SubmittingEmail, source => source.MapFrom(src => src.SubmittingUser.Email))
                .ForMember(d => d.SubmittingState, source => source.MapFrom(src => src.SubmittingUser.State))
                .ForMember(d => d.SubmittingZIP, source => source.MapFrom(src => src.SubmittingUser.ZIP))
                .ForMember(d => d.SubmittingFirstname, source => source.MapFrom(src => src.SubmittingUser.FirstName))
                .ForMember(d => d.SubmittingLastname, source => source.MapFrom(src => src.SubmittingUser.LastName)) //now winner
                .ForMember(d => d.WinnerAddress, source => source.MapFrom(src => src.Winner.Address))
                .ForMember(d => d.WinnerCity, source => source.MapFrom(src => src.Winner.City))
                .ForMember(d => d.WinnerEmail, source => source.MapFrom(src => src.Winner.Email))
                .ForMember(d => d.WinnerState, source => source.MapFrom(src => src.Winner.State))
                .ForMember(d => d.WinnerZIP, source => source.MapFrom(src => src.Winner.ZIP))
                .ForMember(d => d.WinnerFirstname, source => source.MapFrom(src => src.Winner.FirstName))
                .ForMember(d => d.WinnerLastname, source => source.MapFrom(src => src.Winner.LastName));
        }

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
            public string SubmittingFirstname { get; set; }
            [Optional]
            public string SubmittingLastname { get; set; }
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
            public string WinnerFirstname { get; set; }
            [Optional]
            public string WinnerLastname { get; set; }
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

            var censoredRows = Mapper.Map<List<BasketReportRow>>(completeRows);
            censoredRows.ForEach(r => r.Cleanse(auction.DeliveryType == DeliveryTypes.Pickup, auction.BasketRetrieval == BasketRetrieval.UserDeliver)); //remove all private info

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

        /// <summary>
        /// Cleanses private info from PrivBasketDtos; if the isUser flag is set, removes names and emails from cleansed references.
        /// </summary>
        /// <param name="basket"></param>
        /// <param name="cleanseWinner"></param>
        /// <param name="cleanseSubmitter"></param>
        /// <param name="isUser"></param>
        private static void Cleanse(this BasketReportRow basket, bool cleanseWinner, bool cleanseSubmitter)
        {
            if (cleanseWinner)
            {
                basket.WinnerAddress = null;
                basket.WinnerCity = null;
                basket.WinnerState = null;
                basket.WinnerCity = null;
            }
            if (cleanseSubmitter)
            {
                basket.SubmittingAddress = null;
                basket.SubmittingCity = null;
                basket.SubmittingState = null;
                basket.SubmittingCity = null;
            }
        }
    }
}
