using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Media.AppBroadcasting;
using Windows.Storage;
using Windows.UI.Composition;
using Windows.UI.WebUI;
using Windows.UI.Xaml.Input;

namespace WID
{
    public class NotebookConfig
    {
        public long configVersion { get; set; }
        public ObservableCollection<PageConfig> pageMapping { get; set; }
        public int maxPageID { get; set; }
        public List<int> usablePageIDs { get; set; }
        public LastNotebookState lastNotebookState { get; set; }
        public int maxTextID { get; set; }
        public List<int> usableTextIDs { get; set; }
        public DefaultTemplate defaultTemplate { get; set; }
        public int maxImageID { get; set; }
        public List<int> usableImageIDs { get; set; }

        public NotebookConfig(
            long configVersion,
            ObservableCollection<PageConfig> pageMapping,
            int maxPageID,
            List<int> usablePageIDs,
            LastNotebookState lastNotebookState,
            int maxTextID,
            List<int> usableTextIDs,
            DefaultTemplate defaultTemplate,
            int maxImageID,
            List<int> usableImageIDs
            )
        {
            this.configVersion = configVersion;
            this.pageMapping = pageMapping;
            this.maxPageID = maxPageID;
            this.usablePageIDs = usablePageIDs;
            this.lastNotebookState = lastNotebookState;
            this.maxTextID = maxTextID;
            this.usableTextIDs = usableTextIDs;
            this.defaultTemplate = defaultTemplate;
            this.maxImageID = maxImageID;
            this.usableImageIDs = usableImageIDs;
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

        public async Task SerializeToFile(StorageFolder folder)
        {
            using (Stream opStream = await (await folder.GetFileAsync("config.json")).OpenStreamForWriteAsync())
                JsonSerializer.Serialize(opStream, this, NotebookConfigJsonContext.Default.NotebookConfig);
        }

        public async Task SerializeToFile(StorageFile file)
        {
            using (Stream opStream = await file.OpenStreamForWriteAsync())
                JsonSerializer.Serialize(opStream, this, NotebookConfigJsonContext.Default.NotebookConfig);
        }

        public static async Task<NotebookConfig?> DeserializeFile(StorageFolder folder)
        {
            using (Stream ipStream = await (await folder.GetFileAsync("config.json")).OpenStreamForReadAsync())
                return JsonSerializer.Deserialize(ipStream, NotebookConfigJsonContext.Default.NotebookConfig);
        }

        public static async Task<NotebookConfig?> DeserializeFile(StorageFile file)
        {
            using (Stream ipStream = await file.OpenStreamForReadAsync())
                return JsonSerializer.Deserialize(ipStream, NotebookConfigJsonContext.Default.NotebookConfig);
        }
    }

    [JsonSerializable(typeof(NotebookConfig))]
    internal partial class NotebookConfigJsonContext : JsonSerializerContext {}

    public class LastNotebookState
    {
        public double vertScrollPos { get; set; }
        public double horizScrollPos { get; set; }
        public float zoomFactor { get; set; }

        public LastNotebookState()
        {
            vertScrollPos = 0d;
            horizScrollPos = 0d;
            zoomFactor = 1f;

        }

        public LastNotebookState(double vertScrollPos, double horizScrollPos, float zoomFactor)
        {
            this.vertScrollPos = vertScrollPos;
            this.horizScrollPos = horizScrollPos;
            this.zoomFactor = zoomFactor;
        }
    }

    public class PageConfig
    {
        public int id { get; set; }
        public string fileName { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public bool hasBg { get; set; }
        public List<TextData> textBoxes { get; set; }
        public List<ImageData> images { get; set; }

        public bool hasTemplate { get; set; }
        public PageTemplatePattern? pagePattern { get; set; }

        public PageConfig()
        {
            this.id = -1;
            this.fileName = "";
            this.width = this.height = 0d;
            this.hasBg = false;
            textBoxes = new List<TextData>();
            images = new List<ImageData>();
        }

        public PageConfig(int id, double width, double height, bool hasBg)
        {
            this.id = id;
            this.fileName = "page" + (this.id == 0 ? "" : (" (" + this.id + ")")) + ".gif";
            this.width = width;
            this.height = height;
            this.hasBg = hasBg;
            textBoxes = new List<TextData>();
            images = new List<ImageData>();
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

    public class TextData
    {
        public int id { get; set; }
        public int containingPageId { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public double top { get; set; }
        public double left { get; set; }

        public TextData()
        {
            this.id = -1;
            this.containingPageId = -1;
            this.width = 0d;
            this.height = 0d;
            this.top = 0d;
            this.left = 0d;
        }


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

    public class ImageData
    {
        public int id { get; set; }
        public int containingPageId { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public double top { get; set; }
        public double left { get; set; }

        public ImageData()
        {
            this.id = -1;
            this.containingPageId = -1;
            this.width = 0d;
            this.height = 0d;
            this.top = 0d;
            this.left = 0d;
        }

        public ImageData(int id, int containingPageId, double width, double height, double top, double left)
        {
            this.id = id;
            this.containingPageId = containingPageId;
            this.width = width;
            this.height = height;
            this.top = top;
            this.left = left;
        }
    }

    public class DefaultTemplate
    {
        public PageTemplatePattern? pattern { get; set; }

        public DefaultTemplate(PageTemplatePattern? pattern)
        {
            this.pattern = pattern;
        }
    }
}
