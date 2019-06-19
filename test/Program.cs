using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using HL7.Dotnetcore;

namespace HL7.Dotnetcore.Test
{
    [TestClass]
    public class HL7Test
    {
        private string HL7_ORM;
        private string HL7_ADT;

        public static void Main(string[] args)
        {
            // var test = new HL7Test();
            // test.ParseTest2();
        }

        public HL7Test()
        {
            var path = Path.GetDirectoryName(typeof(HL7Test).GetTypeInfo().Assembly.Location) + "/";
            this.HL7_ORM = File.ReadAllText(path + "Sample-ORM.txt");
            this.HL7_ADT = File.ReadAllText(path + "Sample-ADT.txt");
        }

        [TestMethod]
        public void SmokeTest()
        {
            Message message = new Message(this.HL7_ORM);
            Assert.IsNotNull(message);

            // message.ParseMessage();
            // File.WriteAllText("SmokeTestResult.txt", message.SerializeMessage(false));
        }

        [TestMethod]
        public void ParseTest1()
        {
            var message = new Message(this.HL7_ORM);

            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
        }

        [TestMethod]
        public void ParseTest2()
        {
            var message = new Message(this.HL7_ADT);

            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
        }


        [TestMethod]
        public void ReadSegmentTest()
        {
            var message = new Message(this.HL7_ORM);
            message.ParseMessage();

            Segment MSH_1 = message.Segments("MSH")[0];
            Assert.IsNotNull(MSH_1);
        }

        [TestMethod]
        public void ReadDefaultSegmentTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            Segment MSH = message.DefaultSegment("MSH");
            Assert.IsNotNull(MSH);
        }

        [TestMethod]
        public void ReadFieldTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_9 = message.GetValue("MSH.9");
            Assert.AreEqual("ADT^O01", MSH_9);
        }

        [TestMethod]
        public void ReadComponentTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_9_1 = message.GetValue("MSH.9.1");
            Assert.AreEqual("ADT", MSH_9_1);
        }

        [TestMethod]
        public void AddComponentsTest()
        {
            var encoding = new HL7Encoding();
            
            //Create a Segment with name ZIB
            Segment newSeg = new Segment("ZIB", encoding);

            // Create Field ZIB_1
            Field ZIB_1 = new Field("ZIB1", encoding);
            // Create Field ZIB_5
            Field ZIB_5 = new Field("ZIB5", encoding);

            // Create Component ZIB.5.3
            Component com1 = new Component("ZIB.5.3_", encoding);

            // Add Component ZIB.5.3 to Field ZIB_5
            ZIB_5.AddNewComponent(com1, 3);

            // Overwrite the same field again
            ZIB_5.AddNewComponent(new Component("ZIB.5.3", encoding), 3);

            // Add Field ZIB_1 to segment ZIB, this will add a new filed to next field location, in this case first field
            newSeg.AddNewField(ZIB_1);

            // Add Field ZIB_5 to segment ZIB, this will add a new filed as 5th field of segment
            newSeg.AddNewField(ZIB_5, 5);

            // Add segment ZIB to message
            var message = new Message(this.HL7_ADT);
            message.AddNewSegment(newSeg);

            string serializedMessage = message.SerializeMessage(false);
            Assert.AreEqual("ZIB|ZIB1||||ZIB5^^ZIB.5.3\r", serializedMessage);
        }

        [TestMethod]
        public void EmptyFieldsTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var NK1 = message.DefaultSegment("NK1").GetAllFields();
            Assert.AreEqual(34, NK1.Count);
            Assert.AreEqual(string.Empty, NK1[33].Value);
        }

        [TestMethod]
        public void EncodingForOutputTest()
        {
            const string oruUrl = "domain.com/resource.html?Action=1&ID=2";  // Text with special character (&)
            
            var obx = new Segment("OBX", new HL7Encoding());
            obx.AddNewField("1");
            obx.AddNewField("RP");
            obx.AddNewField("70030^Radiologic Exam, Eye, Detection, FB^CDIRadCodes");
            obx.AddNewField("1");
            obx.AddNewField(obx.Encoding.Encode(oruUrl));  // Encoded field
            obx.AddNewField("F", 11);
            obx.AddNewField(MessageHelper.LongDateWithFractionOfSecond(DateTime.Now), 14);            

            var oru = new Message();
            oru.AddNewSegment(obx);

            var str = oru.SerializeMessage(false);

            Assert.IsFalse(str.Contains("&"));  // Should have \T\ instead
        }
        
        [TestMethod]
        public void AddFieldTest()
        {
            var enc = new HL7Encoding();
            Segment PID = new Segment("PID", enc);
            // Creates a new Field
            PID.AddNewField("1", 1);

            // Overwrites the old Field
            PID.AddNewField("2", 1);

            Message message = new Message();
            message.AddNewSegment(PID);
            var str = message.SerializeMessage(false);

            Assert.AreEqual("PID|2\r", str);
        }

        [TestMethod]
        public void GetMSH1Test()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_1 = message.GetValue("MSH.1");
            Assert.AreEqual("|", MSH_1);
        }

        [TestMethod]
        public void GetAckTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();
            var ack = message.GetACK();

            var MSH_3 = message.GetValue("MSH.3");
            var MSH_4 = message.GetValue("MSH.4");
            var MSH_5 = message.GetValue("MSH.5");
            var MSH_6 = message.GetValue("MSH.6");
            var MSH_3_A = ack.GetValue("MSH.3");
            var MSH_4_A = ack.GetValue("MSH.4");
            var MSH_5_A = ack.GetValue("MSH.5");
            var MSH_6_A = ack.GetValue("MSH.6");

            Assert.AreEqual(MSH_3, MSH_5_A);
            Assert.AreEqual(MSH_4, MSH_6_A);
            Assert.AreEqual(MSH_5, MSH_3_A);
            Assert.AreEqual(MSH_6, MSH_4_A);

            var MSH_10 = message.GetValue("MSH.10");
            var MSH_10_A = ack.GetValue("MSH.10");
            var MSA_1_1 = ack.GetValue("MSA.1");
            var MSA_1_2 = ack.GetValue("MSA.2");

            Assert.AreEqual(MSA_1_1, "AA");
            Assert.AreEqual(MSH_10, MSH_10_A);
            Assert.AreEqual(MSH_10, MSA_1_2);
        }

        [TestMethod]
        public void AddSegmentMSHTest()
        {
            var message = new Message();
            message.AddSegmentMSH("test", "sendingFacility", "test","test", "test", "ADR^A19", "test", "D", "2.5");
        }

        [TestMethod]
        public void GetNackTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var error = "Error message";
            var code = "AR";
            var ack = message.GetNACK(code, error);

            var MSH_3 = message.GetValue("MSH.3");
            var MSH_4 = message.GetValue("MSH.4");
            var MSH_5 = message.GetValue("MSH.5");
            var MSH_6 = message.GetValue("MSH.6");
            var MSH_3_A = ack.GetValue("MSH.3");
            var MSH_4_A = ack.GetValue("MSH.4");
            var MSH_5_A = ack.GetValue("MSH.5");
            var MSH_6_A = ack.GetValue("MSH.6");

            Assert.AreEqual(MSH_3, MSH_5_A);
            Assert.AreEqual(MSH_4, MSH_6_A);
            Assert.AreEqual(MSH_5, MSH_3_A);
            Assert.AreEqual(MSH_6, MSH_4_A);

            var MSH_10 = message.GetValue("MSH.10");
            var MSH_10_A = ack.GetValue("MSH.10");
            var MSA_1_1 = ack.GetValue("MSA.1");
            var MSA_1_2 = ack.GetValue("MSA.2");
            var MSA_1_3 = ack.GetValue("MSA.3");

            Assert.AreEqual(MSH_10, MSH_10_A);
            Assert.AreEqual(MSH_10, MSA_1_2);
            Assert.AreEqual(MSA_1_1, code);
            Assert.AreEqual(MSA_1_3, error);
        }

        [TestMethod]
        public void EmptyAndNullFieldsTest()
        {
            const string sampleMessage = "MSH|^~\\&|SA|SF|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\r\nEVN|A04|20110613083617||\"\"\r\n";

            var message = new Message(sampleMessage);
            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
            Assert.IsTrue(message.SegmentCount > 0);
            var evn = message.Segments("EVN")[0];
            var expectEmpty = evn.Fields(3).Value;
            Assert.AreEqual(string.Empty, expectEmpty);
            var expectNull = evn.Fields(4).Value;
            Assert.AreEqual(null, expectNull);
        }

        [TestMethod]
        public void MessageWithNullsIsReversable() 
        {
            const string sampleMessage = "MSH|^~\\&|SA|SF|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\r\nEVN|A04|20110613083617||\"\"\r\n";
            var message = new Message(sampleMessage);
            message.ParseMessage();
            var serialized = message.SerializeMessage(false);
            Assert.AreEqual(sampleMessage, serialized);
        }

        [TestMethod]
        public void RemoveSegment() 
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();
            Assert.AreEqual(message.Segments("NK1").Count, 2);
            message.RemoveSegment("NK1", 1);
            Assert.AreEqual(message.Segments("NK1").Count, 1);
            message.RemoveSegment("NK1");
            Assert.AreEqual(message.Segments("NK1").Count, 0);
        }

        [TestMethod]
        public void ParseDateTime()
        {
            var dateTest4 = "2019";
            var expectedResultDateTest4 = new DateTime(2019, 1, 1);

            var dateTest6 = "201904";
            var expectedResultDateTest6 = new DateTime(2019, 4, 1);

            var dateTest8 = "20190401";
            var expectedResultDateTest8 = new DateTime(2019, 4, 1);

            var dateTest10 = "2019040113";
            var expectedResultDateTest10 = new DateTime(2019, 4, 1, 13, 0, 0);

            var dateTest12 = "201904011341";
            var expectedResultDateTest12 = new DateTime(2019, 4, 1, 13, 41, 0);

            var dateTest14 = "20190401134151";
            var expectedResultDateTest14 = new DateTime(2019, 4, 1, 13, 41, 51);

            var dateTest16 = "2019040113415145";
            var expectedResultDateTest16 = new DateTime(2019, 4, 1, 13, 41, 51, 450);

            var dateTest19 = "2019040113415145123";
            var expectedResultDateTest19 = new DateTime(2019, 4, 1, 13, 41, 51, 450).AddTicks(TimeSpan.TicksPerMillisecond / 100 * 123);

            var result4 = MessageHelper.ParseDateTime(dateTest4);
            var result6 = MessageHelper.ParseDateTime(dateTest6);
            var result8 = MessageHelper.ParseDateTime(dateTest8);
            var result10 = MessageHelper.ParseDateTime(dateTest10);
            var result12 = MessageHelper.ParseDateTime(dateTest12);
            var result14 = MessageHelper.ParseDateTime(dateTest14);
            var result16 = MessageHelper.ParseDateTime(dateTest16);
            var result19 = MessageHelper.ParseDateTime(dateTest19);

            Assert.AreEqual(result4, expectedResultDateTest4);
            Assert.AreEqual(result6, expectedResultDateTest6);
            Assert.AreEqual(result8, expectedResultDateTest8);
            Assert.AreEqual(result10, expectedResultDateTest10);
            Assert.AreEqual(result12, expectedResultDateTest12);
            Assert.AreEqual(result14, expectedResultDateTest14);
            Assert.AreEqual(result16, expectedResultDateTest16);
            Assert.AreEqual(result19, expectedResultDateTest19);
        }
    }
}
