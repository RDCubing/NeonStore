using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        // ✅ Add item without duplicates + save
        public async void Add(DownloadItem item)
        {
            if (item == null) return;

            // Check and remove existing item matching DownloadUrl, FileName, or Title
            var existing = Downloads.FirstOrDefault(d =>
                (!string.IsNullOrEmpty(item.DownloadUrl) && string.Equals(d.DownloadUrl, item.DownloadUrl, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(item.FileName) && string.Equals(d.FileName, item.FileName, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(item.Title) && string.Equals(d.Title, item.Title, StringComparison.OrdinalIgnoreCase)));

            if (existing != null)
            {
                Downloads.Remove(existing);
            }

            Downloads.Insert(0, item);
            BadgeService.UpdateCount(Downloads.Count);
            await SaveAsync();
        }

        public async void Remove(DownloadItem item)
        {
            if (item == null) return;

            if (Downloads.Remove(item))
            {
                BadgeService.UpdateCount(Downloads.Count);
                await SaveAsync();
            }
        }

        public async void Clear()
        {
            Downloads.Clear();
            BadgeService.UpdateCount(0);
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

        // ✅ Load from local storage with duplicate deduplication safeguard
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
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var item in items)
                    {
                        string key = !string.IsNullOrEmpty(item.DownloadUrl)
                            ? item.DownloadUrl
                            : item.FileName ?? item.Title;

                        if (!string.IsNullOrEmpty(key) && seen.Add(key))
                        {
                            Downloads.Add(item);
                        }
                    }
                }
                BadgeService.UpdateCount(Downloads.Count);
            }
            catch
            {
                // first run = file not found, ignore
            }
        }
    }
}