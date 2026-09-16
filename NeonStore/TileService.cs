using System;
using System.Collections.Generic;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace NeonStore
{
    public static class TileService
    {
        public static void UpdateTile(AppItem project, string tag = "SingleApp")
        {
            string img = project.ImagePath;

            // Medium Tile
            XmlDocument mediumTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150PeekImageAndText04);
            var mediumText = mediumTileXml.GetElementsByTagName("text");
            if (mediumText.Length > 0) mediumText[0].InnerText = project.Title;

            var mediumImage = mediumTileXml.GetElementsByTagName("image").Item(0);
            if (mediumImage != null && !string.IsNullOrEmpty(img))
                ((XmlElement)mediumImage).SetAttribute("src", img);

            // Wide Tile
            XmlDocument wideTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150SmallImageAndText02);
            var wideImage = wideTileXml.GetElementsByTagName("image").Item(0);
            if (wideImage != null && !string.IsNullOrEmpty(img))
                ((XmlElement)wideImage).SetAttribute("src", img);

            var wideText = wideTileXml.GetElementsByTagName("text");
            if (wideText.Length > 0) wideText[0].InnerText = project.Title;
            if (wideText.Length > 1) wideText[1].InnerText = project.Subtitle;
            if (wideText.Length > 2) wideText[2].InnerText = "Free";

            // Large Tile
            XmlDocument largeTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare310x310SmallImageAndText01);
            var imageNode = largeTileXml.GetElementsByTagName("image").Item(0);
            if (imageNode != null && !string.IsNullOrEmpty(project.ImagePath))
                ((XmlElement)imageNode).SetAttribute("src", project.ImagePath);

            var textNodes = largeTileXml.GetElementsByTagName("text");
            if (textNodes.Length > 0) textNodes[0].InnerText = project.Title;
            if (textNodes.Length > 1) textNodes[1].InnerText = project.Description;
            if (textNodes.Length > 2) textNodes[2].InnerText = "Free";

            // Combine
            IXmlNode visualNode = mediumTileXml.GetElementsByTagName("visual").Item(0);
            visualNode.AppendChild(mediumTileXml.ImportNode(wideTileXml.GetElementsByTagName("binding").Item(0), true));
            visualNode.AppendChild(mediumTileXml.ImportNode(largeTileXml.GetElementsByTagName("binding").Item(0), true));

            TileNotification tile = new TileNotification(mediumTileXml) { Tag = tag };
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tile);
        }

        public static void UpdateTopAppsTile(IList<AppItem> apps, string headerTitle = "Best rated", string tag = "TopAppsList")
        {
            if (apps == null || apps.Count == 0) return;

            AppItem featuredApp = apps[0];

            // Medium Tile
            XmlDocument mediumTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150PeekImageAndText04);
            var mediumText = mediumTileXml.GetElementsByTagName("text");
            if (mediumText.Length > 0) mediumText[0].InnerText = featuredApp.Title;

            var mediumImage = mediumTileXml.GetElementsByTagName("image").Item(0);
            if (mediumImage != null && !string.IsNullOrEmpty(featuredApp.ImagePath))
                ((XmlElement)mediumImage).SetAttribute("src", featuredApp.ImagePath);

            // Wide Tile
            XmlDocument wideTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150SmallImageAndText02);
            var wideImage = wideTileXml.GetElementsByTagName("image").Item(0);
            if (wideImage != null && !string.IsNullOrEmpty(featuredApp.ImagePath))
                ((XmlElement)wideImage).SetAttribute("src", featuredApp.ImagePath);

            var wideText = wideTileXml.GetElementsByTagName("text");
            if (wideText.Length > 0) wideText[0].InnerText = featuredApp.Title;
            if (wideText.Length > 1) wideText[1].InnerText = featuredApp.Subtitle;
            if (wideText.Length > 2) wideText[2].InnerText = "Free";

            // Large Tile (3-item summary)
            XmlDocument largeTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare310x310SmallImagesAndTextList05);
            var textElements = largeTileXml.GetElementsByTagName("text");
            var imageElements = largeTileXml.GetElementsByTagName("image");

            if (textElements.Length > 0) textElements[0].InnerText = headerTitle;

            int count = Math.Min(apps.Count, 3);
            for (int i = 0; i < count; i++)
            {
                int titleIdx = 1 + (i * 2);
                int subtitleIdx = 2 + (i * 2);

                if (titleIdx < textElements.Length) textElements[titleIdx].InnerText = apps[i].Title ?? "";
                if (subtitleIdx < textElements.Length) textElements[subtitleIdx].InnerText = "Free";

                if (i < imageElements.Length && !string.IsNullOrEmpty(apps[i].ImagePath))
                    ((XmlElement)imageElements[i]).SetAttribute("src", apps[i].ImagePath);
            }

            // Combine
            IXmlNode visualNode = mediumTileXml.GetElementsByTagName("visual").Item(0);
            visualNode.AppendChild(mediumTileXml.ImportNode(wideTileXml.GetElementsByTagName("binding").Item(0), true));
            visualNode.AppendChild(mediumTileXml.ImportNode(largeTileXml.GetElementsByTagName("binding").Item(0), true));

            TileNotification tile = new TileNotification(mediumTileXml) { Tag = tag };
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tile);
        }
    }
}