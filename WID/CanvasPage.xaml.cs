using ABI.Windows.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Preview.Notes;
using Windows.Data.Pdf;
using Windows.Devices.Usb;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static System.Net.WebRequestMethods;

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

        private Stack<string> pendingDeletions = new Stack<string>();
        private Stack<StorageFile> pendingMoves = new Stack<StorageFile>();
        private Stack<RenameItem> pendingRenames = new Stack<RenameItem>();

        private Task? savingTask;

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

        private void SaveFile(object sender, RoutedEventArgs e)
        {
            SaveFileSafe();
        }

        private void SaveFileSafe()
        {
            if (savingTask == null)
            {
                savingTask = SaveFileWithDialog();
                savingTask.ContinueWith(_ => savingTask = null);
            }
        }

        private async Task SaveFileWithDialog()
        {
            if (file is null || configFile is null)
                return;

            ContentDialog popup = Utils.ShowLoadingPopup("Saving file...");

            ObservableCollection<string> pages = new ObservableCollection<string>();
            ObservableCollection<string> bgImages = new ObservableCollection<string>();
            int i = -1;
            foreach (NotebookPage page in spPageView.Children)
            {
                StorageFile pageFile = await file.CreateFileAsync("page" + (page.id == 0 ? "" : " ("+page.id+")") + ".gif", CreationCollisionOption.OpenIfExists);
                pages.Add(pageFile.Name);
                if (page.hasBg)
                    bgImages.Add("bg" + (page.id == 0 ? "" : " (" + page.id + ")") + ".jpg");
                else
                    bgImages.Add(string.Empty);
                await page.SaveToFile(pageFile);
                ++i;
            }
            if (config is null)
                config = new FileConfig(pages, bgImages, i, new List<int>());
            else
            {
                config.pageMapping = pages;
                config.bgMapping = bgImages;
            }
            configFile = await file.CreateFileAsync("config.json", CreationCollisionOption.ReplaceExisting);
            using (Stream opStream = await configFile.OpenStreamForWriteAsync())
                await JsonSerializer.SerializeAsync(opStream, config, FileConfigJsonContext.Default.FileConfig);

            await Utils.DeletePending(pendingDeletions, file!);
            await Utils.MovePending(pendingMoves, file!);
            await Utils.RenamePending(pendingRenames);

            popup.Hide();
            await Utils.ShowTeachingTip(ttInfoPopup, "File saved successfully", "", 3000);
        }

        private void UndoStroke(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count == 0) return;

            redoStack.Push(undoStack.Peek().Clone());
            undoStack.Pop().Selected = true;
            //inkPres.StrokeContainer.DeleteSelected();
            btUndoStroke.IsEnabled = undoStack.Count != 0;
            btRedoStroke.IsEnabled = true;
        }

        private void RedoStroke(object sender, RoutedEventArgs e)
        {
            if (redoStack.Count != 0) return;

            undoStack.Push(redoStack.Pop());
            //inkPres.StrokeContainer.AddStroke(undoStack.Peek());
            btUndoStroke.IsEnabled = true;
            btRedoStroke.IsEnabled = redoStack.Count != 0;
        }

        private void PageBack(object sender, RoutedEventArgs e)
        {
            SaveFileSafe();

            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            file = e.Parameter as StorageFolder;
            if (file is null)
                return;

            tbAppTitle.Text += file.DisplayName[..(file.DisplayName.Length-9)];

            configFile = await file.CreateFileAsync("config.json", CreationCollisionOption.OpenIfExists);
            if ((new FileInfo(configFile.Path)).Length != 0)
            {
                using (Stream ipStream = await configFile.OpenStreamForReadAsync())
                    config = JsonSerializer.Deserialize(ipStream, FileConfigJsonContext.Default.FileConfig);
                for (int i = 0; i < config!.pageMapping.Count; ++i)
                {
                    StorageFile ink = await file.GetFileAsync(config!.pageMapping[i]);
                    NotebookPage page;

                    bool firstParanthesis = config.pageMapping[i].Contains("(");
                    int pageId = -1;
                    if (firstParanthesis)
                        pageId = int.Parse(config!.pageMapping[i][(config!.pageMapping[i].IndexOf("(") + 1)..config!.pageMapping[i].IndexOf(")")]);
                    else
                        pageId = 0;

                    if (config!.bgMapping[i] == string.Empty)
                    {
                        page = new NotebookPage(pageId, 1920, 2880);
                    }
                    else
                    {
                        BitmapImage bgImage = new BitmapImage();
                        StorageFile bgFile = await file.GetFileAsync(config.bgMapping[i]);
                        using (IRandomAccessStream stream = await bgFile.OpenAsync(FileAccessMode.Read))
                            await bgImage.SetSourceAsync(stream);
                        page = new NotebookPage(pageId, bgImage);
                    }
                    
                    await page.LoadFromFile(ink);
                    if (this.IsLoaded)
                        page.SetupForDrawing((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked!, inkToolbar);
                    else
                        this.Loaded += (s, e) => page.SetupForDrawing((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked!, inkToolbar);
                    spPageView.Children.Add(page);
                }
            } else
            {
                config = new FileConfig(new ObservableCollection<string>(), new ObservableCollection<string>(), -1, new List<int>());
                if (this.IsLoaded)
                    AddPage();
                else
                    this.Loaded += (s, e) => AddPage();
            }
        }

        private void AddPageClicked(object sender, RoutedEventArgs e)
        {
            AddPage();
            AddItemFlyout.Hide();
        }

        private void AddPage()
        {
            NotebookPage page = new NotebookPage(config!.usableIDs.Count != 0 ? config!.usableIDs.Pop(0) : ++config!.maxID, 1920, 2880);
            config!.pageMapping.Add("page" + (page.id == 0 ? "" : (" (" + page.id + ")")) + ".gif");
            config!.bgMapping.Add(string.Empty);
            page.SetupForDrawing((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked!, inkToolbar);
            spPageView.Children.Add(page);
            BringIntoViewOptions options = new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.1d,
                HorizontalAlignmentRatio = 0.5d,
            };
            page.StartBringIntoView(options);
            page.AnimateIn();
        }

        private async Task AddPage(StorageFile bg)
        {
            int pageId = config!.usableIDs.Count != 0 ? config!.usableIDs.Pop(0) : ++config!.maxID;
            NotebookPage page;
            config.pageMapping.Add("page" + (pageId == 0 ? "" : (" (" + pageId + ")")) + ".gif");
            config.bgMapping.Add("bg" + (pageId == 0 ? "" : (" (" + pageId + ")")) + ".jpg");
            pendingMoves.Push(bg);
            pendingRenames.Push(new RenameItem(bg, config.bgMapping.Last()));
            using (IRandomAccessStream stream = await bg.OpenAsync(FileAccessMode.Read))
            {
                BitmapImage bmpImage = new BitmapImage();
                bmpImage.DecodePixelWidth = 1920;
                await bmpImage.SetSourceAsync(stream);
                page = new NotebookPage(pageId, bmpImage);
            }

            page.SetupForDrawing((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked!, inkToolbar);
            spPageView.Children.Add(page);
            BringIntoViewOptions options = new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.1d,
                HorizontalAlignmentRatio = 0.5d,
            };
            page.StartBringIntoView(options);
            page.AnimateIn();
        }

        private async Task AddPage(PdfDocument bg)
        {
            ContentDialog popup = Utils.ShowLoadingPopup("Importing PDF");
            for (uint i = 0; i < bg.PageCount; ++i)
            {
                int pageId = config!.usableIDs.Count != 0 ? config!.usableIDs.Pop(0) : ++config!.maxID;
                NotebookPage page;
                config.pageMapping.Add("page" + (pageId == 0 ? "" : (" (" + pageId + ")")) + ".gif");
                config.bgMapping.Add("bg" + (pageId == 0 ? "" : (" (" + pageId + ")")) + ".jpg");
                StorageFile bgFile;
                if (!System.IO.File.Exists(ApplicationData.Current.TemporaryFolder.Path + "\\" + config.bgMapping.Last()))
                    bgFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(config.bgMapping.Last());
                else
                    bgFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(config.bgMapping.Last(), CreationCollisionOption.ReplaceExisting);
                pendingMoves.Push(bgFile);
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    BitmapImage bmpImage = new BitmapImage();
                    bmpImage.DecodePixelWidth = 1920;
                    await bg.GetPage(i).RenderToStreamAsync(stream);
                    await bmpImage.SetSourceAsync(stream);
                    page = new NotebookPage(pageId, bmpImage);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                    SoftwareBitmap bmp = await decoder.GetSoftwareBitmapAsync();
                    using (IRandomAccessStream fileStream = await bgFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fileStream);
                        encoder.SetSoftwareBitmap(bmp);
                        await encoder.FlushAsync();
                    }
                }

                page.SetupForDrawing((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked!, inkToolbar);
                spPageView.Children.Add(page);
                BringIntoViewOptions options = new BringIntoViewOptions
                {
                    AnimationDesired = true,
                    VerticalAlignmentRatio = 0.1d,
                    HorizontalAlignmentRatio = 0.5d,
                };
                page.StartBringIntoView(options);
                page.AnimateIn();
            }
            popup.Hide();
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
            (sender as ToggleButton)!.IsChecked = svPageOverview.IsPaneOpen;
            gvThumbnails.Items.Clear();
            foreach (NotebookPage page in spPageView.Children)
            {
                PageThumbnail pageThumb = new PageThumbnail(page.id, page.Width, page.Height);
                //pageThumb.SetupAsThumbnail();
                pageThumb.page.inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
                pageThumb.page.inkPres.StrokeContainer = page.inkPres.StrokeContainer;
                pageThumb.page.RenderTransform = new ScaleTransform
                {
                    ScaleX = 176 / page.Width,
                    ScaleY = 264 / page.Height,
                    CenterX = 0,
                    CenterY = 0,
                };
                pageThumb.RequestPageDelete += (s, e) => DeletePage(s!, e);
                GridViewItem gvI = new GridViewItem();
                gvI.Content = pageThumb;
                gvI.Width = 176;
                gvI.Height = 264;
                gvI.Margin = new Thickness(10);
                gvI.HorizontalAlignment = HorizontalAlignment.Center;
                gvI.VerticalAlignment = VerticalAlignment.Center;
                gvThumbnails.Items.Add(gvI);
            }
        }

        private void ThumbnailGridViewLoaded(object sender, RoutedEventArgs e)
        {
            foreach (NotebookPage page in spPageView.Children)
            {
                NotebookPage pageThumb = new NotebookPage(page.id);
                pageThumb.inkPres.StrokeContainer = page.inkPres.StrokeContainer;
                pageThumb.Width = 200;
                pageThumb.Height = 200;
                gvThumbnails.Items.Clear();
                gvThumbnails.Items.Add(pageThumb);
            }
        }

        private async Task<SoftwareBitmapSource> RenderThumbnail(NotebookPage page)
        {
            RenderTargetBitmap rtbmp = new RenderTargetBitmap();
            await rtbmp.RenderAsync(page);

            SoftwareBitmap swbmp = SoftwareBitmap.CreateCopyFromBuffer(
                await rtbmp.GetPixelsAsync(),
                BitmapPixelFormat.Rgba8,
                rtbmp.PixelWidth,
                rtbmp.PixelHeight,
                BitmapAlphaMode.Premultiplied);

            SoftwareBitmap resized = SoftwareBitmap.Convert(swbmp, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            SoftwareBitmapSource source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(resized);

            return source;
        }

        private void PagesReordered(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            int movedPageID = ((PageThumbnail)args.Items[0]).page.id;
            int oldIndex = -1, newIndex = -1, i = 0;

            foreach (NotebookPage page in spPageView.Children)
            {
                if (page.id == movedPageID)
                {
                    oldIndex = i;
                    break;
                }
                ++i;
            }

            i = 0;
            foreach (GridViewItem page in sender.Items)
            {
                if (((PageThumbnail)page.Content).page.id == movedPageID)
                {
                    newIndex = i;
                    break;
                }
                ++i;
            }

            if (oldIndex != -1 && newIndex != -1 && oldIndex != newIndex)
            {
                spPageView.Children.Move((uint)oldIndex, (uint)newIndex);
                config!.pageMapping.Move(oldIndex, newIndex);
            }
        }

        private void DeletePage(object sender, DeletePageArgs args)
        {
            int i = 0;
            foreach (GridViewItem pageThumb in gvThumbnails.Items)
            {
                if (((PageThumbnail)pageThumb.Content).page.id == args.id)
                {
                    gvThumbnails.Items.RemoveAt(i);
                    break;
                }
                ++i;
            }

            i = 0;
            foreach(NotebookPage page in spPageView.Children)
            {
                if (page.id == args.id)
                {
                    spPageView.Children.RemoveAt(i);
                    break;
                }
                ++i;
            }

            config!.DeletePageWithId(args.id);
            if (args.id == 0)
            {
                pendingDeletions.Push("page.gif");
                pendingDeletions.Push("bg.jpg");
            } else
            {
                pendingDeletions.Push("page (" + args.id + ").gif");
                pendingDeletions.Push("bg (" + args.id + ").jpg");
            }
        }

        private async void OpenCameraForFileImport(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI cap = new CameraCaptureUI();
            cap.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            cap.PhotoSettings.AllowCropping = true;

            StorageFile picture = await cap.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (picture is not null)
            {
                await AddPage(picture);
            }

            AddItemFlyout.Hide();
        }

        private async void ImportFromFile(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker
            {
                FileTypeFilter = { ".pdf", ".jpg", ".png", ".jpeg" },
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.Downloads,
                CommitButtonText = "Pick file",
            };

            IReadOnlyList<StorageFile> files = await picker.PickMultipleFilesAsync();

            foreach (StorageFile file in files)
            {
                string newFilePath = ApplicationData.Current.TemporaryFolder.Path + "\\" + file.Name;
                if (System.IO.File.Exists(newFilePath))
                {
                    System.IO.File.Delete(newFilePath);
                }
                await file.CopyAsync(ApplicationData.Current.TemporaryFolder);
                if (file.Name.EndsWith(".pdf"))
                    await AddPage(await PdfDocument.LoadFromFileAsync(file));
                else
                {
                    await AddPage(file);
                }
            }
        }

        private void AddTextToCurrentPage(object sender, RoutedEventArgs e)
        {

        }
    }
}
