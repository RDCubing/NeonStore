using System;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace NeonStore
{
    public static class ColorService
    {
        private const string Key = "AccentColor";

        public static void SetAccentHex(string hex)
        {
            ApplicationData.Current.LocalSettings.Values[Key] = hex;
            // optional: apply instantly
            ThemeManager.ApplyAccent();
        }

        public static string GetAccentHex()
        {
            object value = ApplicationData.Current.LocalSettings.Values[Key];
            return value != null ? value.ToString() : "#0F4C4C";
        }

        public static Color GetAccentColor()
        {
            return HexToColor(GetAccentHex());
        }

        public static SolidColorBrush GetBrush()
        {
            return new SolidColorBrush(GetAccentColor());
        }

        private static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", "");

            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
    }
}