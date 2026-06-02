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
using System.Threading.Tasks;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace NeonStore
{
    public sealed partial class Updates : SettingsFlyout
    {
        public Updates()
        {
            this.InitializeComponent();
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateProgressRing.Visibility = Visibility.Visible;
                UpdateProgressRing.IsActive = true;

                UpdateStatusText.Text = "Checking for updates...";

                // ✅ staged fake loading UX
                UpdateStatusText.Text = "Connecting...";
                await Task.Delay(750);

                UpdateStatusText.Text = "Downloading update metadata...";
                await Task.Delay(750);

                var result = await AutoUpdateService.CheckAsync();

                if (result == UpdateResult.Available)
                {
                    UpdateStatusText.Text = "Update available!";
                }
                else if (result == UpdateResult.UpToDate)
                {
                    UpdateStatusText.Text = "You're running the latest version.";
                }
                else
                {
                    UpdateStatusText.Text = "Update check failed.";
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = "Update check failed.";
                await new Windows.UI.Popups.MessageDialog(ex.Message, "Update Error").ShowAsync();
            }
            finally
            {
                UpdateProgressRing.IsActive = false;
                UpdateProgressRing.Visibility = Visibility.Collapsed;
            }
        }
    }
}
