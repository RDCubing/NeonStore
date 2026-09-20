using NeonStore.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NeonStore
{
    public sealed partial class PublisherPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private ObservableCollection<AppItem> allApps;

        public ObservableCollection<AppItem> Apps { get; set; }

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public PublisherPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);

            string publisher = e.Parameter as string;
            pageTitle.Text = publisher ?? "Publisher";

            var filtered = NeonStoreService.NeonStore
                .Where(a => string.Equals(a.Publisher, publisher, StringComparison.OrdinalIgnoreCase))
                .ToList();

            allApps = new ObservableCollection<AppItem>(filtered);
            Apps = new ObservableCollection<AppItem>(filtered);

            ProjectsGrid.ItemsSource = Apps;
            this.DataContext = this;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void ProjectsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var app = e.ClickedItem as AppItem;
            if (app == null) return;

            AppState.SelectedApp = app;
            Frame.Navigate(typeof(SelectedApp), app);
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                if (btn != null) btn.IsEnabled = false;

                await NeonStoreService.LoadAsync();

                string currentPublisher = pageTitle.Text;
                var filtered = NeonStoreService.NeonStore
                    .Where(a => string.Equals(a.Publisher, currentPublisher, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                allApps = new ObservableCollection<AppItem>(filtered);
                Apps = new ObservableCollection<AppItem>(filtered);
                ProjectsGrid.ItemsSource = Apps;

                if (btn != null) btn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PublisherPage Refresh ERROR: " + ex.Message);
            }
        }

        private void SearchBox_QueryChanged(SearchBox sender, SearchBoxQueryChangedEventArgs args)
        {
            FilterApps(sender.QueryText);
        }

        private void SearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            FilterApps(sender.QueryText);
        }

        private void FilterApps(string query)
        {
            if (allApps == null) return;

            query = (query ?? "").Trim().ToLower();

            if (string.IsNullOrWhiteSpace(query))
            {
                ProjectsGrid.ItemsSource = allApps;
                return;
            }

            var filtered = allApps.Where(app =>
                (app.Title ?? "").ToLower().Contains(query) ||
                (app.Description ?? "").ToLower().Contains(query) ||
                (app.Category ?? "").ToLower().Contains(query)
            ).ToList();

            ProjectsGrid.ItemsSource = new ObservableCollection<AppItem>(filtered);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            bool reduceMotion = (bool?)ApplicationData.Current.LocalSettings.Values["ReduceMotion"] ?? false;

            if (!reduceMotion)
            {
                SlideInStoryboard.Begin();
            }
            else
            {
                MainPanelTransform.X = 0;
            }
        }
    }
}