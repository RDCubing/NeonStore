using Newtonsoft.Json.Linq;
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace NeonStore
{
    public sealed partial class Feedback : SettingsFlyout
    {
        public Feedback()
        {
            this.InitializeComponent();
            this.Loaded += Feedback_Loaded;
        }

        private async void Feedback_Loaded(object sender, RoutedEventArgs e)
        {
            bool expired = await AuthService.LogoutIfExpired();
            CheckAuthAndToggleForm(!expired && AuthService.IsLoggedIn);
        }

        private void CheckAuthAndToggleForm(bool isAuthenticated)
        {
            if (isAuthenticated)
            {
                AccountRequiredPanel.Visibility = Visibility.Collapsed;
                FeedbackFormPanel.Visibility = Visibility.Visible;
            }
            else
            {
                AccountRequiredPanel.Visibility = Visibility.Visible;
                FeedbackFormPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void SubmitFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            string comments = FeedbackTextBox.Text;

            if (string.IsNullOrWhiteSpace(comments))
            {
                ShowStatus("Please enter your comments before submitting.", false);
                return;
            }

            string token = FeedbackService.GetStoredAuthToken();
            if (string.IsNullOrEmpty(token) || !AuthService.IsLoggedIn)
            {
                ShowStatus("You must be signed in to submit feedback.", false);
                CheckAuthAndToggleForm(false);
                return;
            }

            var selectedTypeItem = FeedbackTypeComboBox.SelectedItem as ComboBoxItem;
            string feedbackType = selectedTypeItem != null ? selectedTypeItem.Content.ToString() : "General Feedback";

            int? rating = null;
            if (RatingComboBox.SelectedIndex > 0)
            {
                rating = 6 - RatingComboBox.SelectedIndex;
            }

            SubmitFeedbackButton.IsEnabled = false;
            SubmissionProgressBar.Visibility = Visibility.Visible;
            StatusTextBlock.Visibility = Visibility.Collapsed;

            // Submit using the new FeedbackService
            string responseJson = await FeedbackService.SubmitFeedback(feedbackType, comments.Trim(), rating);

            SubmissionProgressBar.Visibility = Visibility.Collapsed;
            SubmitFeedbackButton.IsEnabled = true;

            if (string.IsNullOrEmpty(responseJson))
            {
                ShowStatus("Network error: Unable to contact the feedback service.", false);
                return;
            }

            bool isSuccess = false;
            string message = "Feedback submitted successfully.";

            try
            {
                var json = JObject.Parse(responseJson);

                // Detect standard success flag or presence of error string
                if (json["error"] != null)
                {
                    isSuccess = false;
                    message = json["error"].ToString();
                }
                else if (json["message"] != null)
                {
                    message = json["message"].ToString();
                    isSuccess = json["success"] != null ? (bool)json["success"] : true;
                }
                else
                {
                    isSuccess = true;
                }
            }
            catch
            {
                // Fallback for non-JSON responses
                isSuccess = responseJson.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isSuccess) message = responseJson;
            }

            if (isSuccess)
            {
                ShowStatus(message, true);
                FeedbackTextBox.Text = string.Empty;
                RatingComboBox.SelectedIndex = 0;
                FeedbackTypeComboBox.SelectedIndex = 0;
            }
            else
            {
                ShowStatus(message, false);

                if (message.IndexOf("unauthorized", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    message.IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    message.IndexOf("signed in", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bool expired = await AuthService.LogoutIfExpired();
                    CheckAuthAndToggleForm(!expired && AuthService.IsLoggedIn);
                }
            }
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isSuccess
                ? new SolidColorBrush(Color.FromArgb(255, 78, 201, 120))
                : new SolidColorBrush(Color.FromArgb(255, 235, 87, 87));
            StatusTextBlock.Visibility = Visibility.Visible;
        }
    }
}