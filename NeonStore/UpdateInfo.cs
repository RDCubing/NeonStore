using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeonStore
{
    public class UpdateInfo
    {
        public string Version { get; set; }
        public string Name { get; set; }
        public string DownloadUrl { get; set; }
        public string Changelog { get; set; }
        public string Message { get; set; }
    }
}
