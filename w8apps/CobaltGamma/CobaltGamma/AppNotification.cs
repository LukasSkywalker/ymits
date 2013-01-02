
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CobaltGamma
{
    class AppNotification
    {
        public String Heading { get; set; }
        public List<Item> Items { get; set; }
        public String Type { get; set; }

        public AppNotification(String Heading, String type) {
            this.Heading = Heading;
            this.Items = new List<Item>();
            this.Type = type;
        }

        public void AddMessage(Item it){
            it.Type = this.Type;
            this.Items.Add(it);
        }

        public class Item {
            public String Message { get; set; }
            public String Term { get; set; }
            public String Type { get; set; }

            public Item(String message) {
                this.Message = message;
                this.Term = message;
            }

            public Item(String message, String tag) {
                this.Message = message;
                this.Term = tag;
            }
        }
    }
}
