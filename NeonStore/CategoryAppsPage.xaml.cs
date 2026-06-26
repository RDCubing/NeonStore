using NeonStore.Common;
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
using Windows.Storage;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace NeonStore
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class CategoryAppsPage : Page
    {

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        public ObservableCollection<AppItem> Apps { get; set; }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }


        public CategoryAppsPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="Common.NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="Common.SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="Common.NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="Common.NavigationHelper.LoadState"/>
        /// and <see cref="Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);

            string category = e.Parameter as string;
            pageTitle.Text = category;

            var filtered = NeonStoreService.NeonStore
                .Where(a => a.Category == category);

            AllApps = new ObservableCollection<AppItem>(filtered);

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
            var app = (AppItem)e.ClickedItem;

            AppState.SelectedApp = app;

            Frame.Navigate(typeof(SelectedApp));
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // optional: simple UX feedback
                var btn = sender as Button;
                if (btn != null)
                    btn.IsEnabled = false;

                System.Diagnostics.Debug.WriteLine("NeonStore: Manual refresh triggered");

                await NeonStoreService.LoadAsync();

                System.Diagnostics.Debug.WriteLine("NeonStore: Refresh complete ✔");

                if (btn != null)
                    btn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Refresh ERROR: " + ex.Message);
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
            if (AllApps == null) return;

            query = (query ?? "").ToLower();

            if (string.IsNullOrWhiteSpace(query))
            {
                ProjectsGrid.ItemsSource = AllApps;
                return;
            }

            var filtered = AllApps.Where(app =>
    (app.Title ?? "").ToLower().Contains(query) ||
    (app.Description ?? "").ToLower().Contains(query) ||
    (app.Publisher ?? "").ToLower().Contains(query) ||
    (app.Category ?? "").ToLower().Contains(query)
);

            ProjectsGrid.ItemsSource = new ObservableCollection<AppItem>(filtered);
        }

        private ObservableCollection<AppItem> AllApps;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            bool reduceMotion =
    (bool?)ApplicationData.Current.LocalSettings.Values["ReduceMotion"] ?? false;

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
