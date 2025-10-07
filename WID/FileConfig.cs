using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Media.AppBroadcasting;

namespace WID
{
    class FileConfig
    {
        public ObservableCollection<string> pageMapping { get; set; }
        public int maxID { get; set; }
        public List<int> usableIDs { get; set; }

        public FileConfig(ObservableCollection<string> pageMapping, int maxID, List<int> usableIDs)
        {
            this.pageMapping = pageMapping;
            this.maxID = maxID;
            this.usableIDs = usableIDs;
        }

        public void DeletePageWithId(int id)
        {
            if (id == 0)
                pageMapping.Remove("page.gif");
            else
            {
                for (int i = 0; i < pageMapping.Count; ++i)
                {
                    if (pageMapping[i] == ("page ("+id+").gif"))
                    {
                        pageMapping.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }

    [JsonSerializable(typeof(FileConfig))]
    internal partial class FileConfigJsonContext : JsonSerializerContext
    {}
}
