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
using Newtonsoft.Json.Linq;
using Windows.UI.Popups;
using Windows.Storage;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace NeonStore
{
    public sealed partial class Account : SettingsFlyout
    {
        public Account()
        {
            this.InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string result = await AuthService.Login(
                    UsernameBox.Text,
                    PasswordBox.Password
                );

                JObject obj = JObject.Parse(result);

                if (obj["token"] != null)
                {
                    string token = obj["token"].ToString();
                    string username = obj["username"].ToString();

                    // Save session
                    ApplicationData.Current.LocalSettings.Values["token"] = token;
                    ApplicationData.Current.LocalSettings.Values["username"] = username;

                    // Update UI
                    LoginStatusText.Text = "Logged in as " + username;
                    LoginStatusText.Visibility = Visibility.Visible;
                    SignOutButton.Visibility = Visibility.Visible;

                    SignInButton.Visibility = Visibility.Collapsed;
                    RegisterButton.Visibility = Visibility.Collapsed;

                    this.Hide();

                    await new MessageDialog(
                        "Welcome, " + username + "!",
                        "Login successful"
                    ).ShowAsync();
                }
                else
                {
                    await new MessageDialog(
                        obj["error"]?.ToString() ?? "Login failed",
                        "Login failed"
                    ).ShowAsync();
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(
                    "Login error: " + ex.Message,
                    "Login error"
                ).ShowAsync();
            }
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            if (RegPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                await new MessageDialog(
                    "Passwords do not match",
                    "Registration error"
                ).ShowAsync();

                return;
            }

            string result = await AuthService.Register(
                EmailBox.Text,
                RegUsernameBox.Text,
                RegPasswordBox.Password
            );

            JObject obj = JObject.Parse(result);

            if (obj["success"] != null)
            {
                await new MessageDialog(
                    "Account created! You can now sign in.",
                    "Registration successful"
                ).ShowAsync();
            }
            else
            {
                await new MessageDialog(
                    obj["error"]?.ToString() ?? "Registration failed",
                    "Registration failed"
                ).ShowAsync();
            }
        }

        private void ConfirmSignOut_Click(object sender, RoutedEventArgs e)
        {
            // Clear stored session
            ApplicationData.Current.LocalSettings.Values.Remove("token");
            ApplicationData.Current.LocalSettings.Values.Remove("username");

            // Reset UI
            LoginStatusText.Visibility = Visibility.Collapsed;
            SignOutButton.Visibility = Visibility.Collapsed;

            SignInButton.Visibility = Visibility.Visible;
            RegisterButton.Visibility = Visibility.Visible;

            UsernameBox.Text = "";
            PasswordBox.Password = "";

            // close flyout + settings
            this.Hide();
        }

        private void CancelSignOut_Click(object sender, RoutedEventArgs e)
        {
            // just close flyout automatically
            var btn = SignOutButton;
            var flyout = btn.Flyout;
            flyout.Hide();
        }

        private void SettingsFlyout_Loaded(object sender, RoutedEventArgs e)
        {
            var username = ApplicationData.Current.LocalSettings.Values["username"] as string;

            if (!string.IsNullOrEmpty(username))
            {
                LoginStatusText.Text = "Logged in as " + username;

                LoginStatusText.Visibility = Visibility.Visible;
                SignOutButton.Visibility = Visibility.Visible;

                SignInButton.Visibility = Visibility.Collapsed;
                RegisterButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoginStatusText.Visibility = Visibility.Collapsed;
                SignOutButton.Visibility = Visibility.Collapsed;

                SignInButton.Visibility = Visibility.Visible;
                RegisterButton.Visibility = Visibility.Visible;
            }
        }
    }
}
