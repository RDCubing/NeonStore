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
            => NeonStoreService.TopApps;

        public ObservableCollection<AppItem> Apps
            => NeonStoreService.NeonStore;
    }
}
