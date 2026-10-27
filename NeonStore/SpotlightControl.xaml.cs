using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace NeonStore
{
    public sealed partial class SpotlightControl : UserControl
    {
        private AppItem currentBillboardApp = null;
        private int selectedIndex = 0;
        private List<AppItem> spotlightApps = new List<AppItem>();

        private const double SlotStride = 38.0;
        private DispatcherTimer autoTimer;

        public SpotlightControl()
        {
            this.InitializeComponent();
            this.Loaded += SpotlightControl_Loaded;
            this.Unloaded += SpotlightControl_Unloaded;

            // Pause auto-rotation when hovering
            this.PointerEntered += (s, e) => autoTimer?.Stop();
            this.PointerExited += (s, e) => autoTimer?.Start();
        }

        private void SpotlightControl_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateDynamicStrip();

            selectedIndex = 0;
            IndicatorTransform.Y = 0;

            CollectionView.Visibility = Visibility.Visible;
            AppBillboardView.Visibility = Visibility.Collapsed;

            StripScrollViewer.PointerWheelChanged += StripScrollViewer_PointerWheelChanged;
            this.KeyDown += SpotlightControl_KeyDown;

            // Initialize auto-rotation timer (5.5 seconds interval)
            if (autoTimer == null)
            {
                autoTimer = new DispatcherTimer();
                autoTimer.Interval = TimeSpan.FromSeconds(5.5);
                autoTimer.Tick += AutoTimer_Tick;
            }
            autoTimer.Start();
        }

        private void SpotlightControl_Unloaded(object sender, RoutedEventArgs e)
        {
            autoTimer?.Stop();
        }

        private void AutoTimer_Tick(object sender, object e)
        {
            int totalCount = 1 + spotlightApps.Count;
            if (totalCount <= 1) return;

            int nextIndex = (selectedIndex + 1) % totalCount;
            SelectIndex(nextIndex);
        }

        private void PopulateDynamicStrip()
        {
            if (NeonStoreService.NeonStore == null || NeonStoreService.NeonStore.Count == 0) return;

            var random = new Random();

            // 1. Group apps by category, shuffle categories randomly, and take up to 10 distinct categories
            var randomCategoryGroups = NeonStoreService.NeonStore
                .Where(a => !string.IsNullOrWhiteSpace(a.Category))
                .GroupBy(a => a.Category)
                .OrderBy(_ => random.Next())
                .Take(10)
                .ToList();

            var selectedApps = new List<AppItem>();

            // 2. Pick one random AppItem representative per selected category
            foreach (var group in randomCategoryGroups)
            {
                var appList = group.ToList();
                var representativeApp = appList[random.Next(appList.Count)];
                selectedApps.Add(representativeApp);
            }

            spotlightApps = selectedApps;

            // ItemsSource is bound to List<AppItem>, preserving all XAML bindings (Title, ImagePath, etc.)
            StripAppsItemsControl.ItemsSource = spotlightApps;
        }

        private void StripScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(StripScrollViewer);
            int delta = pointerPoint.Properties.MouseWheelDelta;

            e.Handled = true;
            RestartTimer();

            if (delta < 0)
            {
                SelectIndex(selectedIndex + 1);
            }
            else if (delta > 0)
            {
                SelectIndex(selectedIndex - 1);
            }
        }

        private void SpotlightControl_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            RestartTimer();

            if (e.Key == Windows.System.VirtualKey.Down)
            {
                e.Handled = true;
                SelectIndex(selectedIndex + 1);
            }
            else if (e.Key == Windows.System.VirtualKey.Up)
            {
                e.Handled = true;
                SelectIndex(selectedIndex - 1);
            }
        }

        private void StripItem_Click(object sender, RoutedEventArgs e)
        {
            RestartTimer();
            SelectIndex(0);
        }

        private void DynamicStripItem_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var app = btn?.DataContext as AppItem;
            if (app == null) return;

            RestartTimer();

            int appIndex = spotlightApps.IndexOf(app);
            if (appIndex >= 0)
            {
                SelectIndex(appIndex + 1);
            }
        }

        public void SelectIndex(int targetIndex)
        {
            int totalCount = 1 + spotlightApps.Count;
            if (targetIndex < 0 || targetIndex >= totalCount || targetIndex == selectedIndex) return;

            int previousIndex = selectedIndex;
            selectedIndex = targetIndex;

            // 1. Scroll strip view if needed
            double itemTop = selectedIndex * SlotStride;
            double currentScroll = StripScrollViewer.VerticalOffset;
            double viewHeight = StripScrollViewer.ActualHeight > 0 ? StripScrollViewer.ActualHeight : 460;

            if (itemTop < currentScroll)
            {
                StripScrollViewer.ChangeView(null, itemTop, null);
            }
            else if (itemTop + SlotStride > currentScroll + viewHeight)
            {
                StripScrollViewer.ChangeView(null, (itemTop + SlotStride) - viewHeight, null);
            }

            // 2. Animate Selection Indicator
            IndicatorAnimation.To = selectedIndex * SlotStride;
            IndicatorMoveStoryboard.Begin();

            // 3. Update view visibility & load data
            if (selectedIndex == 0)
            {
                CollectionView.Visibility = Visibility.Visible;
                AppBillboardView.Visibility = Visibility.Collapsed;
                currentBillboardApp = null;
            }
            else
            {
                CollectionView.Visibility = Visibility.Collapsed;
                AppBillboardView.Visibility = Visibility.Visible;

                var app = spotlightApps[selectedIndex - 1];
                LoadAppDetails(app);
            }

            // 4. Trigger the appropriate Storyboard
            int delta = selectedIndex - previousIndex;

            if (delta == 1)
            {
                // Next adjacent item -> Slide Up
                SlideUpStoryboard.Begin();
            }
            else if (delta == -1)
            {
                // Previous adjacent item -> Slide Down
                SlideDownStoryboard.Begin();
            }
            else
            {
                // Non-adjacent manual jump -> Cross-fade
                FadeStoryboard.Begin();
            }
        }

        private void RestartTimer()
        {
            if (autoTimer != null)
            {
                autoTimer.Stop();
                autoTimer.Start();
            }
        }

        private void LoadAppDetails(AppItem app)
        {
            if (app == null) return;

            currentBillboardApp = app;

            AppNameText.Text = app.Title ?? "Unknown App";
            AppCategoryText.Text = !string.IsNullOrEmpty(app.Category) ? app.Category : "Apps";
            AppPriceText.Text = "Free";
            AppRatingCountText.Text = app.TopApp == "Yes" ? "Top App" : "Featured";

            if (!string.IsNullOrEmpty(app.ImagePath))
            {
                try { AppIconImage.Source = new BitmapImage(new Uri(app.ImagePath)); }
                catch { AppIconImage.Source = null; }
            }

            string billboardUri = !string.IsNullOrEmpty(app.DetailImagePath) ? app.DetailImagePath : app.ImagePath;
            if (!string.IsNullOrEmpty(billboardUri))
            {
                try { BillboardImage.Source = new BitmapImage(new Uri(billboardUri)); }
                catch { BillboardImage.Source = null; }
            }
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            string targetId = btn?.Tag as string;

            if (!string.IsNullOrEmpty(targetId))
            {
                NavigateToApp(targetId);
            }
        }

        private void Billboard_Click(object sender, RoutedEventArgs e)
        {
            if (currentBillboardApp != null)
            {
                NavigateToApp(currentBillboardApp.Id);
            }
        }

        private void NavigateToApp(string id)
        {
            var app = NeonStoreService.NeonStore
                .FirstOrDefault(a =>
                    string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.Title, id, StringComparison.OrdinalIgnoreCase));

            if (app == null) return;

            AppState.SelectedApp = app;

            var frame = Window.Current.Content as Frame;
            frame?.Navigate(typeof(SelectedApp), app);
        }
    }
}