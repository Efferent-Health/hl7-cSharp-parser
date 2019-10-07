using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HL7.Dotnetcore
{
    public class Message
    {
        private List<string> allSegments = null;
        internal Dictionary<string, List<Segment>> SegmentList { get; set;} = new Dictionary<string, List<Segment>>();

        public string HL7Message { get; set; }
        public string Version { get; set; }
        public string MessageStructure { get; set; }
        public string MessageControlID { get; set; }
        public string ProcessingID { get; set; }
        public short SegmentCount { get; set; }
        public HL7Encoding Encoding { get; set; } = new HL7Encoding();

        public Message()
        {
        }

        public Message(string strMessage)
        {
            HL7Message = strMessage;
        }

        public override bool Equals(object obj)
        {
            if (obj is Message)
                return this.Equals((obj as Message).HL7Message);

            if (obj is string)
            {
                var arr1 = MessageHelper.SplitString(this.HL7Message, this.Encoding.SegmentDelimiter, StringSplitOptions.RemoveEmptyEntries);
                var arr2 = MessageHelper.SplitString(obj as string, this.Encoding.SegmentDelimiter, StringSplitOptions.RemoveEmptyEntries);

                return arr1.SequenceEqual(arr2);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.HL7Message.GetHashCode();
        }

        /// <summary>
        /// Parse the HL7 message in text format, throws HL7Exception if error occurs
        /// </summary>
        /// <returns>boolean</returns>
        public bool ParseMessage()
        {
            bool isValid = false;
            bool isParsed = false;

            try
            {
                isValid = this.validateMessage();
            }
            catch (HL7Exception ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new HL7Exception("Unhandled Exception in validation - " + ex.Message, HL7Exception.BAD_MESSAGE);
            }

            if (isValid)
            {
                try
                {
                    if (this.allSegments == null || this.allSegments.Count <= 0)
                        this.allSegments = MessageHelper.SplitMessage(HL7Message);

                    short SegSeqNo = 0;

                    foreach (string strSegment in this.allSegments)
                    {
                        if (string.IsNullOrWhiteSpace(strSegment))
                            continue;

                        Segment newSegment = new Segment(this.Encoding);
                        string segmentName = strSegment.Substring(0, 3);
                        newSegment.Name = segmentName;
                        newSegment.Value = strSegment;
                        newSegment.SequenceNo = SegSeqNo++;

                        this.AddNewSegment(newSegment);
                    }

                    this.SegmentCount = SegSeqNo;

                    string strSerializedMessage = string.Empty;

                    try
                    {
                        strSerializedMessage = this.SerializeMessage(false); 
                    }
                    catch (HL7Exception ex)
                    {
                        throw new HL7Exception("Failed to serialize parsed message with error - " + ex.Message, HL7Exception.PARSING_ERROR);
                    }

                    if (!string.IsNullOrEmpty(strSerializedMessage))
                    {
                        if (this.Equals(strSerializedMessage))
                            isParsed = true;
                    }
                    else
                    {
                        throw new HL7Exception("Unable to serialize to original message - ", HL7Exception.PARSING_ERROR);
                    }
                }
                catch (Exception ex)
                {
                    throw new HL7Exception("Failed to parse the message with error - " + ex.Message, HL7Exception.PARSING_ERROR);
                }
            }
            
            return isParsed;
        }

        /// <summary>
        /// Serialize the message in text format
        /// </summary>
        /// <param name="validate">Validate the message before serializing</param>
        /// <returns>string with HL7 message</returns>
        public string SerializeMessage(bool validate)
        {
            if (validate && !this.validateMessage())
                throw new HL7Exception("Failed to validate the updated message", HL7Exception.BAD_MESSAGE);

            var strMessage = new StringBuilder();
            string currentSegName = string.Empty;
            List<Segment> _segListOrdered = getAllSegmentsInOrder();

            try
            {
                try
                {
                    foreach (Segment seg in _segListOrdered)
                    {
                        currentSegName = seg.Name;
                        strMessage.Append(seg.Name).Append(Encoding.FieldDelimiter);

                        int startField = currentSegName == "MSH" ? 1 : 0;

                        for (int i = startField; i<seg.FieldList.Count; i++)
                        {
                            if (i > startField)
                                strMessage.Append(Encoding.FieldDelimiter);

                            var field = seg.FieldList[i];

                            if (field.IsDelimiters)
                            {
                                strMessage.Append(field.Value);
                                continue;
                            }

                            if (field.HasRepetitions)
                            {
                                for (int j = 0; j < field.RepeatitionList.Count; j++)
                                {
                                    if (j > 0)
                                        strMessage.Append(Encoding.RepeatDelimiter);

                                    serializeField(field.RepeatitionList[j], strMessage);
                                }
                            }
                            else
                                serializeField(field, strMessage);
                        }
                        
                        strMessage.Append(Encoding.SegmentDelimiter);
                    }
                }
                catch (Exception ex)
                {
                    if (currentSegName == "MSH")
                        throw new HL7Exception("Failed to serialize the MSH segment with error - " + ex.Message, HL7Exception.SERIALIZATION_ERROR);
                    else 
                        throw;
                }

                return strMessage.ToString();
            }
            catch (Exception ex)
            {
                throw new HL7Exception("Failed to serialize the message with error - " + ex.Message, HL7Exception.SERIALIZATION_ERROR);
            }
        }

        /// <summary>
        /// Get the Value of specific Field/Component/SubCpomponent, throws error if field/component index is not valid
        /// </summary>
        /// <param name="strValueFormat">Field/Component position in format SEGMENTNAME.FieldIndex.ComponentIndex.SubComponentIndex example PID.5.2</param>
        /// <returns>Value of specified field/component/subcomponent</returns>
        public string GetValue(string strValueFormat)
        {
            bool isValid = false;

            string segmentName = string.Empty;
            int fieldIndex = 0;
            int componentIndex = 0;
            int subComponentIndex = 0;
            int comCount = 0;
            string strValue = string.Empty;

            List<string> allComponents = MessageHelper.SplitString(strValueFormat, new char[] { '.' });
            comCount = allComponents.Count;

            isValid = validateValueFormat(allComponents);

            if (isValid)
            {
                segmentName = allComponents[0];

                if (SegmentList.ContainsKey(segmentName))
                {
                    if (comCount == 4)
                    {
                        Int32.TryParse(allComponents[1], out fieldIndex);
                        Int32.TryParse(allComponents[2], out componentIndex);
                        Int32.TryParse(allComponents[3], out subComponentIndex);

                        try
                        {
                            strValue = SegmentList[segmentName].First().FieldList[fieldIndex - 1].ComponentList[componentIndex - 1].SubComponentList[subComponentIndex - 1].Value;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("SubComponent not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                    else if (comCount == 3)
                    {
                        Int32.TryParse(allComponents[1], out fieldIndex);
                        Int32.TryParse(allComponents[2], out componentIndex);

                        try
                        {
                            strValue = SegmentList[segmentName].First().FieldList[fieldIndex - 1].ComponentList[componentIndex - 1].Value;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("Component not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                    else if (comCount == 2)
                    {
                        Int32.TryParse(allComponents[1], out fieldIndex);

                        try
                        {
                            strValue = SegmentList[segmentName].First().FieldList[fieldIndex - 1].Value;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("Field not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                            strValue = SegmentList[segmentName].First().Value;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("Segment value not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                }
                else
                {
                    throw new HL7Exception("Segment name not available: " + strValueFormat);
                }
            }
            else
            {
                throw new HL7Exception("Request format is not valid: " + strValueFormat);
            }

            return strValue;
        }

        /// <summary>
        /// Sets the Value of specific Field/Component/SubCpomponent, throws error if field/component index is not valid
        /// </summary>
        /// <param name="strValueFormat">Field/Component position in format SEGMENTNAME.FieldIndex.ComponentIndex.SubComponentIndex example PID.5.2</param>
        /// <param name="strValue">Value for the specified field/component</param>
        /// <returns>boolean</returns>
        public bool SetValue(string strValueFormat, string strValue)
        {
            bool isValid = false;
            bool isSet = false;

            string segmentName = string.Empty;
            int fieldIndex = 0;
            int componentIndex = 0;
            int subComponentIndex = 0;
            int comCount = 0;

            List<string> AllComponents = MessageHelper.SplitString(strValueFormat, new char[] { '.' });
            comCount = AllComponents.Count;

            isValid = validateValueFormat(AllComponents);

            if (isValid)
            {
                segmentName = AllComponents[0];
                if (SegmentList.ContainsKey(segmentName))
                {
                    if (comCount == 4)
                    {
                        Int32.TryParse(AllComponents[1], out fieldIndex);
                        Int32.TryParse(AllComponents[2], out componentIndex);
                        Int32.TryParse(AllComponents[3], out subComponentIndex);

                        try
                        {
                            SegmentList[segmentName].First().FieldList[fieldIndex - 1].ComponentList[componentIndex - 1].SubComponentList[subComponentIndex - 1].Value = strValue;
                            isSet = true;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("SubComponent not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                    else if (comCount == 3)
                    {
                        Int32.TryParse(AllComponents[1], out fieldIndex);
                        Int32.TryParse(AllComponents[2], out componentIndex);

                        try
                        {
                            SegmentList[segmentName].First().FieldList[fieldIndex - 1].ComponentList[componentIndex - 1].Value = strValue;
                            isSet = true;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("Component not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                    else if (comCount == 2)
                    {
                        Int32.TryParse(AllComponents[1], out fieldIndex);
                        try
                        {
                            SegmentList[segmentName].First().FieldList[fieldIndex - 1].Value = strValue;
                            isSet = true;
                        }
                        catch (Exception ex)
                        {
                            throw new HL7Exception("Field not available - " + strValueFormat + " Error: " + ex.Message);
                        }
                    }
                    else
                    {
                        throw new HL7Exception("Cannot overwrite a segment value");
                    }
                }
                else
                    throw new HL7Exception("Segment name not available");
            }
            else
                throw new HL7Exception("Request format is not valid");

            return isSet;
        }

        /// <summary>
        /// check if specified field has components
        /// </summary>
        /// <param name="strValueFormat">Field/Component position in format SEGMENTNAME.FieldIndex.ComponentIndex.SubComponentIndex example PID.5.2</param>
        /// <returns>boolean</returns>
        public bool IsComponentized(string strValueFormat)
        {
            bool isComponentized = false;
            bool isValid = false;

            string segmentName = string.Empty;
            int fieldIndex = 0;
            int comCount = 0;

            List<string> AllComponents = MessageHelper.SplitString(strValueFormat, new char[] { '.' });
            comCount = AllComponents.Count;

            isValid = validateValueFormat(AllComponents);

            if (isValid)
            {
                segmentName = AllComponents[0];
                if (comCount >= 2)
                {
                    try
                    {
                        Int32.TryParse(AllComponents[1], out fieldIndex);

                        isComponentized = SegmentList[segmentName].First().FieldList[fieldIndex - 1].IsComponentized;
                    }
                    catch (Exception ex)
                    {
                        throw new HL7Exception("Field not available - " + strValueFormat + " Error: " + ex.Message);
                    }
                }
                else
                    throw new HL7Exception("Field not identified in request");
            }
            else
                throw new HL7Exception("Request format is not valid");

            return isComponentized;
        }

        /// <summary>
        /// check if specified fields has repeatitions
        /// </summary>
        /// <param name="strValueFormat">Field/Component position in format SEGMENTNAME.FieldIndex.ComponentIndex.SubComponentIndex example PID.5.2</param>
        /// <returns>boolean</returns>
        public bool HasRepetitions(string strValueFormat)
        {
            bool hasRepetitions = false;
            bool isValid = false;

            string segmentName = string.Empty;
            int fieldIndex = 0;
            int comCount = 0;

            List<string> AllComponents = MessageHelper.SplitString(strValueFormat, new char[] { '.' });
            comCount = AllComponents.Count;

            isValid = validateValueFormat(AllComponents);

            if (isValid)
            {
                segmentName = AllComponents[0];
                if (comCount >= 2)
                {
                    try
                    {
                        Int32.TryParse(AllComponents[1], out fieldIndex);

                        hasRepetitions = SegmentList[segmentName].First().FieldList[fieldIndex - 1].HasRepetitions;
                    }
                    catch (Exception ex)
                    {
                        throw new HL7Exception("Field not available - " + strValueFormat + " Error: " + ex.Message);
                    }
                }
                else
                    throw new HL7Exception("Field not identified in request");
            }
            else
                throw new HL7Exception("Request format is not valid");

            return hasRepetitions;
        }

        /// <summary>
        /// check if specified component has sub components
        /// </summary>
        /// <param name="strValueFormat">Field/Component position in format SEGMENTNAME.FieldIndex.ComponentIndex.SubComponentIndex example PID.5.2</param>
        /// <returns>boolean</returns>
        public bool IsSubComponentized(string strValueFormat)
        {
            bool isSubComponentized = false;
            bool isValid = false;

            string segmentName = string.Empty;
            int fieldIndex = 0;
            int componentIndex = 0;
            int comCount = 0;

            List<string> AllComponents = MessageHelper.SplitString(strValueFormat, new char[] { '.' });
            comCount = AllComponents.Count;

            isValid = validateValueFormat(AllComponents);

            if (isValid)
            {
                segmentName = AllComponents[0];

                if (comCount >= 3)
                {
                    try
                    {
                        Int32.TryParse(AllComponents[1], out fieldIndex);
                        Int32.TryParse(AllComponents[2], out componentIndex);
                        isSubComponentized = SegmentList[segmentName].First().FieldList[fieldIndex - 1].ComponentList[componentIndex - 1].IsSubComponentized;
                    }
                    catch (Exception ex)
                    {
                        throw new HL7Exception("Component not available - " + strValueFormat + " Error: " + ex.Message);
                    }
                }
                else
                    throw new HL7Exception("Component not identified in request");
            }
            else
                throw new HL7Exception("Request format is not valid");

            return isSubComponentized;
        }

        /// <summary>
        /// Builds the acknowledgement message for this message
        /// </summary>
        /// <returns>An ACK message if success, otherwise null</returns>
        public Message GetACK()
        {
            return this.createAckMessage("AA", false, null);
        }

        /// <summary>
        /// Builds a negative ack for this message
        /// </summary>
        /// <param name="code">ack code like AR, AE</param>
        /// <param name="errMsg">Error message to be sent with NACK</param>
        /// <returns>A NACK message if success, otherwise null</returns>
        public Message GetNACK(string code, string errMsg)
        {
            return this.createAckMessage(code, true, errMsg);
        }

        /// <summary>
        /// Adds a segemnt to the message
        /// </summary>
        /// <param name="newSegment">Segment to be appended to the end of the message</param>
        /// <returns>True if added sucessfully, otherwise false</returns>
        public bool AddNewSegment(Segment newSegment)
        {
            try
            {
                newSegment.SequenceNo = SegmentCount++;

                if (!SegmentList.ContainsKey(newSegment.Name))
                    SegmentList[newSegment.Name] = new List<Segment>();

                SegmentList[newSegment.Name].Add(newSegment);
                return true;
            }
            catch (Exception ex)
            {
                SegmentCount--;
                throw new HL7Exception("Unable to add new segment. Error - " + ex.Message);
            }
        }

        /// <summary>
        /// Removes a segment from the message
        /// </summary>
        /// <param name="segmentName">Segment to be removed/param>
        /// <param name="index">Zero-based index of the sement to be removed, in case of multiple. Default is 0.</param>
        /// <returns>True if found and removed sucessfully, otherwise false</returns>
        public bool RemoveSegment(string segmentName, int index = 0) 
        {
            try
            {
                if (!SegmentList.ContainsKey(segmentName))
                    return false;

                var list = SegmentList[segmentName];
                if (list.Count <= index)
                    return false;

                list.RemoveAt(index);
                return true;
            }
            catch (Exception ex)
            {
                throw new HL7Exception("Unable to add remove segment. Error - " + ex.Message);
            }
        }

        public List<Segment> Segments()
        {
            return getAllSegmentsInOrder();
        }

        public List<Segment> Segments(string segmentName)
        {
            return getAllSegmentsInOrder().FindAll(o=> o.Name.Equals(segmentName));
        }

        public Segment DefaultSegment(string segmentName)
        {
            return getAllSegmentsInOrder().First(o => o.Name.Equals(segmentName));
        }

        /// <summary>
        /// Addsthe header segment to a new message
        /// </summary>
        /// <param name="sendingApplication">Sending application name</param>
        /// <param name="sendingFacility">Sending facility name</param>
        /// <param name="receivingApplication">Receiving application name</param>
        /// <param name="receivingFacility">Receiving facility name</param>
        /// <param name="security">Security features. Can be null.</param>
        /// <param name="messageType">Message type ^ trigger event</param>
        /// <param name="messageControlID">Message control unique ID</param>
        /// <param name="processingID">Processing ID ^ processing mode</param>
        /// <param name="version">HL7 message version (2.x)</param>
        public void AddSegmentMSH(string sendingApplication, string sendingFacility, string receivingApplication, string receivingFacility,
            string security, string messageType, string messageControlID, string processingID, string version)
        {
                var dateString = MessageHelper.LongDateWithFractionOfSecond(DateTime.Now);
                var delim = this.Encoding.FieldDelimiter;

                string response = "MSH" + this.Encoding.AllDelimiters + delim + sendingApplication + delim + sendingFacility + delim 
                    + receivingApplication + delim + receivingFacility + delim
                    + dateString + delim + (security ?? string.Empty) + delim + messageType + delim + messageControlID + delim 
                    + processingID + delim + version + this.Encoding.SegmentDelimiter;

                var message = new Message(response);
                message.ParseMessage();
                this.AddNewSegment(message.DefaultSegment("MSH"));
        }

        /// <summary>
        /// Builds an ACK or NACK message for this message
        /// </summary>
        /// <param name="code">ack code like AA, AR, AE</param>
        /// <param name="isNack">true for generating a NACK message, otherwise false</param>
        /// <param name="errMsg">error message to be sent with NACK</param>
        /// <returns>An ACK or NACK message if success, otherwise null</returns>
        private Message createAckMessage(string code, bool isNack, string errMsg)
        {
            var response = new StringBuilder();

            if (this.MessageStructure != "ACK")
            {
                var dateString = MessageHelper.LongDateWithFractionOfSecond(DateTime.Now);
                var msh = this.SegmentList["MSH"].First();
                var delim = this.Encoding.FieldDelimiter;
                
                response.Append("MSH").Append(this.Encoding.AllDelimiters).Append(delim).Append(msh.FieldList[4].Value).Append(delim).Append(msh.FieldList[5].Value).Append(delim)
                    .Append(msh.FieldList[2].Value).Append(delim).Append(msh.FieldList[3].Value).Append(delim)
                    .Append(dateString).Append(delim).Append(delim).Append("ACK").Append(delim).Append(this.MessageControlID).Append(delim)
                    .Append(this.ProcessingID).Append(delim).Append(this.Version).Append(this.Encoding.SegmentDelimiter);
                
                response.Append("MSA").Append(delim).Append(code).Append(delim).Append(this.MessageControlID).Append((isNack ? delim + errMsg : string.Empty)).Append(this.Encoding.SegmentDelimiter);
            }
            else
            {
                return null;
            }

            try 
            {
                var message = new Message(response.ToString());
                message.ParseMessage();
                return message;
            }
            catch 
            {
                return null;
            }
        }

        /// <summary>
        /// Validates the HL7 message for basic syntax
        /// </summary>
        /// <returns>A boolean indicating whether the whole message is valid or not</returns>
        private bool validateMessage()
        {
            try
            {
                if (!string.IsNullOrEmpty(HL7Message))
                {
                    //check message length - MSH+Delimeters+12Fields in MSH
                    if (HL7Message.Length < 20)
                    {
                        throw new HL7Exception("Message Length too short: " + HL7Message.Length + " chars.", HL7Exception.BAD_MESSAGE);
                    }

                    //check if message starts with header segment
                    if (!HL7Message.StartsWith("MSH"))
                    {
                        throw new HL7Exception("MSH segment not found at the beggining of the message", HL7Exception.BAD_MESSAGE);
                    }

                    this.Encoding.EvaluateSegmentDelimiter(this.HL7Message);
                    this.HL7Message = string.Join(this.Encoding.SegmentDelimiter, MessageHelper.SplitMessage(this.HL7Message)) + this.Encoding.SegmentDelimiter;

                    //check Segment Name & 4th character of each segment
                    char fourthCharMSH = HL7Message[3];
                    this.allSegments = MessageHelper.SplitMessage(HL7Message);

                    foreach (string strSegment in this.allSegments)
                    {
                        if (string.IsNullOrWhiteSpace(strSegment))
                            continue;

                        bool isValidSegName = false;
                        string segmentName = strSegment.Substring(0, 3);
                        string segNameRegEx = "[A-Z][A-Z][A-Z1-9]";
                        isValidSegName = System.Text.RegularExpressions.Regex.IsMatch(segmentName, segNameRegEx);

                        if (!isValidSegName)
                        {
                            throw new HL7Exception("Invalid segment name found: " + strSegment, HL7Exception.BAD_MESSAGE);
                        }

                        char fourthCharSEG = strSegment[3];

                        if (fourthCharMSH != fourthCharSEG)
                        {
                            throw new HL7Exception("Invalid segment found: " + strSegment, HL7Exception.BAD_MESSAGE);
                        }
                    }

                    string _fieldDelimiters_Message = this.allSegments[0].Substring(3, 8 - 3);
                    this.Encoding.EvaluateDelimiters(_fieldDelimiters_Message);

                    // Count field separators, MSH.12 is required so there should be at least 11 field separators in MSH
                    int countFieldSepInMSH = this.allSegments[0].Count(f => f == Encoding.FieldDelimiter);

                    if (countFieldSepInMSH < 11)
                    {
                        throw new HL7Exception("MSH segment doesn't contain all the required fields", HL7Exception.BAD_MESSAGE);
                    }

                    // Find Message Version
                    var MSHFields = MessageHelper.SplitString(this.allSegments[0], Encoding.FieldDelimiter);

                    if (MSHFields.Count >= 12)
                    {
                        this.Version = MessageHelper.SplitString(MSHFields[11], Encoding.ComponentDelimiter)[0];
                    }
                    else
                    {
                        throw new HL7Exception("HL7 version not found in the MSH segment", HL7Exception.REQUIRED_FIELD_MISSING);
                    }

                    //Find Message Type & Trigger Event
                    try
                    {
                        string MSH_9 = MSHFields[8];

                        if (!string.IsNullOrEmpty(MSH_9))
                        {
                            var MSH_9_comps = MessageHelper.SplitString(MSH_9, this.Encoding.ComponentDelimiter);

                            if (MSH_9_comps.Count >= 3)
                            {
                                this.MessageStructure = MSH_9_comps[2];
                            }
                            else if (MSH_9_comps.Count > 0 && MSH_9_comps[0] != null && MSH_9_comps[0].Equals("ACK"))
                            {
                                this.MessageStructure = "ACK";
                            }
                            else if (MSH_9_comps.Count == 2)
                            {
                                this.MessageStructure = MSH_9_comps[0] + "_" + MSH_9_comps[1];
                            }
                            else
                            {
                                throw new HL7Exception("Message Type & Trigger Event value not found in message", HL7Exception.UNSUPPORTED_MESSAGE_TYPE);
                            }
                        }
                        else
                            throw new HL7Exception("MSH.10 not available", HL7Exception.UNSUPPORTED_MESSAGE_TYPE);
                    }
                    catch (System.IndexOutOfRangeException e)
                    {
                        throw new HL7Exception("Can't find message structure (MSH.9.3) - " + e.Message, HL7Exception.UNSUPPORTED_MESSAGE_TYPE);
                    }

                    try
                    {
                        this.MessageControlID = MSHFields[9];

                        if (string.IsNullOrEmpty(this.MessageControlID))
                            throw new HL7Exception("MSH.10 - Message Control ID not found", HL7Exception.REQUIRED_FIELD_MISSING);
                    }
                    catch (Exception ex)
                    {
                        throw new HL7Exception("Error occured while accessing MSH.10 - " + ex.Message, HL7Exception.REQUIRED_FIELD_MISSING);
                    }

                    try
                    {
                        this.ProcessingID = MSHFields[10];

                        if (string.IsNullOrEmpty(this.ProcessingID))
                            throw new HL7Exception("MSH.11 - Processing ID not found", HL7Exception.REQUIRED_FIELD_MISSING);
                    }
                    catch (Exception ex)
                    {
                        throw new HL7Exception("Error occured while accessing MSH.11 - " + ex.Message, HL7Exception.REQUIRED_FIELD_MISSING);
                    }
                }
                else
                    throw new HL7Exception("No Message Found", HL7Exception.BAD_MESSAGE);
            }
            catch (Exception ex)
            {
                throw new HL7Exception("Failed to validate the message with error - " + ex.Message, HL7Exception.BAD_MESSAGE);
            }

            return true;
        }

        /// <summary>
        /// Serializes a field into a string with proper encoding
        /// </summary>
        /// <returns>A serialized string</returns>
        private void serializeField(Field field, StringBuilder strMessage)
        {
            if (field.ComponentList.Count > 0)
            {
                int indexCom = 0;

                foreach (Component com in field.ComponentList)
                {
                    indexCom++;
                    if (com.SubComponentList.Count > 0)
                        strMessage.Append(string.Join(Encoding.SubComponentDelimiter.ToString(), com.SubComponentList.Select(sc => Encoding.Encode(sc.Value))));
                    else
                        strMessage.Append(Encoding.Encode(com.Value));

                    if (indexCom < field.ComponentList.Count)
                        strMessage.Append(Encoding.ComponentDelimiter);
                }
            }
            else
                strMessage.Append(Encoding.Encode(field.Value));

        }

        /// <summary> 
        /// Get all segments in order as they appear in original message. This the usual order: IN1|1 IN2|1 IN1|2 IN2|2
        /// </summary>
        /// <returns>A list of segments in the proper order</returns>
        private List<Segment> getAllSegmentsInOrder()
        {
            List<Segment> _list = new List<Segment>();

            foreach (string segName in SegmentList.Keys)
            {
                foreach (Segment seg in SegmentList[segName])
                {
                    _list.Add(seg);
                }
            }

            return _list.OrderBy(o => o.SequenceNo).ToList();
        }

        /// <summary>
        /// Validates the components of a value's position descriptor
        /// </summary>
        /// <returns>A boolean indicating whether all the components are valid or not</returns>
        private bool validateValueFormat(List<string> allComponents)
        {
            string segNameRegEx = "[A-Z][A-Z][A-Z1-9]";
            string otherRegEx = @"^[1-9]([0-9]{1,2})?$";
            bool isValid = false;

            if (allComponents.Count > 0)
            {
                if (Regex.IsMatch(allComponents[0], segNameRegEx))
                {
                    for (int i = 1; i < allComponents.Count; i++)
                    {
                        if (Regex.IsMatch(allComponents[i], otherRegEx))
                            isValid = true;
                        else
                            return false;
                    }
                }
                else
                {
                    isValid = false;
                }
            }

            return isValid;
        }
    }
}
