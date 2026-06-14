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

        public static async Task<string> GetReviews(string appId)
        {
            string url =
                "https://neonstore-api.onrender.com/reviews/" +
                appId +
                "?t=" +
                DateTime.UtcNow.Ticks;

            var response = await client.GetAsync(
                new System.Uri(url)
            );

            string result = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine("[GET REVIEWS] " + result);

            return result;
        }

        public static async Task<string> SubmitReview(string token, string jsonBody)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                new System.Uri("https://neonstore-api.onrender.com/reviews")
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

        private static string CacheBuster()
        {
            return DateTime.UtcNow.Ticks.ToString();
        }
    }
}
