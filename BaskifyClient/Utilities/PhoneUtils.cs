using BaskifyClient.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;

namespace BaskifyClient.Utilities
{
    public static class PhoneUtils
    {
        public enum MessageTypes
        {
            Call,
            Text
        }

        /// <summary>
        /// Texts/Calls the given number with the given data
        /// </summary>
        /// <param name="phoneNum">Phone number WITH country code in format +###########</param>
        /// <param name="text">The data to send</param>
        /// <param name="type">The type of message (call, text,...)</param>
        /// <returns>The SID for locating the text</returns>
        public static string Message(string phoneNum, string text, MessageTypes type)
        {
            switch (type)
            {
                case MessageTypes.Text:
                    return MessageResource.Create(
                        body: text,
                        messagingServiceSid: ConfigurationManager.AppSettings["TwilioMessagingServiceSid"],
                        to: new Twilio.Types.PhoneNumber(phoneNum)
                    ).Sid;
                default:
                case MessageTypes.Call:
                    return CallResource.Create(
                        twiml: new Twilio.Types.Twiml($"<Response><Say>{text}</Say></Response>"),
                        from: new Twilio.Types.PhoneNumber(ConfigurationManager.AppSettings["TwilioNum"]),
                        to: new Twilio.Types.PhoneNumber(phoneNum)
                    ).Sid;
            }
           
        }

        /// <summary>
        /// Sends a validation message with the given code to the given phone number
        /// </summary>
        /// <param name="phoneNum"></param>
        /// <param name="code"></param>
        public static void SendValidationMessage(string phoneNum, string code)
        {
            Message(phoneNum, $"Your Baskify verification code is: {code}", MessageTypes.Text);
        }
    }
}
