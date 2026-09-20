using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace NeonStore
{
    public static class FeedbackService
    {
        private static HttpClient client = new HttpClient();
        private const string BaseApiUrl = "https://gdcr.dankassassin368.com/ns-status/feedback";

        public static string GetStoredAuthToken()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("token"))
            {
                return localSettings.Values["token"] as string;
            }

            return null;
        }

        public static string GetClientVersion()
        {
            try
            {
                var version = Package.Current.Id.Version;
                return string.Format("{0}.{1}.{2}.{3}",
                    version.Major,
                    version.Minor,
                    version.Build,
                    version.Revision);
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        /// <summary>
        /// Submits serialized JSON feedback payload with user Bearer token.
        /// </summary>
        public static async Task<string> SubmitFeedback(string token, string jsonBody)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    new Uri(BaseApiUrl)
                );

                request.Content = new HttpStringContent(
                    jsonBody,
                    Windows.Storage.Streams.UnicodeEncoding.Utf8,
                    "application/json"
                );

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization =
                        new HttpCredentialsHeaderValue("Bearer", token);
                }

                var response = await client.SendRequestAsync(request);
                string result = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[SUBMIT FEEDBACK] {response.StatusCode} - {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SUBMIT FEEDBACK ERROR] " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Convenience overload to package and dispatch feedback fields directly.
        /// </summary>
        public static async Task<string> SubmitFeedback(string feedbackType, string comments, int? rating = null)
        {
            string token = GetStoredAuthToken();

            var bodyObj = new
            {
                feedbackType = feedbackType ?? "General Feedback",
                comments = comments ?? "",
                clientVersion = GetClientVersion(),
                rating = rating
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(bodyObj);
            return await SubmitFeedback(token, json);
        }
    }
}