using System;
using System.Diagnostics;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;

namespace NeonStore
{
    public class AvatarFallbackConverter : IValueConverter
    {
        private static readonly Uri DefaultAvatarUri = new Uri("ms-appx:///Assets/user-200.png");
        private static readonly HttpClient httpClient = new HttpClient();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string url = value as string;

            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(DefaultAvatarUri);
            }

            var bitmap = new BitmapImage();
            LoadRemoteImage(bitmap, url);
            return bitmap;
        }

        private async void LoadRemoteImage(BitmapImage target, string url)
        {
            try
            {
                // Force PNG format and append size query
                if (url.Contains("cdn.discordapp.com") && !url.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    url = url.Split('?')[0] + ".png?size=128";
                }

                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await httpClient.SendRequestAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var buffer = await response.Content.ReadAsBufferAsync();
                    var randomAccessStream = new InMemoryRandomAccessStream();
                    await randomAccessStream.WriteAsync(buffer);
                    randomAccessStream.Seek(0);

                    await target.SetSourceAsync(randomAccessStream);
                }
                else
                {
                    Debug.WriteLine($"[Avatar] HTTP Error: {response.StatusCode} for {url}");
                    target.UriSource = DefaultAvatarUri;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Avatar] Exception loading {url}: {ex.Message}");
                target.UriSource = DefaultAvatarUri;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}