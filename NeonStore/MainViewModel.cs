using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace NeonStore
{
    public class MainViewModel
    {
        public ObservableCollection<AppItem> TopApps
        {
            get
            {
                return new ObservableCollection<AppItem>(
                    NeonStoreService.TopApps.Take(6)
                );
            }
        }

        public ObservableCollection<AppItem> Apps
            => NeonStoreService.NeonStore;

        public ObservableCollection<string> Categories
            => NeonStoreService.Categories;

        public ObservableCollection<AppItem> BooksReference
        {
            get
            {
                return new ObservableCollection<AppItem>(
                    NeonStoreService.NeonStore
                        .Where(app => app.Category == "Books/Reference")
                        .Take(6)
                );
            }
        }

        public ObservableCollection<AppItem> News
        {
            get
            {
                return new ObservableCollection<AppItem>(
                    NeonStoreService.NeonStore
                        .Where(app => app.Category == "News")
                        .Take(6)
                );
            }
        }

        public ObservableCollection<AppItem> Entertainment
        {
            get
            {
                return new ObservableCollection<AppItem>(
                    NeonStoreService.NeonStore
                        .Where(app => app.Category == "Entertainment")
                        .Take(6)
                );
            }
        }
    }
}
