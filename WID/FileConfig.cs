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
        public ObservableCollection<string> bgMapping { get; set; }
        public int maxID { get; set; }
        public List<int> usableIDs { get; set; }
        public LastNotebookState lastNotebookState { get; set; }

        public FileConfig(ObservableCollection<string> pageMapping, ObservableCollection<string> bgMapping, int maxID, List<int> usableIDs, LastNotebookState lastNotebookState)
        {
            this.pageMapping = pageMapping;
            this.bgMapping = bgMapping;
            this.maxID = maxID;
            this.usableIDs = usableIDs;
            this.lastNotebookState = lastNotebookState;
        }

        public void DeletePageWithId(int id)
        {
            if (id == 0)
            {
                pageMapping.Remove("page.gif");
                bgMapping.Remove("bg.jpg");
            }
            else
            {
                for (int i = 0; i < pageMapping.Count; ++i)
                {
                    if (pageMapping[i] == ("page ("+id+").gif"))
                    {
                        pageMapping.RemoveAt(i);
                        bgMapping.RemoveAt(i);
                        break;
                    }
                }
            }
            if (id == maxID)
                --maxID;
            else
                usableIDs.Add(id);
        }
    }

    [JsonSerializable(typeof(FileConfig))]
    internal partial class FileConfigJsonContext : JsonSerializerContext
    {}

    class LastNotebookState
    {
        public double vertScrollPos { get; set; }
        public double horizScrollPos { get; set; }
        public float zoomFactor { get; set; }

        public LastNotebookState()
        {
            vertScrollPos = 0d;
            horizScrollPos = 0d;
            zoomFactor = 0f;

        }

        public LastNotebookState(double vertScrollPos, double horizScrollPos, float zoomFactor)
        {
            this.vertScrollPos = vertScrollPos;
            this.horizScrollPos = horizScrollPos;
            this.zoomFactor = zoomFactor;
        }
    }
}
