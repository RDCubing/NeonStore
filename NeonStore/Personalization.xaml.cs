using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace NeonStore
{
    public sealed partial class Personalization : SettingsFlyout
    {
        public Personalization()
        {
            this.InitializeComponent();
            LoadAccentSelection();

            object value =
        ApplicationData.Current.LocalSettings.Values["UseStoreHeader"];

            StoreHeaderToggle.IsOn = value == null ? false : (bool)value;
        }

        private void StoreHeaderToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["UseStoreHeader"] =
                StoreHeaderToggle.IsOn;

            var frame = Window.Current.Content as Frame;
            var mainPage = frame?.Content as MainPage;

            if (mainPage != null)
            {
                mainPage.SetHeaderText(
                    StoreHeaderToggle.IsOn ? "Store" : "NeonStore");
            }
        }

        private void LoadAccentSelection()
        {
            string saved = ColorService.GetAccentHex();

            ComboBoxItem matchedItem = null;

            foreach (ComboBoxItem item in AccentColorCombo.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == saved)
                {
                    matchedItem = item;
                    break;
                }
            }

            if (matchedItem != null)
            {
                AccentColorCombo.SelectedItem = matchedItem;
            }
            else
            {
                // fallback to default (#0F4C4C)
                foreach (ComboBoxItem item in AccentColorCombo.Items)
                {
                    if (item.Tag != null && item.Tag.ToString() == "#0F4C4C")
                    {
                        AccentColorCombo.SelectedItem = item;
                        break;
                    }
                }

                // optional: also save default if missing
                ColorService.SetAccentHex("#0F4C4C");
            }
        }

        private void AccentColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = AccentColorCombo.SelectedItem as ComboBoxItem;

            if (item == null)
                return;

            string hex = item.Tag.ToString();

            ColorService.SetAccentHex(hex);
        }
    }
}
