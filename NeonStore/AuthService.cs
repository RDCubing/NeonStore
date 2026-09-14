using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace NeonStore
{
    public static class AuthService
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private const string BaseApiUrl = "https://gdcr.dankassassin368.com/ns-status/auth";

        #region Session Helpers

        public static string CurrentToken => localSettings.Values["token"] as string;
        public static string CurrentUsername => localSettings.Values["username"] as string;
        public static string CurrentAvatar => localSettings.Values["avatar"] as string;
        public static bool IsLoggedIn => !string.IsNullOrEmpty(CurrentToken);

        public static void SaveSession(string token, string username, string avatar = null)
        {
            if (!string.IsNullOrEmpty(token)) localSettings.Values["token"] = token;
            if (!string.IsNullOrEmpty(username)) localSettings.Values["username"] = username;

            if (!string.IsNullOrEmpty(avatar))
            {
                localSettings.Values["avatar"] = avatar;
            }
            else
            {
                localSettings.Values.Remove("avatar");
            }
        }

        public static void Logout()
        {
            localSettings.Values.Remove("token");
            localSettings.Values.Remove("username");
            localSettings.Values.Remove("avatar");
        }

        #endregion

        #region Discord OAuth2 Authentication

        /// <summary>
        /// Launches the modern system default browser to bypass Trident/IE11 script crashes in WebAuthenticationBroker.
        /// </summary>
        public static async Task<bool> LoginWithDiscordAsync(string appProtocol = "neonstore")
        {
            try
            {
                // Add app_protocol so the web dashboard knows which desktop client to call back
                Uri authUri = new Uri($"{BaseApiUrl}/discord?origin=https://gdcr.dankassassin368.com&app_protocol={Uri.EscapeDataString(appProtocol)}");
                bool launched = await Launcher.LaunchUriAsync(authUri);

                if (launched)
                {
                    Debug.WriteLine("Discord OAuth: System browser launched successfully.");
                }
                else
                {
                    Debug.WriteLine("Discord OAuth: Failed to launch system browser.");
                }

                return launched;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Discord OAuth Exception: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Direct exchange for authorization codes via the POST /discord/token native endpoint.
        /// </summary>
        public static async Task<string> ExchangeDiscordTokenAsync(string code, string redirectUri = null)
        {
            try
            {
                var payload = new
                {
                    code,
                    redirect_uri = redirectUri
                };

                string json = JsonConvert.SerializeObject(payload);
                var content = new HttpStringContent(
                    json,
                    Windows.Storage.Streams.UnicodeEncoding.Utf8,
                    "application/json"
                );

                var response = await client.PostAsync(new Uri($"{BaseApiUrl}/discord/token"), content);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JObject.Parse(result);
                    string token = data["token"]?.Value<string>();
                    string username = data["user"]?["username"]?.Value<string>();
                    string avatar = data["user"]?["avatar"]?.Value<string>();

                    SaveSession(token, username, avatar);
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ExchangeDiscordToken ERROR: " + ex.Message);
                return "{ \"error\": \"Discord token exchange failed\" }";
            }
        }

        #endregion

        #region Live Profile Hydration (/me)

        /// <summary>
        /// Fetches the live database profile using the current JWT and updates local storage.
        /// </summary>
        public static async Task<bool> SyncProfileAsync()
        {
            string token = CurrentToken;
            if (string.IsNullOrEmpty(token)) return false;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{BaseApiUrl}/me"));
                request.Headers.Authorization = new HttpCredentialsHeaderValue("Bearer", token);

                var response = await client.SendRequestAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var profile = JObject.Parse(json);

                    string username = profile["username"]?.Value<string>();
                    string avatar = profile["avatar"]?.Value<string>();

                    if (!string.IsNullOrEmpty(username)) localSettings.Values["username"] = username;
                    if (!string.IsNullOrEmpty(avatar)) localSettings.Values["avatar"] = avatar;
                    else localSettings.Values.Remove("avatar");

                    return true;
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Logout();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SyncProfile ERROR: " + ex.Message);
            }

            return false;
        }

        #endregion

        #region Traditional Auth

        public static async Task<string> Login(string username, string password)
        {
            try
            {
                Debug.WriteLine("AuthService: Login started");

                var body = new { username, password };
                string json = JsonConvert.SerializeObject(body);

                var content = new HttpStringContent(
                    json,
                    Windows.Storage.Streams.UnicodeEncoding.Utf8,
                    "application/json"
                );

                var response = await client.PostAsync(new Uri($"{BaseApiUrl}/login"), content);
                string result = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("Login Response: " + result);

                if (response.IsSuccessStatusCode)
                {
                    var data = JObject.Parse(result);
                    string token = data["token"]?.Value<string>();
                    string returnedUser = data["username"]?.Value<string>() ?? username;
                    string avatar = data["avatar"]?.Value<string>();

                    SaveSession(token, returnedUser, avatar);
                }

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

                var body = new { email, username, password };
                string json = JsonConvert.SerializeObject(body);

                var content = new HttpStringContent(
                    json,
                    Windows.Storage.Streams.UnicodeEncoding.Utf8,
                    "application/json"
                );

                var response = await client.PostAsync(new Uri($"{BaseApiUrl}/register"), content);
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

        #endregion

        #region JWT Expiration & Countdown Methods

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
        public static async Task<int> GetUserReviewCountAsync()
        {
            string token = CurrentToken;
            if (string.IsNullOrEmpty(token)) return 0;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{BaseApiUrl}/me"));
                request.Headers.Authorization = new HttpCredentialsHeaderValue("Bearer", token);

                var response = await client.SendRequestAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var profile = JObject.Parse(json);

                    // Extracts stats.reviews from the /me response payload
                    return profile["stats"]?["reviews"]?.Value<int>() ?? 0;
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Logout();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetUserReviewCountAsync ERROR: " + ex.Message);
            }

            return 0;
        }

        #endregion
    }
}