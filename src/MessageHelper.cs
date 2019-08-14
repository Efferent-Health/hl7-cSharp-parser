using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace HL7.Dotnetcore
{
    public static class MessageHelper
    {
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
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

        public static DateTime? ParseDateTime(string dateTimeString)
        {
            try {
                var result = ParseDateTimeOffset(dateTimeString, assumeLocalTime: false);
                return result.DateTime;
            } catch {
                return null;
            }
        }

        public static DateTime? ParseDateTime(string dateTimeString, out TimeSpan offset)
        {
            try {
                var result = ParseDateTimeOffset(dateTimeString, assumeLocalTime: false);
                offset = result.Offset;
                return result.DateTime;
            } catch {
                offset = default;
                return null;
            }
        }
        
        /// <summary>
        /// Parses a HL7 Timestamp in format YYYY[MM[DD[HH[MM[SS[.S[S[S[S]]]]]]]]][+/-ZZZZ]
        /// </summary>
        /// <param name="value">Timestamp value</param>
        /// <param name="assumeLocalTime"><c>true</c>: assume local time if <paramref name="value"/> does not contain timezone information. If <c>false</c> assume UTC.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="value"/> is null</exception>
        /// <exception cref="FormatException">If <paramref name="value"/> is not a valid HL7 timestamp (DTM).</exception>
        /// <returns>The timestamp as <see cref="DateTimeOffset"/></returns>
        public static DateTimeOffset ParseDateTimeOffset(string value, bool assumeLocalTime = false) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            var timestamp = value.Trim();
            if (timestamp == string.Empty) {
                throw new FormatException(CreateExceptionMessage(value));
            }

            var styles = GetDateTimeStyles(assumeLocalTime);
            
            DateTimeOffset tmp;
            switch (timestamp.Length) {
                case 4: // e.g. 2012
                    return DateTimeOffset.ParseExact(timestamp, "yyyy", InvariantCulture, styles);
                case 6: // e.g. 201202 (February 2012)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMM", InvariantCulture, styles);
                case 8: // e.g. 20120207 (7. February 2012)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMdd", InvariantCulture, styles);
                case 9: // e.g. 2012+0200 (2012, CEST)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyzzz", InvariantCulture, styles);
                case 10: // e.g. 2012020713 (13:00, 7. February 2012)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddHH", InvariantCulture, styles);
                case 11: // e.g. 201202+0200 (February 2012 CEST)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMzzz", InvariantCulture, styles);
                case 12: // e.g. 201202071332 (13:32, 7. February 2012)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHmm", InvariantCulture, styles);
                case 13: // e.g. 20120207+0200 (7. February 2012 CEST)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddzzz", InvariantCulture, styles);
                case 14: // e.g. 20120207133245 (13:32:45, 7. February 2012)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHmmss", InvariantCulture, styles);
                case 15: // e.g. 2012020713+0200 (13:00, 7. February 2012 CEST)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHzzz", InvariantCulture, styles);
                case 16: // e.g. 20120207133245.1 (13:32:45.1, 7. February 2012)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHmmss.f", InvariantCulture, styles);
                case 17: /* e.g. 20120207133245.12 (13:32:45.12, 7. February 2012)
                            or   201202071332+0200 (13:32, 7. February 2012 CEST) */
                    return DateTimeOffset.TryParseExact(timestamp, "yyyyMMddHHmmss.ff", InvariantCulture, styles,
                        out tmp)
                        ? tmp
                        : DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHmmzzz", InvariantCulture, styles);
                case 18: // e.g. 20120207133245.123 (13:32:45.123, 7. February 2012)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHmmss.fff", InvariantCulture, styles);
                case 19: /* e.g. 20120207133245.1234 (13:32:45.1234, 7. February 2012)
                            or   20120207133245+0200 (13:32:45, 7. February 2012 CEST) */
                    return DateTimeOffset.TryParseExact(timestamp, "yyyyMMddHHmmss.ffff", InvariantCulture, styles,
                        out tmp)
                        ? tmp
                        : DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHmmsszzz", InvariantCulture, styles);
                case 21: // e.g. 20120207133245.1+0200 (13:32:45.1, 7. February 2012 CEST)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHmmss.fzzz", InvariantCulture, styles);
                case 22: // e.g. 20120207133245.12+0200 (13:32:45.12, 7. February 2012 CEST)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHmmss.ffzzz", InvariantCulture, styles);
                case 23: // e.g. 20120207133245.123+0200 (13:32:45.123, 7. February 2012 CEST)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHmmss.fffzzz", InvariantCulture, styles);
                default: // e.g. 20120207133245.1234+0200 (13:32:45.123, 7. February 2012 CEST)
                    return DateTimeOffset.ParseExact(timestamp, "yyyyMMddHHmmss.ffffzzz", InvariantCulture, styles);
            }
        }

        private static DateTimeStyles GetDateTimeStyles(bool assumeLocalTime)
            => assumeLocalTime
                ? DateTimeStyles.AssumeLocal
                : DateTimeStyles.AssumeUniversal;
        
        private static string CreateExceptionMessage(string value)
            => $"'{value}' is not a valid HL7 Date/Time (DTM). It must have the following format: YYYY[MM[DD[HH[MM[SS[.S[S[S[S]]]]]]]]][+/-ZZZZ]";
    }
}
