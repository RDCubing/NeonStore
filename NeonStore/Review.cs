using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeonStore
{
    public class Review
    {
        public string _id { get; set; }
        public string userId { get; set; }
        public string username { get; set; }
        public int rating { get; set; }
        public string comment { get; set; }
        public string appId { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }
}
