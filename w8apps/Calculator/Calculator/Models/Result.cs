using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calculator.Models
{
    class Result : INotifyPropertyChanged
    {
        public bool Success { get; set; }
        public bool Error {get; set; }
        public int NumPods { get; set; }
        public int TimedOutPods { get; set; }
        public string Id { get; set; }
        public List<Pod> Pods { get; set; }
        public List<Assumption> Assumptions { get; set; }
        public List<Warning> Warnings { get; set; }
        public List<Source> Sources { get; set; }
    }

    class Pod : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public String Scanner { get; set; }
        public string Id { get; set; }
        public string Error {get; set;}
        public string NumSubPods { get; set; }
        public List<SubPod> SubPods {get; set;}
        public List<State> States { get; set; }
        public List<Info> Infos { get; set; }
    }

    class SubPod : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public string PlainText { get; set; }
        public Image Image { get; set; }
    }

    class Image : INotifyPropertyChanged
    {
        public String Source { get; set; }
        public String Alt { get; set; }
        public String Title { get; set; }
        public String Width { get; set; }
        public String Height { get; set; }
    }

    class State : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public String Input { get; set; }
    }

    class Info : INotifyPropertyChanged
    {
        public String Text { get; set; }
        public Image Image { get; set; }
        public List<String> Links { get; set; }
    }

    class Assumption : INotifyPropertyChanged
    {
        public AssumptionType Type { get; set; }
        public String Desc { get; set; }
        public String Current { get; set; }
        public String Word { get; set; }
        public String Template { get; set; }
        public List<AssumptionValue> AssumptionValues { get; set; }
    }

    enum AssumptionType {
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

    class AssumptionValue : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public String Desc { get; set; }
        public String Input { get; set; }
    }

    class Warning : INotifyPropertyChanged
    {
        public WarningType Type { get; set; }

        public enum WarningType {
            Spellcheck,
            Delimiters,
            Translation,
            Reinterpret
        }
    }

    class Source : INotifyPropertyChanged
    {
        public String URL { get; set; }
        public String Text { get; set;}
    }

    class Generalization : INotifyPropertyChanged
    {
        public String Topic { get; set; }
        public String Desc { get; set; }
        public String URL { get; set; }
    }
}
