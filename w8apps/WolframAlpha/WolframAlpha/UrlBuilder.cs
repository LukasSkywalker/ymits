using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolframAlpha
{
    public class UrlBuilder
    {
        //public const string ServiceURLAssumption = "http://api.wolframalpha.com/v2/query?input={1}&appid={0}&assumption={2}";
        // ServiceURLState = "http://api.wolframalpha.com/v2/query?input={1}&appid={0}&podstate={3}@{2}&includepodid={4}&assumption={5}";
        //public const string ServiceURL = "http://api.wolframalpha.com/v2/query?appid={0}&input={1}&assumption={2}"; //&latlong={5}
        public string AppId { get; set; }
        public string Input { get; set; }
        public string Assumption { get; set; }
        public string State { get; set; }
        public string PodId { get; set; }

        private Dictionary<string, Dictionary<String, int>> StateCounter;

        public UrlBuilder() {
        }

        public UrlBuilder AddAppId(String AppId){
            this.AppId = AppId;
            return this;
        }

        public UrlBuilder AddInput(String Input) {
            this.Input = Input;
            return this;
        }

        public UrlBuilder AddAssumption(String Assumption) {
            this.Assumption = Assumption;
            return this;
        }

        public UrlBuilder AddState(String State, String PodId) {
            this.State = State;
            this.PodId = PodId;
            
            return this;
        }

        public string Build() {
            String s = String.Format("http://api.wolframalpha.com/v2/query?input={1}&appid={0}&podstate={3}@{2}&includepodid={4}&assumption={5}", AppId, Input, State, 3, PodId, Assumption);
            this.State = "";
            this.PodId = "";
            return s;
        }

    }
}
