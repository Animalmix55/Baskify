using AutoMapper;
using baskifyCore.DTOs;
using baskifyCore.Models;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Data.Entity;

namespace baskifyCore.Utilities
{
    public static class NonProfitUtils
    {
        public static readonly HttpClient _client;
        static NonProfitUtils()
        {
            _client = new HttpClient();
        }

        public static List<NonProfitDto> Search(string searchString, ApplicationDbContext _context, string state=null)
        {
            int ein;
            List<IRSNonProfit> results;
            if(int.TryParse(searchString, out ein))
            {
               //results = _context.IRSNonProfit.Where(np => np.EIN.Contains(searchString)).Take(10).ToList();
               results = _context.IRSNonProfit.SqlQuery("SELECT TOP(5) * FROM IRSNonProfits WHERE EIN LIKE @ein", new SqlParameter("@ein", $"%{searchString}%")).ToList();
            }
            else
            {
                //results = _context.IRSNonProfit.Where(np => np.OrganizationName.ToLower().Contains(searchString.ToLower())).Take(10).ToList();
                results = _context.IRSNonProfit.SqlQuery("SELECT TOP(5) * FROM IRSNonProfits WHERE (OrganizationName LIKE @name)", new SqlParameter("@name", $"%{searchString}%")).ToList();
            }
            

            return Mapper.Map<List<NonProfitDto>>(results);
        }

        public static NonProfitDto Select(int ein, ApplicationDbContext _context)
        {
            var documents = _context.IRSNonProfitDocument.Include(np => np.IRSNonProfit).Where(np => np.EIN == ein).OrderByDescending(d => d.TaxPeriod).ToList();

            if (documents.Count == 0)
                return null;

            var dto = new NonProfitDto()
            {
                OrganizationName = documents[0].IRSNonProfit.OrganizationName,
                EIN = ein
            };

            foreach (var document in documents)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(document.URL);

                XmlNamespaceManager namespaces = new XmlNamespaceManager(doc.NameTable);
                namespaces.AddNamespace("d", "http://www.irs.gov/efile");

                XmlNodeList filer = doc.SelectNodes("//d:Filer", namespaces);

                if (filer.Count == 0)
                    continue;

                var phoneNode = filer[0].SelectSingleNode("./d:PhoneNum", namespaces); //GET PHONE NUMBER FROM 990 IF EXISTS
                if(phoneNode != null)
                {
                    var phoneString = phoneNode.InnerText;
                    long phoneNum;
                    if (long.TryParse(phoneString, out phoneNum))
                        dto.Phone = phoneNum;
                }

                var addressDoc = filer[0].SelectSingleNode("./d:USAddress", namespaces);
                //get address
                if (dto.Address == null && addressDoc != null)
                {
                    dto.Address = addressDoc.SelectSingleNode("./d:AddressLine1Txt", namespaces).InnerText;
                    dto.City = addressDoc.SelectSingleNode("./d:CityNm", namespaces).InnerText;
                    dto.State = addressDoc.SelectSingleNode("./d:StateAbbreviationCd", namespaces).InnerText;
                    dto.ZIP = addressDoc.SelectSingleNode("./d:ZIPCd", namespaces).InnerText.Length < 6? addressDoc.SelectSingleNode("./d:ZIPCd", namespaces).InnerText : $"{addressDoc.SelectSingleNode("./d:ZIPCd", namespaces).InnerText.Substring(0, 5)}-{addressDoc.SelectSingleNode("./d:ZIPCd", namespaces).InnerText.Substring(5, 4)}";
                    dto.Country = "United States"; //uses USAddress XPath
                }

                //get officers
                dto.Members = new List<NonProfitMember>();
                XmlNodeList members = doc.SelectNodes("//d:BusinessOfficerGrp", namespaces);
                foreach(XmlNode member in members)
                {
                    try //some don't have phone numbers
                    {
                        var memberDto = new NonProfitMember()
                        {
                            Name = member.SelectSingleNode("./d:PersonNm", namespaces).InnerText,
                            PhoneNumber = long.Parse(member.SelectSingleNode("./d:PhoneNum", namespaces).InnerText),
                            Position = member.SelectSingleNode("./d:PersonTitleTxt", namespaces).InnerText,
                        };

                        if(!dto.Members.Any(m => m.PhoneNumber == memberDto.PhoneNumber)) //avoid duplicates
                            dto.Members.Add(memberDto); //add member
                    }
                    catch (Exception) { }
                }

                //now lets check BooksInCareOf
                var InCareOf = doc.SelectSingleNode("//d:BooksInCareOfDetail", namespaces);
                if(InCareOf != null)
                {
                    try //business names will break this
                    {
                        var memberDto = new NonProfitMember()
                        {
                            Name = InCareOf.SelectSingleNode("./d:PersonNm", namespaces).InnerText,
                            PhoneNumber = long.Parse(InCareOf.SelectSingleNode("./d:PhoneNum", namespaces).InnerText),
                            Position = "BOOKS IN CARE OF"
                        };
                        if (!dto.Members.Any(m => m.PhoneNumber == memberDto.PhoneNumber)) //if unique
                            dto.Members.Add(memberDto); //add member
                    }
                    catch (Exception) { }
                }
            }

            return dto;
        }
    }
}
