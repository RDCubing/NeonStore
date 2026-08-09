using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace NeonStore
{
    public static class AuthService
    {
        private static HttpClient client = new HttpClient();

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
    }
}
