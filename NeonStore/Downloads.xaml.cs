using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace NeonStore
{
    public sealed partial class Downloads : SettingsFlyout
    {
        public Downloads()
        {
            this.InitializeComponent();
            this.DataContext = DownloadHistoryService.Instance;
            UpdateEmptyState();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            DownloadHistoryService.Instance.Clear();
            UpdateEmptyState();
        }

        private void CancelDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            // Closes the flyout without taking action
            DeleteAllConfirmationFlyout?.Hide();
        }

        private async void ConfirmDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            // 1. Close flyout
            DeleteAllConfirmationFlyout?.Hide();

            // 2. Physically delete downloaded files from Pictures\NeonStore
            try
            {
                StorageFolder pictures = KnownFolders.PicturesLibrary;
                IStorageItem folderItem = await pictures.TryGetItemAsync("NeonStore");
                StorageFolder neonStoreFolder = folderItem as StorageFolder;

                if (neonStoreFolder != null)
                {
                    var files = await neonStoreFolder.GetFilesAsync();
                    foreach (var file in files)
                    {
                        // Preserve the helper executable so sideloading remains working
                        if (string.Equals(file.Name, "NeonStore.Helper.exe", StringComparison.OrdinalIgnoreCase))
                            continue;

                        try
                        {
                            await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                        }
                        catch
                        {
                            // Ignore transient locks or in-use errors
                        }
                    }
                }
            }
            catch
            {
                // Ignore folder access errors
            }

            // 3. Clear history records and reset badge count
            DownloadHistoryService.Instance.Clear();

            // 4. Update UI empty state
            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            bool hasItems = DownloadHistoryService.Instance.Downloads.Count > 0;

            // Toggle Empty state placeholder text
            EmptyText.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;

            // Disable both action buttons when there is no history
            if (ClearButton != null)
            {
                ClearButton.IsEnabled = hasItems;
            }

            if (DeleteAllButton != null)
            {
                DeleteAllButton.IsEnabled = hasItems;
            }
        }

        private void DownloadsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as DownloadItem;
            if (item == null) return;

            var listView = sender as ListView;
            var container = listView?.ContainerFromItem(e.ClickedItem) as FrameworkElement;
            if (container == null) return;

            var menu = new MenuFlyout();

            // 1. Primary actions
            var sideloadItem = new MenuFlyoutItem { Text = "Install through sideload" };
            sideloadItem.Click += async (s, args) => await PerformSideloadInstallAsync(item);

            var openItem = new MenuFlyoutItem { Text = "Open file" };
            openItem.Click += async (s, args) => await PerformOpenFileAsync(item);

            menu.Items.Add(sideloadItem);
            menu.Items.Add(openItem);

            // 2. Separator
            menu.Items.Add(new MenuFlyoutSeparator());

            // 3. Clear item (History record only)
            var clearItem = new MenuFlyoutItem { Text = "Clear item" };
            clearItem.Click += (s, args) =>
            {
                DownloadHistoryService.Instance.Remove(item);
                UpdateEmptyState();
            };

            // 4. Delete file (Triggers confirmation Flyout)
            var deleteFileItem = new MenuFlyoutItem { Text = "Delete file" };
            deleteFileItem.Click += (s, args) =>
            {
                ShowDeleteFileConfirmationFlyout(container, item);
            };

            menu.Items.Add(clearItem);
            menu.Items.Add(deleteFileItem);

            menu.ShowAt(container);
        }

        // Handler to delete the single physical file and clean up history
        private async Task PerformDeleteSingleFileAsync(DownloadItem item)
        {
            if (item == null) return;

            var confirmDialog = new MessageDialog(
                string.Format("Are you sure you want to permanently delete '{0}' from your device?", item.FileName),
                "Delete File");

            confirmDialog.Commands.Add(new UICommand("Delete"));
            confirmDialog.Commands.Add(new UICommand("Cancel"));

            var choice = await confirmDialog.ShowAsync();
            if (choice.Label != "Delete") return;

            try
            {
                StorageFile file = await FindDownloadedFileAsync(item.FileName);
                if (file != null)
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch
            {
                // Ignore file access or lock errors
            }

            // Remove from the history list and update the badge/empty state
            DownloadHistoryService.Instance.Remove(item);
            UpdateEmptyState();
        }

        private void ShowDeleteFileConfirmationFlyout(FrameworkElement targetElement, DownloadItem item)
        {
            if (targetElement == null || item == null) return;

            var flyout = new Flyout();

            var panel = new StackPanel
            {
                Width = 240,
                Margin = new Thickness(4)
            };

            var titleBlock = new TextBlock
            {
                Text = "Delete downloaded file?",
                FontSize = 15,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = Windows.UI.Text.FontWeights.SemiLight,
                Foreground = new SolidColorBrush(Windows.UI.Colors.Black),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var descBlock = new TextBlock
            {
                Text = string.Format("Are you sure you want to permanently delete '{0}'?", item.FileName),
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Windows.UI.Colors.Black),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 14)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Foreground = new SolidColorBrush(Windows.UI.Colors.Black),
                Margin = new Thickness(0, 0, 8, 0)
            };
            cancelButton.Click += (s, args) => flyout.Hide();

            var deleteButton = new Button
            {
                Content = "Delete",
                Background = new SolidColorBrush(Windows.UI.Colors.DarkRed),
                Foreground = new SolidColorBrush(Windows.UI.Colors.White),
                BorderThickness = new Thickness(0)
            };
            deleteButton.Click += async (s, args) =>
            {
                flyout.Hide();
                await ExecuteSingleFileDeletionAsync(item);
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(deleteButton);

            panel.Children.Add(titleBlock);
            panel.Children.Add(descBlock);
            panel.Children.Add(buttonPanel);

            flyout.Content = panel;
            flyout.ShowAt(targetElement);
        }

        private async Task ExecuteSingleFileDeletionAsync(DownloadItem item)
        {
            if (item == null) return;

            try
            {
                StorageFile file = await FindDownloadedFileAsync(item.FileName);
                if (file != null)
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch
            {
                // Ignore file access or lock errors
            }

            // Remove from the history list and sync empty state
            DownloadHistoryService.Instance.Remove(item);
            UpdateEmptyState();
        }

        private async Task PerformSideloadInstallAsync(DownloadItem item)
        {
            if (item == null) return;

            // 1. Locate file in Pictures\NeonStore
            StorageFile file = await FindDownloadedFileAsync(item.FileName);
            if (file == null)
            {
                await new MessageDialog(
                    string.Format("Could not locate file '{0}' in Pictures\\NeonStore.", item.FileName),
                    "File Not Found").ShowAsync();
                return;
            }

            // 2. Validate package format
            string ext = Path.GetExtension(file.Name) ?? string.Empty;
            bool isAppxPackage = ext.Equals(".appx", StringComparison.OrdinalIgnoreCase) ||
                                 ext.Equals(".appxbundle", StringComparison.OrdinalIgnoreCase);

            if (!isAppxPackage)
            {
                var invalidPkgDialog = new MessageDialog(
                    string.Format("'{0}' is not a modern application package (.appx or .appxbundle).\n\nOpening with the default system handler instead.", file.Name),
                    "Unsupported Package Type");

                invalidPkgDialog.Commands.Add(new UICommand("OK"));
                await invalidPkgDialog.ShowAsync();

                await Launcher.LaunchFileAsync(file);
                return;
            }

            // 3. Show Modal Progress Dialog
            var progressDialog = SideloadProgressModal.Show(item.Title ?? file.Name);

            try
            {
                progressDialog.UpdateStatus("Preparing helper...");

                StorageFolder pictures = KnownFolders.PicturesLibrary;
                StorageFolder neonStoreFolder = await pictures.CreateFolderAsync(
                    "NeonStore",
                    CreationCollisionOption.OpenIfExists);

                // Ensure NeonStore.Helper.exe is present in Pictures\NeonStore
                try
                {
                    var installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                    IStorageItem pkgItem = await installedLocation.TryGetItemAsync("NeonStore.Helper.exe");
                    StorageFile packagedHelper = pkgItem as StorageFile;
                    if (packagedHelper != null)
                    {
                        await packagedHelper.CopyAsync(neonStoreFolder, "NeonStore.Helper.exe", NameCollisionOption.ReplaceExisting);
                    }
                }
                catch { }

                // 4. Launch sideload helper via custom protocol
                string taskId = Guid.NewGuid().ToString("N");
                string uriString = string.Format(
                    "neonstore-helper://install?path={0}&taskId={1}",
                    Uri.EscapeDataString(file.Path),
                    Uri.EscapeDataString(taskId));

                progressDialog.UpdateStatus("Connecting to helper...");
                await Launcher.LaunchUriAsync(new Uri(uriString));

                // 5. Monitor install progress
                bool helperResponded = await MonitorSideloadProgressAsync(neonStoreFolder, taskId, progressDialog);

                progressDialog.Close();

                if (helperResponded)
                {
                    bool toast =
                        (bool?)ApplicationData.Current.LocalSettings.Values["Toast"] ?? true;

                    if (toast)
                    {
                        ToastService.Show(
                            "App installed successfully",
                            item.Title ?? file.Name,
                            item.ImagePath);
                    }

                    var successDialog = new MessageDialog(
                        string.Format("\"{0}\" has been successfully installed onto your device.", item.Title ?? file.Name),
                        "Installation Complete");
                    successDialog.Commands.Add(new UICommand("OK"));
                    await successDialog.ShowAsync();
                }
                else
                {
                    var setupDialog = new MessageDialog(
                        "NeonStore Helper is not active or hasn't been set up yet.\n\n" +
                        "To complete setup:\n" +
                        "1. Open File Explorer on your Desktop.\n" +
                        "2. Navigate to: Pictures -> NeonStore\n" +
                        "3. Double-click 'NeonStore.Helper.exe' and approve the UAC prompt.\n" +
                        "4. Return to NeonStore and try installing again.\n\n" +
                        "If the helper is already set up, the deployment likely encountered an issue.",
                        "Sideload Helper Setup Required");

                    setupDialog.Commands.Add(new UICommand("OK"));
                    await setupDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                progressDialog.Close();
                await new MessageDialog(ex.ToString(), "Installation Error").ShowAsync();
            }
        }

        private async Task PerformOpenFileAsync(DownloadItem item)
        {
            StorageFile file = await FindDownloadedFileAsync(item.FileName);
            if (file != null)
            {
                await Launcher.LaunchFileAsync(file);
            }
            else
            {
                await new MessageDialog(string.Format("File '{0}' was not found in Pictures\\NeonStore.", item.FileName), "File Missing").ShowAsync();
            }
        }

        private async Task<bool> MonitorSideloadProgressAsync(
            StorageFolder folder,
            string taskId,
            SideloadProgressModal progressDialog = null)
        {
            string statusFileName = string.Format("status_{0}.txt", taskId);
            bool finished = false;
            bool helperStarted = false;
            int timeoutSeconds = 60;
            int handshakeTimeoutSeconds = 8;
            DateTime startTime = DateTime.Now;

            while (!finished)
            {
                await Task.Delay(300);

                double elapsed = (DateTime.Now - startTime).TotalSeconds;

                if (!helperStarted && elapsed > handshakeTimeoutSeconds)
                {
                    progressDialog?.UpdateStatus("Helper failed to respond.");
                    return false;
                }

                if (elapsed > timeoutSeconds)
                {
                    progressDialog?.UpdateStatus("Sideloading timed out.");
                    return false;
                }

                try
                {
                    IStorageItem item = await folder.TryGetItemAsync(statusFileName);
                    StorageFile statusFile = item as StorageFile;

                    if (statusFile != null)
                    {
                        helperStarted = true;
                        string content = await FileIO.ReadTextAsync(statusFile);

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            string[] parts = content.Split('|');
                            if (parts.Length >= 4)
                            {
                                int percent;
                                int.TryParse(parts[0], out percent);
                                string message = parts[1];
                                bool isComplete = parts[2].Equals("True", StringComparison.OrdinalIgnoreCase);
                                bool isError = parts[3].Equals("True", StringComparison.OrdinalIgnoreCase);

                                progressDialog?.UpdateStatus(message, percent);

                                if (isComplete || isError)
                                {
                                    finished = true;
                                    try
                                    {
                                        await statusFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                                    }
                                    catch { }

                                    return !isError;
                                }
                            }
                        }
                    }
                    else
                    {
                        int remainingSeconds = (int)Math.Max(0, Math.Ceiling(handshakeTimeoutSeconds - elapsed));
                        progressDialog?.UpdateStatus(string.Format("Waiting for helper... ({0}s)", remainingSeconds));
                    }
                }
                catch
                {
                    // File access lock catch
                }
            }

            return true;
        }

        private async Task<StorageFile> FindDownloadedFileAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            try
            {
                StorageFolder pictures = KnownFolders.PicturesLibrary;
                StorageFolder neonFolder = await pictures.GetFolderAsync("NeonStore");
                IStorageItem foundItem = await neonFolder.TryGetItemAsync(fileName);
                return foundItem as StorageFile;
            }
            catch
            {
                return null;
            }
        }

        // Authentic Metro Full Edge-to-Edge Modal Banner (No Outlines)
        private class SideloadProgressModal
        {
            private Popup _popup;
            private TextBlock _statusText;
            private ProgressBar _progressBar;

            public static SideloadProgressModal Show(string appTitle)
            {
                var modal = new SideloadProgressModal();
                var bounds = Window.Current.Bounds;

                // Root container covering the entire viewport
                var rootGrid = new Grid
                {
                    Width = bounds.Width,
                    Height = bounds.Height,
                    // Classic Metro dim overlay
                    Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0))
                };

                // Darkened Accent Tone for the full-width strip
                Brush bannerBackground = new SolidColorBrush(Color.FromArgb(255, 20, 20, 24)); // Fallback dark
                if (Application.Current.Resources.ContainsKey("AccentBrush"))
                {
                    var accentBrush = Application.Current.Resources["AccentBrush"] as SolidColorBrush;
                    if (accentBrush != null)
                    {
                        Color c = accentBrush.Color;
                        // Darken by 65% for the ribbon surface
                        byte darkR = (byte)(c.R * 0.35);
                        byte darkG = (byte)(c.G * 0.35);
                        byte darkB = (byte)(c.B * 0.35);
                        bannerBackground = new SolidColorBrush(Color.FromArgb(255, darkR, darkG, darkB));
                    }
                }

                // Full edge-to-edge horizontal ribbon banner (No borders)
                var fullWidthBanner = new Border
                {
                    Width = bounds.Width,
                    Background = bannerBackground,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0, 28, 0, 32),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Centered container with left-aligned contents
                var contentStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Width = Math.Min(bounds.Width - 48, 680),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(24, 0, 24, 0)
                };

                // Metro Header: Segoe UI Light, 28px, Pure White
                var titleText = new TextBlock
                {
                    Text = "Installing " + appTitle,
                    FontSize = 28,
                    FontFamily = new FontFamily("Segoe UI Light"),
                    FontWeight = FontWeights.Light,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 16)
                };

                // Flat Accent Progress Bar
                modal._progressBar = new ProgressBar
                {
                    IsIndeterminate = true,
                    Height = 4,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 0, 0, 12)
                };

                if (Application.Current.Resources.ContainsKey("AccentBrush"))
                {
                    modal._progressBar.Foreground = Application.Current.Resources["AccentBrush"] as Brush;
                }

                // Subtitle status: 75% opacity white
                modal._statusText = new TextBlock
                {
                    Text = "Preparing installation...",
                    FontSize = 14,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontWeight = FontWeights.Normal,
                    Foreground = new SolidColorBrush(Colors.White),
                    Opacity = 0.75,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                contentStack.Children.Add(titleText);
                contentStack.Children.Add(modal._progressBar);
                contentStack.Children.Add(modal._statusText);

                fullWidthBanner.Child = contentStack;
                rootGrid.Children.Add(fullWidthBanner);

                modal._popup = new Popup
                {
                    Child = rootGrid,
                    IsOpen = true
                };

                return modal;
            }

            public void UpdateStatus(string message, double? percent = null)
            {
                if (_statusText != null && !string.IsNullOrEmpty(message))
                {
                    _statusText.Text = message;
                }

                if (_progressBar != null)
                {
                    if (percent.HasValue)
                    {
                        _progressBar.IsIndeterminate = false;
                        _progressBar.Value = percent.Value;
                    }
                    else
                    {
                        _progressBar.IsIndeterminate = true;
                    }
                }
            }

            public void Close()
            {
                if (_popup != null)
                {
                    _popup.IsOpen = false;
                    _popup = null;
                }
            }
        }
    }
}