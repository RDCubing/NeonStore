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

        public static ObservableCollection<AppItem> OtherApps { get; }
    = new ObservableCollection<AppItem>();

        public static ObservableCollection<string> Categories { get; }
    = new ObservableCollection<string>();

        public static async Task LoadAsync()
        {
            try
            {
                Debug.WriteLine("NeonStoreService: Loading...");

                HttpClient client = new HttpClient();

                string url =
                    "https://raw.githubusercontent.com/RDCubing/geekhubapi/main/projects.json?t=" +
                    DateTime.UtcNow.Ticks;

                string json = await client.GetStringAsync(new Uri(url));

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
                    OtherApps.Clear();
                    Categories.Clear();

                    foreach (var app in data.NeonStore)
                    {
                        NeonStore.Add(app);

                // TOP APPS
                if (app.TopApp == "Yes")
                            TopApps.Add(app);
                        else
                            OtherApps.Add(app);

                // CATEGORY (NEW)
                if (!string.IsNullOrEmpty(app.Category))
                        {
                            if (!Categories.Contains(app.Category))
                                Categories.Add(app.Category);
                        }

                        Debug.WriteLine("Added: " + app.Title);
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