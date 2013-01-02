using CharmFlyoutLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CobaltGamma
{
    class NotificationHandler
    {

        public static List<AppNotification> GetNotifications(QueryResult qr) {
            List<AppNotification> list = new List<AppNotification>();

            // TODO does not work for
            // Here is an example for the query "Francee splat", which suggests "Frances split" as an alternative.
            if (qr.DidYouMeans != null)
            {
                if (qr.DidYouMeans.Count > 0)
                {
                    AppNotification an = new AppNotification("Did you mean", "dym");
                    foreach (DidYouMean DidYouMean in qr.DidYouMeans)
                    {
                        AppNotification.Item it = new AppNotification.Item(DidYouMean.Value);
                        an.AddMessage(it);
                    }
                    //throw new Exception(an.Items.Count.ToString());
                    list.Add(an);
                }
            }

            if (qr.Tips != null)
            {
                if (qr.Tips.Count > 0)
                {
                    AppNotification an = new AppNotification("Tips", "tips");
                    foreach (Tip Tip in qr.Tips)
                    {
                        AppNotification.Item it = new AppNotification.Item(Tip.Text);
                        an.AddMessage(it);
                    }
                    list.Add(an);
                }
            }

            if (qr.LanguageMessage != null)
            {
                AppNotification an = new AppNotification("Language", "lang");
                AppNotification.Item it = new AppNotification.Item(qr.LanguageMessage.English);
                an.AddMessage(it);
                list.Add(an);
            }

            if (qr.FutureTopic != null)
            {
                AppNotification an = new AppNotification("Future topic", "ft");
                AppNotification.Item it = new AppNotification.Item(qr.FutureTopic.Message);
                an.AddMessage(it);
                list.Add(an);
            }

            if (qr.RelatedExamples != null)
            {
                if (qr.RelatedExamples.Count > 0)
                {
                    AppNotification an = new AppNotification("Related Example", "relex");
                    foreach (RelatedExample RelatedExample in qr.RelatedExamples)
                    {
                        AppNotification.Item it = new AppNotification.Item(RelatedExample.Description, RelatedExample.CategoryPage);
                        an.AddMessage(it);
                    }
                    list.Add(an);
                }
            }

            if (qr.ExamplePage != null)
            {
                AppNotification an = new AppNotification("Example Page", "expg");
                AppNotification.Item it = new AppNotification.Item(qr.ExamplePage.Category, qr.ExamplePage.URL);
                an.AddMessage(it);
                list.Add(an);
            }

            // Tips
            // LanguageMessage
            // FutureTopic
            // RelatedExample
            // ExamplePage

            return list;
        }
    }
}
