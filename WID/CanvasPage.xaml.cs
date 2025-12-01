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
using System.Security.Cryptography;
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
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static System.Net.Mime.MediaTypeNames;
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

        private NotebookConfig? config;

        private Stack<InkStroke> undoStack = new Stack<InkStroke>();
        private Stack<InkStroke> redoStack = new Stack<InkStroke>();

        private List<string> pendingCreations = new List<string>();
        private List<string> pendingDeletions = new List<string>();
        private List<StorageFile> pendingMoves = new List<StorageFile>();
        private List<RenameItem> pendingRenames = new List<RenameItem>();

        OnPageText? lastEditedText;

        private Task? savingTask;

        private NotebookPage? currentPage;

        public CanvasPage()
        {
            InitializeComponent();
            SetTitlebar();

            bdTextTools.GotFocus += (s, e) => {
                ppTextTools.IsHitTestVisible = true;
                ppTextTools.Opacity = 1d;
            };
            bdTextTools.LostFocus += (s, e) => {
                ppTextTools.IsHitTestVisible = false;
                ppTextTools.Opacity = 0d;
            };
        }

        private void SetTitlebar()
        {
            Window.Current.SetTitleBar(TitleBar);
            tbAppTitle.Text = AppInfo.Current.DisplayInfo.DisplayName+": ";
        }

        private void ScrollToLastPage(object? sender, object e)
        {
            ((NotebookPage)spPageView.Children.Last()).LayoutUpdated -= ScrollToLastPage;

            if (spPageView.Children.Count > 0)
            {
                spPageView.Children.Last().StartBringIntoView(
                    new BringIntoViewOptions
                    {
                        AnimationDesired = false,
                        VerticalAlignmentRatio = 0d,
                        HorizontalAlignmentRatio = 0.5d,
                    });
            }
        }

        private void AddStrokeToUndoStack(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            foreach (InkStroke stroke in args.Strokes)
            {
                undoStack.Push(stroke);
            }
            btUndoStroke.IsEnabled = true;
        }

        //private async void RecognizeStroke(InkPresenter sender, InkStrokesCollectedEventArgs args)
        //{
        //    foreach (InkRecognizer recognizer in inkRec.GetRecognizers())
        //    {
        //        if (recognizer.Name.Equals("Microsoft English (US) Handwriting Recognizer"))
        //        {
        //            inkRec.SetDefaultRecognizer(recognizer);
        //            break;
        //        }
        //    }
        //    inkRec.RecognizeAsync(inkPres.StrokeContainer, InkRecognitionTarget.Recent).Completed = (resAsync, status) =>
        //    {
        //        IReadOnlyList<InkRecognitionResult> res = resAsync.GetResults();
        //        if (res.Count > 0)
        //        {
        //            txtTest.Text = string.Empty;
        //            foreach (InkRecognitionResult result in res)
        //            {
        //                txtTest.Text += result.GetTextCandidates().FirstOrDefault() + " ";
        //            }
        //        }
        //    };
        //    if (!inkPres.StrokeContainer.GetStrokes().Any())
        //        return;

        //    IReadOnlyList<InkRecognitionResult> results = await inkRec.RecognizeAsync(inkPres.StrokeContainer, InkRecognitionTarget.All);
        //    if (results.Count > 0)
        //    {
        //        tbTest.Text = string.Empty;
        //        foreach (InkRecognitionResult result in results)
        //        {
        //            tbTest.Text += result.GetTextCandidates().FirstOrDefault() + " ";
        //        }
        //    }
        //}

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

            ObservableCollection<PageConfig> pages = new ObservableCollection<PageConfig>();

            await Utils.CreatePending(pendingCreations, file);

            foreach (NotebookPage page in spPageView.Children)
            {
                StorageFile pageFile = await file.CreateFileAsync("page" + (page.id == 0 ? "" : " ("+page.id+")") + ".gif", CreationCollisionOption.OpenIfExists);
                PageConfig currentConfig = new PageConfig(page.id, page.Width, page.Height, page.hasBg);

                foreach (OnPageText txt in page.textBoxes)
                {
                    StorageFile rtfFile = await file.GetFileAsync("text" + (txt.id == 0 ? "" : (" (" + txt.id + ")")) + ".rtf");
                    using (IRandomAccessStream stream = await rtfFile.OpenAsync(FileAccessMode.ReadWrite))
                        txt.SaveToStream(stream);

                    currentConfig.textBoxes.Add(new TextData(
                        txt.id,
                        page.id,
                        txt.Width,
                        txt.Height,
                        Canvas.GetTop(txt),
                        Canvas.GetLeft(txt) )
                        );
                }

                pages.Add(currentConfig);
                await page.SaveToFile(pageFile);
            }

            if (config is null) // This should never happen, because config is created in OnNavigatedTo if empty
                config = new NotebookConfig( // Would most likely break config (or at least leave it inconsistent), because usableIDs is not being calculated
                    pages,
                    spPageView.Children.Count-1,
                    new List<int>(),
                    new LastNotebookState(),
                    -1,
                    new List<int>()
                    );
            else
            {
                config.pageMapping = pages;
                config.lastNotebookState.vertScrollPos = svPageZoom.VerticalOffset;
                config.lastNotebookState.horizScrollPos = svPageZoom.HorizontalOffset;
                config.lastNotebookState.zoomFactor = svPageZoom.ZoomFactor;
            }
            configFile = await file.CreateFileAsync("config.json", CreationCollisionOption.ReplaceExisting);
            using (Stream opStream = await configFile.OpenStreamForWriteAsync())
                await JsonSerializer.SerializeAsync(opStream, config, NotebookConfigJsonContext.Default.NotebookConfig);

            await Utils.DeletePending(pendingDeletions, file);
            await Utils.MovePending(pendingMoves, file);
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
                    config = JsonSerializer.Deserialize(ipStream, NotebookConfigJsonContext.Default.NotebookConfig);
                for (int i = 0; i < config!.pageMapping.Count; ++i)
                {
                    StorageFile ink = await file.GetFileAsync(config!.pageMapping[i].fileName);
                    NotebookPage page;

                    if (config!.pageMapping[i].hasBg)
                    {
                        WriteableBitmap bgImage = await Utils.GetWBMPFromFileWithWidth(
                            await file.GetFileAsync(config.pageMapping[i].GetBgName()),
                            (int)config!.pageMapping[i].width
                            );
                        page = new NotebookPage(config!.pageMapping[i].id, bgImage);
                    }
                    else
                    {
                        page = new NotebookPage(config!.pageMapping[i].id, config!.pageMapping[i].width, config!.pageMapping[i].height);
                    }

                    foreach (TextData textData in config!.pageMapping[i].textBoxes)
                    {
                        OnPageText txt = new OnPageText(
                            textData.id,
                            textData.width,
                            textData.height,
                            textData.top,
                            textData.left,
                            page,
                            svPageZoom
                            );
                        using (IRandomAccessStream stream = await
                            (await file.GetFileAsync("text" + (textData.id == 0 ? "" : (" (" + textData.id + ")")) + ".rtf"))
                            .OpenAsync(FileAccessMode.Read))
                        {
                            txt.LoadFromStream(stream);
                        }
                        page.AddTextToPage(txt);
                        txt.TextBoxGotFocus += StartTyping;
                        txt.TextBoxLostFocus += StopTyping;
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
                config = new NotebookConfig(
                    new ObservableCollection<PageConfig>(),
                    -1,
                    new List<int>(),
                    new LastNotebookState(),
                    -1,
                    new List<int>());
                if (this.IsLoaded)
                    AddPage();
                else
                    this.Loaded += (s, e) => AddPage();
            }


            if (spPageView.Children.Count > 0)
            {
                ((NotebookPage)spPageView.Children.Last()).LayoutUpdated += ScrollToLastPage;
                ConnectedAnimation anim = ConnectedAnimationService.GetForCurrentView().GetAnimation("OpenNotebook");
                if (anim is not null)
                {
                    anim.TryStart(spPageView.Children.Last());
                }
            }
        }

        private void AddPageClicked(object sender, RoutedEventArgs e)
        {
            AddPage();
            AddItemFlyout.Hide();
        }

        private void AddPage()
        {
            NotebookPage page = new NotebookPage(config!.usablePageIDs.Count != 0 ? config!.usablePageIDs.Pop(0) : ++config!.maxPageID, 1920, 2880);
            config!.pageMapping.Add(new PageConfig(page.id, page.Width, page.Height, false));

            pendingDeletions.Remove(config!.pageMapping.Last().fileName);

            page.SetupForDrawing((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked!, inkToolbar);
            spPageView.Children.Add(page);
            BringIntoViewOptions options = new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.1d,
                HorizontalAlignmentRatio = 0.5d,
            };
            page.StartBringIntoView(options);
        }

        private async Task AddPage(StorageFile bg)
        {
            int pageId = config!.usablePageIDs.Count != 0 ? config!.usablePageIDs.Pop(0) : ++config!.maxPageID;
            NotebookPage page;
            WriteableBitmap wbmp = await Utils.GetWBMPFromFileWithWidth(bg, 2100);
            page = new NotebookPage(pageId, wbmp);

            page.SetupForDrawing((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked!, inkToolbar);
            spPageView.Children.Add(page);

            config.pageMapping.Add(new PageConfig(page.id, page.Width, page.Height, true));

            pendingMoves.Add(bg);
            pendingRenames.Add(new RenameItem(bg, config.pageMapping.Last().GetBgName()));
            // Remove background from pending deletions so it doesn't get deleted when it should be present
            pendingDeletions.Remove(config.pageMapping.Last().GetBgName());
            pendingDeletions.Remove(config.pageMapping.Last().fileName);


            BringIntoViewOptions options = new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.1d,
                HorizontalAlignmentRatio = 0.5d,
            };
            page.StartBringIntoView(options);
        }

        private async Task AddPage(PdfDocument bg)
        {
            ContentDialog popup = Utils.ShowLoadingPopup("Importing PDF");
            for (uint i = 0; i < bg.PageCount; ++i)
            {
                int pageId = config!.usablePageIDs.Count != 0 ? config!.usablePageIDs.Pop(0) : ++config!.maxPageID;
                NotebookPage page;
                StorageFile bgFile;
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    BitmapImage bmpImage = new BitmapImage();
                    bmpImage.DecodePixelWidth = 2100;
                    await bg.GetPage(i).RenderToStreamAsync(stream, new PdfPageRenderOptions
                    {
                        DestinationWidth = 2100,
                    });
                    await bmpImage.SetSourceAsync(stream);

                    WriteableBitmap wbmp = new WriteableBitmap(bmpImage.PixelWidth, bmpImage.PixelHeight);
                    stream.Seek(0);
                    await wbmp.SetSourceAsync(stream);
                    stream.Seek(0);
                    page = new NotebookPage(pageId, wbmp);

                    config.pageMapping.Add(new PageConfig(page.id, page.Width, page.Height, true));

                    if (!System.IO.File.Exists(ApplicationData.Current.TemporaryFolder.Path + "\\" + config.pageMapping.Last().GetBgName()))
                        bgFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(config.pageMapping.Last().GetBgName());
                    else
                        bgFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(config.pageMapping.Last().GetBgName(), CreationCollisionOption.ReplaceExisting);
                    pendingMoves.Add(bgFile);

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

                // Remove background from pending deletions so it doesn't get deleted when it should be present
                pendingDeletions.Remove(config.pageMapping.Last().GetBgName());
                pendingDeletions.Remove(config.pageMapping.Last().fileName);

            }
            BringIntoViewOptions options = new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.1d,
                HorizontalAlignmentRatio = 0.5d,
            };
            spPageView.Children.Last().StartBringIntoView(options);
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
                PageThumbnail pageThumb;
                if (page.hasBg)
                    pageThumb = new PageThumbnail(page.id, page.Width, page.Height, page.bgImage!);
                else
                    pageThumb = new PageThumbnail(page.id, page.Width, page.Height);
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
                gvI.Tapped += (s, e) => NavigateToPage(s, e);
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

        private double GetCurrentPage()
        {
            int pageIndex = 0;
            double verticalOffset = svPageZoom.VerticalOffset/svPageZoom.ZoomFactor + Window.Current.Bounds.Height/(2*svPageZoom.ZoomFactor); // Add half window height because user likely refers to middle page

            do
            {
                verticalOffset -= ((NotebookPage)spPageView.Children[Math.Min(spPageView.Children.Count-1, pageIndex++)]).Height;
            } while (verticalOffset > 0);

            currentPage = (NotebookPage)spPageView.Children[Math.Min(spPageView.Children.Count-1, --pageIndex)];

            return Math.Min(currentPage.Height, Math.Max(0, verticalOffset + currentPage.Height));
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
            foreach (NotebookPage page in spPageView.Children)
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
                pendingDeletions.Add("page.gif");
                pendingDeletions.Add("bg.jpg");
            } else
            {
                pendingDeletions.Add("page (" + args.id + ").gif");
                pendingDeletions.Add("bg (" + args.id + ").jpg");
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
            double pageOffset = GetCurrentPage();
            OnPageText txt = new OnPageText(
                config!.usableTextIDs.Count == 0 ? ++config!.maxTextID : config!.usableTextIDs.Pop(0),
                500d,
                500d,
                Math.Min(pageOffset, currentPage!.Height - 500d),
                (currentPage.Width - 500d) / 2,
                currentPage,
                svPageZoom
                );
            pendingCreations.Add("text" + (txt.id == 0 ? "" : (" (" + txt.id + ")")) + ".rtf");
            currentPage!.AddTextToPage(txt);
            txt.TextBoxGotFocus += (s, e) => StartTyping(s, e);
            txt.TextBoxLostFocus += (s, e) => StopTyping(s, e);
            AddItemFlyout.Hide();
        }

        private void NavigateToPage(object sender, TappedRoutedEventArgs e)
        {
            int pageId = ((PageThumbnail)((GridViewItem)sender).Content).page.id;
            foreach (NotebookPage page in spPageView.Children)
            {
                if (page.id == pageId)
                {
                    page.StartBringIntoView(new BringIntoViewOptions
                    {
                        AnimationDesired = true,
                        VerticalAlignmentRatio = 0.1d,
                        HorizontalAlignmentRatio = 0.5d,
                    });
                    break;
                }
            }
        }

        private void ToolPopupLoaded(object sender, RoutedEventArgs e)
        {
            Popup popup = (Popup)sender;
            popup.HorizontalOffset = -((FrameworkElement)popup.Child).ActualWidth / 2;
        }

        private void StartTyping(object? sender, EventArgs e)
        {
            lastEditedText = (OnPageText?)sender;
            ppTextTools.Opacity = 1d;
            ppTextTools.IsHitTestVisible = true;
        }

        private void StopTyping(object? sender, EventArgs e)
        {
            ppTextTools.IsHitTestVisible = false;
            ppTextTools.Opacity = 0d;
        }

        private void ToggleBoldText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextContent.Document.Selection.CharacterFormat.Bold = Windows.UI.Text.FormatEffect.Toggle;
        }

        private void ToggleItalicText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextContent.Document.Selection.CharacterFormat.Italic = Windows.UI.Text.FormatEffect.Toggle;
        }

        private void ToggleUnderlinedText(object sender, RoutedEventArgs e)
        {
            if (lastEditedText!.TextContent.Document.Selection.CharacterFormat.Underline != UnderlineType.Single)
                lastEditedText!.TextContent.Document.Selection.CharacterFormat.Underline = Windows.UI.Text.UnderlineType.Single;
            else
                lastEditedText!.TextContent.Document.Selection.CharacterFormat.Underline = UnderlineType.None;
        }

        private void ToggleStrikethroughText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextContent.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Toggle;
        }

        private void ToggleSuperscriptText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextContent.Document.Selection.CharacterFormat.Superscript = FormatEffect.Toggle;
        }

        private void ToggleSubscriptText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextContent.Document.Selection.CharacterFormat.Subscript = FormatEffect.Toggle;
        }
    }
}
