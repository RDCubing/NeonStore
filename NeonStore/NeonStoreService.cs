using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Web.Http;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace NeonStore
{
    public static class NeonStoreService
    {
        public static ObservableCollection<AppItem> NeonStore { get; }
            = new ObservableCollection<AppItem>();

        public static ObservableCollection<AppItem> TopApps { get; }
    = new ObservableCollection<AppItem>();

        public static async Task LoadAsync()
        {
            try
            {
                Debug.WriteLine("NeonStoreService: Loading...");

                HttpClient client = new HttpClient();

                // SAFE cache buster (VS2015 compatible)
                string url =
                    "https://raw.githubusercontent.com/RDCubing/geekhubapi/main/projects.json?t=" +
                    DateTime.UtcNow.Ticks;

                Debug.WriteLine("URL: " + url);

                string json = await client.GetStringAsync(new Uri(url));

                Debug.WriteLine("Downloaded JSON size: " + json.Length);

                RootObject data = JsonConvert.DeserializeObject<RootObject>(json);

                if (data?.NeonStore == null)
                {
                    Debug.WriteLine("NeonStore is NULL");
                    return;
                }

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
    CoreDispatcherPriority.Normal,
    () =>
    {
        NeonStore.Clear();
        TopApps.Clear();

        foreach (var app in data.NeonStore)
        {
            NeonStore.Add(app);
            Debug.WriteLine("Added: " + app.Title);

            if (app.TopApp == "Yes")
            {
                TopApps.Add(app);
                Debug.WriteLine("TopApp Added: " + app.Title);
            }
        }
    });

                Debug.WriteLine("NeonStoreService: Done ✔");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NeonStoreService ERROR: " + ex.Message);
            }
        }
    }
}