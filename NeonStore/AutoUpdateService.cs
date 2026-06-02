using System;
using System.Threading.Tasks;
using Windows.Web.Http;
using Newtonsoft.Json;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using System.IO;
using Windows.ApplicationModel;

namespace NeonStore
{
    public enum UpdateResult
    {
        Available,
        UpToDate,
        Failed
    }

    public class AutoUpdateService
    {
        public static async Task<UpdateResult> CheckAsync()
        {
            try
            {
                string updateUrl =
                    "https://raw.githubusercontent.com/RDCubing/geekhubapi/main/update.json?t="
                    + DateTime.UtcNow.Ticks;

                HttpClient client = new HttpClient();
                string json = await client.GetStringAsync(new Uri(updateUrl));

                UpdateInfo update =
                    JsonConvert.DeserializeObject<UpdateInfo>(json);

                Version current = GetAppVersion();
                Version latest = new Version(update.Version);

                if (latest > current)
                {
                    await ShowUpdateDialog(update);
                    return UpdateResult.Available;
                }

                return UpdateResult.UpToDate;
            }
            catch
            {
                return UpdateResult.Failed;
            }
        }

        // ✅ Package version helper
        private static Version GetAppVersion()
        {
            var v = Package.Current.Id.Version;
            return new Version($"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}");
        }

        private static async Task ShowUpdateDialog(UpdateInfo update)
        {
            MessageDialog dialog = new MessageDialog(
                update.Message + "\n\nVersion: " + update.Version,
                update.Name);

            dialog.Commands.Add(new UICommand("Update"));
            dialog.Commands.Add(new UICommand("Later"));

            var result = await dialog.ShowAsync();

            if (result.Label == "Update")
            {
                try
                {
                    var uri = new Uri(update.DownloadUrl);

                    ToastService.Show("Update started", update.Name, null);

                    var picker = new Windows.Storage.Pickers.FileSavePicker();
                    picker.SuggestedStartLocation =
                        Windows.Storage.Pickers.PickerLocationId.Downloads;

                    string fileName = Path.GetFileName(uri.LocalPath);

                    if (string.IsNullOrEmpty(fileName))
                        fileName = update.Name + ".appx";

                    picker.SuggestedFileName = fileName;

                    string extension = Path.GetExtension(uri.AbsolutePath);
                    if (string.IsNullOrEmpty(extension))
                        extension = ".appx";

                    picker.FileTypeChoices.Add(
                        "Package",
                        new System.Collections.Generic.List<string> { extension }
                    );

                    var file = await picker.PickSaveFileAsync();
                    if (file == null) return;

                    var client = new HttpClient();
                    var buffer = await client.GetBufferAsync(uri);

                    await FileIO.WriteBufferAsync(file, buffer);

                    ToastService.Show("Update downloaded", update.Name, null);

                    var installDialog = new MessageDialog(
                        $"\"{file.Name}\" downloaded. Install now?");

                    installDialog.Commands.Add(new UICommand("Open"));
                    installDialog.Commands.Add(new UICommand("Cancel"));

                    var result2 = await installDialog.ShowAsync();

                    if (result2.Label == "Open")
                    {
                        await Launcher.LaunchFileAsync(file);
                    }
                }
                catch (Exception ex)
                {
                    await new MessageDialog(ex.ToString(), "Update Error").ShowAsync();
                }
            }
        }

        
    }
}