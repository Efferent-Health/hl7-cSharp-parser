using System;
using System.Collections.Generic;

namespace HL7.Dotnetcore
{
    public class Component : MessageElement
    {
        internal List<SubComponent> SubComponentList { get; set; }

        public bool IsSubComponentized { get; set; } = false;

        public Component(HL7Encoding encoding)
        {
            this.SubComponentList = new List<SubComponent>();
            this.Encoding = encoding;
        }
        public Component(string pValue, HL7Encoding encoding)
        {
            this.SubComponentList = new List<SubComponent>();
            this.Encoding = encoding;
            this.Value = pValue;
        }

        protected override void ProcessValue()
        {
            List<string> AllSubComponents = MessageHelper.SplitString(_value, this.Encoding.SubComponentDelimiter);

            if (AllSubComponents.Count > 1)
            {
                this.IsSubComponentized = true;
            }

            SubComponentList = new List<SubComponent>();
            foreach (string strSubComponent in AllSubComponents)
            {
                SubComponent subComponent = new SubComponent(this.Encoding.Decode(strSubComponent), this.Encoding);
                SubComponentList.Add(subComponent);
            }
        }

        public SubComponent SubComponents(int position)
        {
            position = position - 1;
            SubComponent sub = null;

            try
            {
                sub = SubComponentList[position];
            }
            catch (Exception ex)
            {
                throw new HL7Exception("SubComponent not availalbe Error-" + ex.Message);
            }

            return sub;
        }

        public List<SubComponent> SubComponents()
        {
            return SubComponentList;
        }
    }

    internal class ComponentCollection : List<Component>
    {
        /// <summary>
        /// Component indexer
        /// </summary>
        internal new Component this[int index]
        {
            get
            {
                Component com = null;
                if (index < base.Count)
                    com = base[index];
                return com;
            }
            set
            {
                base[index] = value;
            }
        }

        /// <summary>
        /// Add Component at next position
        /// </summary>
        /// <param name="com">Component</param>
        internal new void Add(Component com)
        {
            base.Add(com);
        }

        /// <summary>
        /// Add component at specific position
        /// </summary>
        /// <param name="component">Component</param>
        /// <param name="position">Position</param>
        internal void Add(Component component, int position)
        {
            int listCount = base.Count;
            position = position - 1;

            if (position < listCount)
                base[position] = component;
            else
            {
                for (int comIndex = listCount; comIndex < position; comIndex++)
                {
                    Component blankCom = new Component(component.Encoding);
                    blankCom.Value = string.Empty;
                    base.Add(blankCom);
                }
                base.Add(component);
            }
        }
    }
}
