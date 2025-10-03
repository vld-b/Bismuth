using ABI.Windows.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Preview.Notes;
using Windows.Devices.Usb;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
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
    public sealed partial class CanvasPage : Page
    {
        private StorageFolder? file;
        private StorageFile? configFile;

        private FileConfig? config;

        private Stack<InkStroke> undoStack = new Stack<InkStroke>();
        private Stack<InkStroke> redoStack = new Stack<InkStroke>();

        public CanvasPage()
        {
            InitializeComponent();
            SetTitlebar();
        }

        private void SetTitlebar()
        {
            Window.Current.SetTitleBar(TitleBar);
            tbAppTitle.Text = AppInfo.Current.DisplayInfo.DisplayName+": ";
        }

        private void AddStrokeToUndoStack(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            foreach (InkStroke stroke in args.Strokes)
            {
                undoStack.Push(stroke);
            }
            btUndoStroke.IsEnabled = true;
        }

        private async void RecognizeStroke(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            //foreach (InkRecognizer recognizer in inkRec.GetRecognizers())
            //{
            //    if (recognizer.Name.Equals("Microsoft English (US) Handwriting Recognizer"))
            //    {
            //        inkRec.SetDefaultRecognizer(recognizer);
            //        break;
            //    }
            //}
            //inkRec.RecognizeAsync(inkPres.StrokeContainer, InkRecognitionTarget.Recent).Completed = (resAsync, status) => {
            //    IReadOnlyList<InkRecognitionResult> res = resAsync.GetResults();
            //    if (res.Count > 0)
            //    {
            //        txtTest.Text = string.Empty;
            //        foreach (InkRecognitionResult result in res)
            //        {
            //            txtTest.Text += result.GetTextCandidates().FirstOrDefault() + " ";
            //        }
            //    }
            //};
            //if (!inkPres.StrokeContainer.GetStrokes().Any())
            //    return;

            //IReadOnlyList<InkRecognitionResult> results = await inkRec.RecognizeAsync(inkPres.StrokeContainer, InkRecognitionTarget.All);
            //if (results.Count > 0)
            //{
            //    tbTest.Text = string.Empty;
            //    foreach (InkRecognitionResult result in results)
            //    {
            //        tbTest.Text += result.GetTextCandidates().FirstOrDefault() + " ";
            //    }
            //}
        }

        private async void SaveFileWithDialog(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (file is null || configFile is null)
                return;

            List<string> pages = new List<string>();
            int i = 0;
            foreach (NotebookPage page in spPageView.Children)
            {
                StorageFile pageFile = await file.CreateFileAsync("page" + (i == 0 ? "" : " ("+i+")") + ".gif", CreationCollisionOption.OpenIfExists);
                pages.Add(pageFile.Name);
                await page.SaveToFile(pageFile);
                ++i;
            }
            if (config is null)
                config = new FileConfig(pages);
            else
                config.pageMapping = pages;
            configFile = await file.CreateFileAsync("config.json", CreationCollisionOption.OpenIfExists);
            using (Stream opStream = await configFile.OpenStreamForWriteAsync())
                await JsonSerializer.SerializeAsync(opStream, config, FileConfigJsonContext.Default.FileConfig);
        }

        private void UndoStroke(object sender, RoutedEventArgs e)
        {
            if (!undoStack.Any()) return;

            redoStack.Push(undoStack.Peek().Clone());
            undoStack.Pop().Selected = true;
            //inkPres.StrokeContainer.DeleteSelected();
            btUndoStroke.IsEnabled = undoStack.Any();
            btRedoStroke.IsEnabled = true;
        }

        private void RedoStroke(object sender, RoutedEventArgs e)
        {
            if (!redoStack.Any()) return;

            undoStack.Push(redoStack.Pop());
            //inkPres.StrokeContainer.AddStroke(undoStack.Peek());
            btUndoStroke.IsEnabled = true;
            btRedoStroke.IsEnabled = redoStack.Any();
        }

        private void PageBack(object sender, RoutedEventArgs e)
        {
            SaveFileWithDialog(sender, e);

            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            file = e.Parameter as StorageFolder;
            if (file is null)
                return;

            tbAppTitle.Text += file.DisplayName;

            configFile = await file.CreateFileAsync("config.json", CreationCollisionOption.OpenIfExists);
            if ((new FileInfo(configFile.Path)).Length != 0)
            {
                using (Stream ipStream = await configFile.OpenStreamForReadAsync())
                    config = JsonSerializer.Deserialize(ipStream, FileConfigJsonContext.Default.FileConfig);
                foreach (string pageName in config!.pageMapping)
                {
                    StorageFile ink = await file.GetFileAsync(pageName);
                    NotebookPage page = new NotebookPage();
                    await page.LoadFromFile(ink);
                    page.Loaded += (s, e) => SetupPage(page);
                    spPageView.Children.Add(page);
                }
            }
        }

        private void AddPage(object sender, RoutedEventArgs e)
        {
            NotebookPage page = new NotebookPage();
            SetupPage(page);
            spPageView.Children.Add(page);
        }

        private void SetupPage(NotebookPage page)
        {
            page.inkPres.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Mouse;
            if ((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked)
                page.inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
            page.inkPres.UpdateDefaultDrawingAttributes(inkToolbar.InkDrawingAttributes);
        }

        private void InkToolChanged(InkToolbar sender, object args)
        {
            foreach (NotebookPage page in spPageView.Children)
            {
                page.inkPres.UpdateDefaultDrawingAttributes(inkToolbar.InkDrawingAttributes);
            }
        }

        private void RulerChecked(object sender, RoutedEventArgs e)
        {
            foreach (NotebookPage page in spPageView.Children)
            {
                page.ruler.IsVisible = true;
            }
        }

        private void RulerUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (NotebookPage page in spPageView.Children)
            {
                page.ruler.IsVisible = false;
            }
        }

        private void ProtractorChecked(object sender, RoutedEventArgs e)
        {
            foreach (NotebookPage page in spPageView.Children)
            {
                page.protractor.IsVisible = true;
            }
        }

        private void ProtractorUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (NotebookPage page in spPageView.Children)
            {
                page.protractor.IsVisible = false;
            }
        }

        private void EraserChecked(object sender, RoutedEventArgs e)
        {
            foreach (NotebookPage page in spPageView.Children)
            {
                page.inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
            }
        }

        private void EraserUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (NotebookPage page in spPageView.Children)
            {
                page.inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
            }
        }

        private void OpenPageOverview(object sender, RoutedEventArgs e)
        {
            svPageOverview.IsPaneOpen = !svPageOverview.IsPaneOpen;
            (sender as ToggleButton).IsChecked = svPageOverview.IsPaneOpen;
            ThumbnailGridViewLoaded(null, null);
        }

        private async void ThumbnailGridViewLoaded(object sender, RoutedEventArgs e)
        {
            foreach (NotebookPage page in spPageView.Children)
            {
                NotebookPage pageThumb = new NotebookPage();
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    await page.SaveToStream(stream);
                    stream.Seek(0);
                    await pageThumb.LoadFromStream(stream);
                }
                pageThumb.inkPres.StrokeContainer = page.inkPres.StrokeContainer;
                gvThumbnails.Items.Clear();
                gvThumbnails.Items.Add(pageThumb);
            }
        }
    }
}
