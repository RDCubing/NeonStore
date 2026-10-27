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
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Text;

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
            SetDownloadButtonsEnabled(false);

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

                bool toast =
                    (bool?)ApplicationData.Current.LocalSettings.Values["Toast"] ?? true;

                if (toast)
                {
                    ToastService.Show("Download started", project.Title, project.ImagePath);
                }

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

                reader.Dispose();
                writer.Dispose();
                input.Dispose();
                output.Dispose();

                if (toast)
                {
                    ToastService.Show("Download complete", project.Title, project.ImagePath);
                }

                var dialog = new MessageDialog(string.Format("\"{0}\" downloaded. Open it?", file.Name));
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

                SetDownloadButtonsEnabled(true);
            }
        }

        private async void InstantDownload_Click(object sender, RoutedEventArgs e)
        {
            SetDownloadButtonsEnabled(false);

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

                StorageFolder pictures = KnownFolders.PicturesLibrary;
                StorageFolder neonStoreFolder = await pictures.CreateFolderAsync(
                    "NeonStore",
                    CreationCollisionOption.OpenIfExists);

                StorageFile file = null;
                IStorageItem existingItem = await neonStoreFolder.TryGetItemAsync(fileName);
                bool alreadyDownloaded = false;

                if (existingItem is StorageFile)
                {
                    file = (StorageFile)existingItem;
                    var basicProps = await file.GetBasicPropertiesAsync();

                    if (basicProps.Size > 0)
                    {
                        alreadyDownloaded = true;
                        DownloadProgressBar.Value = 100;
                        DownloadStatusText.Text = "Using existing download";
                    }
                }

                if (!alreadyDownloaded)
                {
                    DownloadStatusText.Text = "Preparing download...";

                    file = await neonStoreFolder.CreateFileAsync(
                        fileName,
                        CreationCollisionOption.OpenIfExists);

                    bool toast =
    (bool?)ApplicationData.Current.LocalSettings.Values["Toast"] ?? true;

                    if (toast)
                    {
                        ToastService.Show(
                        "Download started",
                        project.Title,
                        project.ImagePath);
                    }

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

                    if (toast)
                    {
                        ToastService.Show(
                        "Download complete",
                        project.Title,
                        project.ImagePath);
                    }
                }

                // --- SIDELOAD PROTOCOL EXECUTION ---
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

                string uriString = string.Format(
                    "neonstore-helper://install?path={0}&taskId={1}",
                    Uri.EscapeDataString(file.Path),
                    Uri.EscapeDataString(taskId));

                await Windows.System.Launcher.LaunchUriAsync(new Uri(uriString));

                bool helperResponded = await MonitorSideloadProgressAsync(neonStoreFolder, taskId);

                if (helperResponded)
                {
                    bool toast =
    (bool?)ApplicationData.Current.LocalSettings.Values["Toast"] ?? true;

                    if (toast)
                    {
                        ToastService.Show(
                        "App installed successfully",
                        project.Title,
                        project.ImagePath);
                    }
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
                await new MessageDialog(ex.ToString(), "Crash Debug").ShowAsync();
            }
            finally
            {
                DownloadProgressRing.IsActive = false;
                DownloadProgressRing.Visibility = Visibility.Collapsed;
                DownloadStatusText.Text = "Ready";

                SetDownloadButtonsEnabled(true);
            }
        }

        private async void OldDownload_Click(object sender, RoutedEventArgs e)
        {
            SetDownloadButtonsEnabled(false);

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

                StorageFolder pictures = KnownFolders.PicturesLibrary;
                StorageFolder neonStoreFolder = await pictures.CreateFolderAsync(
                    "NeonStore",
                    CreationCollisionOption.OpenIfExists);

                StorageFile file = null;
                IStorageItem existingItem = await neonStoreFolder.TryGetItemAsync(fileName);
                bool alreadyDownloaded = false;

                if (existingItem is StorageFile)
                {
                    file = (StorageFile)existingItem;
                    var basicProps = await file.GetBasicPropertiesAsync();

                    if (basicProps.Size > 0)
                    {
                        alreadyDownloaded = true;
                        DownloadProgressBar.Value = 100;
                        DownloadStatusText.Text = "Using existing download";
                    }
                }

                if (!alreadyDownloaded)
                {
                    DownloadStatusText.Text = "Preparing download...";

                    file = await neonStoreFolder.CreateFileAsync(
                        fileName,
                        CreationCollisionOption.OpenIfExists);

                    bool toast =
    (bool?)ApplicationData.Current.LocalSettings.Values["Toast"] ?? true;

                    if (toast)
                    {
                        ToastService.Show(
                        "Download started",
                        project.Title,
                        project.ImagePath);
                    }

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

                    if (toast)
                    {
                        ToastService.Show(
                        "Download complete",
                        project.Title,
                        project.ImagePath);
                    }
                }

                // --- DIRECT LAUNCH WITH SYSTEM SHELL ---
                await Windows.System.Launcher.LaunchFileAsync(file);

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
                await new MessageDialog(ex.ToString(), "Crash Debug").ShowAsync();
            }
            finally
            {
                DownloadProgressRing.IsActive = false;
                DownloadProgressRing.Visibility = Visibility.Collapsed;
                DownloadStatusText.Text = "Ready";

                SetDownloadButtonsEnabled(true);
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

        // 1. Pagination Fields (6 items per page)
        private const int PageSize = 6;
        private int _currentReviewPage = 1;
        private int _totalReviewPages = 1;
        private List<Review> _allReviews = new List<Review>();

        // 2. Load and parse reviews into the master list with 6-per-page pagination
        private async void LoadReviews()
        {
            try
            {
                var app = this.DataContext as AppItem;
                if (app == null) return;

                string json = await ReviewService.GetReviews(app.Id);
                var arr = JArray.Parse(json);

                _allReviews.Clear();
                myExistingReview = null;

                var currentUserId = ApplicationData.Current.LocalSettings.Values["userId"] as string;
                var currentUsername = ApplicationData.Current.LocalSettings.Values["username"] as string;

                foreach (var item in arr)
                {
                    string rawAvatar = item["avatar"]?.ToString();
                    var r = new Review
                    {
                        _id = item["_id"]?.ToString(),
                        userId = item["userId"]?.ToString(),
                        username = item["username"]?.ToString(),
                        avatar = string.IsNullOrWhiteSpace(rawAvatar) ? null : rawAvatar,
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

                    _allReviews.Add(r);
                }

                // Update statistics and empty state across the entire review collection
                UpdateReviewStatistics(_allReviews);
                UpdateEmptyState(_allReviews);

                // Adjust review submission/edit form controls
                UpdateReviewFormState();

                // Calculate total pages (6 reviews per page) and slice the first page
                _totalReviewPages = Math.Max(1, (int)Math.Ceiling((double)_allReviews.Count / PageSize));
                _currentReviewPage = 1;
                DisplayCurrentReviewPage();

                System.Diagnostics.Debug.WriteLine("[REVIEWS LOADED] Total: " + _allReviews.Count + ", Pages: " + _totalReviewPages);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LOAD REVIEWS ERROR] " + ex.Message);
            }
        }



        // 3. Slice and bind exactly 6 reviews for the current page
        private void DisplayCurrentReviewPage()
        {
            if (_allReviews.Count == 0)
            {
                ReviewsGrid.ItemsSource = null;
                if (PaginationPanel != null) PaginationPanel.Visibility = Visibility.Collapsed;
                return;
            }

            _totalReviewPages = Math.Max(1, (int)Math.Ceiling((double)_allReviews.Count / PageSize));

            // Clamp page number
            if (_currentReviewPage > _totalReviewPages) _currentReviewPage = _totalReviewPages;
            if (_currentReviewPage < 1) _currentReviewPage = 1;

            // Slice 6 items
            var pagedItems = _allReviews
                .Skip((_currentReviewPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ReviewsGrid.ItemsSource = pagedItems;

            // Update pagination controls
            UpdatePaginationUI();
        }

        // 4. Update Previous/Next button states and label
        private void UpdatePaginationUI()
        {
            if (PaginationPanel != null)
            {
                // Only show pagination if there are more than 6 reviews (2+ pages)
                PaginationPanel.Visibility = _totalReviewPages > 1 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (PageIndicatorText != null)
            {
                PageIndicatorText.Text = string.Format("Page {0} of {1}", _currentReviewPage, _totalReviewPages);
            }

            if (PrevPageBtn != null)
            {
                PrevPageBtn.IsEnabled = _currentReviewPage > 1;
            }

            if (NextPageBtn != null)
            {
                NextPageBtn.IsEnabled = _currentReviewPage < _totalReviewPages;
            }
        }

        // 5. Button Click Handlers
        private void PrevPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReviewPage > 1)
            {
                _currentReviewPage--;
                DisplayCurrentReviewPage();
            }
        }

        private void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReviewPage < _totalReviewPages)
            {
                _currentReviewPage++;
                DisplayCurrentReviewPage();
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
                Frame.Navigate(typeof(Apps), publisher);
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

        private void SetDownloadButtonsEnabled(bool isEnabled)
        {
            // Regular Buttons / Flyout items
            if (InstantDownloadBtn != null) InstantDownloadBtn.IsEnabled = isEnabled;
            if (DownloadOld != null) DownloadOld.IsEnabled = isEnabled;
            if (SaveAs != null) SaveAs.IsEnabled = isEnabled;

            // Bottom AppBar Buttons
            if (DownloadBar != null) DownloadBar.IsEnabled = isEnabled;
            if (DownloadOldBar != null) DownloadOldBar.IsEnabled = isEnabled;
            if (OpenProjectBar != null) OpenProjectBar.IsEnabled = isEnabled;
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

        // 1. GridView Item Click Handler
        private void ReviewsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var review = e.ClickedItem as Review;
            if (review != null)
            {
                ReviewDetailModal.Show(review);
            }
        }

        // 2. Metro Edge-to-Edge Review Modal Helper
        private class ReviewDetailModal
        {
            private Popup _popup;

            public static ReviewDetailModal Show(Review review)
            {
                if (review == null) return null;

                var modal = new ReviewDetailModal();
                var bounds = Window.Current.Bounds;

                // Root dim overlay (60% Black)
                var rootGrid = new Grid
                {
                    Width = bounds.Width,
                    Height = bounds.Height,
                    Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0))
                };

                // Darkened Accent surface (matching the Metro install/message box ribbon)
                Brush bannerBackground = new SolidColorBrush(Color.FromArgb(255, 20, 20, 24));
                if (Application.Current.Resources.ContainsKey("AccentBrush"))
                {
                    var accentBrush = Application.Current.Resources["AccentBrush"] as SolidColorBrush;
                    if (accentBrush != null)
                    {
                        Color c = accentBrush.Color;
                        byte darkR = (byte)(c.R * 0.35);
                        byte darkG = (byte)(c.G * 0.35);
                        byte darkB = (byte)(c.B * 0.35);
                        bannerBackground = new SolidColorBrush(Color.FromArgb(255, darkR, darkG, darkB));
                    }
                }

                // Full Edge-to-Edge Ribbon (No outline borders)
                var fullWidthBanner = new Border
                {
                    Width = bounds.Width,
                    Background = bannerBackground,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0, 32, 0, 32),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Inner standard Metro column container (centered on screen, content left-aligned)
                var contentStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Width = Math.Min(bounds.Width - 48, 720),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(24, 0, 24, 0)
                };

                // --- ROW 0: Reviewer Avatar + Details Header ---
                var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Avatar container with pure XAML fallback
                var avatarContainer = new Grid
                {
                    Width = 56,
                    Height = 56,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 16, 0)
                };

                var fallbackImg = new Image
                {
                    Source = new BitmapImage(new Uri("ms-appx:///Assets/user-200.png")),
                    Stretch = Stretch.UniformToFill,
                    Width = 56,
                    Height = 56
                };
                avatarContainer.Children.Add(fallbackImg);

                if (!string.IsNullOrWhiteSpace(review.avatar))
                {
                    try
                    {
                        var userAvatarImg = new Image
                        {
                            Source = new BitmapImage(new Uri(review.avatar)),
                            Stretch = Stretch.UniformToFill,
                            Width = 56,
                            Height = 56
                        };
                        avatarContainer.Children.Add(userAvatarImg);
                    }
                    catch { }
                }

                // Reviewer Name & Metadata
                var metaStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var nameText = new TextBlock
                {
                    Text = review.username ?? "Anonymous",
                    FontFamily = new FontFamily("Segoe UI Light"),
                    FontSize = 26,
                    FontWeight = FontWeights.Light,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                var bylineStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 4, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var starIcon = new TextBlock
                {
                    Text = "★",
                    FontSize = 14,
                    Foreground = (Application.Current.Resources.ContainsKey("AccentBrush")
                        ? Application.Current.Resources["AccentBrush"] as Brush
                        : new SolidColorBrush(Colors.White)),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var ratingText = new TextBlock
                {
                    Text = review.rating.ToString(),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 14,
                    Foreground = starIcon.Foreground,
                    Margin = new Thickness(4, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var bulletText = new TextBlock
                {
                    Text = "•",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120)),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var dateText = new TextBlock
                {
                    Text = review.updatedAt.ToString("MMM d, yyyy"),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 180, 185)),
                    VerticalAlignment = VerticalAlignment.Center
                };

                bylineStack.Children.Add(starIcon);
                bylineStack.Children.Add(ratingText);
                bylineStack.Children.Add(bulletText);
                bylineStack.Children.Add(dateText);

                metaStack.Children.Add(nameText);
                metaStack.Children.Add(bylineStack);

                Grid.SetColumn(avatarContainer, 0);
                Grid.SetColumn(metaStack, 1);
                headerGrid.Children.Add(avatarContainer);
                headerGrid.Children.Add(metaStack);

                // --- ROW 1: Scrollable Review Comment Body ---
                var commentScroll = new ScrollViewer
                {
                    MaxHeight = 280,
                    HorizontalScrollMode = ScrollMode.Disabled,
                    VerticalScrollMode = ScrollMode.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(0, 0, 0, 24)
                };

                var commentText = new TextBlock
                {
                    Text = review.comment ?? string.Empty,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 15,
                    LineHeight = 22,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220)),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                commentScroll.Content = commentText;

                // --- ROW 2: Metro Action Button (Close) ---
                var closeButton = new Button
                {
                    Content = "Close",
                    MinWidth = 120,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                closeButton.Click += (s, args) => modal.Close();

                contentStack.Children.Add(headerGrid);
                contentStack.Children.Add(commentScroll);
                contentStack.Children.Add(closeButton);

                fullWidthBanner.Child = contentStack;
                rootGrid.Children.Add(fullWidthBanner);

                // Clicking outside the ribbon also dismisses the dialog
                rootGrid.Tapped += (s, args) =>
                {
                    if (args.OriginalSource == rootGrid)
                    {
                        modal.Close();
                    }
                };

                modal._popup = new Popup
                {
                    Child = rootGrid,
                    IsOpen = true
                };

                return modal;
            }

            public void Close()
            {
                if (_popup != null)
                {
                    _popup.IsOpen = false;
                    _popup = null;
                }
            }
        }
    }
}
