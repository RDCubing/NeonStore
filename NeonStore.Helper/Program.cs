using System;
using System.IO;
using System.Threading.Tasks;

namespace NeonStore.Helper
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. Auto-register protocol in current user registry
            ProtocolRegistrar.EnsureRegistered();

            // If user double-clicks the .exe manually
            if (args.Length == 0)
            {
                Console.Title = "NeonStore Helper";
                Console.WriteLine("NeonStore Sideload Helper is registered and ready.");
                Console.WriteLine("This program opens automatically when installing apps from NeonStore.");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            // Run deployment task
            MainAsync(args[0]).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string rawUri)
        {
            string statusFilePath = null;

            try
            {
                Uri uri = new Uri(rawUri);

                string packagePath = GetQueryValue(rawUri, "path");
                string taskId = GetQueryValue(rawUri, "taskId");
                string actionStr = uri.Host.ToLowerInvariant();

                // Locate the shared status file
                if (!string.IsNullOrEmpty(taskId))
                {
                    string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    statusFilePath = Path.Combine(pictures, "NeonStore", string.Format("status_{0}.txt", taskId));
                }

                if (string.IsNullOrEmpty(packagePath) || !File.Exists(packagePath))
                {
                    WriteStatus(statusFilePath, 0, "Error: AppX package file not found.", isComplete: true, isError: true);
                    return;
                }

                // Determine action
                SideloadAction action = SideloadAction.Install;
                if (actionStr.Contains("force") || rawUri.Contains("force=true"))
                    action = SideloadAction.ForceUpdate;
                else if (actionStr.Contains("update"))
                    action = SideloadAction.Update;

                // Step 1: Trust cert
                WriteStatus(statusFilePath, 10, "Trusting application certificate...");
                CertificateManager.TrustPackageCertificate(packagePath);

                // Step 2: Sideload
                WriteStatus(statusFilePath, 25, string.Format("Starting {0}...", action));
                await PackageInstaller.ExecuteAsync(packagePath, action, (pct) =>
                {
                    int uiPct = 25 + (int)(pct * 0.70);
                    WriteStatus(statusFilePath, uiPct, string.Format("Installing ({0}%)...", pct));
                });

                // Complete
                WriteStatus(statusFilePath, 100, "Installation complete!", isComplete: true);
            }
            catch (Exception ex)
            {
                WriteStatus(statusFilePath, 0, "Failed: " + ex.Message, isComplete: true, isError: true);
            }
        }

        private static void WriteStatus(string path, int percent, string message, bool isComplete = false, bool isError = false)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string payload = string.Format("{0}|{1}|{2}|{3}", percent, message, isComplete, isError);

                // Open with FileShare.ReadWrite so the WinRT app can read while helper writes
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(payload);
                    sw.Flush();
                }
            }
            catch { }
        }

        private static string GetQueryValue(string uriString, string key)
        {
            string marker = key + "=";
            int idx = uriString.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx == -1) return null;

            string val = uriString.Substring(idx + marker.Length);
            int amp = val.IndexOf('&');
            if (amp != -1) val = val.Substring(0, amp);

            return Uri.UnescapeDataString(val);
        }
    }
}