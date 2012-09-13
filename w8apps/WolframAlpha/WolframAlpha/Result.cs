using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WolframAlpha
{
    [XmlRoot("queryresult")]
    public class QueryResult {
        [XmlAttribute("success")]
        public bool Success { get; set; }

        [XmlAttribute("error")]
        public bool Error { get; set; }

        [XmlAttribute("numpods")]
        public int NumPods { get; set; }

        [XmlAttribute("datatypes")]
        public string DataTypes { get; set; }

        [XmlAttribute("timedout")]
        public string TimedOut { get; set; }

        [XmlAttribute("timedoutpods")]
        public string TimedOutPods { get; set; }

        [XmlAttribute("timing")]
        public double Timing { get; set; }

        [XmlAttribute("parsetiming")]
        public double ParseTiming { get; set; }

        [XmlAttribute("parsetimedout")]
        public bool ParseTimedOut { get; set; }

        [XmlElement("pod", typeof(Pod))]
        public Pod[] Pods { get; set; }

        //  [XmlElement("assumption", typeof(Assumption))]
        //  public Assumption[] Assumptions { get; set; }

        //[XmlArray("sources")]
        //[XmlElement("source", typeof(Source))]
        //public Source[] Sources { get; set; }
    }

    public class Pod {
        [XmlAttribute("title")]
        public string Title { get; set; }

        [XmlAttribute("scanner")]
        public string Scanner { get; set; }

        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("error")]
        public bool Error { get; set; }

        [XmlAttribute("numsubpods")]
        public int NumSubPods { get; set; }

        [XmlElement("subpod", typeof(SubPod))]
        public SubPod[] SubPods { get; set; }

        /*
         * [XmlArray("states")]
         * [XmlElement("states", typeof(State))]
         * public State[] State { get; set; }
        */
        /*[XmlArray("infos")]
        [XmlElement("info", typeof(Info))]
        public Info[] Infos { get; set; }*/
    }

    public class SubPod {
        [XmlAttribute("title")]
        public string Title { get; set; }

        [XmlElement("plaintext")]
        public string Plaintext { get; set; }

        [XmlElement("img")]
        public Image Image { get; set; }

    }

    public class Info {
        [XmlElement("link", typeof(Link))]
        public Link Link{get; set;}
    }

    public class Link{
        [XmlAttribute("url")]
        public string Url { get; set; }
    }

    public class State
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("input")]
        public string Input { get; set; }
    }

    public class Image
    {
        [XmlAttribute("src")]
        public string Src { get; set; }

        [XmlAttribute("alt")]
        public string Alt { get; set; }

        [XmlAttribute("title")]
        public string Title { get; set; }

        [XmlAttribute("width")]
        public int Width { get; set; }

        [XmlAttribute("height")]
        public int Height { get; set; }
    }

    public class Source {
        [XmlAttribute("url")]
        public String URL { get; set; }

        [XmlAttribute("text")]
        public String Text { get; set; }
    }


    /*
     * 
     * class Assumption
    {
        public AssumptionType Type { get; set; }
        public String Desc { get; set; }
        public String Current { get; set; }
        public String Word { get; set; }
        public String Template { get; set; }
        public List<AssumptionValue> AssumptionValues { get; set; }
    }
     * 
     * class AssumptionValue
    {
        public string Name { get; set; }
        public String Desc { get; set; }
        public String Input { get; set; }
    }

    class Warning
    {
        public WarningType Type { get; set; }

        public enum WarningType
        {
            Spellcheck,
            Delimiters,
            Translation,
            Reinterpret
        }
    }

    class Generalization
    {
        public String Topic { get; set; }
        public String Desc { get; set; }
        public String URL { get; set; }
    }
    
    class State
    {
        public string Name { get; set; }
        public String Input { get; set; }
    }

    class Info
    {
        public String Text { get; set; }
        public Image Image { get; set; }
        public List<String> Links { get; set; }
    }

    enum AssumptionType
    {
        Clash,
        Unit,
        AngleUnit,
        Function,
        MultiClash,
        SubCategory,
        Attribute,
        TimeAMOrPM,
        DateOrder,
        ListOrTimes,
        ListOrNumber,
        CoordinateSystem,
        I,
        NumberBase,
        MixedFraction,
        MortalityYearDOB,
        DNAOrString,
        TideStation,
        FormulaSelect,
        FormulaSolve,
        FormulaVariable,
        FormulaVariableInclude,
        FormulaVariableOption

    }

    */
}
