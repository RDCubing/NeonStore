using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Web.Http;

namespace NeonStore
{
    public static class AuthService
    {
        private static HttpClient client = new HttpClient();
        private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        // Session helpers
        public static string CurrentToken => localSettings.Values["token"] as string;
        public static string CurrentUsername => localSettings.Values["username"] as string;
        public static bool IsLoggedIn => !string.IsNullOrEmpty(CurrentToken);

        public static void Logout()
        {
            localSettings.Values.Remove("token");
            localSettings.Values.Remove("username");
        }

        public static async Task<string> Login(string username, string password)
        {
            try
            {
                Debug.WriteLine("AuthService: Login started");

                var body = new
                {
                    username,
                    password
                };

                string json = JsonConvert.SerializeObject(body);
                Debug.WriteLine("Login JSON: " + json);

                var content = new HttpStringContent(
                    json,
                    Windows.Storage.Streams.UnicodeEncoding.Utf8,
                    "application/json"
                );

                var response = await client.PostAsync(
                    new Uri("https://gdcr.dankassassin368.com/ns-status/auth/login"),
                    content
                );

                string result = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("Login Response: " + result);

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Login ERROR: " + ex.Message);
                return "{ \"error\": \"Login failed\" }";
            }
        }

        public static async Task<string> Register(string email, string username, string password)
        {
            try
            {
                Debug.WriteLine("AuthService: Register started");

                var body = new
                {
                    email,
                    username,
                    password
                };

                string json = JsonConvert.SerializeObject(body);
                Debug.WriteLine("Register JSON: " + json);

                var content = new HttpStringContent(
                    json,
                    Windows.Storage.Streams.UnicodeEncoding.Utf8,
                    "application/json"
                );

                var response = await client.PostAsync(
                    new Uri("https://gdcr.dankassassin368.com/ns-status/auth/register"),
                    content
                );

                string result = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("Register Response: " + result);

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Register ERROR: " + ex.Message);
                return "{ \"error\": \"Register failed\" }";
            }
        }

        #region JWT Expiration & Countdown Methods

        /// <summary>
        /// Formats the remaining session time including days (e.g., "5d 14:32:05" or "14:32:05").
        /// </summary>
        public static string GetFormattedRemainingTokenTime()
        {
            TimeSpan remaining = GetRemainingTokenTime();

            if (remaining.TotalSeconds <= 0)
            {
                return "00:00:00";
            }

            if (remaining.Days > 0)
            {
                return $"{remaining.Days}d {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }

            return $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        public static DateTime? GetTokenExpirationDate()
        {
            string token = CurrentToken;
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                string[] parts = token.Split('.');
                if (parts.Length < 2) return null;

                string payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                byte[] bytes = Convert.FromBase64String(payload);
                string json = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                var claims = JObject.Parse(json);
                if (claims["exp"] != null)
                {
                    long expSeconds = claims["exp"].Value<long>();
                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    return epoch.AddSeconds(expSeconds);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("JWT Decode Error: " + ex.Message);
            }

            return null;
        }

        public static TimeSpan GetRemainingTokenTime()
        {
            DateTime? expDate = GetTokenExpirationDate();
            if (!expDate.HasValue) return TimeSpan.Zero;

            TimeSpan remaining = expDate.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public static bool IsTokenExpired()
        {
            DateTime? expDate = GetTokenExpirationDate();
            if (!expDate.HasValue) return true;

            return DateTime.UtcNow >= expDate.Value;
        }

        public static async Task<bool> LogoutIfExpired()
        {
            if (string.IsNullOrEmpty(CurrentToken)) return false;

            if (IsTokenExpired())
            {
                Logout();

                var dialog = new MessageDialog(
                    "Your session has expired. Please sign in again to continue.",
                    "Session Expired"
                );
                await dialog.ShowAsync();

                return true;
            }

            return false;
        }

        #endregion
    }
}
