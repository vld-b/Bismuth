using AppSettings;
using Shared;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Preview.Notes;
using Windows.Data.Pdf;
using Windows.Devices.Usb;
using Windows.Devices.WiFiDirect;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.Collation;
using Windows.Graphics.Capture;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.Protection;
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
        private SearchNavigation? searchNav;

        private NotebookConfig? config;

        private bool finishedLoading = false;

        private UndoRedoSystem undoRedoSystem = new UndoRedoSystem();

        private PendingFileOperationsSystem pending = new PendingFileOperationsSystem(ApplicationData.Current.LocalFolder);

        private OnPageText? lastEditedText;
        private OnPageImage? lastEditedImage;
        private PageState pageState;
        private List<RecoloredStroke>? recoloredStrokes;

        private Task? savingTask;

        private NotebookPage? currentPage;
        private NotebookPage? pageToScrollTo;
        private OnPageText? textToScrollTo;
        private CurrentInkingTool currentInkingTool = CurrentInkingTool.Drawing;
        private InkDrawingAttributes attrs = new InkDrawingAttributes();
        private CurrentlySelectedColors currentColors = new CurrentlySelectedColors();

        private double toolOptionsNormalWidth;
        private double inkStackpanelNormalWidth;
        private double inkToolbarNormalHorzontalOffset;

        private Task periodicalSavingTask;
        private CancellationTokenSource periodicalSavingTaskCancellationToken;
        private Microsoft.UI.Xaml.Controls.ProgressBar? savingBar;

        public CanvasPage()
        {
            InitializeComponent();
            SetTitlebar();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;

            pageState = new PageState(ppSelectionTools);

            undoRedoSystem.RegisterUndoButton(btUndo);
            undoRedoSystem.RegisterUndoButton(btFloatUndo);

            undoRedoSystem.RegisterRedoButton(btRedo);
            undoRedoSystem.RegisterRedoButton(btFloatRedo);

            undoRedoSystem.BindPendingFileOperationsSystem(pending);

            btInkTool.isSelected = true;
            btInkTool.Foreground = new SolidColorBrush(App.AppSettings.drawingColors[0]);
            btHighlightTool.Foreground = new SolidColorBrush(App.AppSettings.highlightColors[0]);
            btPencilTool.Foreground = new SolidColorBrush(App.AppSettings.pencilColors[0]);
            btCalligraphyTool.Foreground = new SolidColorBrush(App.AppSettings.calligraphyColors[0]);

            currentInkingTool = CurrentInkingTool.Drawing;
            attrs = new InkDrawingAttributes
            {
                PenTip = PenTipShape.Circle,
                DrawAsHighlighter = false,
                Color = App.AppSettings.drawingColors[currentColors.drawing],
                Size = new Windows.Foundation.Size(App.AppSettings.tipSize, App.AppSettings.tipSize),
            };

            periodicalSavingTaskCancellationToken = new CancellationTokenSource();
            periodicalSavingTask = SaveFilePeriodically(periodicalSavingTaskCancellationToken.Token);
        }

        private void SetTitlebar()
        {
            Window.Current.SetTitleBar(TitleBar);
            tbAppTitle.Text = AppInfo.Current.DisplayInfo.DisplayName+": ";
        }

        private async void ScrollToLastPage(object? sender, object e)
        {
            if (pageToScrollTo is null)
                return;
            NotebookPage page = pageToScrollTo;
            pageToScrollTo.LayoutUpdated -= ScrollToLastPage;
            pageToScrollTo = null;

            if (searchNav is null)
            {
                page.StartBringIntoView(
                    new BringIntoViewOptions
                    {
                        AnimationDesired = false,
                        VerticalAlignmentRatio = 0d,
                        HorizontalAlignmentRatio = .5d,
                    }
                    );
            } else
            {
                foreach (NotebookPage p in spPageView.Children)
                {
                    if (p.id == searchNav.pageId)
                    {
                        page = p;

                        double verticalAlignmentRatio = 0d;
                        if (searchNav!.recText.boudingBox is not null)
                            verticalAlignmentRatio = searchNav!.recText.boudingBox.y / page.Height;
                        else
                        {
                            foreach (IOnPageItem item in page.onPageItems)
                                if (item is OnPageText text && text.id == searchNav!.recText.textBoxId)
                                {
                                    verticalAlignmentRatio = text.Top / page.Height + .2d;
                                    textToScrollTo = text;
                                }
                        }

                        page.StartBringIntoView(
                            new BringIntoViewOptions
                            {
                                AnimationDesired = false,
                                VerticalAlignmentRatio = verticalAlignmentRatio,
                                HorizontalAlignmentRatio = .5d,
                            }
                            );
                        break;
                    }
                }
                await Task.Delay(1000);
                await page.HighlightText(searchNav.searchKeyword, searchNav.recText);
            }

            finishedLoading = true;
        }

        private void ShowFileStatus()
        {
            spFileInfo.Visibility = Visibility.Collapsed;
            pbFileStatus.Visibility = Visibility.Visible;
        }

        private void HideFileStatus()
        {
            spFileInfo.Visibility = Visibility.Visible;
            pbFileStatus.Visibility = Visibility.Collapsed;
        }

        private async Task SaveFilePeriodically(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(10000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                if (!cancellationToken.IsCancellationRequested)
                    await SaveFileSilent();
            }
        }

        private async void SaveFile(object sender, RoutedEventArgs e)
        {
            await SaveFileSafe();
        }

        private async Task HaltPeriodicSave()
        {
            periodicalSavingTaskCancellationToken.Cancel();
            await periodicalSavingTask;
            periodicalSavingTaskCancellationToken.Dispose();
        }

        private void ResumePeriodicSave()
        {
            periodicalSavingTaskCancellationToken.Dispose();
            periodicalSavingTaskCancellationToken = new CancellationTokenSource(); // Resume periodical saving
            periodicalSavingTask = SaveFilePeriodically(periodicalSavingTaskCancellationToken.Token);
        }

        private async Task SaveFileSafe()
        {
            if (savingTask == null && finishedLoading)
            {
                await HaltPeriodicSave(); // Make sure previous save is finished or cancelled the wait

                savingTask = SaveFileWithDialog();
                await savingTask;
                savingTask = null;

                ResumePeriodicSave();
            }
        }

        private async Task SaveFileWithDialog()
        {
            ContentDialog popup = Utils.ShowLoadingPopup("Saving file...");
            savingBar = (Microsoft.UI.Xaml.Controls.ProgressBar)popup.Content;

            await SaveFileSilent();

            savingBar = null;
            popup.Hide();
            _ = Utils.ShowTeachingTip(ttInfoPopup, "File saved successfully ✅", "", 3000);
        }

        private async Task SaveFileSilent()
        {
            if (file is null || configFile is null)
                return;

            prSilentSave.IsActive = true;
            prSilentSave.Visibility = Visibility.Visible;

            pending.Lock();

            config!.pageMapping = new ObservableCollection<PageConfig>(new List<PageConfig>(spPageView.Children.Count));

            await pending.ApplyPendingFileOperations();

            if (savingBar is not null)
            {
                savingBar.Minimum = 0.0d;
                savingBar.Maximum = spPageView.Children.Count;
                savingBar.Value = 0.0d;
            }

            foreach (NotebookPage page in spPageView.Children)
            {
                await config!.AddPageWhileSaving(page, file, file, false);
                if (savingBar is not null)
                    savingBar.Value += 1.0d;
            }

            config.lastNotebookState.vertScrollPos = svPageZoom.VerticalOffset;
            config.lastNotebookState.horizScrollPos = svPageZoom.HorizontalOffset;
            config.lastNotebookState.zoomFactor = svPageZoom.ZoomFactor;

            configFile = await file.CreateFileAsync("config.json", CreationCollisionOption.ReplaceExisting);
            await config.SerializeToFile(configFile);
            if ((new FileInfo(configFile.Path)).Length == 0)
                Debugger.Break();

            int currentSearchableNotebookIndex = App.SearchableNotebooks.FindIndex(0, (sn) => { return Utils.GetNotebookPathFromFolder(file!) == Utils.GetNotebookPathFromFolder(sn.notebookFolder); });
            if (currentSearchableNotebookIndex == -1)
                App.SearchableNotebooks.Add(await SearchableNotebook.FromConfig(config, file));
            else
                App.SearchableNotebooks[currentSearchableNotebookIndex] = await SearchableNotebook.FromConfig(config, file);

            pending.Unlock();

            prSilentSave.IsActive = false;
            prSilentSave.Visibility = Visibility.Collapsed;
        }

        private void UndoLastAction(object sender, RoutedEventArgs e)
        {
            pageState.DeselectStrokes();
            undoRedoSystem.Undo();
            foreach (NotebookPage page in spPageView.Children)
                page.RemoveManipulationRect();
        }

        private void RedoLastAction(object sender, RoutedEventArgs e)
        {
            undoRedoSystem.Redo();
            foreach (NotebookPage page in spPageView.Children)
                page.RemoveManipulationRect();
        }

        private async void PageBack(object sender, RoutedEventArgs e)
        {
            if (finishedLoading)
            {
                await SaveFileSafe();
            }
            await HaltPeriodicSave();
            if (textToScrollTo is not null)
            {
                ITextRange docRange = textToScrollTo.TextBox.Document.GetRange(0, TextConstants.MaxUnitCount);
                Color defaultBg = ((SolidColorBrush)textToScrollTo.TextBox.Background).Color;
                Color defaultFg = ((SolidColorBrush)textToScrollTo.TextBox.Foreground).Color;
                docRange.CharacterFormat.BackgroundColor = defaultBg;
                docRange.CharacterFormat.ForegroundColor = defaultFg;
            }

            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            bool customNavigationNeeded = false;
            if (e.Parameter is SearchNavigation nav)
            {
                searchNav = nav;
                file = nav.notebookFolder;
                customNavigationNeeded = true;
            }
            else
                file = (StorageFolder)e.Parameter;

            if (file is null)
                return;

            pending.notebookFolder = file;

            tbAppTitle.Text += Utils.GetNotebookNameFromFolder(file!);
            ShowFileStatus();

            configFile = await file.CreateFileAsync("config.json", CreationCollisionOption.OpenIfExists);
            if ((new FileInfo(configFile.Path)).Length != 0)
            {
                config = NotebookUpgrader.UpgradeToLastVersion((await NotebookConfig.DeserializeFile(configFile))!);
                undoRedoSystem.notebookConfig = config;

                pbFileStatus.Maximum = config!.pageMapping.Count;

                for (int i = 0; i < config!.pageMapping.Count; ++i)
                {
                    NotebookPage page = await config!.LoadPage(file!, i, svPageZoom, FocusedOnPageItem, UnfocusedOnPageItem, undoRedoSystem, pageState);
                    if (customNavigationNeeded && page.id == searchNav!.pageId)
                    {
                        page.LayoutUpdated += ScrollToLastPage;
                        pageToScrollTo = page;
                    }

                    if (i == config!.pageMapping.Count - 1 && !customNavigationNeeded)
                        pageToScrollTo = page;
                    undoRedoSystem.RegisterPageToSystem(page, spPageView);

                    if (this.IsLoaded)
                        page.SetupForDrawing(attrs, currentInkingTool);

                    else
                        this.Loaded += (s, e) => page.SetupForDrawing(attrs, currentInkingTool);
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
                    new List<int>(),
                    null
                    );
                if (this.IsLoaded)
                    await AddPage();
                else
                    this.Loaded += async (s, e) => await AddPage();
            }


            if (pageToScrollTo is not null && !customNavigationNeeded)
            {
                pageToScrollTo.LayoutUpdated += ScrollToLastPage;
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

        private async void AddPageClicked(object sender, RoutedEventArgs e)
        {
            await AddPage();
        }

        private async Task AddPage()
        {
            await HaltPeriodicSave();

            NotebookPage page = new NotebookPage(
                config!.GetNewPageID(),
                2100,
                2970,
                config!.defaultTemplate.pattern,
                config!.defaultTemplate.pattern is not null,
                undoRedoSystem,
                pageState
                )
            {
                hasBeenModifiedSinceSave = true,
            };
            undoRedoSystem.RegisterPageToSystem(page, spPageView);
            //undoRedoSystem.AddToUndoStack(new UndoAddPages(new List<NotebookPage> { page }, spPageView, undoRedoSystem));

            config!.pageMapping.Add(new PageConfig(page.id, page.Width, page.Height, false));

            pending.RemovePendingDeletions(config!.pageMapping.Last().fileName);
            pending.RemovePendingDeletions(config!.pageMapping.Last().RecognizedTextFilename);

            page.SetupForDrawing(attrs, currentInkingTool);
            spPageView.Children.Add(page);
            BringIntoViewOptions options = new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.1d,
                HorizontalAlignmentRatio = .5d,
            };
            page.StartBringIntoView(options);

            ResumePeriodicSave();
        }

        private async Task AddPage(NotebookPage page)
        {
            await HaltPeriodicSave();

            undoRedoSystem.RegisterPageToSystem(page, spPageView);
            //undoRedoSystem.AddToUndoStack(new UndoAddPages(new List<NotebookPage> { page }, spPageView, undoRedoSystem));
            page.hasBeenModifiedSinceSave = true;
            config!.pageMapping.Add(new PageConfig(page.id, page.Width, page.Height, page.hasBg));
            pending.RemovePendingDeletions(config!.pageMapping.Last().fileName);
            pending.RemovePendingDeletions(config!.pageMapping.Last().RecognizedTextFilename);
            if (page.hasBg)
                pending.RemovePendingDeletions(config!.pageMapping.Last().BgName);
            foreach (IOnPageItem onPageItem in page.onPageItems)
            {
                pending.AddPendingCreations(onPageItem.FileName);
                pending.RemovePendingDeletions(onPageItem.FileName);
                onPageItem.HasBeenModified = true;
            }
            page.SetupForDrawing(attrs, currentInkingTool);
            spPageView.Children.Add(page);
            BringIntoViewOptions options = new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.1d,
                HorizontalAlignmentRatio = .5d,
            };
            page.StartBringIntoView(options);

            ResumePeriodicSave();
        }

        private async Task AddPage(StorageFile bg)
        {
            await HaltPeriodicSave();

            // Make a safe copy of the background; in case user deletes the original file, pendingMoves and pendingRenames would not work; this fixes that
            StorageFile safeBgFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("bg", CreationCollisionOption.GenerateUniqueName);
            await bg.CopyAndReplaceAsync(safeBgFile);

            NotebookPage page;
            BitmapImage bmp = await Utils.GetBMPFromFileWithWidth(safeBgFile, 2100);
            page = new NotebookPage(config!.GetNewPageID(), bmp, undoRedoSystem, pageState)
            {
                hasBeenModifiedSinceSave = true,
            };
            undoRedoSystem.RegisterPageToSystem(page, spPageView);
            //undoRedoSystem.AddToUndoStack(new UndoAddPages(new List<NotebookPage> { page }, spPageView, undoRedoSystem));

            page.SetupForDrawing(attrs, currentInkingTool);
            spPageView.Children.Add(page);

            config.pageMapping.Add(new PageConfig(page.id, page.Width, page.Height, true));

            pending.AddPendingMoves(safeBgFile);
            pending.AddPendingRenames(new RenameItem(safeBgFile, config.pageMapping.Last().BgName));
            // Remove background from pending deletions so it doesn't get deleted when it should be present
            pending.RemovePendingDeletions(config.pageMapping.Last().BgName);
            pending.RemovePendingDeletions(config.pageMapping.Last().fileName);
            pending.RemovePendingDeletions(config!.pageMapping.Last().RecognizedTextFilename);


            BringIntoViewOptions options = new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.1d,
                HorizontalAlignmentRatio = .5d,
            };
            page.StartBringIntoView(options);

            ResumePeriodicSave();
        }

        private async Task AddPage(PdfDocument bg)
        {
            await HaltPeriodicSave();

            ContentDialog popup = Utils.ShowLoadingPopup("Importing PDF");
            Microsoft.UI.Xaml.Controls.ProgressBar progress = (Microsoft.UI.Xaml.Controls.ProgressBar)popup.Content;
            progress.IsIndeterminate = false;
            progress.Minimum = 0.0d;
            progress.Maximum = (double)bg.PageCount;
            progress.Value = 0.0d;

            List<NotebookPage> addedPages = new List<NotebookPage>();

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
                    page = new NotebookPage(pageId, bmpImage, undoRedoSystem, pageState)
                    {
                        hasBeenModifiedSinceSave = true,
                    };
                    undoRedoSystem.RegisterPageToSystem(page, spPageView);
                    addedPages.Add(page);

                    config.pageMapping.Add(new PageConfig(page.id, page.Width, page.Height, true));

                    if (!File.Exists(ApplicationData.Current.TemporaryFolder.Path + "\\" + config.pageMapping.Last().BgName))
                        bgFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(config.pageMapping.Last().BgName);
                    else
                        bgFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(config.pageMapping.Last().BgName, CreationCollisionOption.ReplaceExisting);
                    pending.AddPendingMoves(bgFile);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                    SoftwareBitmap bmp = await decoder.GetSoftwareBitmapAsync();
                    using (IRandomAccessStream fileStream = await bgFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fileStream);
                        encoder.SetSoftwareBitmap(bmp);
                        await encoder.FlushAsync();
                    }
                }

                page.SetupForDrawing(attrs, currentInkingTool);
                spPageView.Children.Add(page);

                // Remove background from pending deletions so it doesn't get deleted when it should be present
                pending.RemovePendingDeletions(config.pageMapping.Last().BgName);
                pending.RemovePendingDeletions(config.pageMapping.Last().fileName);
                pending.RemovePendingDeletions(config!.pageMapping.Last().RecognizedTextFilename);

                progress.Value += 1.0d;
            }

            //undoRedoSystem.AddToUndoStack(new UndoAddPages(addedPages, spPageView, undoRedoSystem));

            BringIntoViewOptions options = new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.1d,
                HorizontalAlignmentRatio = .5d,
            };
            spPageView.Children.Last().StartBringIntoView(options);
            popup.Hide();

            ResumePeriodicSave();
        }

        private async Task ImportBismuth(StorageFile bismuthFile)
        {
            await HaltPeriodicSave();

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

            List<NotebookPage> addedPages = new List<NotebookPage>();

            foreach (PageConfig currentPage in importConfig.pageMapping)
            {
                NotebookPage page;
                int pageId = config!.GetNewPageID();
                if (currentPage.hasBg)
                {
                    BitmapImage img = new BitmapImage();
                    ZipArchiveEntry? bgEntry = archive.GetEntry(currentPage.BgName);
                    if (bgEntry is not null)
                    {
                        using (Stream bgStream = bgEntry.Open())
                        {
                            InMemoryRandomAccessStream memStream = new InMemoryRandomAccessStream();
                            bgStream.CopyTo(memStream.AsStreamForWrite()); // ZipArchiveEntry streams aren't seekable, so copy to seekable stream
                            memStream.Seek(0);
                            await img.SetSourceAsync(memStream);
                        }
                        string tempBgPath = ApplicationData.Current.TemporaryFolder.Path + "\\" + "tempBg.png";
                        if (File.Exists(tempBgPath))
                            File.Delete(tempBgPath);
                        bgEntry.ExtractToFile(tempBgPath, true);
                        StorageFile tempBgFile = await ApplicationData.Current.TemporaryFolder.GetFileAsync("tempBg.png");
                        pending.AddPendingMoves(tempBgFile);
                        pending.AddPendingRenames(
                            new RenameItem(
                                tempBgFile,
                                "bg" + (pageId == 0 ? "" : (" (" + pageId + ")")) + ".png"
                                )
                            );
                        pending.RemovePendingDeletions("bg" + (pageId == 0 ? "" : (" (" + pageId + ")")) + ".png");
                    } else
                    {
                        popup.Hide();
                        await Utils.ShowTeachingTip(ttInfoPopup, "Import failed ❌", "Corrupt file provided", 3000);
                        return;
                    }

                    page = new NotebookPage(
                        pageId,
                        img,
                        undoRedoSystem,
                        pageState
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
                        true,
                        undoRedoSystem,
                        pageState
                        );
                }
                else
                {
                    page = new NotebookPage(
                        pageId,
                        currentPage.width,
                        currentPage.height,
                        undoRedoSystem,
                        pageState
                        );
                }
                undoRedoSystem.RegisterPageToSystem(page, spPageView);
                addedPages.Add(page);

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
                    onPageText.hasBeenModifiedSinceSave = true;
                    ZipArchiveEntry? textEntry = archive.GetEntry(text.FileName);
                    if (textEntry is not null)
                    {
                        MemoryStream memStream = new MemoryStream();
                        textEntry.Open().CopyTo(memStream); // ZipArchiveEntry streams aren't seekable, so copy to seekable stream
                        memStream.Seek(0, SeekOrigin.Begin);
                        using (IRandomAccessStream textStream = memStream.AsRandomAccessStream())
                        {
                            onPageText.LoadFromStream(textStream);
                        }
                    }

                    page.AddOnPageItemToPage(onPageText);
                    onPageText.TextBoxGotFocus += FocusedOnPageItem;
                    onPageText.TextBoxLostFocus += UnfocusedOnPageItem;
                }

                foreach (ImageData image in currentPage.images)
                {
                    ZipArchiveEntry? imageEntry = archive.GetEntry(image.FileName);
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
                    page.AddOnPageItemToPage(onPageImage);
                    onPageImage.ImageGotFocus += FocusedOnPageItem;
                    onPageImage.ImageLostFocus += UnfocusedOnPageItem;
                }

                await AddPage(page);
            }
            //undoRedoSystem.AddToUndoStack(new UndoAddPages(addedPages, spPageView, undoRedoSystem));

            popup.Hide();

            ResumePeriodicSave();
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
            //foreach (NotebookPage page in spPageView.Children)
            //{
            //    NotebookPage pageThumb = new NotebookPage(page.id, undoRedoSystem, new PageState(null));
            //    pageThumb.inkPres.StrokeContainer = page.inkPres.StrokeContainer;
            //    pageThumb.Width = 200;
            //    pageThumb.Height = 200;
            //    gvThumbnails.Items.Clear();
            //    gvThumbnails.Items.Add(pageThumb);
            //}
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
                    NotebookPage deletedPage = (NotebookPage)spPageView.Children[i];

                    foreach (IOnPageItem onPageItem in deletedPage.onPageItems)
                        pending.AddPendingDeletions(onPageItem.FileName);

                    spPageView.Children.RemoveAt(i);
                    break;
                }
                ++i;
            }

            config!.DeletePageWithId(args.id); // Takes care of deleting text and image ids as well

            pending.AddPendingDeletions("page" + (args.id == 0 ? "" : (" (" + args.id + ")")) + ".gif");
            pending.AddPendingDeletions("bg" + (args.id == 0 ? "" : (" (" + args.id + ")")) + ".png");
            pending.AddPendingDeletions("recText" + (args.id == 0 ? "" : (" (" + args.id + ")")) + ".json");
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
                else // Can only be image
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
                (currentPage.Width - 500d) * .5d,
                currentPage,
                svPageZoom
                );
            pending.AddPendingCreations("text" + (txt.id == 0 ? "" : (" (" + txt.id + ")")) + ".rtf");
            pending.RemovePendingDeletions("text" + (txt.id == 0 ? "" : (" (" + txt.id + ")")) + ".rtf");
            currentPage!.AddOnPageItemToPage(txt);
            undoRedoSystem.AddToUndoStack(new UndoAddOnPageElement(currentPage!, txt, undoRedoSystem));
            txt.TextBoxGotFocus += FocusedOnPageItem;
            txt.TextBoxLostFocus += UnfocusedOnPageItem;

            ChangeCurrentInkingTool(btObjectTool, new RoutedEventArgs());
            txt.SetIsSelectable(true);
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
                        VerticalAlignmentRatio = .1d,
                        HorizontalAlignmentRatio = .5d,
                    });
                    break;
                }
            }
        }

        private void ToolPopupLoaded(object sender, RoutedEventArgs e)
        {
            Popup pp = (Popup)sender;
            pp.HorizontalOffset = -((FrameworkElement)pp.Child).ActualWidth * .5d;
            if (App.AppSettings.inkToolbarPlacement == InkToolbarPlacement.Top)
            {
                pp.VerticalAlignment = VerticalAlignment.Bottom;
                pp.VerticalOffset = -((FrameworkElement)pp.Child).ActualHeight;
            }
            else
            {
                pp.VerticalAlignment = VerticalAlignment.Top;
                pp.VerticalOffset = 0d;
            }
        }

        private void ToolPopupFocused(object sender, RoutedEventArgs e)
        {
            Popup parent = (Popup)((Border)sender).Parent;

            parent.IsHitTestVisible = true;
            parent.Opacity = 1d;
        }

        private void ToolPopupUnfocused(object sender, RoutedEventArgs e)
        {
            Popup parent = (Popup)((Border)sender).Parent;

            parent.IsHitTestVisible = false;
            parent.Opacity = 0d;
        }

        private void FocusedOnPageItem(object? sender, EventArgs e)
        {
            if (sender is OnPageText opt)
            {
                lastEditedText = opt;
                ppTextTools.Opacity = 1d;
                ppTextTools.IsHitTestVisible = true;
            } else if (sender is OnPageImage img)
            {
                lastEditedImage = img;
                ppImageTools.Opacity = 1d;
                ppImageTools.IsHitTestVisible = true;
            }
        }

        private void UnfocusedOnPageItem(object? sender, EventArgs e)
        {
            if (sender is OnPageText opt)
            {
                ppTextTools.IsHitTestVisible = false;
                ppTextTools.Opacity = 0d;
            } else if (sender is OnPageImage img)
            {
                ppImageTools.Opacity = 0d;
                ppImageTools.IsHitTestVisible = false;
            }
        }

        private void OnPageTextHasChanged()
        {
            lastEditedText!.hasBeenModifiedSinceSave = true;
            lastEditedText!.TextBox.Focus(FocusState.Programmatic);
        }

        private void HasOpenedTextBoxListOptions(object sender, RoutedEventArgs e)
        {
            ToolPopupFocused(bdTextTools, e);
        }

        private void ToggleBoldText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextBox.Document.Selection.CharacterFormat.Bold = Windows.UI.Text.FormatEffect.Toggle;
            OnPageTextHasChanged();
        }

        private void ToggleItalicText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextBox.Document.Selection.CharacterFormat.Italic = Windows.UI.Text.FormatEffect.Toggle;
            OnPageTextHasChanged();
        }

        private void ToggleUnderlinedText(object sender, RoutedEventArgs e)
        {
            if (lastEditedText!.TextBox.Document.Selection.CharacterFormat.Underline != UnderlineType.Single)
                lastEditedText!.TextBox.Document.Selection.CharacterFormat.Underline = Windows.UI.Text.UnderlineType.Single;
            else
                lastEditedText!.TextBox.Document.Selection.CharacterFormat.Underline = UnderlineType.None;
            OnPageTextHasChanged();
        }

        private void ToggleStrikethroughText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Toggle;
            OnPageTextHasChanged();
        }

        private void ToggleSuperscriptText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.Toggle;
            OnPageTextHasChanged();
        }

        private void ToggleSubscriptText(object sender, RoutedEventArgs e)
        {
            lastEditedText!.TextBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.Toggle;
            OnPageTextHasChanged();
        }

        private void DecreaseFontSize(object sender, RoutedEventArgs e)
        {
            if (lastEditedText!.TextBox.Document.Selection.CharacterFormat.Size > 2f)
            {
                lastEditedText!.TextBox.Document.Selection.CharacterFormat.Size -= 2f;
                OnPageTextHasChanged();
            }
            lastEditedText!.TextBox.Focus(FocusState.Programmatic); // Focus TextBox anyway
        }

        private void IncreaseFontSize(object sender, RoutedEventArgs e)
        {
            if (lastEditedText!.TextBox.Document.Selection.CharacterFormat.Size > 2f)       // Cannot happen with addition, but crashes the app if
            {                                                                               // text containing multiple font sizes is selected
                lastEditedText!.TextBox.Document.Selection.CharacterFormat.Size += 2f;
                OnPageTextHasChanged();
            }
            lastEditedText!.TextBox.Focus(FocusState.Programmatic); // Focus TextBox anyway
        }

        private void ToggleBulletedList(object sender, RoutedEventArgs e)
        {
            ITextParagraphFormat format = lastEditedText!.TextBox.Document.Selection.ParagraphFormat;
            if (format.ListType == MarkerType.Bullet)
                format.ListType = MarkerType.None;
            else
                format.ListType = MarkerType.Bullet;
            OnPageTextHasChanged();
        }

        private void ToggleNumberedList(object sender, RoutedEventArgs e)
        {
            ITextParagraphFormat format = lastEditedText!.TextBox.Document.Selection.ParagraphFormat;
            if (format.ListType == MarkerType.Arabic)
                format.ListType = MarkerType.None;
            else
            {
                format.ListType = MarkerType.Arabic;
                SetListStyle(format);
                format.ListStart = 1;
            }
            OnPageTextHasChanged();
        }

        private void DecreaseIndent(object sender, RoutedEventArgs e)
        {
            ITextParagraphFormat format = lastEditedText!.TextBox.Document.Selection.ParagraphFormat;
            if ((format.ListType == MarkerType.Arabic || format.ListType == MarkerType.Bullet) && format.LeftIndent > 0f)
            {
                format.SetIndents(format.FirstLineIndent, format.LeftIndent - 40.0f, format.RightIndent);
                if (format.ListType == MarkerType.Arabic)
                    SetListStyle(format);
            }
            OnPageTextHasChanged();
        }

        private void IncreaseIndent(object sender, RoutedEventArgs e)
        {
            ITextParagraphFormat format = lastEditedText!.TextBox.Document.Selection.ParagraphFormat;
            if ((format.ListType == MarkerType.Arabic || format.ListType == MarkerType.Bullet) && format.LeftIndent < 160.0f)
            {
                format.SetIndents(format.FirstLineIndent, format.LeftIndent + 40.0f, format.RightIndent);
                if (format.ListType == MarkerType.Arabic)
                    SetListStyle(format);
            }
            OnPageTextHasChanged();
        }

        private void SetListStyle(ITextParagraphFormat format)
        {
            switch (format.LeftIndent)
            {
                case 0.0f:
                    format.ListStyle = MarkerStyle.Parentheses;
                    break;
                case 40.0f:
                    format.ListStyle = MarkerStyle.Period;
                    break;
                case 80.0f:
                    format.ListStyle = MarkerStyle.Parenthesis;
                    break;
                case 120.0f:
                    format.ListStyle = MarkerStyle.Minus;
                    break;
                case 160.0f:
                    format.ListStyle = MarkerStyle.Plain;
                    break;
                default:
                    format.ListStyle = MarkerStyle.Parentheses;
                    break;
            }
        }

        private void DeleteCurrentTextBox(object sender, RoutedEventArgs e)
        {
            NotebookPage textBoxPage = lastEditedText!.RemoveTextFromPage();
            undoRedoSystem.AddToUndoStack(new UndoRemoveOnPageElement(textBoxPage, lastEditedText!, undoRedoSystem));
            config!.DeleteTextWithId(lastEditedText!.id);
            string textBoxFileName = lastEditedText!.FileName;
            pending.RemovePendingCreations(textBoxFileName);
            pending.AddPendingDeletions(textBoxFileName);
            lastEditedText = null;
            ppTextTools.IsHitTestVisible = false;
            ppTextTools.Opacity = 0d;
        }

        private void DeleteCurrentImage(object sender, RoutedEventArgs e)
        {
            NotebookPage imgPage = lastEditedImage!.RemoveImageFromPage();
            undoRedoSystem.AddToUndoStack(new UndoRemoveOnPageElement(imgPage, lastEditedImage, undoRedoSystem));
            config!.DeleteImageWithId(lastEditedImage!.id);
            string imgFileName = lastEditedImage!.FileName;
            pending.RemovePendingCreations(imgFileName);
            pending.AddPendingDeletions(imgFileName);
            lastEditedImage = null;
            ppImageTools.IsHitTestVisible = false;
            ppImageTools.Opacity = 0d;
        }

        private void SearchTextInCurrentBox()
        {
            RemoveSearchedHighlights();
            RichEditBox current = lastEditedText!.TextBox;

            Color highlightBg = (Color)App.Current.Resources["SystemColorHighlightColor"];
            Color highlightFg = (Color)App.Current.Resources["SystemColorHighlightTextColor"];

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
            Color defaultBg = ((SolidColorBrush)current.Background).Color;
            Color defaultFg = ((SolidColorBrush)current.Foreground).Color;

            docRange.CharacterFormat.BackgroundColor = defaultBg;
            docRange.CharacterFormat.ForegroundColor = defaultFg;
        }

        private void ChangeInkColor(ColorPickerButton button, ChangeColorData changeData)
        {
            switch (currentInkingTool)
            {
                case CurrentInkingTool.Drawing:
                    if (changeData.shouldSave)
                        App.AppSettings.drawingColors[changeData.buttonIndex] = changeData.color;
                    btInkTool.Foreground = new SolidColorBrush(changeData.color);
                    currentColors.drawing = button.btIndex;
                    break;
                case CurrentInkingTool.Highlighter:
                    if (changeData.shouldSave)
                        App.AppSettings.highlightColors[changeData.buttonIndex] = changeData.color;
                    btHighlightTool.Foreground = new SolidColorBrush(changeData.color);
                    currentColors.highlight = button.btIndex;
                    break;
                case CurrentInkingTool.Pencil:
                    if (changeData.shouldSave)
                        App.AppSettings.pencilColors[changeData.buttonIndex] = changeData.color;
                    btPencilTool.Foreground = new SolidColorBrush(changeData.color);
                    currentColors.pencil = button.btIndex;
                    break;
                default:
                    if (changeData.shouldSave)
                        App.AppSettings.calligraphyColors[changeData.buttonIndex] = changeData.color;
                    btCalligraphyTool.Foreground = new SolidColorBrush(changeData.color);
                    currentColors.calligraphy = button.btIndex;
                    break;
            }
            foreach (NotebookPage page in spPageView.Children)
            {
                attrs.Color = changeData.color;
                page.inkPres.UpdateDefaultDrawingAttributes(attrs);
            }
        }

        private async void LoadColorBar(object sender, RoutedEventArgs e)
        {
            await App.AppSettings.LoadColorsIntoStackPanel((SimpleColorPicker)sender, ChangeInkColor, scColorBar, ColorPalette.Drawing, currentColors);
            ((ColorPickerButton)scColorBar.Children[currentColors.drawing]).isSelected = true;
        }

        private async void AddNewColor(object sender, RoutedEventArgs e)
        {
            switch (currentInkingTool)
            {
                case CurrentInkingTool.Drawing:
                    App.AppSettings.drawingColors.Add(cpColor.Color);
                    await App.AppSettings.LoadColorsIntoStackPanel(scColorBar, ChangeInkColor, scColorBar, ColorPalette.Drawing, currentColors);
                    scColorBar.UpdateButtonIndices();
                    ((ColorPickerButton)scColorBar.Children[currentColors.drawing]).isSelected = true;
                    break;
                case CurrentInkingTool.Highlighter:
                    App.AppSettings.highlightColors.Add(cpColor.Color);
                    await App.AppSettings.LoadColorsIntoStackPanel(scColorBar, ChangeInkColor, scColorBar, ColorPalette.Highlight, currentColors);
                    scColorBar.UpdateButtonIndices();
                    ((ColorPickerButton)scColorBar.Children[currentColors.highlight]).isSelected = true;
                    break;
                case CurrentInkingTool.Pencil:
                    App.AppSettings.pencilColors.Add(cpColor.Color);
                    await App.AppSettings.LoadColorsIntoStackPanel(scColorBar, ChangeInkColor, scColorBar, ColorPalette.Pencil, currentColors);
                    scColorBar.UpdateButtonIndices();
                    ((ColorPickerButton)scColorBar.Children[currentColors.pencil]).isSelected = true;
                    break;
                default:
                    App.AppSettings.calligraphyColors.Add(cpColor.Color);
                    await App.AppSettings.LoadColorsIntoStackPanel(scColorBar, ChangeInkColor, scColorBar, ColorPalette.Calligraphy, currentColors);
                    scColorBar.UpdateButtonIndices();
                    ((ColorPickerButton)scColorBar.Children[currentColors.calligraphy]).isSelected = true;
                    break;
            }
            flNewColor.Hide();
        }

        private void SetNewBrushWidth(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!slTipSize.IsLoaded)
                return;

            switch (currentInkingTool)
            {
                case CurrentInkingTool.Drawing:
                    App.AppSettings.tipSize = e.NewValue;
                    attrs.Size = new Windows.Foundation.Size(e.NewValue, e.NewValue);
                    break;
                case CurrentInkingTool.Highlighter:
                    App.AppSettings.highlightTipSize = e.NewValue;
                    attrs.Size = new Windows.Foundation.Size(e.NewValue / 4d, e.NewValue);
                    break;
                case CurrentInkingTool.Pencil:
                    App.AppSettings.pencilTipSize = e.NewValue;
                    attrs.Size = new Windows.Foundation.Size(e.NewValue, e.NewValue);
                    break;
                default:
                    App.AppSettings.calligraphyTipSize = e.NewValue;
                    attrs.Size = new Windows.Foundation.Size(e.NewValue / 4d, e.NewValue);
                    break;
            }

            foreach (NotebookPage page in spPageView.Children)
            {
                page.inkPres.UpdateDefaultDrawingAttributes(attrs);
            }
        }

        private void LoadTipSize(object sender, RoutedEventArgs e)
        {
            Slider slider = (Slider)sender;
            slider.Value = App.AppSettings.tipSize;
        }

        private async void PasteObject(object sender, RoutedEventArgs e)
        {
            if (lastEditedText is not null)
                return;

            DataPackageView clip = Clipboard.GetContent();

            double pageOffset = GetCurrentPage();

            if (currentPage!.inkPres.StrokeContainer.CanPasteFromClipboard())
            {
                try
                {
                    Windows.Foundation.Point inkPos = new Windows.Foundation.Point(0, pageOffset);
                    Windows.Foundation.Rect strokesRect = currentPage!.inkPres.StrokeContainer.PasteFromClipboard(inkPos);
                    List<Windows.Foundation.Point> rectPoints = new List<Windows.Foundation.Point>();
                    rectPoints.Add(new Windows.Foundation.Point(strokesRect.Left, strokesRect.Top));
                    rectPoints.Add(new Windows.Foundation.Point(strokesRect.Left, strokesRect.Bottom));
                    rectPoints.Add(new Windows.Foundation.Point(strokesRect.Right, strokesRect.Bottom));
                    rectPoints.Add(new Windows.Foundation.Point(strokesRect.Right, strokesRect.Top));
                    currentPage!.RemoveManipulationRect();
                    currentPage!.SelectInkWithPolyline(rectPoints);
                    undoRedoSystem.AddToUndoStack(new UndoAddStroke(pageState.selectedStrokes!, currentPage!.inkPres, undoRedoSystem));
                    ChangeCurrentInkingTool(btLassoTool, new RoutedEventArgs());
                }
                catch
                {
                    await Utils.ShowTeachingTip(ttInfoPopup, "Could not paste ink❌", "", 3000);
                }
            }
            else if (clip.Contains(StandardDataFormats.Bitmap))
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

                OnPageImage opI = new OnPageImage(
                    config!.GetNewImageID(),
                    Math.Min(pageOffset, currentPage!.Height - 500d),
                    (currentPage!.Width - wbmp.PixelWidth * 500d / wbmp.PixelHeight) * .5d,
                    wbmp,
                    currentPage!,
                    svPageZoom,
                    true
                    );
                currentPage!.AddOnPageItemToPage(opI);
                opI.ImageGotFocus += FocusedOnPageItem;
                opI.ImageLostFocus += UnfocusedOnPageItem;
                pending.AddPendingCreations(opI.FileName);
                pending.RemovePendingDeletions(opI.FileName);
                undoRedoSystem.AddToUndoStack(new UndoAddOnPageElement(currentPage!, opI, undoRedoSystem));

                ChangeCurrentInkingTool(btObjectTool, new RoutedEventArgs());
                opI.SetIsSelectable(true);
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

                await exportConfig.AddPageWhileSaving(currentPage, tempFolder, file!, true);
            }
            StorageFile configFile = await tempFolder.CreateFileAsync("config.json");
            await exportConfig.SerializeToFile(configFile);

            using(Stream stream = await bismuthFile.OpenStreamForWriteAsync())
                ZipFile.CreateFromDirectory(tempFolder.Path, stream);

            exportingDialog.Hide();
        }

        private void DeleteCurrentlySelectedStrokes(object sender, RoutedEventArgs e)
        {
            undoRedoSystem.AddToUndoStack(new UndoDeleteStroke(pageState.selectedStrokes!, pageState.currentlyActivePage!.inkPres, undoRedoSystem));
            pageState.currentlyActivePage!.inkPres.StrokeContainer.DeleteSelected();
            pageState.currentlyActivePage!.RemoveManipulationRect();
            pageState.DeselectStrokes();
        }

        private void SetColorwheelButtonHeight(object sender, RoutedEventArgs e)
        {
            btChangeSelectedStrokesColor.Height = btDeleteSelectedStrokes.ActualHeight;
            btChangeSelectedStrokesColor.Width = btChangeSelectedStrokesColor.Height;
        }

        private async void CopyStrokesToClipboard(object sender, RoutedEventArgs e)
        {
            pageState.currentlyActivePage!.inkPres.StrokeContainer.CopySelectedToClipboard();
            await Utils.ShowTeachingTip(ttInfoPopup, "Copied ink to clipboard ✅", "", 3000);
        }

        private void ChangeSelectedInkColor(Microsoft.UI.Xaml.Controls.ColorPicker sender, Microsoft.UI.Xaml.Controls.ColorChangedEventArgs args)
        {
            foreach (InkStroke stroke in pageState.selectedStrokes!)
            {
                InkDrawingAttributes strokeAttrs = stroke.DrawingAttributes;
                strokeAttrs.Color = args.NewColor;
                stroke.DrawingAttributes = strokeAttrs;
            }
        }

        private void StartChangeSelectedInkColor(object sender, RoutedEventArgs e)
        {
            ppSelectionTools.Opacity = 1d;
            recoloredStrokes = new List<RecoloredStroke>();
            foreach (InkStroke s in pageState.selectedStrokes!)
                recoloredStrokes.Add(new RecoloredStroke(s, s.DrawingAttributes.Color));
        }

        private void StopChangeSelectedInkColor(object sender, RoutedEventArgs e)
        {
            undoRedoSystem.AddToUndoStack(new UndoRecolorStrokes(recoloredStrokes!, cpInkColor.Color, undoRedoSystem));
            recoloredStrokes = null;
        }

        private void InkToolbarPopupLoaded(object sender, RoutedEventArgs e)
        {
            Popup pp = (Popup)sender;
            pp.HorizontalOffset = -((FrameworkElement)pp.Child).ActualWidth * .5d;
            inkToolbarNormalHorzontalOffset = pp.HorizontalOffset;
            if (App.AppSettings.inkToolbarPlacement == InkToolbarPlacement.Top)
                pp.VerticalAlignment = VerticalAlignment.Top;
            else if (App.AppSettings.inkToolbarPlacement == InkToolbarPlacement.Bottom)
            {
                pp.VerticalAlignment = VerticalAlignment.Bottom;
                pp.VerticalOffset = -((FrameworkElement)pp.Child).ActualHeight;
            }
        }

        private async void ChangeCurrentInkingTool(object sender, RoutedEventArgs e)
        {
            CustomInkToolbarTool btSelectedTool = (CustomInkToolbarTool)sender;

            SolidColorBrush accentColorBrush = (SolidColorBrush)Application.Current.Resources["SystemControlHighlightAccentBrush"];
            foreach (UIElement el in spCustomInkToolbar.Children)
            {
                if (el is CustomInkToolbarTool btn)
                {
                    btn.isSelected = false;
                }
            }
            btSelectedTool.isSelected = true;

            if (btSelectedTool.Name != btLassoTool.Name)
            {
                pageState.DeselectStrokes();
                foreach (NotebookPage page in spPageView.Children)
                {
                    page.inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
                    page.RemoveManipulationRect();
                }
            }

            attrs = new InkDrawingAttributes
            {
                PenTip = PenTipShape.Circle,
                DrawAsHighlighter = false,
                Color = Color.FromArgb(255, 0, 0, 0),
                Size = new Windows.Foundation.Size(4, 4),
            };
            InkInputProcessingMode inkMode = InkInputProcessingMode.Inking;
            SelectionMode selectionMode = SelectionMode.Lasso;
            double newTipSizeSliderValue = 0;
            Task? colorsLoading = null;

            if (btSelectedTool.Name == btInkTool.Name)
            {
                currentInkingTool = CurrentInkingTool.Drawing;
                newTipSizeSliderValue = App.AppSettings.tipSize;
                colorsLoading = App.AppSettings.LoadColorsIntoStackPanel(scColorBar, ChangeInkColor, scColorBar, ColorPalette.Drawing, currentColors);
                SetToolOptionsVisibilityWithAnimation(true);
                attrs = new InkDrawingAttributes
                {
                    PenTip = PenTipShape.Circle,
                    DrawAsHighlighter = false,
                    Color = App.AppSettings.drawingColors[currentColors.drawing],
                    Size = new Windows.Foundation.Size(App.AppSettings.tipSize, App.AppSettings.tipSize),
                };
            }
            else if (btSelectedTool.Name == btHighlightTool.Name)
            {
                currentInkingTool = CurrentInkingTool.Highlighter;
                newTipSizeSliderValue = App.AppSettings.highlightTipSize;
                colorsLoading = App.AppSettings.LoadColorsIntoStackPanel(scColorBar, ChangeInkColor, scColorBar, ColorPalette.Highlight, currentColors);
                SetToolOptionsVisibilityWithAnimation(true);
                attrs = new InkDrawingAttributes
                {
                    PenTip = PenTipShape.Rectangle,
                    DrawAsHighlighter = true,
                    Color = App.AppSettings.highlightColors[currentColors.highlight],
                    Size = new Windows.Foundation.Size(App.AppSettings.highlightTipSize / 4d, App.AppSettings.highlightTipSize),
                };
            }
            else if (btSelectedTool.Name == btPencilTool.Name)
            {
                currentInkingTool = CurrentInkingTool.Pencil;
                newTipSizeSliderValue = App.AppSettings.pencilTipSize;
                colorsLoading = App.AppSettings.LoadColorsIntoStackPanel(scColorBar, ChangeInkColor, scColorBar, ColorPalette.Pencil, currentColors);
                SetToolOptionsVisibilityWithAnimation(true);
                attrs = InkDrawingAttributes.CreateForPencil();
                attrs.IgnorePressure = false;
                attrs.Color = App.AppSettings.pencilColors[currentColors.pencil];
                attrs.Size = new Windows.Foundation.Size(App.AppSettings.pencilTipSize, App.AppSettings.pencilTipSize);
            }
            else if (btSelectedTool.Name == btCalligraphyTool.Name)
            {
                currentInkingTool = CurrentInkingTool.Calligraphy;
                newTipSizeSliderValue = App.AppSettings.calligraphyTipSize;
                colorsLoading = App.AppSettings.LoadColorsIntoStackPanel(scColorBar, ChangeInkColor, scColorBar, ColorPalette.Calligraphy, currentColors);
                SetToolOptionsVisibilityWithAnimation(true);
                attrs = new InkDrawingAttributes
                {
                    DrawAsHighlighter = false,
                    IgnorePressure = false,
                    Color = App.AppSettings.calligraphyColors[currentColors.calligraphy],
                    Size = new Windows.Foundation.Size(App.AppSettings.calligraphyTipSize / 4d, App.AppSettings.calligraphyTipSize),
                    PenTipTransform = new Matrix3x2
                    {
                        M11 = MathF.Cos(35f),
                        M12 = MathF.Sin(35f),
                        M21 = -MathF.Sin(35f),
                        M22 = MathF.Cos(35f),
                        M31 = 0,
                        M32 = 0,
                    },
                };
            }
            else if (btSelectedTool.Name == btEraserTool.Name)
            {
                currentInkingTool = CurrentInkingTool.Eraser;
                inkMode = InkInputProcessingMode.Erasing;
                SetToolOptionsVisibilityWithAnimation(false);
            }
            else if (btSelectedTool.Name == btLassoTool.Name)
            {
                currentInkingTool = CurrentInkingTool.Lasso;
                attrs = new InkDrawingAttributes();
                inkMode = InkInputProcessingMode.None;
                SetToolOptionsVisibilityWithAnimation(false);
            } else
            {
                currentInkingTool = CurrentInkingTool.Object;
                attrs = new InkDrawingAttributes();
                inkMode = InkInputProcessingMode.None;
                selectionMode = SelectionMode.Object;
                SetToolOptionsVisibilityWithAnimation(false);
            }

            foreach (NotebookPage page in spPageView.Children)
            {
                page.inkPres.UpdateDefaultDrawingAttributes(attrs);
                page.inkPres.InputProcessingConfiguration.Mode = inkMode;
                page.selectionMode = selectionMode;
            }
            btSelectedTool.Foreground = new SolidColorBrush(attrs.Color);

            DoubleAnimation sliderValueAnim = new DoubleAnimation
            {
                From = slTipSize.Value,
                To = newTipSizeSliderValue,
                EnableDependentAnimation = true,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseInOut },
            };

            Storyboard sb = new Storyboard();
            sb.Children.Add(sliderValueAnim);

            Storyboard.SetTarget(sliderValueAnim, slTipSize);
            Storyboard.SetTargetProperty(sliderValueAnim, "Value");

            slTipSize.ValueChanged -= SetNewBrushWidth;
            sb.Completed += (s, e) => slTipSize.ValueChanged += SetNewBrushWidth;

            sb.Begin();
            if (colorsLoading is not null)
                await colorsLoading;
        }

        private void SetToolOptionsVisibilityWithAnimation(bool shouldBeVisible)
        {
            bool areToolOptionsVisible = spCustomInkToolbar.Children.Last() == spToolOptions;
            if (shouldBeVisible == areToolOptionsVisible)
                return;

            if (shouldBeVisible)
                spCustomInkToolbar.Children.Add(spToolOptions);

            double inkStackpanelCompressedWidth = inkStackpanelNormalWidth - toolOptionsNormalWidth;
            DoubleAnimation widthAnim = new DoubleAnimation
            {
                From = shouldBeVisible ? inkStackpanelCompressedWidth : inkStackpanelNormalWidth,
                To = shouldBeVisible ? inkStackpanelNormalWidth : inkStackpanelCompressedWidth,
                EnableDependentAnimation = true,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
            };

            double inkToolbarCompressedHorizontalOffset = -inkStackpanelCompressedWidth * .5d;
            DoubleAnimation offsetAnim = new DoubleAnimation
            {
                From = shouldBeVisible ? inkToolbarCompressedHorizontalOffset : inkToolbarNormalHorzontalOffset,
                To = shouldBeVisible ? inkToolbarNormalHorzontalOffset : inkToolbarCompressedHorizontalOffset,
                EnableDependentAnimation = true,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseInOut },
            };

            Storyboard sb = new Storyboard();
            sb.Children.Add(widthAnim);
            sb.Children.Add(offsetAnim);

            Storyboard.SetTarget(widthAnim, spCustomInkToolbar);
            Storyboard.SetTargetProperty(widthAnim, "Width");

            Storyboard.SetTarget(offsetAnim, ppInkToolbar);
            Storyboard.SetTargetProperty(offsetAnim, "HorizontalOffset");

            sb.Begin();

            sb.Completed += (s, e) =>
            {
                if (!shouldBeVisible)
                    spCustomInkToolbar.Children.Remove(spToolOptions);
            };
        }

        private void ToggleRuler(object sender, RoutedEventArgs e)
        {
            ToggleButton tb = (ToggleButton)sender;
            bool isChecked = (bool)tb.IsChecked!;
            ((Grid)tb.Content).Translation = isChecked ? new Vector3(0f, -4f, 0f) : new Vector3(0f, 0f, 0f);
            foreach (NotebookPage page in spPageView.Children)
                page.ruler.IsVisible = isChecked;
        }

        private void ToggleProtractor(object sender, RoutedEventArgs e)
        {
            ToggleButton tb = (ToggleButton)sender;
            bool isChecked = (bool)tb.IsChecked!;
            ((Grid)tb.Content).Translation = isChecked ? new Vector3(0f, -4f, 0f) : new Vector3(0f, 0f, 0f);
            foreach (NotebookPage page in spPageView.Children)
                page.protractor.IsVisible = isChecked;
        }

        private void AdjustUndoRedoButtonsPopupPosition(object sender, RoutedEventArgs e)
        {
            Popup pp = (Popup)sender;
            if (
                App.AppSettings.undoRedoButtonsPlacement == UndoRedoButtonsPlacement.TopLeft
                ||
                App.AppSettings.undoRedoButtonsPlacement == UndoRedoButtonsPlacement.TopRight
                )
            {
                pp.VerticalAlignment = VerticalAlignment.Top;
            } else
            {
                pp.VerticalAlignment = VerticalAlignment.Bottom;
                pp.VerticalOffset = -((FrameworkElement)pp.Child).ActualHeight;
            }
            if (
                App.AppSettings.undoRedoButtonsPlacement == UndoRedoButtonsPlacement.TopLeft
                ||
                App.AppSettings.undoRedoButtonsPlacement == UndoRedoButtonsPlacement.BottomLeft
                )
            {
                pp.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else
            {
                pp.HorizontalAlignment = HorizontalAlignment.Right;
                pp.HorizontalOffset = -((FrameworkElement)pp.Child).ActualWidth;
            }
        }

        private void SetToolOptionsNormalWidth(object sender, RoutedEventArgs e)
        {
            toolOptionsNormalWidth = spToolOptions.ActualWidth;
        }

        private void SetInkToolbarStackpanelNormalWidth(object sender, RoutedEventArgs e)
        {
            inkStackpanelNormalWidth = spCustomInkToolbar.ActualWidth;
        }

        private async void OpenFileOptions(object sender, RoutedEventArgs e)
        {
            CreateNewNotebookOptions options = new CreateNewNotebookOptions();
            options.LoadFromConfig(config!, file!);
            //await options.UpdatePreviewTemplateBackground();

            ContentDialog dialog = new ContentDialog
            {
                Title = "Modify notebook settings",
                Content = options,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            };

            ContentDialogResult res = await dialog.ShowAsync();

            if (res == ContentDialogResult.None)
                return;

            config!.inkRecognizerLanguage = options.chosenInkLanguage;
            config!.defaultTemplate = new DefaultTemplate(options.chosenPattern);
        }
    }

    public enum CurrentInkingTool
    {
        Drawing,
        Highlighter,
        Pencil,
        Calligraphy,
        Eraser,
        Lasso,
        Object,
    };
}