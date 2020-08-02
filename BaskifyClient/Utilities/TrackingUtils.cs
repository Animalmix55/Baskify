using BaskifyClient.DTOs;
using BaskifyClient.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Twilio.Rest.Trunking.V1;

namespace BaskifyClient.Utilities
{
    public static class TrackingUtils
    {
        static HttpClient _client;
        private static string UPSApi;
        private static string UPSKey;

        private static string FedExAPI;
        private static string FedExKey;
        private static string FedExPassword;
        private static string FedExMeterNum;
        private static string FedExAccountNum;

        private static string USPSUsername;
        private static string USPSAPI;

        static TrackingUtils()
        {
            _client = new HttpClient();

            UPSApi = ConfigurationManager.AppSettings["UPSApi"];
            UPSKey = ConfigurationManager.AppSettings["UPSKey"];

            FedExAPI = ConfigurationManager.AppSettings["FedExAPI"];
            FedExKey = ConfigurationManager.AppSettings["FedExKey"];
            FedExPassword = ConfigurationManager.AppSettings["FedExPassword"];
            FedExMeterNum = ConfigurationManager.AppSettings["FedExMeterNum"];
            FedExAccountNum = ConfigurationManager.AppSettings["FedExAccountNum"];

            USPSUsername = ConfigurationManager.AppSettings["USPSUsername"];
            USPSAPI = ConfigurationManager.AppSettings["USPSAPI"];
        }


        /// <summary>
        /// USPS dates are formatted as YYYYMMDD
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime? getUPSDateTime(string date, string time)
        {
            if (date.Length < 8)
                return null;
            var dateString = $"{date.Substring(4, 2)}/{date.Substring(6, 2)}/{date.Substring(0, 4)}";
            if (time != null)
            {
                var timeObj = getUPSTime(time);
                if (timeObj != null)
                    dateString += " " + timeObj.ToString();
            }
            return DateTime.Parse(dateString);
        }

        public static string UPSFormatAddress(string city, string state, string zip, string country)
        {
            var address = "";
            if (!string.IsNullOrWhiteSpace(city))
                address += city + ", ";

            if (!string.IsNullOrWhiteSpace(state))
                address += state + " ";

            if (!string.IsNullOrWhiteSpace(zip))
                address += zip + " ";

            if (!string.IsNullOrWhiteSpace(country))
                address += country;

            return address;
        }

        /// <summary>
        /// USPS formats time as HHMMSS
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static TimeSpan? getUPSTime(string time)
        {
            if (time.Length < 6)
                return null;
            var timeString = $"{time.Substring(0, 2)}:{time.Substring(2, 2)}:{time.Substring(4, 2)}";
            return TimeSpan.Parse(timeString);
        }

        public static TrackingDto trackUPS(string trackingNumber)
        {
            var dto = new TrackingDto();
            dto.Carrier = PostalCarrier.UPS;
            dto.TrackingNumber = trackingNumber;
            var url = UPSApi + trackingNumber; //https://onlinetools.ups.com/track/v1/details/1ZY549V70259637572
            HttpResponseMessage response;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                requestMessage.Headers.Add("AccessLicenseNumber", UPSKey);
                requestMessage.Headers.Add("Accept", "*/*");
                try
                {
                    response = _client.SendAsync(requestMessage).Result;
                }
                catch (Exception)
                {
                    throw new Exception("Error retrieving tracking information");
                }
            }

            var json = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            var responseObject = json.ContainsKey("response") ? json.Value<JObject>("response") : json.Value<JObject>("trackResponse");
            if (responseObject.ContainsKey("errors"))
                throw new Exception("Error retrieving tracking information");

            var package = responseObject.Value<JArray>("shipment")[0].Value<JArray>("package")[0];
            if (package["deliveryDate"] != null)
            {
                var deliveryDate = package.Value<JArray>("deliveryDate")[0];
                if (deliveryDate.Value<string>("type") == "DEL") //is delivered
                    dto.Delivered = true;

                if (package["deliveryTime"] != null)
                {
                    var deliveryTime = package.Value<JObject>("deliveryTime");
                    dto.DeliveryTimeStart = getUPSDateTime(deliveryDate.Value<string>("date"), deliveryTime.Value<string>("startTime"));
                    dto.DeliveryTimeEnd = getUPSDateTime(deliveryDate.Value<string>("date"), deliveryTime.Value<string>("endTime"));
                }
            }

            if (package["activity"] != null)
            {
                dto.Updates = new List<TrackingItem>();
                var activity = package.Value<JArray>("activity");
                int i = 0;
                foreach (var stop in activity)
                {
                    TrackingItem update = new TrackingItem();
                    update.id = i;
                    i++;

                    var location = stop.Value<JObject>("location");
                    var address = location.Value<JObject>("address");

                    update.Location = UPSFormatAddress(address.Value<string>("city"), address.Value<string>("stateProvince"), address.Value<string>("postalCode"), address.Value<string>("country"));
                    var status = stop.Value<JObject>("status");
                    update.Message = status.Value<string>("description");

                    update.Time = getUPSDateTime(stop.Value<string>("date"), stop.Value<string>("time")).Value;
                    dto.Updates.Add(update);

                    if (!string.IsNullOrWhiteSpace(address.Value<string>("city"))) //origin chosen from earliest update with address
                        dto.Origin = UPSFormatAddress(address.Value<string>("city"), address.Value<string>("stateProvince"), address.Value<string>("postalCode"), address.Value<string>("country"));

                    if (status.Value<string>("type") == "D") //delivered
                    {
                        dto.Delivered = true;
                        dto.Destination = UPSFormatAddress(address.Value<string>("city"), address.Value<string>("stateProvince"), address.Value<string>("postalCode"), address.Value<string>("country"));
                    }
                }
            }

            return dto;


        }

        /// <summary>
        /// Tracks a given basket and marks it deliverd if need be... SAVES CHANGES TO DB ASYNC
        /// </summary>
        /// <param name="basket"></param>
        /// <param name="IP"></param>
        /// <returns></returns>
        public static TrackingDto TrackBasket(BasketModel basket, string IP, ApplicationDbContext _context)
        {
            if (basket.TrackingNumber == null || basket.Carrier == null)
                return null;

            TrackingDto dto;
            try
            {
                switch (basket.Carrier)
                {
                    case PostalCarrier.FedEx:
                        dto = trackFedEx(basket.TrackingNumber);
                        break;
                    case PostalCarrier.UPS:
                        dto = trackUPS(basket.TrackingNumber);
                        break;
                    case PostalCarrier.USPS:
                        dto = trackUSPS(basket.TrackingNumber, IP);
                        break;
                    default:
                        return null;
                }

                if(dto != null && dto.Delivered)
                {
                    basket.DeliveryTime = dto.Updates[0].Time;
                    basket.Delivered = true;
                    _context.SaveChangesAsync();
                }
                return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static TrackingDto trackUSPS(string trackingNumber, string clientIP)
        {
            var dto = new TrackingDto();
            dto.Carrier = PostalCarrier.USPS;
            dto.TrackingNumber = trackingNumber;
            var url = new UriBuilder(USPSAPI);
            url.Query = $"API=TrackV2&XML=<TrackFieldRequest USERID=\"{USPSUsername}\"> <Revision>1</Revision><ClientIp>{clientIP}</ClientIp><SourceId>1</SourceId><TrackID ID=\"{trackingNumber}\"></TrackID></TrackFieldRequest>";

            HttpResponseMessage response;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url.ToString()))
            {
                requestMessage.Headers.Add("Accept", "*/*");
                try
                {
                    response = _client.SendAsync(requestMessage).Result;
                }
                catch (Exception)
                {
                    throw new Exception("Error retrieving tracking information");
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response.Content.ReadAsStringAsync().Result);

            XmlNode error;
            if ((error = doc.SelectSingleNode("//Error")) != null) //if error
                throw new Exception(error.SelectSingleNode("./Description").InnerText);

            dto.Destination = UPSFormatAddress(doc.SelectSingleNode("//DestinationCity")?.InnerText, doc.SelectSingleNode("//DestinationState")?.InnerText, doc.SelectSingleNode("//DestinationZip")?.InnerText, null);

            var trackSummary = doc.SelectSingleNode("//TrackSummary");
            var trackingDetails = doc.SelectNodes("//TrackDetail");

            if (trackSummary.SelectSingleNode("./DeliveryAttributeCode")?.InnerText != null) //delivered
                dto.Delivered = true;

            dto.Updates = new List<TrackingItem>();

            var update = new TrackingItem();
            update.id = 0;
            update.Location = UPSFormatAddress(trackSummary.SelectSingleNode("./EventCity")?.InnerText, trackSummary.SelectSingleNode("./EventState")?.InnerText, trackSummary.SelectSingleNode("./EventZIPCode")?.InnerText, null);
            update.Message = trackSummary.SelectSingleNode("./Event")?.InnerText;
            update.Time = DateTime.Parse(trackSummary.SelectSingleNode("./EventDate")?.InnerText + " " + trackSummary.SelectSingleNode("./EventTime")?.InnerText);
            dto.Origin = UPSFormatAddress(trackSummary.SelectSingleNode("./EventCity")?.InnerText, trackSummary.SelectSingleNode("./EventState")?.InnerText, trackSummary.SelectSingleNode("./EventZIPCode")?.InnerText, null);
            dto.Updates.Add(update);

            int i = 1;
            foreach (XmlNode item in trackingDetails)
            {
                var updateDetail = new TrackingItem();
                updateDetail.id = i;
                i++;
                updateDetail.Location = UPSFormatAddress(item.SelectSingleNode("./EventCity")?.InnerText, item.SelectSingleNode("./EventState")?.InnerText, item.SelectSingleNode("./EventZIPCode")?.InnerText, null);
                updateDetail.Message = item.SelectSingleNode("./Event")?.InnerText;
                updateDetail.Time = DateTime.Parse(item.SelectSingleNode("./EventDate")?.InnerText + " " + item.SelectSingleNode("./EventTime")?.InnerText);

                if (!string.IsNullOrWhiteSpace(item.SelectSingleNode("./EventCity")?.InnerText)) //origin, chosen from the closest-to-first event with a location
                    dto.Origin = UPSFormatAddress(item.SelectSingleNode("./EventCity")?.InnerText, item.SelectSingleNode("./EventState")?.InnerText, item.SelectSingleNode("./EventZIPCode")?.InnerText, null);

                dto.Updates.Add(updateDetail);
            }

            return dto;
        }

        public static TrackingDto trackFedEx(string trackingNumber)
        {
            var dto = new TrackingDto();
            dto.Carrier = PostalCarrier.FedEx;
            dto.TrackingNumber = trackingNumber;

            HttpResponseMessage response;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, FedExAPI))
            {
                requestMessage.Headers.Add("Accept", "*/*");
                requestMessage.Content = new StringContent($"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:v16=\"http://fedex.com/ws/track/v16\"><soapenv:Header/><soapenv:Body><v16:TrackRequest><v16:WebAuthenticationDetail> <v16:UserCredential><v16:Key>{FedExKey}</v16:Key><v16:Password>{FedExPassword}</v16:Password></v16:UserCredential></v16:WebAuthenticationDetail><v16:ClientDetail> <v16:AccountNumber>{FedExAccountNum}</v16:AccountNumber><v16:MeterNumber>{FedExMeterNum}</v16:MeterNumber></v16:ClientDetail><v16:Version><v16:ServiceId>trck</v16:ServiceId> <v16:Major>16</v16:Major><v16:Intermediate>0</v16:Intermediate><v16:Minor>0</v16:Minor></v16:Version><v16:SelectionDetails><v16:CarrierCode>FDXE</v16:CarrierCode><v16:PackageIdentifier><v16:Type>TRACKING_NUMBER_OR_DOORTAG</v16:Type><v16:Value>{trackingNumber}</v16:Value></v16:PackageIdentifier><v16:ShipmentAccountNumber/><v16:SecureSpodAccount/></v16:SelectionDetails><v16:ProcessingOptions>INCLUDE_DETAILED_SCANS</v16:ProcessingOptions></v16:TrackRequest></soapenv:Body></soapenv:Envelope>");
                try
                {
                    response = _client.SendAsync(requestMessage).Result;
                }
                catch (Exception)
                {
                    throw new Exception("Error retrieving tracking information");
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response.Content.ReadAsStringAsync().Result);

            XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("f", "http://fedex.com/ws/track/v16");

            var notifications = doc.SelectSingleNode("//f:TrackDetails", ns)?.SelectSingleNode("./f:Notification", ns);
            if (notifications != null && notifications?.SelectSingleNode("./f:Severity", ns)?.InnerText != "SUCCESS")
                throw new Exception(notifications.SelectSingleNode("./f:Message", ns)?.InnerText);

            var trackDetails = doc.SelectSingleNode("//f:TrackDetails", ns);

            var origin = trackDetails.SelectSingleNode("./f:OriginLocationAddress", ns);
            dto.Origin = UPSFormatAddress(origin.SelectSingleNode("./f:City", ns)?.InnerText, origin.SelectSingleNode("./f:StateOrProvinceCode", ns)?.InnerText, null, origin.SelectSingleNode("./f:CountryCode", ns)?.InnerText);

            var destination = trackDetails.SelectSingleNode("./f:DestinationAddress", ns);
            dto.Destination = UPSFormatAddress(destination.SelectSingleNode("./f:City", ns)?.InnerText, destination.SelectSingleNode("./f:StateOrProvinceCode", ns)?.InnerText, null, destination.SelectSingleNode("./f:CountryCode", ns)?.InnerText);

            var events = trackDetails.SelectNodes("./f:Events", ns); //all events
            dto.Updates = new List<TrackingItem>();

            int i = 0;
            foreach (XmlNode update in events)
            {
                TrackingItem item = new TrackingItem();
                item.id = i;
                i++;
                if (update.SelectSingleNode("./f:EventType", ns)?.InnerText == "DL")//delivered
                {
                    dto.Delivered = true;
                    dto.DeliveryTimeEnd = DateTime.Parse(update.SelectSingleNode("./f:Timestamp", ns)?.InnerText);
                }

                item.Message = update.SelectSingleNode("./f:EventDescription", ns)?.InnerText;
                item.Time = DateTime.Parse(update.SelectSingleNode("./f:Timestamp", ns)?.InnerText);

                var address = update.SelectSingleNode("./f:Address", ns);
                item.Location = UPSFormatAddress(address.SelectSingleNode("./f:City", ns)?.InnerText, address.SelectSingleNode("./f:StateOrProvinceCode", ns)?.InnerText, address.SelectSingleNode("./f:PostalCode", ns)?.InnerText, address.SelectSingleNode("./f:CountryCode", ns)?.InnerText);

                dto.Updates.Add(item);
            }

            return dto;
        }
    }
}
