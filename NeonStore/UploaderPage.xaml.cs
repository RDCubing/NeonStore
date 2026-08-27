using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using NeonStore.Common;
using Windows.Storage;

namespace NeonStore
{
    public sealed partial class UploaderPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public UploaderPage()
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
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            bool reduceMotion =
(bool?)ApplicationData.Current.LocalSettings.Values["ReduceMotion"] ?? false;

            if (!reduceMotion)
            {
                SlideInStoryboard.Begin();
                SlideInStoryboard1.Begin();
            }
            else
            {
                MainPanelTransform.X = 0;
                MainPanelTransform1.X = 0;
            }
            bool expired = await AuthService.LogoutIfExpired();
            CheckAuthAndToggleForm(!expired && AuthService.IsLoggedIn);
        }

        private void CheckAuthAndToggleForm(bool isAuthenticated)
        {
            if (isAuthenticated)
            {
                SubmissionFormPanel.Visibility = Visibility.Visible;
                AuthRequiredPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                SubmissionFormPanel.Visibility = Visibility.Collapsed;
                AuthRequiredPanel.Visibility = Visibility.Visible;
            }
        }

        private void CancelSubmit_Click(object sender, RoutedEventArgs e)
        {
            SubmitConfirmFlyout.Hide();
        }

        private async void ConfirmSubmit_Click(object sender, RoutedEventArgs e)
        {
            SubmitConfirmFlyout.Hide();

            if (AuthService.IsTokenExpired())
            {
                await AuthService.LogoutIfExpired();
                CheckAuthAndToggleForm(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(TitleBox.Text) ||
                string.IsNullOrWhiteSpace(CategoryBox.Text) ||
                string.IsNullOrWhiteSpace(SubtitleBox.Text) ||
                string.IsNullOrWhiteSpace(PublisherBox.Text) ||
                string.IsNullOrWhiteSpace(IconUrlBox.Text) ||
                string.IsNullOrWhiteSpace(ScreenshotUrlBox.Text) ||
                string.IsNullOrWhiteSpace(VersionBox.Text) ||
                string.IsNullOrWhiteSpace(FrameworkBox.Text) ||
                string.IsNullOrWhiteSpace(DescriptionBox.Text) ||
                string.IsNullOrWhiteSpace(DownloadUrlBox.Text))
            {
                await new MessageDialog("Please fill out all fields before submitting.", "Missing Information").ShowAsync();
                return;
            }

            SubmitButton.IsEnabled = false;

            var model = new AppSubmissionModel
            {
                Title = TitleBox.Text.Trim(),
                Category = CategoryBox.Text.Trim(),
                Subtitle = SubtitleBox.Text.Trim(),
                Publisher = PublisherBox.Text.Trim(),
                ImagePath = IconUrlBox.Text.Trim(),
                DetailImagePath = ScreenshotUrlBox.Text.Trim(),
                Version = VersionBox.Text.Trim(),
                Framework = FrameworkBox.Text.Trim(),
                Description = DescriptionBox.Text.Trim(),
                DownloadUrl = DownloadUrlBox.Text.Trim()
            };

            string responseJson = await AppSubmissionService.SubmitApp(model);
            SubmitButton.IsEnabled = true;

            try
            {
                JObject res = JObject.Parse(responseJson);

                if (res["success"] != null && (bool)res["success"])
                {
                    string msg = res["message"]?.ToString() ?? "Your application has been submitted and is pending review.";
                    await new MessageDialog(msg, "Submission Successful").ShowAsync();
                    ClearForm();
                }
                else
                {
                    string err = res["error"]?.ToString() ?? "An error occurred while submitting the application.";
                    await new MessageDialog(err, "Submission Failed").ShowAsync();
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Failed to process response: " + ex.Message, "Error").ShowAsync();
            }
        }

        private void ClearForm()
        {
            TitleBox.Text = string.Empty;
            CategoryBox.Text = string.Empty;
            SubtitleBox.Text = string.Empty;
            PublisherBox.Text = string.Empty;
            IconUrlBox.Text = string.Empty;
            ScreenshotUrlBox.Text = string.Empty;
            VersionBox.Text = string.Empty;
            FrameworkBox.Text = string.Empty;
            DescriptionBox.Text = string.Empty;
            DownloadUrlBox.Text = string.Empty;
        }
    }
}