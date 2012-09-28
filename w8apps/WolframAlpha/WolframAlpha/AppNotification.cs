
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WolframAlpha
{
    class AppNotification
    {
        public String Heading { get; set; }
        public List<Item> Items { get; set; }

        public AppNotification(String Heading) {
            this.Heading = Heading;
            this.Items = new List<Item>();
        }

        public void AddMessage(Item it){
            this.Items.Add(it);
        }

        public class Item {
            public String Message { get; set; }
            public ICommand Command { get; set; }

            public Item(String message, ICommand cmd) {
                this.Message = message;
                this.Command = cmd;
            }
        }
    }
}
