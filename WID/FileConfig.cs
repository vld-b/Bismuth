using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WID
{
    class FileConfig
    {
        public string chamoy { get; set; }

        public FileConfig(string chamoy)
        {
            this.chamoy = chamoy;
        }
    }

    [JsonSerializable(typeof(FileConfig))]
    internal partial class FileConfigJsonContext : JsonSerializerContext
    {}
}
