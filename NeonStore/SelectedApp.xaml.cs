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
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        public ObservableCollection<AppItem> OtherApps
        {
            get
            {
                return new ObservableCollection<AppItem>(
                    AppData.NeonStore
                        .Where(a => a.Title != AppState.SelectedApp?.Title)
                        .Take(3)
                );
            }
        }

        private void OtherApps_ItemClick(object sender, ItemClickEventArgs e)
        {
            var app = (AppItem)e.ClickedItem;

            AppState.SelectedApp = app;

            Frame.Navigate(typeof(SelectedApp));
        }

        private async void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var project = this.DataContext as AppItem;
                if (project?.DownloadUrl == null)
                    return;

                var uri = new Uri(project.DownloadUrl);

                // ✅ Notification: download started
                ToastService.Show("Download started", project.Title);

                var picker = new Windows.Storage.Pickers.FileSavePicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
                picker.SuggestedFileName = project.Title;

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
                ToastService.Show("Download complete", project.Title);

                var dialog = new MessageDialog($"\"{file.Name}\" downloaded. Open it?");
                dialog.Commands.Add(new UICommand("Open"));
                dialog.Commands.Add(new UICommand("Cancel"));

                var result = await dialog.ShowAsync();

                if (result.Label == "Open")
                {
                    await Windows.System.Launcher.LaunchFileAsync(file);
                }
            }
            catch (Exception ex)
            {
                var dlg = new MessageDialog(ex.ToString(), "Crash Debug");
                await dlg.ShowAsync();
            }
        }

        #endregion
    }
}
