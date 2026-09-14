using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Popups;
using Windows.Storage;
using Newtonsoft.Json.Linq;

namespace NeonStore
{
    public sealed partial class Account : SettingsFlyout
    {
        private DispatcherTimer _sessionTimer;

        public Account()
        {
            this.InitializeComponent();
            this.Unloaded += Account_Unloaded;
            // Refresh UI automatically if Window gains focus after browser sign-in
            Window.Current.Activated += Current_Activated;
        }

        private void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState != Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                UpdateUI();
            }
        }

        private async void SettingsFlyout_Loaded(object sender, RoutedEventArgs e)
        {
            // Check token expiration first
            bool expired = await AuthService.LogoutIfExpired();

            // Instant optimistic render from local settings
            UpdateUI();

            if (AuthService.IsLoggedIn && !expired)
            {
                StartSessionCountdown();

                // Hydrate live profile (avatar and updated username) from backend /me
                bool synced = await AuthService.SyncProfileAsync();
                if (synced)
                {
                    UpdateUI();
                }
            }
        }

        private void Account_Unloaded(object sender, RoutedEventArgs e)
        {
            StopSessionCountdown();
        }

        private async void UpdateUI()
        {
            var username = ApplicationData.Current.LocalSettings.Values["username"] as string;
            var avatarUrl = ApplicationData.Current.LocalSettings.Values["avatar"] as string;

            if (!string.IsNullOrEmpty(username) && AuthService.IsLoggedIn)
            {
                ProfilePanel.Visibility = Visibility.Visible;
                LoginStatusText.Text = "Logged in as " + username;
                SignOutButton.Visibility = Visibility.Visible;

                SignInButton.Visibility = Visibility.Collapsed;
                RegisterButton.Visibility = Visibility.Collapsed;
                DiscordSignInButton.Visibility = Visibility.Collapsed;

                // Handle profile picture visibility and Discord CDN normalization
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    try
                    {
                        if (avatarUrl.Contains("cdn.discordapp.com") && !avatarUrl.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        {
                            avatarUrl = avatarUrl.Split('?')[0] + ".png?size=128";
                        }

                        ProfileAvatarBrush.ImageSource = new BitmapImage(new Uri(avatarUrl));
                        ProfileAvatarContainer.Visibility = Visibility.Visible;
                    }
                    catch
                    {
                        ProfileAvatarContainer.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    ProfileAvatarBrush.ImageSource = null;
                    ProfileAvatarContainer.Visibility = Visibility.Collapsed;
                }

                UpdateExpiryCountdown();

                // Fetch and display user's review count
                int reviewCount = await AuthService.GetUserReviewCountAsync();
                UserReviewCountText.Text = reviewCount.ToString();
            }
            else
            {
                StopSessionCountdown();

                ProfilePanel.Visibility = Visibility.Collapsed;
                ProfileAvatarContainer.Visibility = Visibility.Collapsed;
                ProfileAvatarBrush.ImageSource = null;
                SignOutButton.Visibility = Visibility.Collapsed;

                // Reset review count when logged out
                UserReviewCountText.Text = "0";

                SignInButton.Visibility = Visibility.Visible;
                RegisterButton.Visibility = Visibility.Visible;
                DiscordSignInButton.Visibility = Visibility.Visible;
            }
        }

        private void StartSessionCountdown()
        {
            StopSessionCountdown();
            UpdateExpiryCountdown();

            _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _sessionTimer.Tick += SessionTimer_Tick;
            _sessionTimer.Start();
        }

        private void StopSessionCountdown()
        {
            if (_sessionTimer != null)
            {
                _sessionTimer.Stop();
                _sessionTimer = null;
            }
        }

        private async void SessionTimer_Tick(object sender, object e)
        {
            if (AuthService.IsTokenExpired())
            {
                StopSessionCountdown();
                await AuthService.LogoutIfExpired();
                UpdateUI();
            }
            else
            {
                UpdateExpiryCountdown();
            }
        }

        private void UpdateExpiryCountdown()
        {
            SessionExpiryText.Text = $"Expires: {AuthService.GetFormattedRemainingTokenTime()}";
        }

        private async void DiscordSignIn_Click(object sender, RoutedEventArgs e)
        {
            bool launched = await AuthService.LoginWithDiscordAsync();

            if (launched)
            {
                await new MessageDialog(
                    "Discord sign-in has opened in your default browser. Complete your login there to continue.",
                    "Opening Browser"
                ).ShowAsync();
            }
            else
            {
                await new MessageDialog(
                    "Could not open the browser for Discord sign in.",
                    "Sign In Failed"
                ).ShowAsync();
            }
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
                    UpdateUI();
                    StartSessionCountdown();

                    if (SignInButton.Flyout != null)
                    {
                        SignInButton.Flyout.Hide();
                    }

                    PasswordBox.Password = "";
                    this.Hide();

                    await new MessageDialog(
                        "Welcome, " + (AuthService.CurrentUsername ?? UsernameBox.Text) + "!",
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
                if (RegisterButton.Flyout != null)
                {
                    RegisterButton.Flyout.Hide();
                }

                UsernameBox.Text = RegUsernameBox.Text;
                RegPasswordBox.Password = "";
                ConfirmPasswordBox.Password = "";

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
            AuthService.Logout();
            StopSessionCountdown();

            UpdateUI();

            UsernameBox.Text = "";
            PasswordBox.Password = "";

            this.Hide();
        }

        private void CancelSignOut_Click(object sender, RoutedEventArgs e)
        {
            var flyout = SignOutButton.Flyout;
            if (flyout != null) flyout.Hide();
        }

        private void SettingsFlyout_Unloaded(object sender, RoutedEventArgs e)
        {
            StopSessionCountdown();
        }
    }
}