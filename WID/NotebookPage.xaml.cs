using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
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
using Windows.Media.Core;
using Windows.Security.EnterpriseData;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
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
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NotebookPage : Grid, IDisposable
    {
        public int id { get; private set; }
        public bool hasBg { get => bgImg is not null; }
        public bool hasBeenModifiedSinceSave { get; set; } = false;
        public BitmapImage? bgImage { get; private set; }
        public Image? bgImg { get; private set; }
        public string inkFileName { get => "page" + (id == 0 ? "" : (" (" + id + ")")) + ".gif"; }
        public string bgFileName { get => "bg" + (id == 0 ? "" : (" (" + id + ")")) + ".png"; }
        public List<IOnPageItem> onPageItems { get; private set; } = new List<IOnPageItem>();
        public RecognizedTextCollection recTextCollection { get; set; } = new RecognizedTextCollection();
        InkRecognizerContainer recContainer = new InkRecognizerContainer();

        public Canvas contentCanvas { get; private set; }
        public InkCanvas canvas { get; private set; }
        public InkPresenter inkPres { get; private set; }
        public InkPresenterRuler ruler { get; private set; }
        public InkPresenterProtractor protractor { get; private set; }

        public UndoRedoSystem undoRedoSystem { get; private set; }

        private Polyline? selectionLasso;
        private ManipulateInkRect? selectionRect;
        private PageState pageState;
        private SelectionMode _selectionMode;
        public SelectionMode selectionMode
        {
            get => _selectionMode;
            set
            {
                if (_selectionMode != value)
                {
                    _selectionMode = value;
                    if (_selectionMode == SelectionMode.Lasso)
                    {
                        foreach (IOnPageItem onPageItem in onPageItems)
                            onPageItem.SetIsSelectable(false);
                        inkCanvas.IsHitTestVisible = true;
                        inkCanvas.InkPresenter.InputDeviceTypes = App.AppSettings.inputDevices;
                    }
                    else if (_selectionMode == SelectionMode.Object)
                    {
                        foreach (IOnPageItem onPageItem in onPageItems)
                            onPageItem.SetIsSelectable(true);
                        inkCanvas.IsHitTestVisible = false;
                        inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.None;
                    }
                }
            }
        }

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
                        _currPattern.TemplatePropertiesChanged += UpdateTemplateBackgroundEvent;
                    }
                }
            }
        }
        public bool hasPattern { get; set; }

        private UIElement[]? storedElements;
        public bool isLoaded { get; private set; } = true;

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
            canvas.InkPresenter.InputProcessingConfiguration.RightDragAction = App.AppSettings.selectWithRightClick;
            inkCanvas.InkPresenter.StrokeInput.StrokeStarted += StartedDrawingInk;
            inkCanvas.InkPresenter.StrokesErased += DeletedStrokes;
            currentPattern = null;

            foreach (InkRecognizer rec in recContainer.GetRecognizers())
            {
                if (rec.Name == App.AppSettings.defaultInkLanguage)
                {
                    recContainer.SetDefaultRecognizer(rec);
                    break;
                }
            }

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

        public void Dispose()
        {
            Children.Clear();
            bgImage = null;
            bgImg = null;
            onPageItems.Clear();
            recTextCollection.recText.Clear();
            contentCanvas.Children.Clear();
            selectionLasso = null;
            selectionRect = null;
            templateCanvas?.RemoveFromVisualTree();
            templateCanvas = null;
            if (currentPattern is not null)
                currentPattern.TemplatePropertiesChanged -= UpdateTemplateBackgroundEvent;
            currentPattern = null;
            storedElements = null;
        }

        public void Unload()
        {
            if (!isLoaded)
                return;

            storedElements = new UIElement[Children.Count];
            for (int i = 0; i < Children.Count; ++i)
                storedElements[i] = Children[i];
            Children.Clear();
            isLoaded = false;
        }

        public void Load()
        {
            if (isLoaded)
                return;

            for (int i = 0; i < storedElements!.Length; ++i)
                Children.Add(storedElements[i]);
            storedElements = null;
            isLoaded = true;
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
                    await notebookDir.GetFileAsync(notebookConfig.pageMapping.Last().BgName),
                    (int)notebookConfig.pageMapping.Last().width
                    );
                this.LoadBackground(bgImage);
            }
        }

        public void SelectInkWithPolyline(IEnumerable<Point> points)
        {
            if (pageState.selectedStrokes is not null)
            {
                RemoveManipulationRect();
                pageState.DeselectStrokes();
            }
            inkPres.StrokeContainer.SelectWithPolyLine(points);

            pageState.selectedStrokes = inkPres.StrokeContainer.GetStrokes().Where(s => s.Selected).ToList();
            if (pageState.selectedStrokes.Count == 0)
            {
                pageContent.Children.Remove(selectionLasso!);
                selectionLasso = null;
                pageState.DeselectStrokes();
                return;
            }
            Rect selectionRect = pageState.selectedStrokes[0].BoundingRect;
            foreach (InkStroke stroke in pageState.selectedStrokes)
                selectionRect = RectHelper.Union(selectionRect, stroke.BoundingRect);

            pageState.currentlyActivePage = this;
            pageState.ShowInkSelectionPopup();

            pageContent.Children.Remove(selectionLasso!);
            selectionLasso = null;
            this.selectionRect = new ManipulateInkRect(selectionRect, this, pageState.selectedStrokes, undoRedoSystem);
            cvManipulationRects.Children.Add(this.selectionRect);
        }

        public void SetInkLanguage(string? lang)
        {
            if (lang is null)
                return;

            foreach (InkRecognizer rec in recContainer.GetRecognizers())
            {
                if (rec.Name == lang)
                {
                    recContainer.SetDefaultRecognizer(rec);
                    break;
                }
            }
        }

        public async Task<RecognizedTextCollection> CollectText()
        {
            List<RecognizedText> recognizedInk = new List<RecognizedText>();

            if (canvas.InkPresenter.StrokeContainer.GetStrokes().Count == 0)
                goto afterInkAnalysis;

            IReadOnlyList<InkRecognitionResult> res = await recContainer.RecognizeAsync(canvas.InkPresenter.StrokeContainer, InkRecognitionTarget.All);

            foreach (InkRecognitionResult r in res)
            {
                recognizedInk.Add(new RecognizedText(r.GetTextCandidates()[0], SimpleRect.FromRect(r.BoundingRect)));
            }

            //InkAnalyzer analyzer = new InkAnalyzer();
            //IReadOnlyList<InkStroke> strokesToBeAnalyzed = canvas.InkPresenter.StrokeContainer.GetStrokes();
            //analyzer.AddDataForStrokes(strokesToBeAnalyzed);
            //foreach (InkStroke stroke in strokesToBeAnalyzed)
            //    analyzer.SetStrokeDataKind(stroke.Id, InkAnalysisStrokeKind.Writing);
            //InkAnalysisResult result = await analyzer.AnalyzeAsync();

            //IReadOnlyList<IInkAnalysisNode> words = analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkWord);
            //foreach (IInkAnalysisNode word in words)
            //{
            //    InkAnalysisInkWord inkWord = (InkAnalysisInkWord)word;
            //    recognizedInk.Add(new RecognizedText(inkWord.RecognizedText, SimpleRect.FromRect(inkWord.BoundingRect)));
            //}

        afterInkAnalysis:

            List<RecognizedText> textFromTextboxes = new List<RecognizedText>();
            foreach (IOnPageItem item in onPageItems)
            {
                if (item is OnPageText text)
                {
                    text = (OnPageText)item;
                    text.TextBox.Document.GetText(Windows.UI.Text.TextGetOptions.None, out string containedText);
                    textFromTextboxes.Add(new RecognizedText(containedText, text.id));
                }
            }

            recTextCollection.recText = recognizedInk;
            recTextCollection.recText.Add(textFromTextboxes);
            return recTextCollection;
        }

        public async Task HighlightText(string searchKeywords, RecognizedText recText)
        {
            string[] words = searchKeywords.Split(' ');

            if (recText.textBoxId == -1)
            {
                int opacityTransitionDuration = 500;
                Rectangle drawingRect = new Rectangle
                {
                    IsHitTestVisible = false,
                    Opacity = 0d,
                    Fill = new SolidColorBrush(Colors.Yellow),
                    OpacityTransition = new ScalarTransition { Duration = TimeSpan.FromMilliseconds(opacityTransitionDuration) },
                    Width = recText.boudingBox!.width,
                    Height = recText.boudingBox!.height,
                };
                Canvas.SetLeft(drawingRect, recText.boudingBox!.x);
                Canvas.SetTop(drawingRect, recText.boudingBox!.y);

                pageContent.Children.Add(drawingRect);
                drawingRect.Opacity = 1d;
                await Task.Delay(opacityTransitionDuration + 2000);
                drawingRect.Opacity = 0d;
                await Task.Delay(opacityTransitionDuration);
                pageContent.Children.Remove(drawingRect);
            } else
            {
                OnPageText? opT = null;
                foreach (IOnPageItem item in pageContent.Children)
                    if (item is OnPageText onPageText && onPageText.id == recText.textBoxId)
                        opT = onPageText;

                RichEditBox reb = opT!.TextBox;

                Color highlightBg = (Color)App.Current.Resources["SystemColorHighlightColor"];
                Color highlightFg = (Color)App.Current.Resources["SystemColorHighlightTextColor"];

                ITextRange searchRange = reb.Document.GetRange(0, 0);
                foreach (string str in words)
                {
                    while (searchRange.FindText(str, TextConstants.MaxUnitCount, FindOptions.None) > 0)
                    {
                        searchRange.CharacterFormat.BackgroundColor = highlightBg;
                        searchRange.CharacterFormat.ForegroundColor = highlightFg;
                    }
                }

                await Task.Delay(2000);

                ITextRange docRange = reb.Document.GetRange(0, TextConstants.MaxUnitCount);
                Color defaultBg = ((SolidColorBrush)reb.Background).Color;
                Color defaultFg = ((SolidColorBrush)reb.Foreground).Color;

                docRange.CharacterFormat.BackgroundColor = defaultBg;
                docRange.CharacterFormat.ForegroundColor = defaultFg;

                opT.hasBeenModifiedSinceSave = false;
            }
        }

        private void UpdateTemplateBackgroundEvent(object? s, EventArgs e)
        {
            UpdateTemplateBackground();
        }

        public void UpdateTemplateBackground()
        {
            if (_currPattern is null)
                return;

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

        public void RemoveOnPageItemFromPage(IOnPageItem item)
        {
            onPageItems.Remove(item);
            if (item is OnPageText txt)
            {
                contentCanvas.Children.Remove(txt);
                for (int i = recTextCollection.recText.Count - 1; i >= 0; --i)
                {
                    if (recTextCollection.recText[i].textBoxId == txt.id)
                    {
                        recTextCollection.recText.RemoveAt(i);
                        break;
                    }
                }
            }
            else if (item is OnPageImage img)
                contentCanvas.Children.Remove(img);
        }

        public void AddOnPageItemToPage(IOnPageItem item)
        {
            onPageItems.Add(item);
            if (item is OnPageText txt)
                contentCanvas.Children.Add(txt);
            else if (item is OnPageImage img)
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
            cvManipulationRects.Children.Remove(selectionRect);
            this.selectionRect = null;
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
            pageState.DeselectStrokes();
            this.RemoveManipulationRect();
        }
        private void DeletedStrokes(InkPresenter sender, InkStrokesErasedEventArgs e) => this.hasBeenModifiedSinceSave = true;

        private void StartLasso(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs e)
        {
            if (selectionRect is not null)
            {
                cvManipulationRects.Children.Remove(selectionRect);
                selectionRect = null; 
            }
            pageState.selectedStrokes?.Clear();
            pageState.selectedStrokes = null;
            pageState.currentlyActivePage = this;

            if (selectionMode != SelectionMode.Lasso)
                return;

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
            selectionLasso?.Points.Add(e.CurrentPoint.RawPosition);
        }

        private void EndLasso(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs e)
        {
            if (selectionMode != SelectionMode.Lasso)
                return;

            selectionLasso!.Points.Add(e.CurrentPoint.RawPosition);
            SelectInkWithPolyline(selectionLasso!.Points);
        }
    }

    public class PageState
    {
        public List<InkStroke>? selectedStrokes = null;
        public NotebookPage? currentlyActivePage = null;
        public Popup? ppInkManipulation;

        public PageState(Popup? ppInkManipulation)
        {
            this.ppInkManipulation = ppInkManipulation;
        }

        public void ShowInkSelectionPopup()
        {
            ppInkManipulation!.Opacity = 1d;
            ppInkManipulation!.IsHitTestVisible = true;
        }

        public void DeselectStrokes()
        {
            selectedStrokes = null;
            currentlyActivePage = null;
            ppInkManipulation!.Opacity = 0d;
            ppInkManipulation!.IsHitTestVisible = false;
        }
    }

    public enum SelectionMode
    {
        Lasso,
        Object,
    }
}
