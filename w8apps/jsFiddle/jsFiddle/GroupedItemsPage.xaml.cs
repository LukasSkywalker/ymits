using jsFiddle.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Grouped Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234231

namespace jsFiddle
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class GroupedItemsPage : jsFiddle.Common.LayoutAwarePage
    {
        public GroupedItemsPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var sampleDataGroups = SampleDataSource.GetGroups((String)navigationParameter);
            this.DefaultViewModel["Groups"] = sampleDataGroups;
        }

        /// <summary>
        /// Invoked when a group header is clicked.
        /// </summary>
        /// <param name="sender">The Button used as a group header for the selected group.</param>
        /// <param name="e">Event data that describes how the click was initiated.</param>
        void Header_Click(object sender, RoutedEventArgs e)
        {
            // Determine what group the Button instance represents
            var group = (sender as FrameworkElement).DataContext;

            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            //this.Frame.Navigate(typeof(GroupDetailPage), ((SampleDataGroup)group).UniqueId);
        }

        /// <summary>
        /// Invoked when an item within a group is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((SampleDataItem)e.ClickedItem).UniqueId;
            //this.Frame.Navigate(typeof(ItemDetailPage), itemId);
        }

        private void ApplyColor(RichEditBox TextBox, String pattern, Windows.UI.Color color)
        {
            Regex regexp = new Regex(pattern);
            string str;
            TextBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out str);
            MatchCollection mc = regexp.Matches(str);
            foreach (Match m in mc)
            {
                int start = m.Index;
                int length = m.Length;
                TextBox.Document.GetRange(start, start + length).CharacterFormat.ForegroundColor = color;
            }
        }

        private void HtmlBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            RichEditBox tb = (RichEditBox)sender;
            ApplyColor(tb, "<\\s*(.*?)>", Windows.UI.Colors.Blue);
            ApplyColor(tb, "'(.*?)'", Windows.UI.Colors.Chocolate);
            ApplyColor(tb, "\"(.*?)\"", Windows.UI.Colors.Chocolate);
        }

        private void CssBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            RichEditBox tb = (RichEditBox)sender;
            ApplyColor(tb, "(.*?)\\s*", Windows.UI.Colors.Blue);
        }

        private void JsBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            RichEditBox tb = (RichEditBox)sender;
            string Regex = "\b(break|export|return|case|for|switch|comment|function|this|continue|if|typeof|default|import|var|delete|in|void|do|label|while|else|new|with)\b";
            ApplyColor(tb, Regex, Windows.UI.Colors.Blue);
        }

        private void CheckTab(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                RichEditBox richEditBox = sender as RichEditBox;
                if (richEditBox != null)
                {
                    richEditBox.Document.Selection.TypeText("\t");
                    e.Handled = true;
                }
            }
        }

        private async void SendCode(object sender, RoutedEventArgs e) {
            String Html;
            HtmlBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out Html);

            String Css;
            CssBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out Css);

            String Js;
            JsBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out Js);

            String page = "<html>"+
                            "<head>"+
                                "<title>Loading...</title>"+
                                "<script type='text/javascript'>"+
                                    "document.write('asdf');"+
                                    "window.onerror = window.showError;"+
                                    "senddata = function(){}" +
                                    "window.showError = function(Nachricht, Datei, Zeile){document.write('Msg='+Nachricht+' File='+Datei+' Ln='+Zeile);}"+
                                    
                                    //"window.sendData = function(){"+
                                        //"document.write('sending...');"+
                                        /*"var oMyForm = new FormData();"+

                                        "oMyForm.append('html', '"+Html+"');"+
                                        "oMyForm.append('css', '"+Css+"');" + 
                                        "oMyForm.append('js', '"+Js+"');" +

                                        "var oReq = new XMLHttpRequest();"+
                                        "oReq.open('POST', 'http://jsfiddle.net/api/post/library/pure');" +
                                        "oReq.send(oMyForm);"+*/
                                    //"}"+
                                "</script>"+
                              "</head>"+
                              "<body>"+
                                /*"<form method='POST' action='http://jsfiddle.net/api/post/library/pure'>"+
                                  "<input id='html' type='hidden' name='html' value='"+Html+"'/>"+
                                  "<input id='css' type='hidden' name='css' value='"+Css+"'/>"+
                                  "<input id='js' type='hidden' name='js' value='"+Js+"'/>"+*/
                                  "<a href='javascript:window.sendData()'>send</a><br>"+
                                /*"</form>"+*/
                              "</body>"+
                            "</html>";

            OutBox.NavigateToString(page);
        }
    }
}
