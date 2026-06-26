using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace NeonStore
{
    public static class BadgeService
    {
        public static void UpdateCount(int count)
        {
            var badgeXml = BadgeUpdateManager.GetTemplateContent(
                BadgeTemplateType.BadgeNumber);

            var badgeElement =
                (XmlElement)badgeXml.SelectSingleNode("/badge");

            badgeElement.SetAttribute("value", count.ToString());

            var badge = new BadgeNotification(badgeXml);

            BadgeUpdateManager
                .CreateBadgeUpdaterForApplication()
                .Update(badge);
        }

        public static void Clear()
        {
            BadgeUpdateManager
                .CreateBadgeUpdaterForApplication()
                .Clear();
        }
    }
}
