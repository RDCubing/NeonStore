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
using Windows.UI.Popups;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Windows.Storage;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace NeonStore
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class SelectedApp : Page
    {

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

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


        public SelectedApp()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
            this.DataContext = AppState.SelectedApp;

        }

        public bool IsOwner { get; set; }

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
            var app = e.Parameter as AppItem;

            if (app != null)
            {
                this.DataContext = app;
            }

            LoadReviews();
            UpdateReviewUI();

            PrivacyNoteText.Visibility = Visibility.Visible;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        private void OtherApps_ItemClick(object sender, ItemClickEventArgs e)
        {
            var app = (AppItem)e.ClickedItem;

            AppState.SelectedApp = app;

            Frame.Navigate(typeof(SelectedApp));
        }

        public ObservableCollection<AppItem> TopApps
        {
            get
            {
                return new ObservableCollection<AppItem>(
                    TopApp.TopApps
                        .Where(a => a.Title != AppState.SelectedApp?.Title)
                );
            }
        }

        public ObservableCollection<AppItem> OtherApps
        {
            get
            {
                return OtherApp.OtherApps
                    .Where(a => a.Title != AppState.SelectedApp?.Title)
                    .Take(5)
                    .ToObservableCollection();
            }
        }

        private async void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            PrivacyNoteText.Visibility = Visibility.Collapsed;
            DownloadProgressRing.Visibility = Visibility.Visible;
            DownloadProgressRing.IsActive = true;

            // allow UI to update before heavy work starts
            await Task.Delay(500);

            try
            {
                var project = this.DataContext as AppItem;
                if (project?.DownloadUrl == null)
                    return;

                var uri = new Uri(project.DownloadUrl);

                // ✅ Notification: download started
                ToastService.Show("Download started", project.Title, project.ImagePath);

                var picker = new Windows.Storage.Pickers.FileSavePicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
                string fileName = System.IO.Path.GetFileName(new Uri(project.DownloadUrl).LocalPath);

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = project.Title;
                }

                picker.SuggestedFileName = fileName;

                string extension = System.IO.Path.GetExtension(uri.AbsolutePath);
                if (string.IsNullOrEmpty(extension))
                    extension = ".bin";

                picker.FileTypeChoices.Add("File", new List<string> { extension });

                var file = await picker.PickSaveFileAsync();
                if (file == null) return;

                var client = new Windows.Web.Http.HttpClient();

                var buffer = await client.GetBufferAsync(uri);

                await Windows.Storage.FileIO.WriteBufferAsync(file, buffer);

                // ✅ Notification: download completed
                ToastService.Show("Download complete", project.Title, project.ImagePath);

                var dialog = new MessageDialog($"\"{file.Name}\" downloaded. Open it?");
                dialog.Commands.Add(new UICommand("Open"));
                dialog.Commands.Add(new UICommand("Cancel"));

                var result = await dialog.ShowAsync();

                if (result.Label == "Open")
                {
                    await Windows.System.Launcher.LaunchFileAsync(file);
                }
                DownloadHistoryService.Instance.Add(new DownloadItem
                {
                    Title = project.Title,
                    FileName = file.Name,
                    DownloadUrl = project.DownloadUrl,
                    ImagePath = project.ImagePath,
                    DownloadedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                var dlg = new MessageDialog(ex.ToString(), "Crash Debug");
                await dlg.ShowAsync();
            }
            finally
            {
                DownloadProgressRing.IsActive = false;
                DownloadProgressRing.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        private async void LoadReviews()
        {
            try
            {
                var app = this.DataContext as AppItem;
                if (app == null) return;

                string json = await ReviewService.GetReviews(app.Id);

                var arr = JArray.Parse(json);

                var list = new ObservableCollection<Review>();

                var currentUserId = ApplicationData.Current.LocalSettings.Values["userId"] as string;

                foreach (var item in arr)
                {
                    list.Add(new Review
                    {
                        _id = item["_id"]?.ToString(),
                        userId = item["userId"]?.ToString(),
                        username = item["username"]?.ToString(),
                        rating = int.Parse(item["rating"]?.ToString() ?? "0"),
                        comment = item["comment"]?.ToString(),
                        appId = item["appId"]?.ToString(),
                        createdAt = DateTime.Parse(item["createdAt"]?.ToString() ?? DateTime.MinValue.ToString()),
                        updatedAt = DateTime.Parse(item["updatedAt"]?.ToString() ?? DateTime.MinValue.ToString())
                    });
                }

                ReviewsGrid.ItemsSource = list;

                UpdateReviewStatistics(list);

                UpdateEmptyState(list);

                System.Diagnostics.Debug.WriteLine("[REVIEWS LOADED] " + list.Count);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LOAD REVIEWS ERROR] " + ex.Message);
            }
        }

        private async void SubmitReview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var token = Windows.Storage.ApplicationData.Current.LocalSettings.Values["token"] as string;
                var username = Windows.Storage.ApplicationData.Current.LocalSettings.Values["username"] as string;

                if (string.IsNullOrEmpty(token))
                {
                    await new Windows.UI.Popups.MessageDialog("You must be logged in").ShowAsync();
                    return;
                }

                var app = this.DataContext as AppItem;

                int rating = int.Parse(((ComboBoxItem)ReviewRatingBox.SelectedItem).Content.ToString());

                var bodyObj = new
                {
                    appId = app.Id,
                    rating = rating,
                    comment = ReviewCommentBox.Text
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(bodyObj);

                string result = await ReviewService.SubmitReview(token, json);

                System.Diagnostics.Debug.WriteLine("[SUBMIT REVIEW] " + result);

                LoadReviews();

                ReviewCommentBox.Text = "";
            }
            catch (System.Exception ex)
            {
                await new Windows.UI.Popups.MessageDialog(ex.Message).ShowAsync();
            }
        }

        private void UpdateReviewStatistics(IEnumerable<Review> reviews)
        {
            int count = reviews.Count();

            ReviewCountText.Text = count.ToString();

            if (count == 0)
            {
                AverageRatingText.Text = "0.0";
                return;
            }

            double average = reviews.Average(r => r.rating);

            AverageRatingText.Text = average.ToString("0.0");
        }

        private void UpdateEmptyState(IEnumerable<Review> reviews)
        {
            EmptyStatePanel.Visibility = reviews.Any()
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateReviewUI()
        {
            var token = ApplicationData.Current.LocalSettings.Values["token"] as string;

            bool loggedIn = !string.IsNullOrEmpty(token);

            ReviewFormPanel.Visibility = loggedIn
                ? Visibility.Visible
                : Visibility.Collapsed;

            SignInToReviewPanel.Visibility = loggedIn
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}
