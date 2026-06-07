using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeonStore
{
    public class DownloadItem
    {
        public string Title { get; set; }
        public string FileName { get; set; }
        public string DownloadUrl { get; set; }
        public string ImagePath { get; set; }
        public DateTime DownloadedAt { get; set; }
    }
}
