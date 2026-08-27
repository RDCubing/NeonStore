using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeonStore
{
    public class AppSubmissionModel
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public string Subtitle { get; set; }
        public string Publisher { get; set; }
        public string ImagePath { get; set; }
        public string DetailImagePath { get; set; }
        public string Version { get; set; }
        public string Framework { get; set; }
        public string Description { get; set; }
        public string DownloadUrl { get; set; }
    }
}
