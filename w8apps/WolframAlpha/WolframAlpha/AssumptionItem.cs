using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolframAlpha
{
    class AssumptionStore
    {
        public Dictionary<String, List<String>> Values { get; set; }

        public AssumptionStore() {
            Values = new Dictionary<string, List<string>>();
        }

        public void Add(String type, String item)
        {
            if (!Values.ContainsKey(type)) {
                Values[type] = new List<String>();
            }
            Values[type].Add(item);
        }

        public void Clear(String type)
        {
            Values.Remove(type);
        }

        public void ClearAll() {
            Values.Clear();
        }

        public List<String> Get(String type)
        {
            return Values[type];
        }

        public String GetAll() {
            StringBuilder sb = new StringBuilder();
            foreach (List<String> ls in Values.Values) {
                foreach (String item in ls) {
                    sb.Append("&assumption="+item);
                }
            }
            return sb.ToString();
        }
    }

    public class ComboboxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

}
