using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace baskifyCore.Utilities
{
    public static class CaptchaConsts
    {
        public static string Public;
        public static string Private;
        static CaptchaConsts()
        {
            Public = ConfigurationManager.AppSettings["ReCAPTCHAPublic"];
            Private = ConfigurationManager.AppSettings["ReCAPTCHAPrivate"];
        }
    }
    public static class CaptchaUtils
    {
        private static HttpClient _client;
        static CaptchaUtils()
        {
            _client = new HttpClient();
        }

        /// <summary>
        /// Verifies the reCAPTCHA token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static bool verifyToken(string token, string ip = null)
        {
            var values = new Dictionary<string, string>
            {
                { "secret", CaptchaConsts.Private },
                { "response", token }
            };

            if (ip != null)
                values.Add("remoteip", ip);

            var content = new FormUrlEncodedContent(values);

            var response = _client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content).Result;

            var responseObject = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            if (responseObject.ContainsKey("success"))
                return (bool)responseObject["success"];
            else
                return false;
        }
    }
}
