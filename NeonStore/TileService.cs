using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace NeonStore
{
    public static class TileService
    {
        public static void UpdateTile(AppItem project)
        {
            string img = project.ImagePath;

            // =========================
            // MEDIUM TILE
            // =========================
            XmlDocument mediumTileXml =
                TileUpdateManager.GetTemplateContent(
                    TileTemplateType.TileSquare150x150PeekImageAndText04);

            var mediumText = mediumTileXml.GetElementsByTagName("text");

            if (mediumText.Length > 0)
                mediumText[0].InnerText = project.Title;

            var mediumImage = mediumTileXml.GetElementsByTagName("image").Item(0);

            if (mediumImage != null && !string.IsNullOrEmpty(img))
            {
                ((XmlElement)mediumImage)
                    .SetAttribute("src", img);
            }

            // =========================
            // WIDE TILE
            // =========================
            XmlDocument wideTileXml =
                TileUpdateManager.GetTemplateContent(
                    TileTemplateType.TileWide310x150SmallImageAndText02);

            var wideImage = wideTileXml.GetElementsByTagName("image").Item(0);

            if (wideImage != null && !string.IsNullOrEmpty(img))
            {
                ((XmlElement)wideImage)
                    .SetAttribute("src", img);
            }

            var wideText = wideTileXml.GetElementsByTagName("text");

            if (wideText.Length > 0)
                wideText[0].InnerText = project.Title;

            if (wideText.Length > 1)
                wideText[1].InnerText = project.Subtitle;

            // =========================
            // LARGE TILE
            // =========================
            XmlDocument largeTileXml =
                TileUpdateManager.GetTemplateContent(
                    TileTemplateType.TileSquare310x310SmallImageAndText01);

            var imageNode = largeTileXml.GetElementsByTagName("image").Item(0);

            if (imageNode != null)
            {
                ((XmlElement)imageNode)
                    .SetAttribute("src", project.ImagePath);
            }

            var textNodes = largeTileXml.GetElementsByTagName("text");

            if (textNodes.Length > 0)
                textNodes[0].InnerText = project.Title;

            if (textNodes.Length > 1)
                textNodes[1].InnerText = project.Description;

            // =========================
            // COMBINE TILES
            // =========================
            IXmlNode visualNode =
                mediumTileXml.GetElementsByTagName("visual").Item(0);

            visualNode.AppendChild(
                mediumTileXml.ImportNode(
                    wideTileXml.GetElementsByTagName("binding").Item(0),
                    true));

            visualNode.AppendChild(
                mediumTileXml.ImportNode(
                    largeTileXml.GetElementsByTagName("binding").Item(0),
                    true));

            // =========================
            // SEND NOTIFICATION
            // =========================
            TileNotification tile =
                new TileNotification(mediumTileXml);

            TileUpdateManager
                .CreateTileUpdaterForApplication()
                .Update(tile);
        }
    }
}
