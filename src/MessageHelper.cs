using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HL7.Dotnetcore
{
    public static class MessageHelper
    {
        private static string[] lineSeparators = { "\r\n", "\n\r", "\r", "\n" };

        public static List<string> SplitString(string strStringToSplit, string splitBy, StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return strStringToSplit.Split(new string[] { splitBy }, splitOptions).ToList();
        }

        public static List<string> SplitString(string strStringToSplit, char chSplitBy, StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return strStringToSplit.Split(new char[] { chSplitBy }, splitOptions).ToList();
        }
        
        public static List<string> SplitString(string strStringToSplit, char[] chSplitBy, StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return strStringToSplit.Split(chSplitBy, splitOptions).ToList();
        }

        public static List<string> SplitMessage(string message)
        {
            return message.Split(lineSeparators, StringSplitOptions.None).Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        }

        public static string LongDateWithFractionOfSecond(DateTime dt)
        {
            return dt.ToString("yyyyMMddHHmmss.FFFF");
        }

        public static string[] ExtractMessages(string messages)
        {
            var expr = "\x0B(.*?)\x1C\x0D";
            var matches = Regex.Matches(messages, expr, RegexOptions.Singleline);
            
            var list = new List<string>();
            foreach (Match m in matches)
                list.Add(m.Groups[1].Value);

            return list.ToArray();
        }

        /// <summary>
        /// Helper method to parse date (and time) values - ATTENTION: time zones are not supported yet
        /// </summary>
        public static DateTime ParseDateTime(string dateTimeValueAsString)
        {
            // set up the supported parsing patterns
            var parsingPatterns = new Dictionary<int, string> {
                {4, "yyyy" },
                {6, "yyyyMM" },
                {8, "yyyyMMdd" },
                {10, "yyyyMMddHH" },
                {12, "yyyyMMddHHmm" },
                {14, "yyyyMMddHHmmss" },
                {16, "yyyyMMddHHmmssff" },
                {19, "yyyyMMddHHmmssfffff" }
            };

            // take care of whitespaces around the value
            dateTimeValueAsString = dateTimeValueAsString.Trim();

            if (!parsingPatterns.ContainsKey(dateTimeValueAsString.Length))
            {
                throw new HL7Exception($"Unsupported input pattern");
            }

            if (DateTime.TryParseExact(dateTimeValueAsString, parsingPatterns[dateTimeValueAsString.Length], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            {
                return dateTime;
            }
            else
            {
                throw new HL7Exception($"Unable to parse date time value: '{dateTimeValueAsString}'");
            }
        }
    }
}
