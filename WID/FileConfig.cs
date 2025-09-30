using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Media.AppBroadcasting;

namespace WID
{
    class FileConfig
    {
        public List<string> pageMapping { get; set; }

        public FileConfig(List<string> pageMapping)
        {
            this.pageMapping = pageMapping;
        }
    }

    [JsonSerializable(typeof(FileConfig))]
    internal partial class FileConfigJsonContext : JsonSerializerContext
    {}
}
