using System;
using System.IO;
using System.Reflection;
using Xunit;

using HL7.Dotnetcore;

namespace HL7.Dotnetcore.Test
{
    public class HL7Test
    {
        private string HL7_ORM;
        private string HL7_ADT;

        public static void Main()
        {
            // var test = new HL7Test();
        }

        public HL7Test()
        {
            var path = Path.GetDirectoryName(typeof(HL7Test).GetTypeInfo().Assembly.Location) + "/";
            this.HL7_ORM = File.ReadAllText(path + "Sample-ORM.txt");
            this.HL7_ADT = File.ReadAllText(path + "Sample-ADT.txt");
        }

        [Fact]
        public void SmokeTest()
        {
            Message message = new Message(this.HL7_ORM);
            Assert.NotNull(message);
        }

        [Fact]
        public void ParseTest()
        {
            var message = new Message(this.HL7_ORM);

            var isParsed = message.ParseMessage();
            Assert.True(isParsed);
        }

        [Fact]
        public void ReadSegmentTest()
        {
            var message = new Message(this.HL7_ORM);
            message.ParseMessage();

            Segment MSH_1 = message.Segments("MSH")[0];
            Assert.NotNull(MSH_1);
        }

        [Fact]
        public void ReadDefaultSegmentTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            Segment MSH_1 = message.DefaultSegment("MSH");
            Assert.NotNull(MSH_1);
        }

        [Fact]
        public void ReadField()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_1_8 = message.GetValue("MSH.8");
            Assert.StartsWith("ADT", MSH_1_8);
        }

        [Fact]
        public void ReadComponent()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_1_8_1 = message.GetValue("MSH.8.1");
            Assert.Equal("ADT", MSH_1_8_1);
        }

        [Fact]
        public void AddComponents()
        {
            //Create a Segment with name ZIB
            Segment newSeg = new Segment("ZIB", new HL7Encoding());

            // Create Field ZIB_1
            Field ZIB_1 = new Field("ZIB1", new HL7Encoding());
            // Create Field ZIB_5
            Field ZIB_5 = new Field("ZIB5", new HL7Encoding());

            // Create Component ZIB.5.2
            Component com1 = new Component("ZIB.5.2", new HL7Encoding());

            // Add Component ZIB.5.2 to Field ZIB_5
            // 2nd parameter here specifies the component position, for inserting segment on particular position
            // If we don’t provide 2nd parameter, component will be inserted to next position (if field has 2 components this will be 3rd, 
            // If field is empty this will be 1st component
            ZIB_5.AddNewComponent(com1, 2);

            // Add Field ZIB_1 to segment ZIB, this will add a new filed to next field location, in this case first field
            newSeg.AddNewField(ZIB_1);

            // Add Field ZIB_5 to segment ZIB, this will add a new filed as 5th field of segment
            newSeg.AddNewField(ZIB_5, 5);

            // Add segment ZIB to message
            var message = new Message(this.HL7_ADT);
            message.AddNewSegment(newSeg);

            string serializedMessage = message.SerializeMessage(false);
            Assert.Equal("ZIB|ZIB1||||ZIB5^ZIB.5.2\r", serializedMessage);
        }

        [Fact]
        public void EmptyFields()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var NK1 = message.DefaultSegment("NK1").GetAllFields();
            Assert.Equal(34, NK1.Count);
            Assert.Equal(string.Empty, NK1[33].Value);
        }

        [Fact]
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

            Assert.DoesNotContain("&", str);  // Should have \T\ instead
        }
    }
}
