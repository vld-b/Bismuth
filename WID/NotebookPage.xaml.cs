using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Printing;
using Windows.Security.EnterpriseData;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NotebookPage : Grid
    {
        public int id { get; private set; }
        public bool hasBg
        {
            get => bgImg is not null;
        }
        public bool hasBeenModifiedSinceSave { get; set; } = false;
        public BitmapImage? bgImage { get; private set; }
        public Image? bgImg { get; private set; }
        public List<IOnPageItem> onPageItems { get; private set; } = new List<IOnPageItem>();

        public Canvas contentCanvas { get; private set; }
        public InkCanvas canvas { get; private set; }
        public InkPresenter inkPres { get; private set; }
        public InkPresenterRuler ruler { get; private set; }
        public InkPresenterProtractor protractor { get; private set; }

        private UndoRedoSystem undoRedoSystem;

        private Polyline? selectionLasso;
        private ManipulateInkRect? selectionRect;
        private PageState pageState;

        private CanvasControl? _templateCanvas;
        public CanvasControl? templateCanvas
        {
            get => _templateCanvas;
            set
            {
                if (_templateCanvas != value)
                {
                    if (this.Children[0] is CanvasControl)
                        this.Children.RemoveAt(0);
                    _templateCanvas = value;
                    if (_templateCanvas != null)
                        this.Children.Insert(0, _templateCanvas);
                }
            }
        }
        private PageTemplatePattern? _currPattern;
        public PageTemplatePattern? currentPattern
        {
            get => _currPattern;
            set
            {
                if (_currPattern != value)
                {
                    _currPattern = value;
                    if (_currPattern == null)
                    {
                        if (this.Children[0] is CanvasControl)
                            this.Children.RemoveAt(0);
                    } else
                    {
                        UpdateTemplateBackground();
                        _currPattern.TemplatePropertiesChanged += (s, e) => UpdateTemplateBackground();
                    }
                }
            }
        }
        public bool hasPattern { get; set; }

        public NotebookPage()
        {
            this.InitializeComponent();
            this.undoRedoSystem = new UndoRedoSystem();
            this.pageState = new PageState(null);
            contentCanvas = pageContent;
            canvas = inkCanvas;
            inkPres = inkCanvas.InkPresenter;
            ruler = new InkPresenterRuler(inkPres);
            protractor = new InkPresenterProtractor(inkPres);
            inkCanvas.InkPresenter.StrokeInput.StrokeStarted += StartedDrawingInk;
            currentPattern = null;

            this.Unloaded += (s, e) => templateCanvas = null;
            inkPres.UnprocessedInput.PointerPressed += StartLasso;
            inkPres.UnprocessedInput.PointerMoved += ContinueLasso;
            inkPres.UnprocessedInput.PointerReleased += EndLasso;
        }

        public NotebookPage(int id, UndoRedoSystem undoRedoSystem, PageState pageState)
            : this()
        {
            this.id = id;
            this.undoRedoSystem = undoRedoSystem;
            this.pageState = pageState;
        }


        public NotebookPage(int id, BitmapImage bg, UndoRedoSystem undoRedoSystem, PageState pageState)
            : this(id, undoRedoSystem, pageState)
        {
            LoadBackground(bg);
        }

        public NotebookPage(int id, double width, double height, UndoRedoSystem undoRedoSystem, PageState pageState)
            : this(id, undoRedoSystem, pageState)
        {
            this.Width = width;
            this.Height = height;
        }

        public NotebookPage(int id, double width, double height, PageTemplatePattern? pattern, bool hasPattern, UndoRedoSystem undoRedoSystem, PageState pageState)
            : this(id, undoRedoSystem, pageState)
        {
            this.Width = width;
            this.Height = height;
            currentPattern = pattern;
            this.hasPattern = hasPattern;
        }

        public void LoadBackground(BitmapImage bg)
        {
            this.Width = 2100;
            this.Height = 2970;
            this.bgImage = bg;
            this.bgImg = new Image
            {
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Uniform,
            };
            bgImg.Source = bg;
            Children.Insert(0, bgImg);
        }

        public void SetupForDrawing(InkDrawingAttributes attrs, CurrentInkingTool currentInkingTool)
        {
            inkPres.InputDeviceTypes = App.AppSettings.inputDevices;
            if (currentInkingTool == CurrentInkingTool.Eraser)
                inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
            else if (currentInkingTool == CurrentInkingTool.Lasso)
                inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
            else
                inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
            inkPres.UpdateDefaultDrawingAttributes(attrs);
        }

        public async Task LoadLastPageFromConfig(NotebookConfig notebookConfig, StorageFolder notebookDir)
        {
            if (notebookConfig.pageMapping.Count == 0)
                return;
            this.Width = notebookConfig.pageMapping.Last().width;
            this.Height = notebookConfig.pageMapping.Last().height;
            this.currentPattern = notebookConfig.pageMapping.Last().pagePattern;
            StorageFile ink = await notebookDir.GetFileAsync(notebookConfig.pageMapping.Last().fileName);
            using (IInputStream ipStream = await ink.OpenAsync(FileAccessMode.Read))
                await this.inkCanvas.InkPresenter.StrokeContainer.LoadAsync(ipStream);

            if (notebookConfig.pageMapping.Last().hasBg)
            {
                bgImage = await Utils.GetBMPFromFileWithWidth(
                    await notebookDir.GetFileAsync(notebookConfig.pageMapping.Last().GetBgName()),
                    (int)notebookConfig.pageMapping.Last().width
                    );
                this.LoadBackground(bgImage);
            }
        }

        public async Task CollectText() // TODO: Implement collecting text from textboxes and from ink
        {
            InkAnalyzer analyzer = new InkAnalyzer();
            analyzer.AddDataForStrokes(canvas.InkPresenter.StrokeContainer.GetStrokes());
            InkAnalysisResult result = await analyzer.AnalyzeAsync();

            if (result.Status != InkAnalysisStatus.Updated)
                return;

            IReadOnlyList<IInkAnalysisNode> words = analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkWord);
        }

        private void UpdateTemplateBackground()
        {
            if (this.Children[0] is CanvasControl)
                this.Children.RemoveAt(0);
            CanvasControl c = new CanvasControl
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            c.Draw += _currPattern!.DrawOnCanvas;
            this.templateCanvas = c;
        }

        public void AddTextToPage(OnPageText text)
        {
            onPageItems.Add(text);
            contentCanvas.Children.Add(text);
        }

        public void AddImageToPage(OnPageImage img)
        {
            onPageItems.Add(img);
            contentCanvas.Children.Add(img);
        }

        public void RemoveManipulationRect()
        {
            if (pageState.selectedStrokes is not null)
            {
                foreach (InkStroke stroke in pageState.selectedStrokes!)
                    stroke.Selected = false;
                pageState.selectedStrokes = null;
            }
            pageState.currentlyActivePage = null;
            contentCanvas.Children.Remove(selectionRect);
            this.selectionRect = null;
        }

        public void RemoveImageFromPage(OnPageImage img)
        {
            onPageItems.Remove(img);
            contentCanvas.Children.Remove(img);
        }

        public void RemoveTextFromPage(OnPageText text)
        {
            onPageItems.Remove(text);
            contentCanvas.Children.Remove(text);
        }

        public async Task LoadFromStream(IInputStream stream)
        {
            await inkPres.StrokeContainer.LoadAsync(stream);
        }

        public async Task LoadFromFile(StorageFile file)
        {
            using (IInputStream stream = (await file.OpenStreamForReadAsync()).AsInputStream())
                await this.LoadFromStream(stream);
        }

        public async Task SaveToStream(IOutputStream stream)
        {
            await inkPres.StrokeContainer.SaveAsync(stream);
        }

        public async Task SaveToFile(StorageFile file)
        {
            using (IOutputStream stream = (await file.OpenStreamForWriteAsync()).AsOutputStream())
                await this.SaveToStream(stream);
        }

        private void StartedDrawingInk(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs e)
        {
            this.hasBeenModifiedSinceSave = true;
        }

        private void StartLasso(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs e)
        {
            if (selectionRect is not null)
            {
                contentCanvas.Children.Remove(selectionRect);
                selectionRect = null; 
            }
            pageState.selectedStrokes?.Clear();
            pageState.selectedStrokes = null;
            pageState.currentlyActivePage = this;

            selectionLasso = new Polyline
            {
                Stroke = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]),
                StrokeThickness = 4,
                StrokeDashArray = new DoubleCollection { 7, 3 },
                IsHitTestVisible = false,
            };
            selectionLasso.Points.Add(e.CurrentPoint.RawPosition);
            contentCanvas.Children.Add(selectionLasso);
        }

        private void ContinueLasso(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs e)
        {
            selectionLasso!.Points.Add(e.CurrentPoint.RawPosition);
        }

        private void EndLasso(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs e)
        {
            selectionLasso!.Points.Add(e.CurrentPoint.RawPosition);
            inkPres.StrokeContainer.SelectWithPolyLine(selectionLasso.Points);
            
            pageState.selectedStrokes = inkPres.StrokeContainer.GetStrokes().Where(s => s.Selected).ToList();
            if (pageState.selectedStrokes.Count == 0)
            {
                contentCanvas.Children.Remove(selectionLasso!);
                selectionLasso = null;
                pageState.DeselectStrokes();
                return;
            }
            Rect selectionRect = pageState.selectedStrokes[0].BoundingRect;
            foreach (InkStroke stroke in pageState.selectedStrokes)
                selectionRect = RectHelper.Union(selectionRect, stroke.BoundingRect);

            pageState.ShowInkSelectionPopup();

            contentCanvas.Children.Remove(selectionLasso!);
            selectionLasso = null;
            this.selectionRect = new ManipulateInkRect(selectionRect, this, pageState.selectedStrokes, undoRedoSystem);
            contentCanvas.Children.Add(this.selectionRect);
        }
    }

    public class PageState
    {
        public List<InkStroke>? selectedStrokes = null;
        public NotebookPage? currentlyActivePage = null;
        public Popup? pp;

        public PageState(Popup? pp)
        {
            this.pp = pp;
        }

        public void ShowInkSelectionPopup()
        {
            pp!.Opacity = 1d;
            pp!.IsHitTestVisible = true;
        }

        public void DeselectStrokes()
        {
            selectedStrokes = null;
            currentlyActivePage = null;
            pp!.Opacity = 0d;
            pp!.IsHitTestVisible = false;
        }
    }
}
