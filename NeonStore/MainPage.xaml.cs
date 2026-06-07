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
using System.Collections.ObjectModel;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NeonStore
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        private void GoToApps_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Apps));
        }

        private void TopApps_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(TopApps));
        }

        private void AppsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var app = (AppItem)e.ClickedItem;

            AppState.SelectedApp = app;

            Frame.Navigate(typeof(SelectedApp), app);
        }

        private void QuoteTileLTS_Click(object sender, RoutedEventArgs e)
        {
            NavigateToApp("QuoteTile LTS");
        }

        private void LiveText_Click(object sender, RoutedEventArgs e)
        {
            NavigateToApp("Live Text 8.1");
        }

        private void QuoteTile_Click(object sender, RoutedEventArgs e)
        {
            NavigateToApp("QuoteTile");
        }

        private void Chris_Click(object sender, RoutedEventArgs e)
        {
            NavigateToApp("ChrisRLillo Music");
        }

        private void OctoStore_Click(object sender, RoutedEventArgs e)
        {
            NavigateToApp("OctoStore");
        }

        private void NavigateToApp(string id)
        {
            var app = NeonStoreService.NeonStore
                .FirstOrDefault(a =>
                    string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a.Title, id, StringComparison.OrdinalIgnoreCase));

            if (app == null)
            {
                System.Diagnostics.Debug.WriteLine(id + " not found");
                return;
            }

            AppState.SelectedApp = app;
            Frame.Navigate(typeof(SelectedApp), app);
        }
    }
}
