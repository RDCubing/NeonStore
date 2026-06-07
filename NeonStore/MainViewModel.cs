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
    }
}
