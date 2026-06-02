using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace NeonStore
{
    public static class OtherApp
    {
        public static ObservableCollection<AppItem> OtherApps
        {
            get
            {
                return new ObservableCollection<AppItem>(
                    NeonStoreService.NeonStore
                        .Where(a => a.TopApp != "Yes")
                );
            }
        }
    }
}
