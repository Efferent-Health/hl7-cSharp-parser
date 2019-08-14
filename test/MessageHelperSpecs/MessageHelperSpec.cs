using System;
using System.Collections.Generic;
using HL7.Dotnetcore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HL7test.MessageHelperSpecs
{
    [TestClass]
    public class If_the_user_parses_a_HL7_DTM_string_to_DateTimeOffset_assuming_UTC
    {
        private static IEnumerable<object[]> TestCases() 
        {
            // yyyyMMddHHmmssSSSSzzzz
            var milliseconds123dot4 = TimeSpan.FromTicks(1234 * TimeSpan.TicksPerMillisecond / 10);
            yield return new object[] {
                "20120606215334.1234+0200",
                new DateTimeOffset(2012, 6, 6, 21, 53, 34, TimeSpan.FromHours(2)) + milliseconds123dot4
            };

            // yyyyMMddHHmmssSSSS
            yield return new object[] {
                "20120609032743.1234",
                new DateTimeOffset(2012, 06, 9, 3, 27, 43, TimeSpan.Zero) + milliseconds123dot4
            };

            // yyyyMMddHHmmssSSS
            yield return new object[] {
                "20120609032743.123",
                new DateTimeOffset(2012, 06, 9, 3, 27, 43, 123, TimeSpan.Zero)
            };

            // yyyyMMddHHmmssSS
            yield return new object[] {
                "20120609032743.12",
                new DateTimeOffset(2012, 06, 9, 3, 27, 43, 120, TimeSpan.Zero)
            };

            // yyyyMMddHHmmssSS
            yield return new object[] {
                "20120609032743.1",
                new DateTimeOffset(2012, 06, 9, 3, 27, 43, 100, TimeSpan.Zero)
            };

            // yyyyMMddHHmmss
            yield return new object[] {
                "20120609032743",
                new DateTimeOffset(2012, 06, 9, 3, 27, 43, 0, TimeSpan.Zero)
            };

            // yyyyMMddHHmm
            yield return new object[] {
                "201206191327",
                new DateTimeOffset(2012, 06, 19, 13, 27, 0, 0, TimeSpan.Zero)
            };

            // yyyyMMddHH
            yield return new object[] {
                "2012061913",
                new DateTimeOffset(2012, 06, 19, 13, 0, 0, 0, TimeSpan.Zero)
            };

            // yyyyMMdd
            yield return new object[] {
                "20120619",
                new DateTimeOffset(2012, 06, 19, 0, 0, 0, 0, TimeSpan.Zero)
            };

            // yyyyMM
            yield return new object[] {
                "201206",
                new DateTimeOffset(2012, 06, 1, 0, 0, 0, 0, TimeSpan.Zero)
            };

            // yyyy
            yield return new object[] {
                "2010",
                new DateTimeOffset(2010, 1, 1, 0, 0, 0, 0, TimeSpan.Zero)
            };

            // Leap year
            yield return new object[] {
                "20120229",
                new DateTimeOffset(2012, 02, 29, 0, 0, 0, 0, TimeSpan.Zero)
            };

            // yyyy UTC
            yield return new object[] {
                "2012+0000",
                new DateTimeOffset(2012, 1, 1, 0, 0, 0, 0, TimeSpan.Zero)
            };

            // yyyy -02:30
            yield return new object[] {
                "2012-0230",
                new DateTimeOffset(2012, 1, 1, 0, 0, 0, 0, TimeSpan.FromHours(-2.5))
            };

            // yyyy +02:30
            yield return new object[] {
                "2012+0230",
                new DateTimeOffset(2012, 1, 1, 0, 0, 0, 0, TimeSpan.FromHours(+2.5))
            };
        }

        [DataTestMethod]
        [DynamicData(nameof(TestCases), DynamicDataSourceType.Method)]
        public void It_should_return_the_correct_DateTimeOffset(string testString, DateTimeOffset expected) 
        {
            var actual = MessageHelper.ParseDateTimeOffset(testString, false);
            Assert.AreEqual(expected, actual);
        }
    }

    [TestClass]
    public class If_the_user_parses_a_HL7_DTM_string_to_DateTimeOffset_assuming_LOCAL_TIME
    {
        private static IEnumerable<object[]> TestCases() 
        {
            // yyyyMMddHHmmssSSSSzzzz (timezone data available -> assumeLocalTime ignored)
            var milliseconds123dot4 = TimeSpan.FromTicks(1234 * TimeSpan.TicksPerMillisecond / 10);
            yield return new object[] {
                "20120606215334.1234+0200",
                new DateTimeOffset(2012, 6, 6, 21, 53, 34, TimeSpan.FromHours(2)) + milliseconds123dot4
            };

            // yyyyMMddHHmmssSSSS
            yield return new object[] {
                "20120609032743.1234",
                new DateTimeOffset(new DateTime(2012, 06, 9, 3, 27, 43, DateTimeKind.Local) + milliseconds123dot4)
            };

            // yyyy
            yield return new object[] {
                "2012",
                new DateTimeOffset(new DateTime(2012, 1, 1, 0, 0, 0, 0, DateTimeKind.Local))
            };

            // day light saving time change -> winter -> summer
            yield return new object[] {
                "20120325033000",
                new DateTimeOffset(new DateTime(2012, 03, 25, 3, 30, 0, 0, DateTimeKind.Local))
            };

            // day light saving time change -> summer -> winter
            yield return new object[] {
                "20121028020000",
                new DateTimeOffset(new DateTime(2012, 10, 28, 2, 0, 0, 0, DateTimeKind.Local))
            };
        }

        [DataTestMethod]
        [DynamicData(nameof(TestCases), DynamicDataSourceType.Method)]
        public void It_should_return_the_correct_DateTimeOffset(string testString, DateTimeOffset expected) 
        {
            var actual = MessageHelper.ParseDateTimeOffset(testString, true);
            Assert.AreEqual(expected, actual);
        }
    }
}