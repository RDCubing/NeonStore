using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace NeonStore
{
    public static class ReviewService
    {
        private static HttpClient client = new HttpClient();
        private const string BaseApiUrl = "https://gdcr.dankassassin368.com/ns-status/reviews";

        public static async Task<string> GetReviews(string appId)
        {
            string url = $"{BaseApiUrl}/{appId}?t={DateTime.UtcNow.Ticks}";

            var response = await client.GetAsync(new System.Uri(url));
            string result = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine("[GET REVIEWS] " + result);
            return result;
        }

        public static async Task<string> SubmitReview(string token, string jsonBody)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                new System.Uri(BaseApiUrl)
            );

            request.Content = new HttpStringContent(
                jsonBody,
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/json"
            );

            request.Headers.Authorization =
                new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Bearer", token);

            var response = await client.SendRequestAsync(request);
            string result = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine("[POST REVIEW] " + result);
            return result;
        }

        public static async Task<bool> DeleteReview(string token, string reviewId)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Delete,
                    new System.Uri($"{BaseApiUrl}/{reviewId}")
                );

                request.Headers.Authorization =
                    new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Bearer", token);

                var response = await client.SendRequestAsync(request);
                string result = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[DELETE REVIEW {reviewId}] {response.StatusCode} - {result}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DELETE REVIEW ERROR] {ex.Message}");
                return false;
            }
        }

        private static string CacheBuster()
        {
            return DateTime.UtcNow.Ticks.ToString();
        }
    }
}