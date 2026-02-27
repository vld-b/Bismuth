using Shared;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.IO.Compression;
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
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Preview.Notes;
using Windows.Data.Pdf;
using Windows.Devices.Usb;
using Windows.Devices.WiFiDirect;
using Windows.Foundation.Collections;
using Windows.Graphics.Capture;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
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
using WinRT;
using WinRT.Interop;

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

        private bool finishedLoading = false;

        private UndoRedoSystem undoRedoSystem = new UndoRedoSystem();

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
            this.NavigationCacheMode = NavigationCacheMode.Disabled;

            undoRedoSystem.RegisterUndoButton(btUndo);
            undoRedoSystem.RegisterUndoButton(btFloatUndo);

            undoRedoSystem.RegisterRedoButton(btRedo);
            undoRedoSystem.RegisterRedoButton(btFloatRedo);

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
            NotebookPage lastPage = (NotebookPage)spPageView.Children.Last();
            lastPage.LayoutUpdated -= ScrollToLastPage;

            lastPage.StartBringIntoView(
                new BringIntoViewOptions
                {
                    AnimationDesired = false,
                    VerticalAlignmentRatio = 0d,
                    HorizontalAlignmentRatio = 0.5d,
                });
            svPageZoom.ChangeView(null, null, (float)(Window.Current.CoreWindow.Bounds.Width / lastPage.Width));

            finishedLoading = true;
        }

        private void ShowFileStatus()
        {
            tbAppTitle.Visibility = Visibility.Collapsed;
            pbFileStatus.Visibility = Visibility.Visible;
        }

        private void HideFileStatus()
        {
            tbAppTitle.Visibility = Visibility.Visible;
            pbFileStatus.Visibility = Visibility.Collapsed;
        }

        private void SaveFile(object sender, RoutedEventArgs e)
        {
            SaveFileSafe();
        }

        private void SaveFileSafe()
        {
            if (savingTask == null && finishedLoading)
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

            config!.pageMapping = new ObservableCollection<PageConfig>();

            await Utils.CreatePending(pendingCreations, file);

            foreach (NotebookPage page in spPageView.Children)
            {
                await config!.AddPageWhileSaving(page, file, false);
            }

            if (config is null) // This should never happen, because config is created in OnNavigatedTo if empty
                config = new NotebookConfig( // Would most likely break config (or at least leave it inconsistent), because usableIDs is not being calculated
                    1L,
                    config!.pageMapping,
                    spPageView.Children.Count-1,
                    new List<int>(),
                    new LastNotebookState(),
                    -1,
                    new List<int>(),
                    new DefaultTemplate(null),
                    -1,
                    new List<int>()
                    );
            else
            {
                config.lastNotebookState.vertScrollPos = svPageZoom.VerticalOffset;
                config.lastNotebookState.horizScrollPos = svPageZoom.HorizontalOffset;
                config.lastNotebookState.zoomFactor = svPageZoom.ZoomFactor;
            }
            configFile = await file.CreateFileAsync("config.json", CreationCollisionOption.ReplaceExisting);
            await config.SerializeToFile(configFile);
            if ((new FileInfo(configFile.Path)).Length == 0)
                Debugger.Break();

            await Utils.DeletePending(pendingDeletions, file);
            await Utils.MovePending(pendingMoves, file);
            await Utils.RenamePending(pendingRenames);

            popup.Hide();
            await Utils.ShowTeachingTip(ttInfoPopup, "File saved successfully", "", 3000);
        }

        private void UndoLastAction(object sender, RoutedEventArgs e)
        {
            undoRedoSystem.Undo();
        }

        private void RedoLastAction(object sender, RoutedEventArgs e)
        {
            undoRedoSystem.Redo();
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

            tbAppTitle.Text += Utils.GetNotebookNameFromFolder(file!);
            ShowFileStatus();

            configFile = await file.CreateFileAsync("config.json", CreationCollisionOption.OpenIfExists);
            if ((new FileInfo(configFile.Path)).Length != 0)
            {
                config = NotebookUpgrader.UpgradeToLastVersion((await NotebookConfig.DeserializeFile(configFile))!);

                pbFileStatus.Maximum = config!.pageMapping.Count;

                for (int i = 0; i < config!.pageMapping.Count; ++i)
                {
                    NotebookPage page = await config!.LoadPage(file!, i, svPageZoom, StartTyping, StopTyping);
                    undoRedoSystem.RegisterPageToSystem(page, spPageView);

                    if (this.IsLoaded)
                        page.SetupForDrawing((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked!, inkToolbar);

                    else
                        this.Loaded += (s, e) => page.SetupForDrawing((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked!, inkToolbar);
                    spPageView.Children.Add(page);

                    pbFileStatus.Value = i + 1;
                }
            } else
            {
                config = new NotebookConfig(
                    1L,
                    new ObservableCollection<PageConfig>(),
                    -1,
                    new List<int>(),
                    new LastNotebookState(),
                    -1,
                    new List<int>(),
                    new DefaultTemplate(null),
                    -1,
                    new List<int>()
                    );
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
            } else
            {
                finishedLoading = true;
            }
            HideFileStatus();

            undoRedoSystem.FlushStacks();
        }

        private void AddPageClicked(object sender, RoutedEventArgs e)
        {
            AddPage();
        }

        private void AddPage()
        {
            NotebookPage page = new NotebookPage(
                config!.GetNewPageID(),
                2100,
                2970,
                config!.defaultTemplate.pattern,
                config!.defaultTemplate.pattern is not null
                )
            {
                hasBeenModifiedSinceSave = true,
            };
            undoRedoSystem.RegisterPageToSystem(page, spPageView);

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

        private void AddPage(NotebookPage page)
        {
            undoRedoSystem.RegisterPageToSystem(page, spPageView);
            page.hasBeenModifiedSinceSave = true;
            config!.pageMapping.Add(new PageConfig(page.id, page.Width, page.Height, page.hasBg));
            pendingDeletions.Remove(config!.pageMapping.Last().fileName);
            if (page.hasBg)
                pendingDeletions.Remove(config!.pageMapping.Last().GetBgName());
            foreach (OnPageText text in page.textBoxes)
            {
                pendingDeletions.Remove("text" + (text.id == 0 ? "" : (" (" + text.id + ")")) + ".rtf");
                text.hasBeenModifiedSinceSave = true;
            }
            foreach (OnPageImage image in page.images)
            {
                pendingDeletions.Remove("img" + (image.id == 0 ? "" : (" (" + image.id + ")")) + ".jpg");
                image.hasBeenModifiedSinceSave = true;
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
        }

        private async Task AddPage(StorageFile bg)
        {
            // Make a safe copy of the background; in case user deletes the original file, pendingMoves and pendingRenames would not work; this fixes that
            StorageFile safeBgFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("bg", CreationCollisionOption.GenerateUniqueName);
            await bg.CopyAndReplaceAsync(safeBgFile);

            int pageId = config!.GetNewPageID();
            NotebookPage page;
            BitmapImage bmp = await Utils.GetBMPFromFileWithWidth(safeBgFile, 2100);
            page = new NotebookPage(pageId, bmp)
            {
                hasBeenModifiedSinceSave = true,
            };
            undoRedoSystem.RegisterPageToSystem(page, spPageView);

            page.SetupForDrawing((bool)inkToolbar.GetToolButton(InkToolbarTool.Eraser).IsChecked!, inkToolbar);
            spPageView.Children.Add(page);

            config.pageMapping.Add(new PageConfig(page.id, page.Width, page.Height, true));

            pendingMoves.Add(safeBgFile);
            pendingRenames.Add(new RenameItem(safeBgFile, config.pageMapping.Last().GetBgName()));
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
                int pageId = config!.GetNewPageID();
                NotebookPage page;
                StorageFile bgFile;
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    BitmapImage bmpImage = new BitmapImage();
                    await bg.GetPage(i).RenderToStreamAsync(stream, new PdfPageRenderOptions
                    {
                        // Have to divide by the display scale, because it gets multiplied by it
                        DestinationWidth = (uint)(2100d / DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel),
                    });
                    await bmpImage.SetSourceAsync(stream);

                    //WriteableBitmap wbmp = new WriteableBitmap(bmpImage.PixelWidth, bmpImage.PixelHeight);
                    //stream.Seek(0);
                    //await wbmp.SetSourceAsync(stream);
                    //stream.Seek(0);
                    page = new NotebookPage(pageId, bmpImage)
                    {
                        hasBeenModifiedSinceSave = true,
                    };
                    undoRedoSystem.RegisterPageToSystem(page, spPageView);

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

        private async Task ImportBismuth(StorageFile bismuthFile)
        {
            ContentDialog popup = Utils.ShowLoadingPopup("Importing Bismuth file");

            using ZipArchive archive = new ZipArchive(await bismuthFile.OpenStreamForReadAsync(), ZipArchiveMode.Read);

            ZipArchiveEntry? configEntry = archive.GetEntry("config.json");
            if (configEntry is null)
            {
                popup.Hide();
                return;
            }

            NotebookConfig? importConfig;
            using (Stream configStream = configEntry.Open())
                importConfig = NotebookConfig.DeserializeStream(configStream);
            if (importConfig is null)
            {
                popup.Hide();
                return;
            }

            foreach (PageConfig currentPage in importConfig.pageMapping)
            {
                NotebookPage page;
                int pageId = config!.GetNewPageID();
                if (currentPage.hasBg)
                {
                    BitmapImage img = new BitmapImage();
                    ZipArchiveEntry? bgEntry = archive.GetEntry(currentPage.GetBgName());
                    if (bgEntry is not null)
                    {
                        using (Stream bgStream = bgEntry.Open())
                        {
                            await img.SetSourceAsync(bgStream.AsRandomAccessStream());
                        }
                        string tempBgPath = ApplicationData.Current.TemporaryFolder.Path + "\\" + "tempBg.png";
                        if (File.Exists(tempBgPath))
                            File.Delete(tempBgPath);
                        bgEntry.ExtractToFile(tempBgPath, true);
                        StorageFile tempBgFile = await ApplicationData.Current.TemporaryFolder.GetFileAsync("tempBg.png");
                        pendingMoves.Add(tempBgFile);
                        pendingRenames.Add(
                            new RenameItem(
                                tempBgFile,
                                "bg" + (pageId == 0 ? "" : (" (" + pageId + ")")) + ".png"
                                )
                            );
                        pendingDeletions.Remove("bg" + (pageId == 0 ? "" : (" (" + pageId + ")")) + ".png");
                    }

                    page = new NotebookPage(
                        pageId,
                        img
                        );
                    page.Width = currentPage.width;
                    page.Height = currentPage.height;
                }
                else if (currentPage.hasTemplate)
                {
                    page = new NotebookPage(
                        pageId,
                        currentPage.width,
                        currentPage.height,
                        currentPage.pagePattern,
                        true
                        );
                }
                else
                {
                    page = new NotebookPage(
                        pageId,
                        currentPage.width,
                        currentPage.height
                        );
                }
                undoRedoSystem.RegisterPageToSystem(page, spPageView);

                ZipArchiveEntry? pageEntry = archive.GetEntry(currentPage.fileName);
                if (pageEntry is not null)
                    using (Stream pageStream = pageEntry.Open())
                        await page.LoadFromStream(pageStream.AsInputStream());

                foreach (TextData text in currentPage.textBoxes)
                {
                    OnPageText onPageText = new OnPageText(
                        config!.GetNewTextID(),
                        text.width,
                        text.height,
                        text.top,
                        text.left,
                        page,
                        svPageZoom
                        );
                    ZipArchiveEntry? textEntry = archive.GetEntry(text.GetFileName());
                    if (textEntry is not null)
                    {
                        using (Stream textStream = textEntry.Open())
                        {
                            onPageText.LoadFromStream(textStream.AsRandomAccessStream());
                        }
                    }

                    string textFileName = "text" + (onPageText.id == 0 ? "" : (" (" + onPageText.id + ")")) + ".rtf";
                    pendingCreations.Add(textFileName);
                    pendingDeletions.Add(textFileName);

                    page.AddTextToPage(onPageText);
                    onPageText.TextBoxGotFocus += StartTyping;
                    onPageText.TextBoxLostFocus += StopTyping;
                }

                foreach (ImageData image in currentPage.images)
                {
                    ZipArchiveEntry? imageEntry = archive.GetEntry(image.GetFileName());
                    if (imageEntry is null)
                        continue;

                    using Stream imageStream = imageEntry.Open();

                    using InMemoryRandomAccessStream memStream = new InMemoryRandomAccessStream();
                    await imageStream.CopyToAsync(memStream.AsStreamForWrite());
                    memStream.Seek(0);

                    BitmapImage bmpImage = new BitmapImage();
                    await bmpImage.SetSourceAsync(memStream);
                    memStream.Seek(0);

                    WriteableBitmap wbmp = new WriteableBitmap(
                        bmpImage.PixelWidth,
                        bmpImage.PixelHeight
                        );
                    await wbmp.SetSourceAsync(memStream);
                    memStream.Seek(0);

                    OnPageImage onPageImage = new OnPageImage(
                        config!.GetNewImageID(),
                        image.top,
                        image.left,
                        wbmp,
                        page,
                        svPageZoom,
                        true
                        );
                    onPageImage.Width = image.width;
                    onPageImage.Height = image.height;
                    page.AddImageToPage(onPageImage);

                    pendingDeletions.Remove("img" + (onPageImage.id == 0 ? "" : (" (" + onPageImage.id + ")")) + ".jpg");
                }

                AddPage(page);
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
            ((ToggleButton)sender).IsChecked = svPageOverview.IsPaneOpen;
            gvThumbnails.Items.Clear();
            if (!svPageOverview.IsPaneOpen)
            {
                btExport.Visibility = Visibility.Collapsed;
                return;
            }
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
        }

        private async void ImportFromFile(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker
            {
                FileTypeFilter = { ".pdf", ".bismuth", ".jpg", ".png", ".jpeg" },
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.Downloads,
                CommitButtonText = "Pick file",
            };

            IReadOnlyList<StorageFile> files = await picker.PickMultipleFilesAsync();

            foreach (StorageFile file in files)
            {
                string newFilePath = ApplicationData.Current.TemporaryFolder.Path + "\\" + file.Name;
                if (File.Exists(newFilePath))
                {
                    File.Delete(newFilePath);
                }
                await file.CopyAsync(ApplicationData.Current.TemporaryFolder);
                if (file.Name.EndsWith(".pdf"))
                    await AddPage(await PdfDocument.LoadFromFileAsync(file));
                else if (file.Name.EndsWith(".bismuth"))
                    await ImportBismuth(file);
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
                config!.GetNewTextID(),
                500d,
                500d,
                Math.Min(pageOffset, currentPage!.Height - 500d), 
                (currentPage.Width - 500d) / 2,
                currentPage,
                svPageZoom
                );
            pendingCreations.Add("text" + (txt.id == 0 ? "" : (" (" + txt.id + ")")) + ".rtf");
            pendingDeletions.Remove("text" + (txt.id == 0 ? "" : (" (" + txt.id + ")")) + ".rtf");
            currentPage!.AddTextToPage(txt);
            txt.TextBoxGotFocus += StartTyping;
            txt.TextBoxLostFocus += StopTyping;
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
            lastEditedText!.TextBox.Document.Selection.CharacterFormat.Bold = Windows.UI.Text.FormatEffect.Toggle;
        }

        private void ToggleItalicText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextBox.Document.Selection.CharacterFormat.Italic = Windows.UI.Text.FormatEffect.Toggle;
        }

        private void ToggleUnderlinedText(object sender, RoutedEventArgs e)
        {
            if (lastEditedText!.TextBox.Document.Selection.CharacterFormat.Underline != UnderlineType.Single)
                lastEditedText!.TextBox.Document.Selection.CharacterFormat.Underline = Windows.UI.Text.UnderlineType.Single;
            else
                lastEditedText!.TextBox.Document.Selection.CharacterFormat.Underline = UnderlineType.None;
        }

        private void ToggleStrikethroughText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Toggle;
        }

        private void ToggleSuperscriptText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.Toggle;
        }

        private void ToggleSubscriptText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.Toggle;
        }

        private void DeleteCurrentTextBox(object sender, RoutedEventArgs e)
        {
            lastEditedText!.RemoveTextFromPage();
            pendingCreations.Remove("text" + (lastEditedText!.id == 0 ? "" : (" (" + lastEditedText!.id + ")")) + ".rtf");
            pendingDeletions.Add("text" + (lastEditedText!.id == 0 ? "" : (" (" + lastEditedText!.id + ")")) + ".rtf");
            lastEditedText = null;
            ppTextTools.IsHitTestVisible = false;
            ppTextTools.Opacity = 0d;
        }

        private void SearchTextInCurrentBox()
        {
            RemoveSearchedHighlights();
            RichEditBox current = lastEditedText!.TextBox;

            Windows.UI.Color highlightBg = (Windows.UI.Color)App.Current.Resources["SystemColorHighlightColor"];
            Windows.UI.Color highlightFg = (Windows.UI.Color)App.Current.Resources["SystemColorHighlightTextColor"];

            if (tbFindText != null)
            {
                ITextRange searchRange = current.Document.GetRange(0, 0);
                while (searchRange.FindText(tbFindText.Text, TextConstants.MaxUnitCount, FindOptions.None) > 0)
                {
                    searchRange.CharacterFormat.BackgroundColor = highlightBg;
                    searchRange.CharacterFormat.ForegroundColor = highlightFg;
                }
            }
        }

        private void RemoveSearchedHighlights()
        {
            RichEditBox current = lastEditedText!.TextBox;

            ITextRange docRange = current.Document.GetRange(0, TextConstants.MaxUnitCount);
            Windows.UI.Color defaultBg = ((SolidColorBrush)current.Background).Color;
            Windows.UI.Color defaultFg = ((SolidColorBrush)current.Foreground).Color;

            docRange.CharacterFormat.BackgroundColor = defaultBg;
            docRange.CharacterFormat.ForegroundColor = defaultFg;
        }

        private void ChangeInkColor(ColorPickerButton button, Windows.UI.Color color)
        {
            inkToolbar.InkDrawingAttributes.Color = color;

            InkToolChanged(inkToolbar, new object());
        }

        private void LoadColorBar(object sender, RoutedEventArgs e)
        {
            App.AppSettings.LoadColorsIntoStackPanel((StackPanel)sender, inkToolbar, ChangeInkColor);
        }

        private void AddNewColor(object sender, RoutedEventArgs e)
        {
            App.AppSettings.drawingColors.Add(cpColor.Color);
            App.AppSettings.LoadColorsIntoStackPanel(scColorBar, inkToolbar, ChangeInkColor);
            flNewColor.Hide();
        }

        private void SetNewBrushWidth(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!slTipSize.IsLoaded)
                return;
            inkToolbar.InkDrawingAttributes.Size = new Windows.Foundation.Size(e.NewValue,e.NewValue);
            InkToolChanged(inkToolbar, new object());

            App.AppSettings.tipSize = e.NewValue;
        }

        private void LoadTipSize(object sender, RoutedEventArgs e)
        {
            Slider slider = (Slider)sender;
            slider.Value = App.AppSettings.tipSize;
            inkToolbar.InkDrawingAttributes.Size = new Windows.Foundation.Size(slider.Value, slider.Value);
            InkToolChanged(inkToolbar, new object());
        }

        private async void PasteObject(object sender, RoutedEventArgs e)
        {
            if (lastEditedText is not null)
                return;

            DataPackageView clip = Clipboard.GetContent();

            if (clip.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference stream = await clip.GetBitmapAsync();
                BitmapImage bmp = new BitmapImage();
                WriteableBitmap wbmp;
                using (IRandomAccessStream randomStream = await stream.OpenReadAsync())
                {
                    await bmp.SetSourceAsync(randomStream);
                    wbmp = new WriteableBitmap(
                        bmp.PixelWidth,
                        bmp.PixelHeight
                        );
                    randomStream.Seek(0);
                    await wbmp.SetSourceAsync(randomStream);
                }

                double pageOffset = GetCurrentPage();
                OnPageImage opI = new OnPageImage(
                    config!.GetNewImageID(),
                    Math.Min(pageOffset, currentPage!.Height - 500d),
                    (currentPage!.Width - wbmp.PixelWidth) * 0.5d,
                    wbmp,
                    currentPage!,
                    svPageZoom,
                    true
                    );
                currentPage!.AddImageToPage(opI);
                pendingCreations.Add("img" + (opI.id == 0 ? "" : (" (" + opI.id + ")")) + ".jpg");
                pendingDeletions.Remove("img" + (opI.id == 0 ? "" : (" (" + opI.id + ")")) + ".jpg");
            }
        }

        private async void ExportCurrentPageAsImage(object sender, RoutedEventArgs e)
        {
            FileSavePicker imgFilePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
                FileTypeChoices =
                {
                    ["PNG image"] = (string[])[".png"],
                },
                SuggestedFileName = "Image",
                DefaultFileExtension = ".png",
            };

            StorageFile imgFile = await imgFilePicker.PickSaveFileAsync();
            if (imgFile is null)
                return;

            GetCurrentPage();
            RenderTargetBitmap rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(currentPage);

            IBuffer pixelBuffer = await rtb.GetPixelsAsync();
            byte[] pixels = ArrayPool<byte>.Shared.Rent((int)pixelBuffer.Length);
            try
            {
                pixelBuffer.CopyTo(pixels);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pixels);
            }
            using (IRandomAccessStream stream = await imgFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder enc = await BitmapEncoder.CreateAsync(
                    BitmapEncoder.PngEncoderId,
                    stream
                    );
                enc.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    (uint)rtb.PixelWidth,
                    (uint)rtb.PixelHeight,
                    96,
                    96,
                    pixels.ToArray()
                    );
                await enc.FlushAsync();
            }
        }

        private void PrepareExportOfMultiplePages(string btnLabel, RoutedEventHandler btAction)
        {
            if (!svPageOverview.IsPaneOpen)
                OpenPageOverview(tbSidepane, new RoutedEventArgs());
            btExport.Content = btnLabel;
            btExport.Visibility = Visibility.Visible;
            btExport.Click -= ExportPagesAsPDF;
            btExport.Click -= ExportAsBismuth;
            btExport.Click += btAction;
            foreach(GridViewItem item in gvThumbnails.Items)
            {
                ((PageThumbnail)item.Content).IsSelectable = true;
            }
        }

        private void PrepareExportAsPDF(object sender, RoutedEventArgs e)
        {
            PrepareExportOfMultiplePages("Export as PDF", ExportPagesAsPDF);
        }

        private async void ExportPagesAsPDF(object sender, RoutedEventArgs e)
        {
            FileSavePicker pdfFilePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                FileTypeChoices =
                {
                    ["PDF file"] = (string[])[".pdf"],
                },
                SuggestedFileName = Utils.GetNotebookNameFromFolder(file!),
                DefaultFileExtension = ".pdf",
            };
            StorageFile pdfFile = await pdfFilePicker.PickSaveFileAsync();
            if (pdfFile is null)
                return;

            ContentDialog exportingDialog = Utils.ShowLoadingPopup("Exporting PDF");

            if (File.Exists(pdfFile.Path))
                File.Delete(pdfFile.Path);
            PdfSharpCore.Pdf.PdfDocument doc = new PdfSharpCore.Pdf.PdfDocument();

            int i = 0;
            foreach (GridViewItem item in gvThumbnails.Items)
            {
                PageThumbnail thumb = (PageThumbnail)item.Content;
                int currentIndex = i;
                ++i;
                if (!thumb.IsSelected)
                    continue;

                PdfSharpCore.Pdf.PdfPage pdfPage = doc.AddPage();
                PdfSharpCore.Drawing.XGraphics gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(pdfPage);
                pdfPage.Width = new PdfSharpCore.Drawing.XUnit(595d);
                pdfPage.Height = new PdfSharpCore.Drawing.XUnit(842d);

                const double imgDPI = 96d;
                const double pdfDPI = 72d;
                const double scaleFactor = imgDPI / pdfDPI;

                NotebookPage currentPage = (NotebookPage)spPageView.Children[currentIndex];
                RenderTargetBitmap rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(currentPage, (int)(pdfPage.Width.Value * scaleFactor), (int)(pdfPage.Height.Value * scaleFactor));

                IBuffer pixelBuffer = await rtb.GetPixelsAsync();
                byte[] pixels = ArrayPool<byte>.Shared.Rent((int)pixelBuffer.Length);
                try
                {
                    pixelBuffer.CopyTo(pixels);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(pixels);
                }

                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    BitmapEncoder enc = await BitmapEncoder.CreateAsync(
                        BitmapEncoder.PngEncoderId,
                        stream
                        );
                    enc.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        (uint)rtb.PixelWidth,
                        (uint)rtb.PixelHeight,
                        imgDPI,
                        imgDPI,
                        pixels
                        );
                    await enc.FlushAsync();

                    gfx.DrawImage(
                        PdfSharpCore.Drawing.XImage.FromStream(stream.AsStream),
                        new PdfSharpCore.Drawing.XPoint(0, 0)
                        );
                }
            }
            using (Stream stream = await pdfFile.OpenStreamForWriteAsync())
            {
                doc.Save(stream);
            }
            exportingDialog.Hide();
        }

        private void PrepareExportAsBismuth(object sender, RoutedEventArgs e)
        {
            PrepareExportOfMultiplePages("Export as Bismuth", ExportAsBismuth);
        }

        private async void ExportAsBismuth(object sender, RoutedEventArgs e)
        {
            FileSavePicker bismuthFilePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                FileTypeChoices =
                {
                    ["Bismuth file"] = (string[])[".bismuth"],
                },
                SuggestedFileName = Utils.GetNotebookNameFromFolder(file!),
                DefaultFileExtension = ".bismuth",
            };
            StorageFile bismuthFile = await bismuthFilePicker.PickSaveFileAsync();
            if (bismuthFile is null)
                return;

            ContentDialog exportingDialog = Utils.ShowLoadingPopup("Exporting to Bismuth file");

            StorageFolder tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("tempFolder", CreationCollisionOption.GenerateUniqueName);

            NotebookConfig exportConfig = new NotebookConfig();

            int i = 0;
            foreach(GridViewItem item in gvThumbnails.Items)
            {
                PageThumbnail thumb = (PageThumbnail)item.Content;
                int currentIndex = i;
                ++i;
                if (!thumb.IsSelected)
                    continue;

                NotebookPage currentPage = (NotebookPage)spPageView.Children[currentIndex];

                await exportConfig.AddPageWhileSaving(currentPage, tempFolder, true);
            }
            StorageFile configFile = await tempFolder.CreateFileAsync("config.json");
            await exportConfig.SerializeToFile(configFile);

            using(Stream stream = await bismuthFile.OpenStreamForWriteAsync())
                ZipFile.CreateFromDirectory(tempFolder.Path, stream);

            exportingDialog.Hide();
        }
    }
}
