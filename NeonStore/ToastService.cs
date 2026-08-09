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
        public static void Show(string title, string message, string imagePath = null)
        {
            var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

            var texts = xml.GetElementsByTagName("text");
            texts[0].AppendChild(xml.CreateTextNode(title));
            texts[1].AppendChild(xml.CreateTextNode(message));

            if (!string.IsNullOrEmpty(imagePath))
            {
                var images = xml.GetElementsByTagName("image");

                if (images.Length > 0)
                {
                    var image = (Windows.Data.Xml.Dom.XmlElement)images[0];
                    image.SetAttribute("src", imagePath);
                    image.SetAttribute("alt", "image");
                }
            }

            var toast = new ToastNotification(xml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public static void Show1(string title, string message)
        {
            var xml = ToastNotificationManager.GetTemplateContent(
                ToastTemplateType.ToastText02);

            var texts = xml.GetElementsByTagName("text");
            texts[0].AppendChild(xml.CreateTextNode(title));
            texts[1].AppendChild(xml.CreateTextNode(message));

            var toast = new ToastNotification(xml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
