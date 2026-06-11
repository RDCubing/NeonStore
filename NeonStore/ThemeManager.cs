using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NeonStore
{
    public static class ThemeManager
    {
        public static void ApplyAccent()
        {
            var brush = ColorService.GetBrush();

            Application.Current.Resources["AccentBrush"] = brush;
        }
    }
}
