using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace NeonStore
{
    public static class AppSubmissionService
    {
        private static readonly HttpClient client = new HttpClient();
        private const string SubmitEndpoint = "https://gdcr.dankassassin368.com/ns-status/apps/submit";

        public static async Task<string> SubmitApp(AppSubmissionModel model)
        {
            try
            {
                string token = AuthService.CurrentToken;
                if (string.IsNullOrEmpty(token))
                {
                    return "{ \"error\": \"User is not authenticated.\" }";
                }

                string json = JsonConvert.SerializeObject(model);
                var content = new HttpStringContent(
                    json,
                    Windows.Storage.Streams.UnicodeEncoding.Utf8,
                    "application/json"
                );

                using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(SubmitEndpoint)))
                {
                    request.Content = content;
                    request.Headers.Authorization = new HttpCredentialsHeaderValue("Bearer", token);

                    var response = await client.SendRequestAsync(request);
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Submission Error: " + ex.Message);
                return $"{{ \"error\": \"{ex.Message}\" }}";
            }
        }
    }
}