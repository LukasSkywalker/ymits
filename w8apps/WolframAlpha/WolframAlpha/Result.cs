using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

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

        [XmlElement("pod")]
        public Pod[] Pods { get; set; }

        [XmlArray("assumptions")]
        [XmlArrayItem("assumption")]
        public Assumption[] Assumptions { get; set; }

        [XmlElement("warnings")]
        public Warning[] Warnings { get; set; }

        [XmlElement("error")]
        public Error[] Errors { get; set; }

        [XmlArray("sources")]
        [XmlArrayItem("source")]
        public Source[] Sources { get; set; }

        [XmlElement("generalization")]
        public Generalization Generalization { get; set; }

        public Pod getPodAt(int Index){
            return Pods[Index];
        }

        public Pod getPodByTitle(String Title) {
            for (int i = 0; i < Pods.Length; i++)
            {
                if (Pods[i].Title.Equals(Title))
                    return Pods[i];
            }
            return null;
        }

        public Pod getPodById(String Id){
            for (int i = 0; i < Pods.Length; i++)
            {
                if (Pods[i].Id.Equals(Id))
                    return Pods[i];
            }
            return null;
        }

        public int getIndex(Pod Pod) {
            for (int i = 0; i < Pods.Length; i++) {
                if (Pods[i].Equals(Pod))
                    return i;
            }
            return -1;
        }

        public int getIndex(SubPod SubPod)
        {
            int counter = -1;
            for (int i = 0; i < Pods.Length; i++)
            {
                for (int j = 0; j < Pods[i].SubPods.Length; j++)
                {
                    counter++;
                    if (Pods[i].SubPods[j].Equals(SubPod))
                        return i;
                }
            }
            return -1;
        }

        public int getIndexByPodTitle(String Title){
            for (int i = 0; i < Pods.Length; i++)
            {
                if (Pods[i].Title.Equals(Title))
                    return i;
            }
            return -1;
        }
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

        [XmlAttribute("primary")]
        public bool Primary { get; set; }

        [XmlElement("subpod")]
        public SubPod[] SubPods { get; set; }

        [XmlArray("states")]
        [XmlArrayItem("state")]
        public State[] States { get; set; }
        
        [XmlArray("infos")]
        [XmlArrayItem("info")]
        public Info[] Infos { get; set; }
    }

    public class SubPod {
        [XmlAttribute("title")]
        public String Title { get; set; }

        [XmlElement("plaintext")]
        public String Plaintext { get; set; }

        [XmlElement("img")]
        public Image Image { get { return Image; } set { ImageSource = value.Src; } }

        public String ImageSource { get; set; }

        public State[] States { get; set; }
    }

    public class Info {
        [XmlAttribute("text")]
        public String Text { get; set; }

        [XmlElement("img")]
        public Image[] Images { get; set; }

        [XmlElement("link")]
        public Link[] Links { get; set; }

        [XmlArray("units")]
        [XmlArrayItem("unit")]
        public Unit[] Units { get; set; }
    }

    public class Link{
        [XmlAttribute("url")]
        public string Url { get; set; }

        [XmlAttribute("text")]
        public string Text { get; set; }
    }

    public class Unit {
        [XmlAttribute("short")]
        public string Short { get; set; }

        [XmlAttribute("long")]
        public string Long { get; set; }
    }

    public class State
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("input")]
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


    public class Assumption
    {
        [XmlAttribute("type")]
        public String Type { get; set; }

        [XmlElement("value")]
        public Value[] Values { get; set; }
    }

    public class Value {
        [XmlAttribute("name")]
        public String Name { get; set; }
        
        [XmlAttribute("desc")]
        public String Description { get; set; }
        
        [XmlAttribute("input")]
        public String Input { get; set; }
    }

    public class Warning {
        [XmlElement("spellcheck")]
        public Spellcheck[] Spellcheck { get; set; }

        [XmlElement("delimiters")]
        public Delimiters[] Delimiters { get; set; }

        [XmlElement("translation")]
        public Translation[] Translation { get; set; }

        [XmlElement("reinterpret")]
        public Reinterpret[] Reinterpret { get; set; }
    }

    public class Spellcheck {
        [XmlAttribute("word")]
        public String Word { get; set; }

        [XmlAttribute("suggestion")]
        public String Suggestion { get; set; }

        [XmlAttribute("text")]
        public String Text { get; set; }
    }

    public class Delimiters
    {
        [XmlAttribute("text")]
        public String Text { get; set; }
    }

    public class Translation
    {
        [XmlAttribute("phrase")]
        public String Phrase { get; set; }

        [XmlAttribute("trans")]
        public String Trans { get; set; }

        [XmlAttribute("lang")]
        public String Lang { get; set; }

        [XmlAttribute("text")]
        public String Text { get; set; }
    }

    public class Reinterpret
    {
        [XmlAttribute("text")]
        public String Text { get; set; }

        [XmlAttribute("new")]
        public String New { get; set; }

        //TODO
        //[XmlElement("alternative")]
        //public Alternative[] Alternatives { get; set; }
    }

    public class Generalization{
        [XmlAttribute("topic")]
        public String Topic { get; set; }

        [XmlAttribute("desc")]
        public String Desc { get; set; }

        [XmlAttribute("url")]
        public String Url { get; set; }
    }

    public class Error {
        [XmlElement("code")]
        public String Code { get; set; }

        [XmlElement("msg")]
        public String Message { get; set; }
    }
}
