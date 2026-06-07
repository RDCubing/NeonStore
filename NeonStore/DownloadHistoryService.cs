using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json;

namespace NeonStore
{
    public class DownloadHistoryService
    {
        public static DownloadHistoryService Instance { get; } = new DownloadHistoryService();

        public ObservableCollection<DownloadItem> Downloads { get; }
            = new ObservableCollection<DownloadItem>();

        private const string FileName = "downloads.json";

        // ✅ Add item + save
        public async void Add(DownloadItem item)
        {
            Downloads.Insert(0, item);
            await SaveAsync();
        }

        public async void Clear()
        {
            Downloads.Clear();
            await SaveAsync();
        }

        // ✅ Save to local storage
        public async Task SaveAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder
                    .CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);

                var json = JsonConvert.SerializeObject(Downloads);
                await FileIO.WriteTextAsync(file, json);
            }
            catch
            {
                // silent fail
            }
        }

        // ✅ Load from local storage
        public async Task LoadAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(FileName);
                var json = await FileIO.ReadTextAsync(file);

                var items = JsonConvert.DeserializeObject<List<DownloadItem>>(json);

                Downloads.Clear();

                if (items != null)
                {
                    foreach (var item in items)
                        Downloads.Add(item);
                }
            }
            catch
            {
                // first run = file not found, ignore
            }
        }
    }
}