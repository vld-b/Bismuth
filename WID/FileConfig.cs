using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Media.AppBroadcasting;
using Windows.UI.WebUI;

namespace WID
{
    class FileConfig
    {
        public ObservableCollection<string> pageMapping { get; set; }
        public ObservableCollection<string> bgMapping { get; set; }
        public int maxPageID { get; set; }
        public List<int> usablePageIDs { get; set; }
        public LastNotebookState lastNotebookState { get; set; }
        public List<TextData> textMapping { get; set; }
        public int maxTextID { get; set; }
        public List<int> usableTextIDs { get; set; }

        public FileConfig(
            ObservableCollection<string> pageMapping,
            ObservableCollection<string> bgMapping,
            int maxPageID,
            List<int> usablePageIDs,
            LastNotebookState lastNotebookState,
            List<TextData> textMapping,
            int maxTextID,
            List<int> usableTextIDs)
        {
            this.pageMapping = pageMapping;
            this.bgMapping = bgMapping;
            this.maxPageID = maxPageID;
            this.usablePageIDs = usablePageIDs;
            this.lastNotebookState = lastNotebookState;
            this.textMapping = textMapping;
            this.maxTextID = maxTextID;
            this.usableTextIDs = usableTextIDs;
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
            if (id == maxPageID)
                --maxPageID;
            else
                usablePageIDs.Add(id);
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

    class TextData
    {
        public int id { get; set; }
        public int containingPageId { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public double top { get; set; }
        public double left { get; set; }

        public TextData(int id, int containingPageId, double width, double height, double top, double left)
        {
            this.id = id;
            this.containingPageId = containingPageId;
            this.width = width;
            this.height = height;
            this.top = top;
            this.left = left;
        }
    }
}
