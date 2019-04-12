namespace HL7.Dotnetcore
{
    public abstract class MessageElement
    {
        protected string _value = string.Empty;
       
        
        public  string Value 
        { 
            get 
            {
                return _value == Encoding.PresentButNull ? null : _value; 
            }
            set 
            { 
                _value = value; 
                ProcessValue(); 
            }
        }

        public HL7Encoding Encoding { get; protected set; }

        protected abstract void ProcessValue();
    }
}
