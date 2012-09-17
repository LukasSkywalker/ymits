using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WolframAlpha
{
    public class HistoryItem
    {
        [XmlElement("Text")]
        public String Text {get; set;}

        [XmlElement("DateTime")]
        public DateTime DateTime {get; set;}

        public HistoryItem(String Text, DateTime DateTime) {
            this.Text = Text;
            this.DateTime = DateTime;
        }

        public HistoryItem() { }
    }
}
