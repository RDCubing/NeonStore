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
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.System;

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
        // 1. Add a page-level field to track the user's existing review
        private Review myExistingReview = null;

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
        /// 
        private DataTransferManager dataTransferManager;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
            var app = e.Parameter as AppItem;

            if (app != null)
            {
                this.DataContext = app;
            }

            dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataRequested;

            LoadReviews();
            UpdateReviewUI();

            PrivacyNoteText.Visibility = Visibility.Visible;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (dataTransferManager != null)
            {
                dataTransferManager.DataRequested -= DataRequested;
            }

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
            DownloadProgressPanel.Visibility = Visibility.Visible;
            DownloadProgressBar.Value = 0;
            DownloadStatusText.Text = "Preparing download...";

            // allow UI to update before heavy work starts
            await Task.Delay(500);

            try
            {
                var project = this.DataContext as AppItem;
                if (project?.DownloadUrl == null)
                    return;

                var uri = new Uri(project.DownloadUrl);

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

                if (file == null)
                {
                    DownloadProgressPanel.Visibility = Visibility.Collapsed;
                    DownloadProgressRing.IsActive = false;
                    DownloadProgressRing.Visibility = Visibility.Collapsed;
                    DownloadStatusText.Text = "";

                    return;
                }

                // ✅ Notification: download started
                ToastService.Show("Download started", project.Title, project.ImagePath);

                var client = new Windows.Web.Http.HttpClient();

                var response = await client.GetAsync(
                    uri,
                    Windows.Web.Http.HttpCompletionOption.ResponseHeadersRead);

                ulong totalBytes = response.Content.Headers.ContentLength ?? 0;

                var input = await response.Content.ReadAsInputStreamAsync();
                var output = await file.OpenAsync(FileAccessMode.ReadWrite);

                var reader = new Windows.Storage.Streams.DataReader(input);
                var writer = new Windows.Storage.Streams.DataWriter(output);

                const uint BufferSize = 8192;
                ulong downloadedBytes = 0;

                while (true)
                {
                    uint loaded = await reader.LoadAsync(BufferSize);

                    if (loaded == 0)
                        break;

                    byte[] bytes = new byte[loaded];
                    reader.ReadBytes(bytes);

                    writer.WriteBytes(bytes);
                    await writer.StoreAsync();

                    downloadedBytes += loaded;

                    if (totalBytes > 0)
                    {
                        double percent =
                            (double)downloadedBytes / totalBytes * 100;

                        double downloadedMB =
                            downloadedBytes / 1024d / 1024d;

                        double totalMB =
                            totalBytes / 1024d / 1024d;

                        DownloadProgressBar.Value = percent;

                        DownloadStatusText.Text =
                            string.Format(
                                "{0:F1} MB / {1:F1} MB ({2:F0}%)",
                                downloadedMB,
                                totalMB,
                                percent);
                    }
                }

                await writer.FlushAsync();

                DownloadProgressBar.Value = 100;

                if (totalBytes > 0)
                {
                    double totalMB = totalBytes / 1024d / 1024d;

                    DownloadStatusText.Text =
                        string.Format(
                            "{0:F1} MB / {0:F1} MB (100%)",
                            totalMB);
                }
                else
                {
                    DownloadStatusText.Text = "Download complete";
                }

                reader.Dispose();
                writer.Dispose();
                input.Dispose();
                output.Dispose();

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

                if (DownloadProgressPanel.Visibility == Visibility.Visible)
                {
                    if (DownloadStatusText.Text != "Download complete")
                        DownloadStatusText.Text = "Ready";
                }
            }
        }

        private async void InstantDownload_Click(object sender, RoutedEventArgs e)
        {
            PrivacyNoteText.Visibility = Visibility.Collapsed;
            DownloadProgressRing.Visibility = Visibility.Visible;
            DownloadProgressRing.IsActive = true;
            DownloadProgressPanel.Visibility = Visibility.Visible;
            DownloadProgressBar.Value = 0;
            DownloadStatusText.Text = "Checking local files...";

            try
            {
                var project = this.DataContext as AppItem;
                if (project?.DownloadUrl == null)
                    return;

                var uri = new Uri(project.DownloadUrl);

                string fileName = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrEmpty(fileName))
                    fileName = project.Title + ".bin";

                // Pictures\NeonStore
                StorageFolder pictures = KnownFolders.PicturesLibrary;
                StorageFolder neonStoreFolder = await pictures.CreateFolderAsync(
                    "NeonStore",
                    CreationCollisionOption.OpenIfExists);

                // Check if file already exists locally
                StorageFile file = null;
                IStorageItem existingItem = await neonStoreFolder.TryGetItemAsync(fileName);
                bool alreadyDownloaded = false;

                if (existingItem is StorageFile)
                {
                    file = (StorageFile)existingItem;
                    var basicProps = await file.GetBasicPropertiesAsync();

                    // Verify file is not empty (e.g. from an aborted previous download)
                    if (basicProps.Size > 0)
                    {
                        alreadyDownloaded = true;
                        DownloadProgressBar.Value = 100;
                        DownloadStatusText.Text = "Using existing download";
                    }
                }

                // If not found locally, proceed with download
                if (!alreadyDownloaded)
                {
                    DownloadStatusText.Text = "Preparing download...";

                    file = await neonStoreFolder.CreateFileAsync(
                        fileName,
                        CreationCollisionOption.OpenIfExists);

                    ToastService.Show(
                        "Download started",
                        project.Title,
                        project.ImagePath);

                    var client = new Windows.Web.Http.HttpClient();
                    var response = await client.GetAsync(
                        uri,
                        Windows.Web.Http.HttpCompletionOption.ResponseHeadersRead);

                    ulong totalBytes = response.Content.Headers.ContentLength ?? 0;

                    var input = await response.Content.ReadAsInputStreamAsync();
                    var output = await file.OpenAsync(FileAccessMode.ReadWrite);

                    var reader = new Windows.Storage.Streams.DataReader(input);
                    var writer = new Windows.Storage.Streams.DataWriter(output);

                    const uint BufferSize = 8192;
                    ulong downloadedBytes = 0;

                    while (true)
                    {
                        uint loaded = await reader.LoadAsync(BufferSize);
                        if (loaded == 0)
                            break;

                        byte[] bytes = new byte[loaded];
                        reader.ReadBytes(bytes);
                        writer.WriteBytes(bytes);
                        await writer.StoreAsync();

                        downloadedBytes += loaded;

                        if (totalBytes > 0)
                        {
                            double percent = (double)downloadedBytes / totalBytes * 100;
                            double downloadedMB = downloadedBytes / 1024d / 1024d;
                            double totalMB = totalBytes / 1024d / 1024d;

                            DownloadProgressBar.Value = percent;
                            DownloadStatusText.Text = string.Format(
                                "{0:F1} MB / {1:F1} MB ({2:F0}%)",
                                downloadedMB,
                                totalMB,
                                percent);
                        }
                    }

                    await writer.FlushAsync();
                    reader.Dispose();
                    writer.Dispose();
                    input.Dispose();
                    output.Dispose();

                    DownloadProgressBar.Value = 100;

                    if (totalBytes > 0)
                    {
                        double totalMB = totalBytes / 1024d / 1024d;
                        DownloadStatusText.Text = string.Format("{0:F1} MB / {0:F1} MB (100%)", totalMB);
                    }
                    else
                    {
                        DownloadStatusText.Text = "Download complete";
                    }

                    ToastService.Show(
                        "Download complete",
                        project.Title,
                        project.ImagePath);
                }

                // --- SIDELOAD / PROTOCOL CHECK ---
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                bool useExperimentalSideload = false;

                if (localSettings.Values.ContainsKey("UseExperimentalSideload"))
                {
                    useExperimentalSideload = (bool)localSettings.Values["UseExperimentalSideload"];
                }

                if (useExperimentalSideload)
                {
                    // Check if file is a modern Windows 8.1 app package
                    string ext = Path.GetExtension(file.Name) ?? string.Empty;
                    bool isAppxPackage = ext.Equals(".appx", StringComparison.OrdinalIgnoreCase) ||
                                         ext.Equals(".appxbundle", StringComparison.OrdinalIgnoreCase);

                    if (!isAppxPackage)
                    {
                        var invalidPkgDialog = new Windows.UI.Popups.MessageDialog(
                            string.Format("'{0}' is not a modern application package (.appx or .appxbundle).\n\nOpening with the default system handler instead.", file.Name),
                            "Unsupported Package Type");

                        invalidPkgDialog.Commands.Add(new Windows.UI.Popups.UICommand("OK"));
                        await invalidPkgDialog.ShowAsync();

                        await Windows.System.Launcher.LaunchFileAsync(file);
                        return;
                    }

                    string taskId = Guid.NewGuid().ToString("N");
                    DownloadStatusText.Text = "Connecting to helper...";

                    // Ensure the helper is in Pictures\NeonStore so the user can easily run it
                    try
                    {
                        var installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                        IStorageItem pkgItem = await installedLocation.TryGetItemAsync("NeonStore.Helper.exe");
                        StorageFile packagedHelper = pkgItem as StorageFile;
                        if (packagedHelper != null)
                        {
                            await packagedHelper.CopyAsync(neonStoreFolder, "NeonStore.Helper.exe", NameCollisionOption.ReplaceExisting);
                        }
                    }
                    catch { }

                    // Launch helper protocol
                    string uriString = string.Format(
                        "neonstore-helper://install?path={0}&taskId={1}",
                        Uri.EscapeDataString(file.Path),
                        Uri.EscapeDataString(taskId));

                    await Windows.System.Launcher.LaunchUriAsync(new Uri(uriString));

                    // Monitor progress with early handshake timeout
                    bool helperResponded = await MonitorSideloadProgressAsync(neonStoreFolder, taskId);

                    if (helperResponded)
                    {
                        ToastService.Show(
                            "App installed successfully",
                            project.Title,
                            project.ImagePath);
                    }
                    else
                    {
                        var setupDialog = new Windows.UI.Popups.MessageDialog(
                                                    "NeonStore Helper is not active or hasn't been set up yet.\n\n" +
                                                    "To complete setup:\n" +
                                                    "1. Open File Explorer on your Desktop.\n" +
                                                    "2. Navigate to: Pictures -> NeonStore\n" +
                                                    "3. Double-click 'NeonStore.Helper.exe' and approve the UAC prompt.\n" +
                                                    "4. Return to NeonStore and tap Download again.\n\n" +
                                                    "If the helper is already set up, the deployment likely encountered an issue. Check the status text above the progress bar to see what went wrong.",
                                                    "Sideload Helper Setup Required");

                        setupDialog.Commands.Add(new Windows.UI.Popups.UICommand("OK"));

                        await setupDialog.ShowAsync();
                    }
                }
                else
                {
                    // Fallback / standard execution
                    await Windows.System.Launcher.LaunchFileAsync(file);
                }

                DownloadHistoryService.Instance.Add(
                    new DownloadItem
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
                await new MessageDialog(
                    ex.ToString(),
                    "Crash Debug").ShowAsync();
            }
            finally
            {
                DownloadProgressRing.IsActive = false;
                DownloadProgressRing.Visibility = Visibility.Collapsed;
                DownloadStatusText.Text = "Ready";
            }
        }
        private async System.Threading.Tasks.Task<bool> MonitorSideloadProgressAsync(StorageFolder folder, string taskId)
        {
            string statusFileName = string.Format("status_{0}.txt", taskId);
            bool finished = false;
            bool helperStarted = false;
            int timeoutSeconds = 60;
            int handshakeTimeoutSeconds = 6;
            DateTime startTime = DateTime.Now;

            while (!finished)
            {
                await System.Threading.Tasks.Task.Delay(300);

                double elapsed = (DateTime.Now - startTime).TotalSeconds;

                if (!helperStarted && elapsed > handshakeTimeoutSeconds)
                {
                    DownloadStatusText.Text = "Helper failed to start.";
                    return false;
                }

                if (elapsed > timeoutSeconds)
                {
                    DownloadStatusText.Text = "Sideloading timed out.";
                    return false;
                }

                try
                {
                    IStorageItem item = await folder.TryGetItemAsync(statusFileName);
                    StorageFile statusFile = item as StorageFile;

                    if (statusFile != null)
                    {
                        helperStarted = true;
                        string content = await FileIO.ReadTextAsync(statusFile);

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            string[] parts = content.Split('|');
                            if (parts.Length >= 4)
                            {
                                int percent;
                                int.TryParse(parts[0], out percent);
                                string message = parts[1];
                                bool isComplete = parts[2].Equals("True", StringComparison.OrdinalIgnoreCase);
                                bool isError = parts[3].Equals("True", StringComparison.OrdinalIgnoreCase);

                                DownloadProgressBar.Value = percent;
                                DownloadStatusText.Text = message;

                                if (isComplete || isError)
                                {
                                    finished = true;
                                    try { await statusFile.DeleteAsync(StorageDeleteOption.PermanentDelete); } catch { }
                                    return !isError;
                                }
                            }
                        }
                    }
                    else
                    {
                        DownloadStatusText.Text = string.Format("Waiting for helper... ({0:F0}s)", handshakeTimeoutSeconds - elapsed);
                    }
                }
                catch
                {
                    // Temporary file lock catch
                }
            }

            return true;
        }
        #endregion

        // 2. Update LoadReviews to check if the current user already has a review
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
                var currentUsername = ApplicationData.Current.LocalSettings.Values["username"] as string;

                myExistingReview = null;

                foreach (var item in arr)
                {
                    var r = new Review
                    {
                        _id = item["_id"]?.ToString(),
                        userId = item["userId"]?.ToString(),
                        username = item["username"]?.ToString(),
                        avatar = item["avatar"]?.ToString(),
                        rating = int.Parse(item["rating"]?.ToString() ?? "0"),
                        comment = item["comment"]?.ToString(),
                        appId = item["appId"]?.ToString(),
                        createdAt = DateTime.Parse(item["createdAt"]?.ToString() ?? DateTime.MinValue.ToString()),
                        updatedAt = DateTime.Parse(item["updatedAt"]?.ToString() ?? DateTime.MinValue.ToString())
                    };

                    // Detect if this review belongs to the currently signed-in user
                    if ((!string.IsNullOrEmpty(currentUserId) && r.userId == currentUserId) ||
                        (!string.IsNullOrEmpty(currentUsername) && r.username == currentUsername))
                    {
                        myExistingReview = r;
                    }

                    list.Add(r);
                }

                ReviewsGrid.ItemsSource = list;
                UpdateReviewStatistics(list);
                UpdateEmptyState(list);

                // Adjust form controls based on whether a review exists
                UpdateReviewFormState();

                System.Diagnostics.Debug.WriteLine("[REVIEWS LOADED] " + list.Count);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LOAD REVIEWS ERROR] " + ex.Message);
            }
        }

        // 3. Helper to toggle buttons and pre-fill form fields
        private void UpdateReviewFormState()
        {
            if (myExistingReview != null)
            {
                // Pre-fill fields with existing review data
                ReviewCommentBox.Text = myExistingReview.comment ?? "";
                _selectedRating = Math.Max(1, Math.Min(5, myExistingReview.rating));
                UpdateInputStarDisplay(_selectedRating);

                SubmitReviewBtn.Visibility = Visibility.Collapsed;
                ExistingReviewActionsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                _selectedRating = 5;
                UpdateInputStarDisplay(_selectedRating);

                SubmitReviewBtn.Visibility = Visibility.Visible;
                ExistingReviewActionsPanel.Visibility = Visibility.Collapsed;
            }
        }

        // 4. Add the Delete Review Click handler
        private async void DeleteReview_Click(object sender, RoutedEventArgs e)
        {
            if (myExistingReview == null) return;

            var confirmDialog = new MessageDialog("Are you sure you want to delete your review?", "Delete Review");
            confirmDialog.Commands.Add(new UICommand("Delete"));
            confirmDialog.Commands.Add(new UICommand("Cancel"));

            var choice = await confirmDialog.ShowAsync();
            if (choice.Label != "Delete") return;

            // Inside DeleteReview_Click in SelectedApp.xaml.cs
            var token = ApplicationData.Current.LocalSettings.Values["token"] as string;
            if (string.IsNullOrEmpty(token)) return;

            // Pass the appId (e.g., myExistingReview.appId), NOT myExistingReview._id
            bool deleted = await ReviewService.DeleteReview(token, myExistingReview.appId);

            if (deleted)
            {
                ReviewCommentBox.Text = "";
                _selectedRating = 5;
                UpdateInputStarDisplay(_selectedRating);
                myExistingReview = null;
                LoadReviews();
            }
            else
            {
                await new MessageDialog("Failed to delete review. Please try again.", "Error").ShowAsync();
            }
        }

        // Default rating is 5
        private int _selectedRating = 5;

        private void RatingStar_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var clickedStar = sender as TextBlock;
            if (clickedStar?.Tag != null)
            {
                int rating;
                if (int.TryParse(clickedStar.Tag.ToString(), out rating))
                {
                    _selectedRating = rating;
                    UpdateInputStarDisplay(_selectedRating);
                }
            }
        }

        private void Publisher_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as HyperlinkButton;
            string publisher = button?.Content as string;

            if (!string.IsNullOrWhiteSpace(publisher))
            {
                Frame.Navigate(typeof(PublisherPage), publisher);
            }
        }

        private void UpdateInputStarDisplay(int rating)
        {
            var stars = new[] { InputStar1, InputStar2, InputStar3, InputStar4, InputStar5 };
            var activeBrush = (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["AccentBrush"];
            var inactiveBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(60, 255, 255, 255));

            for (int i = 0; i < stars.Length; i++)
            {
                if (i < rating)
                {
                    stars[i].Text = "\uE1CF"; // Filled star
                    stars[i].Foreground = activeBrush;
                    stars[i].Opacity = 1.0;
                }
                else
                {
                    stars[i].Text = "\uE1CE"; // Outline star
                    stars[i].Foreground = inactiveBrush;
                    stars[i].Opacity = 0.5;
                }
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

                var bodyObj = new
                {
                    appId = app.Id,
                    rating = _selectedRating,
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
            if (reviews == null)
                return;

            var list = reviews.ToList();
            int totalCount = list.Count;
            ReviewCountText.Text = totalCount.ToString();

            if (totalCount == 0)
            {
                AverageRatingText.Text = "0.0";
                UpdateStarDisplay(0);
                ResetBars();
                return;
            }

            // Safely extract numeric ratings regardless of backing type
            var parsedRatings = new List<double>();
            foreach (var rev in list)
            {
                double val = 0;
                if (double.TryParse(rev.rating.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out val))
                {
                    // If the backend uses 0-10 or 0-100 scales, normalize to 1-5
                    if (val > 5.0 && val <= 10.0) val = val / 2.0;
                    else if (val > 10.0) val = (val / 100.0) * 5.0;

                    parsedRatings.Add(val);
                }
            }

            if (parsedRatings.Count == 0)
            {
                AverageRatingText.Text = "0.0";
                UpdateStarDisplay(0);
                ResetBars();
                return;
            }

            double average = parsedRatings.Average();
            AverageRatingText.Text = average.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            UpdateStarDisplay(average);

            // Count reviews per bracket (1 to 5)
            int count5 = parsedRatings.Count(r => r >= 4.5);
            int count4 = parsedRatings.Count(r => r >= 3.5 && r < 4.5);
            int count3 = parsedRatings.Count(r => r >= 2.5 && r < 3.5);
            int count2 = parsedRatings.Count(r => r >= 1.5 && r < 2.5);
            int count1 = parsedRatings.Count(r => r < 1.5);

            // Update count labels
            Count5Text.Text = count5.ToString();
            Count4Text.Text = count4.ToString();
            Count3Text.Text = count3.ToString();
            Count2Text.Text = count2.ToString();
            Count1Text.Text = count1.ToString();

            // Set bar widths (Total width = 120px)
            const double MaxBarWidth = 120.0;
            SetBarWidth(Bar5, (double)count5 / totalCount, MaxBarWidth);
            SetBarWidth(Bar4, (double)count4 / totalCount, MaxBarWidth);
            SetBarWidth(Bar3, (double)count3 / totalCount, MaxBarWidth);
            SetBarWidth(Bar2, (double)count2 / totalCount, MaxBarWidth);
            SetBarWidth(Bar1, (double)count1 / totalCount, MaxBarWidth);
        }

        private void SetBarWidth(Windows.UI.Xaml.Shapes.Rectangle bar, double ratio, double maxWidth)
        {
            bar.Width = Math.Max(0, ratio * maxWidth);
        }

        private void ResetBars()
        {
            Count5Text.Text = Count4Text.Text = Count3Text.Text = Count2Text.Text = Count1Text.Text = "0";
            Bar5.Width = Bar4.Width = Bar3.Width = Bar2.Width = Bar1.Width = 0;
        }

        private void UpdateStarDisplay(double rating)
        {
            // Round to nearest whole star (4.7 -> 5 stars; 4.2 -> 4 stars)
            int filledStars = (int)Math.Round(rating);
            var stars = new[] { Star1, Star2, Star3, Star4, Star5 };

            // Active accent color (or bright green/white) vs muted gray
            var activeBrush = (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["AccentBrush"];
            var inactiveBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(60, 255, 255, 255));

            for (int i = 0; i < stars.Length; i++)
            {
                if (i < filledStars)
                {
                    stars[i].Text = "\uE1CF"; // Filled star
                    stars[i].Foreground = activeBrush;
                    stars[i].Opacity = 1.0;
                }
                else
                {
                    stars[i].Text = "\uE1CE"; // Outline star
                    stars[i].Foreground = inactiveBrush;
                    stars[i].Opacity = 0.5;
                }
            }
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

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var app = this.DataContext as AppItem;

            if (app == null)
                return;

            var request = args.Request;

            request.Data.Properties.Title = app.Title;
            request.Data.Properties.Description = app.Subtitle ?? app.Description;

            string shareText =
                app.Title + Environment.NewLine +
                (app.Description ?? "") + Environment.NewLine +
                Environment.NewLine +
                "Download: " + app.DownloadUrl;

            request.Data.SetText(shareText);

            if (!string.IsNullOrEmpty(app.DownloadUrl))
            {
                request.Data.SetWebLink(new Uri(app.DownloadUrl));
            }
        }

        private async void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = this.DataContext as AppItem;

                if (app == null || string.IsNullOrWhiteSpace(app.DownloadUrl))
                    return;

                await Windows.System.Launcher.LaunchUriAsync(
                    new Uri(app.DownloadUrl));
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "Error").ShowAsync();
            }
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {

        }

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
