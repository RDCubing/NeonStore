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

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace NeonStore
{
    public sealed partial class Downloads : SettingsFlyout
    {
        public Downloads()
        {
            this.InitializeComponent();
            this.DataContext = DownloadHistoryService.Instance;
            UpdateEmptyState();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            DownloadHistoryService.Instance.Clear();
            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            EmptyText.Visibility =
                DownloadHistoryService.Instance.Downloads.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
