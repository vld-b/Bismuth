using ABI.Windows.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Preview.Notes;
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
        private StorageFolder notes => ApplicationData.Current.LocalFolder;

        private List<NotebookPage> pages = new List<NotebookPage>();
        private readonly InkPresenter inkPres;
        private readonly InkRecognizerContainer inkRec;

        private StorageFile? file;

        private Stack<InkStroke> undoStack = new Stack<InkStroke>();
        private Stack<InkStroke> redoStack = new Stack<InkStroke>();

        public CanvasPage()
        {
            InitializeComponent();
            SetTitlebar();
            //inkPres = inkMain.InkPresenter;
            inkRec = new InkRecognizerContainer();
            //SetupInk();
        }

        private void SetupInk(NotebookPage? pageToSetup = null)
        {
            if (pageToSetup != null)
            {
                return;
            }
            //if (!lvPageView.Items.Any())
                //lvPageView.Items.Add(new NotebookPage());
            //foreach (NotebookPage page in lvPageView.Items)
            {
                //#if DEBUG
                //    page.drawingCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Mouse;
                //#else
                //    page.drawingCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen;
                //#endif
                //page.drawingCanvas.InkPresenter.StrokesCollected += RecognizeStroke;
                //page.drawingCanvas.InkPresenter.StrokesCollected += AddStrokeToUndoStack;

            }
        }
        private void SetTitlebar()
        {
            Window.Current.SetTitleBar(TitleBar);
            tbAppTitle.Text = AppInfo.Current.DisplayInfo.DisplayName;
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
            if (!inkPres.StrokeContainer.GetStrokes().Any())
                return;

            IReadOnlyList<InkRecognitionResult> results = await inkRec.RecognizeAsync(inkPres.StrokeContainer, InkRecognitionTarget.All);
            if (results.Count > 0)
            {
                tbTest.Text = string.Empty;
                foreach (InkRecognitionResult result in results)
                {
                    tbTest.Text += result.GetTextCandidates().FirstOrDefault() + " ";
                }
            }
        }

        private async void SaveFileWithDialog(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (file != null && inkPres.StrokeContainer.GetStrokes().Any())
            {
                ContentDialog saveDialog = new ContentDialog()
                {
                    Title = "Saving file...",
                    Content = new SavingFileDialog(),
                };
                IAsyncOperation<ContentDialogResult> res = saveDialog.ShowAsync();
                using (IOutputStream opStream = (await file.OpenStreamForWriteAsync()).AsOutputStream())
                    await inkPres.StrokeContainer.SaveAsync(opStream);
                saveDialog.Hide();
                await res;
            }
        }

        private void UndoStroke(object sender, RoutedEventArgs e)
        {
            if (!undoStack.Any()) return;

            redoStack.Push(undoStack.Peek().Clone());
            undoStack.Pop().Selected = true;
            inkPres.StrokeContainer.DeleteSelected();
            btUndoStroke.IsEnabled = undoStack.Any();
            btRedoStroke.IsEnabled = true;
        }

        private void RedoStroke(object sender, RoutedEventArgs e)
        {
            if (!redoStack.Any()) return;

            undoStack.Push(redoStack.Pop());
            inkPres.StrokeContainer.AddStroke(undoStack.Peek());
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

            file = e.Parameter as StorageFile;
            if (inkPres == null || file == null)
                return;

            if (new FileInfo(file.Path).Length > 0)
            {
                using (IInputStream ipStream = (await file.OpenStreamForReadAsync()).AsInputStream())
                    await inkPres.StrokeContainer.LoadAsync(ipStream);
            }
        }

        private void AddPage(object sender, RoutedEventArgs e)
        {
            NotebookPage page = new NotebookPage();
            page.inkPres.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Mouse;
            page.inkPres.UpdateDefaultDrawingAttributes(inkToolbar.InkDrawingAttributes);
            spPageView.Children.Add(page);
        }

        private void PageGridLoaded(object sender, RoutedEventArgs e)
        {
            //NotebookPage nbPage = new NotebookPage();
            //((Grid)sender).Children.Add(nbPage.drawingCanvas);
            //nbPage.drawingCanvas.InkPresenter.UpdateDefaultDrawingAttributes(inkToolbar.InkDrawingAttributes);
            //pages.Add(nbPage);
        }

        private void InkToolChanged(InkToolbar sender, object args)
        {
            foreach (NotebookPage page in spPageView.Children)
            {
                page.inkPres.UpdateDefaultDrawingAttributes(inkToolbar.InkDrawingAttributes);
            }
        }

        private void InkToolbarLoaded(object sender, RoutedEventArgs e)
        {
            InkToolbarToolButton eraserButton = inkToolbar.GetToolButton(InkToolbarTool.Eraser);
            eraserButton.Checked += (s, e) =>
            {
                foreach (NotebookPage page in spPageView.Children)
                {
                    page.inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
                }
            };
            eraserButton.Unchecked += (s, e) =>
            {
                foreach (NotebookPage page in spPageView.Children)
                {
                    page.inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
                }
            };
            InkToolbarStencilButton stencilButton = (InkToolbarStencilButton)inkToolbar.GetMenuButton(InkToolbarMenuKind.Stencil);
            //stencilButton.Checked += (s, e) =>
            //{
            //    switch (stencilButton.SelectedStencil)
            //    {
            //        case InkToolbarStencilKind.Ruler:
            //            foreach (NotebookPage page in spPageView.Children)
            //            {
            //                page.ruler.IsVisible = true;
            //            }
            //            break;
            //        case InkToolbarStencilKind.Protractor:
            //            foreach (NotebookPage page in spPageView.Children)
            //            {
            //                page.protractor.IsVisible = true;
            //            }
            //            break;
            //    }

            //};
            inkToolbar.IsStencilButtonCheckedChanged += (s, e) =>
            {
                if (!inkToolbar.IsStencilButtonChecked)
                {
                    foreach (NotebookPage page in spPageView.Children)
                    {
                        page.ruler.IsVisible = page.protractor.IsVisible = false;
                    }
                    return;
                }
                switch (e.StencilKind)
                {
                    case InkToolbarStencilKind.Ruler:
                        foreach (NotebookPage page in spPageView.Children)
                        {
                            page.ruler.IsVisible = true;
                        }
                        e.StencilButton.SelectedStencil = InkToolbarStencilKind.Ruler;
                        break;
                    case InkToolbarStencilKind.Protractor:
                        foreach (NotebookPage page in spPageView.Children)
                        {
                            page.protractor.IsVisible = true;
                        }
                        e.StencilButton.SelectedStencil = InkToolbarStencilKind.Protractor;
                        break;
                }

            };
            //stencilButton.Unchecked += (s, e) =>
            //{
            //    foreach (NotebookPage page in spPageView.Children)
            //    {
            //        page.ruler.IsVisible = page.protractor.IsVisible = false;
            //    }
            //};
        }
    }
}
