using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlickrSuperSyncConsole.Entities
{
    public class Photo
    {
        public string OriginalUrl { get; set; }

        public string PhotoId { get; set; }

        public string Title { get; set; }

        public DateTime? DownloadDate { get; set; }

        public string Error { get; set; }
    }
}
