using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace NeonStore
{
    public static class ToastService
    {
        public static void Show(string title, string message)
        {
            var template = ToastTemplateType.ToastText02;
            var xml = ToastNotificationManager.GetTemplateContent(template);

            var texts = xml.GetElementsByTagName("text");
            texts[0].AppendChild(xml.CreateTextNode(title));
            texts[1].AppendChild(xml.CreateTextNode(message));

            var toast = new ToastNotification(xml);

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
