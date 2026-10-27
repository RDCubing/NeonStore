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

            object reduceMotion =
                ApplicationData.Current.LocalSettings.Values["ReduceMotion"];

            ReduceMotionToggle.IsOn = reduceMotion == null ? false : (bool)reduceMotion;


            object autoUpdate =
                ApplicationData.Current.LocalSettings.Values["AutoUpdate"];

            AutoUpdateToggle.IsOn = autoUpdate == null ? true : (bool)autoUpdate;

            object toast =
                ApplicationData.Current.LocalSettings.Values["Toast"];

            ToastToggle.IsOn = toast == null ? true : (bool)toast;
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

        // 2. Open flyout on direct tap/click of the custom tile
        private void CustomColorBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AccentColorGridView.SelectedItem = CustomColorBorder;
            FlyoutBase.ShowAttachedFlyout(CustomColorBorder);
        }

        // Slider Sync -> Update Preview, Hex input, and Numeric Labels
        private void ColorSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_isUpdatingColorInternally || CustomPreviewBox == null) return;

            _isUpdatingColorInternally = true;

            byte r = (byte)RedSlider.Value;
            byte g = (byte)GreenSlider.Value;
            byte b = (byte)BlueSlider.Value;

            // Update readout labels
            if (RedValText != null) RedValText.Text = r.ToString();
            if (GreenValText != null) GreenValText.Text = g.ToString();
            if (BlueValText != null) BlueValText.Text = b.ToString();

            var color = Windows.UI.Color.FromArgb(255, r, g, b);
            CustomPreviewBox.Background = new SolidColorBrush(color);

            if (CustomHexInput != null)
            {
                CustomHexInput.Text = string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);
            }

            _isUpdatingColorInternally = false;
        }

        // Hex Input Sync -> Update Preview, Sliders, and Numeric Labels
        private void CustomHexInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingColorInternally || CustomHexInput == null) return;

            string hex = CustomHexInput.Text.Trim();
            if (hex.StartsWith("#") && hex.Length == 7)
            {
                try
                {
                    byte r = Convert.ToByte(hex.Substring(1, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(3, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(5, 2), 16);

                    _isUpdatingColorInternally = true;
                    RedSlider.Value = r;
                    GreenSlider.Value = g;
                    BlueSlider.Value = b;

                    if (RedValText != null) RedValText.Text = r.ToString();
                    if (GreenValText != null) GreenValText.Text = g.ToString();
                    if (BlueValText != null) BlueValText.Text = b.ToString();

                    CustomPreviewBox.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
                    _isUpdatingColorInternally = false;
                }
                catch { }
            }
        }

        // 5. Apply Custom Accent
        private void ApplyCustomColor_Click(object sender, RoutedEventArgs e)
        {
            byte r = (byte)RedSlider.Value;
            byte g = (byte)GreenSlider.Value;
            byte b = (byte)BlueSlider.Value;

            string hex = string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);

            // Update tile visual & hide the plus glyph
            CustomColorBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
            CustomAddGlyph.Visibility = Visibility.Collapsed;

            // Apply color and select tile
            ColorService.SetAccentHex(hex);
            AccentColorGridView.SelectedItem = CustomColorBorder;

            CustomColorFlyout.Hide();
        }

        // 6. Updated LoadAccentSelection to support custom hex values
        private void LoadAccentSelection()
        {
            string saved = ColorService.GetAccentHex();
            Border matchedBorder = null;
            Border defaultBorder = null;

            foreach (var obj in AccentColorGridView.Items)
            {
                var border = obj as Border;
                if (border?.Tag == null) continue;

                string tag = border.Tag.ToString();

                if (tag != "CUSTOM" && string.Equals(tag, saved, StringComparison.OrdinalIgnoreCase))
                {
                    matchedBorder = border;
                    break;
                }

                if (string.Equals(tag, "#0F4C4C", StringComparison.OrdinalIgnoreCase))
                {
                    defaultBorder = border;
                }
            }

            if (matchedBorder != null)
            {
                AccentColorGridView.SelectedItem = matchedBorder;
            }
            else if (!string.IsNullOrEmpty(saved))
            {
                // Saved color is a custom hex not matching the presets
                try
                {
                    byte r = Convert.ToByte(saved.Substring(1, 2), 16);
                    byte g = Convert.ToByte(saved.Substring(3, 2), 16);
                    byte b = Convert.ToByte(saved.Substring(5, 2), 16);

                    CustomColorBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
                    CustomAddGlyph.Visibility = Visibility.Collapsed;
                    CustomPreviewBox.Background = CustomColorBorder.Background;
                    CustomHexInput.Text = saved;
                    RedSlider.Value = r;
                    GreenSlider.Value = g;
                    BlueSlider.Value = b;

                    AccentColorGridView.SelectedItem = CustomColorBorder;
                }
                catch
                {
                    AccentColorGridView.SelectedItem = defaultBorder;
                    ColorService.SetAccentHex("#0F4C4C");
                }
            }
            else
            {
                AccentColorGridView.SelectedItem = defaultBorder;
                ColorService.SetAccentHex("#0F4C4C");
            }
        }

        private bool _isUpdatingColorInternally = false;

        // 1. GridView Selection Handling
        private void AccentColorGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var border = AccentColorGridView.SelectedItem as Border;
            if (border?.Tag == null) return;

            string tag = border.Tag.ToString();

            if (tag == "CUSTOM")
            {
                // Re-apply the custom color or launch picker if not yet customized
                var brush = CustomColorBorder.Background as SolidColorBrush;
                string customHex = brush != null
                    ? string.Format("#{0:X2}{1:X2}{2:X2}", brush.Color.R, brush.Color.G, brush.Color.B)
                    : "#333338";

                ColorService.SetAccentHex(customHex);
                FlyoutBase.ShowAttachedFlyout(CustomColorBorder);
            }
            else
            {
                ColorService.SetAccentHex(tag);
            }
        }

        private async void ReinstallHelper_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null) button.IsEnabled = false;

            // Show inline progress indicator inside the flyout
            if (HelperProgressBar != null) HelperProgressBar.Visibility = Visibility.Visible;

            if (HelperStatusText != null)
            {
                HelperStatusText.Text = "Copying helper...";
                HelperStatusText.Foreground = new SolidColorBrush(Windows.UI.Colors.LightGray);
                HelperStatusText.Visibility = Visibility.Visible;
            }

            try
            {
                // 1. Get Pictures\NeonStore target directory
                StorageFolder pictures = KnownFolders.PicturesLibrary;
                StorageFolder neonStoreFolder = await pictures.CreateFolderAsync(
                    "NeonStore",
                    CreationCollisionOption.OpenIfExists);

                // 2. Locate packaged NeonStore.Helper.exe
                var installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                IStorageItem pkgItem = await installedLocation.TryGetItemAsync("NeonStore.Helper.exe");
                StorageFile packagedHelper = pkgItem as StorageFile;

                if (packagedHelper == null)
                {
                    if (HelperStatusText != null)
                    {
                        HelperStatusText.Text = "Error: NeonStore.Helper.exe was not found in the app package.";
                        HelperStatusText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
                    }
                    return;
                }

                // 3. Overwrite/replace the file in Pictures\NeonStore
                await packagedHelper.CopyAsync(neonStoreFolder, "NeonStore.Helper.exe", NameCollisionOption.ReplaceExisting);

                // Success state displayed directly in flyout
                if (HelperStatusText != null)
                {
                    HelperStatusText.Text = "NeonStore.Helper.exe refreshed successfully in Pictures\\NeonStore.";
                    HelperStatusText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
                }
            }
            catch (Exception ex)
            {
                // Error state displayed directly in flyout
                if (HelperStatusText != null)
                {
                    HelperStatusText.Text = "Failed to copy: " + ex.Message;
                    HelperStatusText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
                }
            }
            finally
            {
                // Hide inline progress indicator and re-enable button
                if (HelperProgressBar != null) HelperProgressBar.Visibility = Visibility.Collapsed;
                if (button != null) button.IsEnabled = true;
            }
        }

        private void ReduceMotionToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["ReduceMotion"] =
                ReduceMotionToggle.IsOn;
        }

        private void AutoUpdateToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["AutoUpdate"] =
                AutoUpdateToggle.IsOn;
        }

        private void ToastToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["Toast"] =
                ToastToggle.IsOn;
        }
    }
}
