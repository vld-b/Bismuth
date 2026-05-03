using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NotebookSearchResult : Grid
    {
        public StorageFolder notebookFolder { get; private set; }
        public string notebookName { get; private set; }
        public int pageId { get; private set; }
        public RecognizedText recText { get; private set; }
        public NotebookSearchResult(StorageFolder notebookFolder, string notebookName, int notebookPage, int pageId, RecognizedText recText)
        {
            this.InitializeComponent();

            this.notebookFolder = notebookFolder;
            this.notebookName = notebookName;
            tbNotebookName.Text = notebookName;
            tbNotebookPage.Text = "page: " + notebookPage.ToString();
            this.pageId = pageId;
            this.recText = recText;
        }
    }
}
