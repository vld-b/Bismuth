using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Media.AppBroadcasting;
using Windows.UI.Composition;
using Windows.UI.WebUI;

namespace WID
{
    class NotebookConfig
    {
        public ObservableCollection<PageConfig> pageMapping { get; set; }
        public int maxPageID { get; set; }
        public List<int> usablePageIDs { get; set; }
        public LastNotebookState lastNotebookState { get; set; }
        public int maxTextID { get; set; }
        public List<int> usableTextIDs { get; set; }

        public NotebookConfig(
            ObservableCollection<PageConfig> pageMapping,
            int maxPageID,
            List<int> usablePageIDs,
            LastNotebookState lastNotebookState,
            int maxTextID,
            List<int> usableTextIDs)
        {
            this.pageMapping = pageMapping;
            this.maxPageID = maxPageID;
            this.usablePageIDs = usablePageIDs;
            this.lastNotebookState = lastNotebookState;
            this.maxTextID = maxTextID;
            this.usableTextIDs = usableTextIDs;
        }

        public void DeletePageWithId(int id)
        {
            for (int i = 0; i < pageMapping.Count; ++i)
            {
                if (pageMapping[i].id == id)
                {
                    pageMapping.RemoveAt(i);
                    break;
                }
            }
            if (id == maxPageID)
                --maxPageID;
            else
                usablePageIDs.Add(id);
        }
    }

    [JsonSerializable(typeof(NotebookConfig))]
    internal partial class NotebookConfigJsonContext : JsonSerializerContext
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

    class PageConfig
    {
        public int id { get; private set; }
        public string fileName { get; private set; }
        public double width { get; private set; }
        public double height { get; private set; }
        public bool hasBg { get; private set; }
        public List<TextData> textBoxes { get; private set; }

        public PageConfig(int id, double width, double height, bool hasBg)
        {
            this.id = id;
            this.fileName = "page" + (this.id == 0 ? "" : (" (" + this.id + ")")) + ".gif";
            this.width = width;
            this.height = height;
            this.hasBg = hasBg;
            textBoxes = new List<TextData>();
        }

        public PageConfig(int id, double width, double height, bool hasBg, List<TextData> textBoxes)
        {
            this.id = id;
            this.fileName = "page" + (this.id == 0 ? "" : (" (" + this.id + ")")) + ".gif";
            this.width = width;
            this.height = height;
            this.hasBg = hasBg;
            this.textBoxes = textBoxes;
        }

        public string GetBgName()
        {
            return "bg" + (this.id == 0 ? "" : (" (" + this.id + ")")) + ".png";
        }
    }
}
