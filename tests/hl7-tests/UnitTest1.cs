using HL7.Dotnetcore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;

namespace hl7_tests
{
    [TestClass]
    [DeploymentItem("Sample-ORM.txt")]
    [DeploymentItem("Sample-ADT.txt")]
    public class UnitTest1
    {
        private string HL7_ORM;
        private string HL7_ADT;

        public UnitTest1()
        {
            var path = Path.GetDirectoryName(typeof(UnitTest1).GetTypeInfo().Assembly.Location) + "/";
            this.HL7_ORM = File.ReadAllText(path + "Sample-ORM.txt");
            this.HL7_ADT = File.ReadAllText(path + "Sample-ADT.txt");
        }

        [TestMethod]
        public void SmokeTest()
        {
            Message message = new Message(this.HL7_ORM);
            Assert.IsNotNull(message);
        }

        [TestMethod]
        public void ParseTest()
        {
            var message = new Message(this.HL7_ORM);

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

            Segment MSH_1 = message.DefaultSegment("MSH");
            Assert.IsNotNull(MSH_1);
        }

        [TestMethod]
        public void ReadField()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_1_8 = message.GetValue("MSH.8");
            Assert.IsTrue(MSH_1_8.StartsWith("ADT"));
        }

        [TestMethod]
        public void ReadComponent()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_1_8_1 = message.GetValue("MSH.8.1");
            Assert.AreEqual("ADT", MSH_1_8_1);
        }

        [TestMethod]
        public void EmptyFields()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var NK1 = message.DefaultSegment("NK1").GetAllFields();
            Assert.AreEqual(34, NK1.Count);
            Assert.AreEqual(string.Empty, NK1[33].Value);
        }

        [TestMethod]
        public void EncodingForOutput()
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
    }
}
