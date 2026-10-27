using NeonStore.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace NeonStore
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class Apps : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        // Filter & Sort State
        private string _selectedSort = "Featured";
        private string _selectedCategory = "All";
        private string _selectedPublisher = "All";
        private string _currentSearchQuery = string.Empty;
        private bool _isInitializingFilters = false;
        private string _initialPublisherFilter = null;
        private string _initialCategoryFilter = null;

        // Bound Collection for ProjectsGrid
        public ObservableCollection<AppItem> DisplayedApps { get; } = new ObservableCollection<AppItem>();

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public Apps()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var navParam = e.NavigationParameter as string;
            if (!string.IsNullOrWhiteSpace(navParam))
            {
                if (navParam.StartsWith("Category:", StringComparison.OrdinalIgnoreCase))
                {
                    _initialCategoryFilter = navParam.Substring("Category:".Length).Trim();
                }
                else if (navParam.StartsWith("Publisher:", StringComparison.OrdinalIgnoreCase))
                {
                    _initialPublisherFilter = navParam.Substring("Publisher:".Length).Trim();
                }
                else
                {
                    // Fallback: If sent without a prefix, match against publishers or default to publisher
                    _initialPublisherFilter = navParam;
                }
            }
        }

        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ProjectsGrid.ItemsSource = DisplayedApps;

            InitializeFilterSubmenus();

            // 1. Handle incoming Publisher filter
            if (!string.IsNullOrEmpty(_initialPublisherFilter))
            {
                _selectedPublisher = _initialPublisherFilter;

                if (PublisherComboBox != null)
                {
                    foreach (ComboBoxItem item in PublisherComboBox.Items)
                    {
                        if (string.Equals(item.Tag != null ? item.Tag.ToString() : null, _initialPublisherFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            PublisherComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                _initialPublisherFilter = null;
            }

            // 2. Handle incoming Category filter
            if (!string.IsNullOrEmpty(_initialCategoryFilter))
            {
                _selectedCategory = _initialCategoryFilter;

                if (CategoryComboBox != null)
                {
                    foreach (ComboBoxItem item in CategoryComboBox.Items)
                    {
                        if (string.Equals(item.Tag != null ? item.Tag.ToString() : null, _initialCategoryFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            CategoryComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                _initialCategoryFilter = null;
            }

            ApplyFilterAndSort();

            bool reduceMotion = (bool?)ApplicationData.Current.LocalSettings.Values["ReduceMotion"] ?? false;

            if (!reduceMotion)
            {
                SlideInStoryboard.Begin();
            }
            else
            {
                MainPanelTransform.X = 0;
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                System.Diagnostics.Debug.WriteLine("NeonStore: Manual refresh triggered");
                await NeonStoreService.LoadAsync();

                InitializeFilterSubmenus();
                ApplyFilterAndSort();

                System.Diagnostics.Debug.WriteLine("NeonStore: Refresh complete ✔");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Refresh ERROR: " + ex.Message);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private void ProjectsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var app = (AppItem)e.ClickedItem;
            AppState.SelectedApp = app;
            Frame.Navigate(typeof(SelectedApp), app);
        }

        #region SearchBox Events

        private void SearchBox_QueryChanged(SearchBox sender, SearchBoxQueryChangedEventArgs args)
        {
            _currentSearchQuery = sender.QueryText;
            ApplyFilterAndSort();
        }

        private void SearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            _currentSearchQuery = sender.QueryText;
            ApplyFilterAndSort();
        }

        #endregion

        #region Filter ComboBoxes Setup & Events

        private void InitializeFilterSubmenus()
        {
            _isInitializingFilters = true;

            // 1. Dynamic Categories
            if (CategoryComboBox != null)
            {
                CategoryComboBox.Items.Clear();
                CategoryComboBox.Items.Add(new ComboBoxItem { Content = "All Categories", Tag = "All" });

                foreach (var cat in NeonStoreService.Categories)
                {
                    CategoryComboBox.Items.Add(new ComboBoxItem { Content = cat, Tag = cat });
                }
                CategoryComboBox.SelectedIndex = 0;
            }

            // 2. Dynamic Publishers
            if (PublisherComboBox != null)
            {
                PublisherComboBox.Items.Clear();
                PublisherComboBox.Items.Add(new ComboBoxItem { Content = "All Publishers", Tag = "All" });

                foreach (var pub in NeonStoreService.Publishers)
                {
                    PublisherComboBox.Items.Add(new ComboBoxItem { Content = pub, Tag = pub });
                }
                PublisherComboBox.SelectedIndex = 0;
            }

            _isInitializingFilters = false;
        }

        private void FilterCriteria_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingFilters) return;

            var sortItem = SortComboBox != null ? SortComboBox.SelectedItem as ComboBoxItem : null;
            var catItem = CategoryComboBox != null ? CategoryComboBox.SelectedItem as ComboBoxItem : null;
            var pubItem = PublisherComboBox != null ? PublisherComboBox.SelectedItem as ComboBoxItem : null;

            _selectedSort = (sortItem != null && sortItem.Tag != null) ? sortItem.Tag.ToString() : "Featured";
            _selectedCategory = (catItem != null && catItem.Tag != null) ? catItem.Tag.ToString() : "All";
            _selectedPublisher = (pubItem != null && pubItem.Tag != null) ? pubItem.Tag.ToString() : "All";

            ApplyFilterAndSort();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            _isInitializingFilters = true;

            if (SortComboBox != null) SortComboBox.SelectedIndex = 0;
            if (CategoryComboBox != null) CategoryComboBox.SelectedIndex = 0;
            if (PublisherComboBox != null) PublisherComboBox.SelectedIndex = 0;

            _selectedSort = "Featured";
            _selectedCategory = "All";
            _selectedPublisher = "All";

            _isInitializingFilters = false;

            ApplyFilterAndSort();

            if (FilterFlyout != null)
            {
                FilterFlyout.Hide();
            }
        }

        #endregion

        #region Filtering & Sorting Pipeline

        private void ApplyFilterAndSort()
        {
            if (NeonStoreService.NeonStore == null) return;

            IEnumerable<AppItem> query = NeonStoreService.NeonStore;

            // 1. Search Query Filter
            if (!string.IsNullOrWhiteSpace(_currentSearchQuery))
            {
                string q = _currentSearchQuery.Trim().ToLower();
                query = query.Where(app =>
                    (app.Title ?? "").ToLower().Contains(q) ||
                    (app.Description ?? "").ToLower().Contains(q) ||
                    (app.Publisher ?? "").ToLower().Contains(q) ||
                    (app.Category ?? "").ToLower().Contains(q)
                );
            }

            // 2. Category Filter
            if (_selectedCategory != "All")
            {
                query = query.Where(a => string.Equals(a.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            // 3. Publisher Filter
            if (_selectedPublisher != "All")
            {
                query = query.Where(a => string.Equals(a.Publisher, _selectedPublisher, StringComparison.OrdinalIgnoreCase));
            }

            // 4. Sorting
            switch (_selectedSort)
            {
                case "AlphaAsc":
                    query = query.OrderBy(a => a.Title);
                    break;
                case "AlphaDesc":
                    query = query.OrderByDescending(a => a.Title);
                    break;
                case "TopApps":
                    query = query.OrderByDescending(a => a.TopApp == "Yes").ThenBy(a => a.Title);
                    break;
                case "Featured":
                default:
                    query = query.OrderByDescending(a => a.TopApp == "Yes");
                    break;
            }

            // 5. Update the UI bound collection
            var list = query.ToList();
            // 5. Update the UI bound collection
            DisplayedApps.Clear();
            foreach (var item in query)
            {
                DisplayedApps.Add(item);
            }

            // Update the count label with pluralization
            if (AppsCountText != null)
            {
                int count = DisplayedApps.Count;
                AppsCountText.Text = string.Format("{0} {1}", count, count == 1 ? "app" : "apps");
            }
        }

        #endregion
    }
}