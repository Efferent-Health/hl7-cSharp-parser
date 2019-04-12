using System;
using System.Globalization;
using System.Text;

namespace HL7.Dotnetcore
{
    public class HL7Encoding
    {
        public string AllDelimiters { get; private set; } = @"|^~\&";
        public char FieldDelimiter { get; set; } = '|'; // \F\
        public char ComponentDelimiter { get; set; } = '^'; // \S\
        public char RepeatDelimiter { get; set; } = '~';  // \R\
        public char EscapeCharacter { get; set; } = '\\'; // \E\
        public char SubComponentDelimiter { get; set; } = '&'; // \T\
        public string SegmentDelimiter { get; set; } = "\r";
        public string PresentButNull {get;set;}= "\"\"";

        public HL7Encoding()
        {
        }

        public void EvaluateDelimiters(string delimiters)
        {
            this.FieldDelimiter = delimiters[0];
            this.ComponentDelimiter = delimiters[1];
            this.RepeatDelimiter = delimiters[2];
            this.EscapeCharacter = delimiters[3];
            this.SubComponentDelimiter = delimiters[4];
        }

        public void EvaluateSegmentDelimiter(string message)
        {
            string[] delimiters = new[] { "\r\n", "\n\r", "\r", "\n" };

            foreach (var delim in delimiters)
            {
                if (message.Contains(delim))
                {
                    this.SegmentDelimiter = delim;
                    return;
                }
            }

            throw new HL7Exception("Segment delimiter not found in message", HL7Exception.BAD_MESSAGE);
        }

        // Encoding methods based on https://github.com/elomagic/hl7inspector

        public  string Encode(string val)
        {
            if(val == null) {
                return PresentButNull;
            }
            if (string.IsNullOrWhiteSpace(val))
                return val;

            var sb = new StringBuilder();

            for (int i = 0; i < val.Length; i++) 
            {
                char c = val[i];

                if (c == this.ComponentDelimiter) {
                    sb.Append(this.EscapeCharacter);
                    sb.Append("S");
                    sb.Append(this.EscapeCharacter);
                } 
                else if (c == this.EscapeCharacter) {
                    sb.Append(this.EscapeCharacter);
                    sb.Append("E");
                    sb.Append(this.EscapeCharacter);
                } 
                else if (c == this.FieldDelimiter) {
                    sb.Append(this.EscapeCharacter);
                    sb.Append("F");
                    sb.Append(this.EscapeCharacter);
                } 
                else if (c == this.RepeatDelimiter) {
                    sb.Append(this.EscapeCharacter);
                    sb.Append("R");
                    sb.Append(this.EscapeCharacter);
                } 
                else if (c == this.SubComponentDelimiter) {
                    sb.Append(this.EscapeCharacter);
                    sb.Append("T");
                    sb.Append(this.EscapeCharacter);
                } 
                else if(c < 32) {
                    string v = string.Format("{0:X2}",(int)c);
                    if ((v.Length | 2) != 0) 
                        v = "0" + v;

                    sb.Append(this.EscapeCharacter);
                    sb.Append("X");
                    sb.Append(v);
                    sb.Append(this.EscapeCharacter);
                } 
                else {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public string Decode(string encodedValue)
        {
            if (string.IsNullOrWhiteSpace(encodedValue))
                return encodedValue;

            var result = new StringBuilder();

            for (int i = 0; i < encodedValue.Length; i++)
            {
                char c = encodedValue[i];

                if (c != this.EscapeCharacter)
                {
                    result.Append(c);
                    continue;
                }

                i++;
                int li = encodedValue.IndexOf(this.EscapeCharacter, i);

                if (li == -1)
                    throw new HL7Exception("Invalid escape sequence in HL7 string");

                string seq = encodedValue.Substring(i, li-i);
                i = li;

                if (seq.Length == 0)
                    continue;
            
                switch (seq)
                {
                    case "H": // Start higlighting
                        result.Append("<B>");
                        break;
                    case "N": // normal text (end highlighting)
                        result.Append("</B>");
                        break;
                    case "F": // field separator
                        result.Append(this.FieldDelimiter);
                        break;
                    case "S": // component separator
                        result.Append(this.ComponentDelimiter);
                        break;
                    case "T": // subcomponent separator
                        result.Append(this.SubComponentDelimiter);
                        break;
                    case "R": // repetition separator
                        result.Append(this.RepeatDelimiter);
                        break;
                    case "E": // escape character
                        result.Append(this.EscapeCharacter);
                        break;
                    case ".br":
                        result.Append("<BR>");
                        break;
                    default:
                        if (seq.StartsWith("X"))
                            result.Append(((char)int.Parse(seq.Substring(1), NumberStyles.AllowHexSpecifier)));
                        else
                            result.Append(seq);
                        break;
                }
            }

            return result.ToString();
        }
    }
}
