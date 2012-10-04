using CharmFlyoutLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WolframAlpha
{
    class NotificationHandler
    {

        public static AppNotification GetNotifications(QueryResult qr) {
            List<AppNotification> list = new List<AppNotification>();

            AppNotification an = new AppNotification("Did you mean");

            // TODO does not work for
            // Here is an example for the query "Francee splat", which suggests "Frances split" as an alternative.
            if (qr.DidYouMeans != null)
            {
                foreach (DidYouMean DidYouMean in qr.DidYouMeans)
                {
                    AppNotification.Item it = new AppNotification.Item(DidYouMean.Value);
                    an.AddMessage(it);
                }
            }

            if (qr.Tips != null)
            {
                foreach (Tip Tip in qr.Tips)
                {
                    AppNotification.Item it = new AppNotification.Item(Tip.Text);
                    an.AddMessage(it);
                }
            }

            if (qr.LanguageMessage != null)
            {
                AppNotification.Item it = new AppNotification.Item(qr.LanguageMessage.English);
                an.AddMessage(it);
            }

            if (qr.FutureTopic != null)
            {
                AppNotification.Item it = new AppNotification.Item(qr.FutureTopic.Message);
                an.AddMessage(it);
            }

            if (qr.RelatedExamples != null)
            {
                foreach (RelatedExample RelatedExample in qr.RelatedExamples)
                {
                    AppNotification.Item it = new AppNotification.Item(RelatedExample.Description);
                    an.AddMessage(it);
                }
            }

            if (qr.ExamplePage != null)
            {
                AppNotification.Item it = new AppNotification.Item(qr.ExamplePage.Category);
                an.AddMessage(it);
            }

            // Tips
            // LanguageMessage
            // FutureTopic
            // RelatedExample
            // ExamplePage

            return an;
        }
    }
}
