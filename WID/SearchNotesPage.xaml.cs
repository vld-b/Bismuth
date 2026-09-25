using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Xaml.Controls.Primitives;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Capture;
using Windows.Graphics.Imaging;
using Windows.Media.Audio;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class SearchNotesPage : Page
    {
        private CanvasRadialGradientBrush[] pointColors = new CanvasRadialGradientBrush[3];
        private Vector2[] pointPositions = new Vector2[3];
        private float pointRadius;

        private Vector2[] pointDirections = new Vector2[3];

        private Rect pointBounds;

        private string searchingFor = string.Empty;

        Frame? mainFrame;

        public SearchNotesPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            mainFrame = (Frame)e.Parameter;
        }

        private void LoadAutoSuggestBox(object sender, RoutedEventArgs e)
        {
            asbMainSearch.ScaleTransition.Duration = TimeSpan.FromMilliseconds(1500);
            asbMainSearch.TranslationTransition.Duration = TimeSpan.FromMilliseconds(1500);
            asbMainSearch.CenterPoint = new Vector3((float)asbMainSearch.ActualWidth / 2.0f, (float)asbMainSearch.ActualHeight / 2.0f, 0.0f);
            asbMainSearch.Scale = new Vector3(1.0f);
            asbMainSearch.Translation = new Vector3(0.0f, (float)this.ActualHeight / 2.0f, 1.0f);
        }

        private async void CreateBackgroundBlurResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            pointRadius = (float)sender.Size.Height / 2.0f;

            pointBounds = new Rect(0.0f, 0.0f, (float)sender.Size.Width, (float)sender.Size.Height);

            //Windows.UI.Color mainColor = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"];
            //CanvasGradientStop[] stops = new CanvasGradientStop[]
            //{
            //    new CanvasGradientStop { Position = 0.0f, Color = mainColor },
            //    new CanvasGradientStop { Position = 0.2f, Color = Utils.MultiplyColorWithScalar(mainColor, 0.75f) },
            //    new CanvasGradientStop { Position = 0.4f, Color = Utils.MultiplyColorWithScalar(mainColor, 0.5f) },
            //    new CanvasGradientStop { Position = 0.6f, Color = Utils.MultiplyColorWithScalar(mainColor, 0.25f) },
            //    new CanvasGradientStop { Position = 0.8f, Color = Utils.MultiplyColorWithScalar(mainColor, 0.10f) },
            //    new CanvasGradientStop { Position = 1.0f, Color = Utils.MultiplyColorWithScalar(mainColor, 0.02f) },
            //};

            for (int i = 0; i < 3; ++i)
            {
                float currentX = Random.Shared.NextSingle() * (float)sender.Size.Width;
                float currentY = Random.Shared.NextSingle() * (float)sender.Size.Height;
                pointPositions[i] = new Vector2(currentX, currentY);

                pointColors[i] = new CanvasRadialGradientBrush(sender, ((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]).AdjustSaturation(), Windows.UI.Colors.Transparent)
                {
                    Center = new System.Numerics.Vector2(currentX, currentY),
                    RadiusX = pointRadius,
                    RadiusY = pointRadius,
                    Opacity = .5f,
                };

                float randomAngle = Random.Shared.NextSingle() * 2 * MathF.PI;
                pointDirections[i] = new Vector2(MathF.Cos(randomAngle), MathF.Sin(randomAngle));
            }
        }

        private void DrawBackgroundBlur(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

            using CanvasCommandList commands = new CanvasCommandList(sender);
            using CanvasDrawingSession ds = commands.CreateDrawingSession();
            for (int i = 0; i < 3; ++i)
            {
                Vector2 currentPos = pointPositions[i];
                CanvasRadialGradientBrush currentBrush = pointColors[i];

                currentBrush.Center = currentPos;
                ds.FillCircle(currentPos, pointRadius, currentBrush);

                Vector2 currentDir = pointDirections[i];

                Vector2 predictedPos = new Vector2(currentPos.X + currentDir.X, currentPos.Y + currentDir.Y);

                if (predictedPos.X <= pointBounds.X || predictedPos.X >= pointBounds.Right)
                {
                    currentDir = new Vector2(-currentDir.X, currentDir.Y);
                }
                if (predictedPos.Y <= pointBounds.Y || predictedPos.Y >= pointBounds.Bottom)
                {
                    currentDir = new Vector2(currentDir.X, -currentDir.Y);
                }

                Vector2 nextPos = new Vector2(currentPos.X + currentDir.X, currentPos.Y + currentDir.Y);

                pointDirections[i] = currentDir;
                pointPositions[i] = nextPos;
            }

            using GaussianBlurEffect blur = new GaussianBlurEffect
            {
                Source = commands,
                BlurAmount = 30.0f,
                Optimization = EffectOptimization.Speed,
            };

            args.DrawingSession.DrawImage(blur);
        }

        private void SearchNotebooks(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.SuggestionChosen || args.Reason == AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
                return;

            //if (string.IsNullOrWhiteSpace(sender.Text))
            //    asbMainSearch.Translation = new Vector3(0.0f, (float)this.ActualHeight / 2.0f, 1.0f);
            //else
            //    asbMainSearch.Translation = new Vector3(0.0f, 0.0f, 1.0f);

            sender.ItemsSource = SearchForContentInNotebooks(ref searchingFor, sender);
        }

        private void AdjustElementSizes(object sender, SizeChangedEventArgs e)
        {
            float factorWidth = (float)(e.NewSize.Width / e.PreviousSize.Width);
            float factorHeight = (float)(e.NewSize.Height / e.PreviousSize.Height);
            for (int i = 0; i < 3; ++i)
                pointPositions[i] = new Vector2(pointPositions[i].X * factorWidth, pointPositions[i].Y * factorHeight);

            pointRadius = (float)e.NewSize.Height / 2.0f;

            if (asbMainSearch.Text == string.Empty)
            {
                Vector3Transition searchBoxTransition = asbMainSearch.TranslationTransition;
                asbMainSearch.TranslationTransition = null;
                asbMainSearch.Translation = new Vector3(0.0f, (float)e.NewSize.Height / 2.0f, 0.0f);
                asbMainSearch.TranslationTransition = searchBoxTransition;
            }
        }

        private void NavigateToSelectedItem(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            NavigateToItem(args, searchingFor, mainFrame!);
        }

        public static void NavigateToItem(AutoSuggestBoxQuerySubmittedEventArgs args, string searchingFor, Frame mainFrame)
        {
            if (args.ChosenSuggestion is null)
                return;

            NotebookSearchResult selItem = (NotebookSearchResult)args.ChosenSuggestion;

            SearchNavigation objectToPass = new SearchNavigation(selItem.notebookFolder, searchingFor, selItem.pageId, selItem.recText);

            mainFrame.Navigate(
                typeof(CanvasPage),
                objectToPass,
                new DrillInNavigationTransitionInfo()
                );
        }

        public static List<NotebookSearchResult>? SearchForContentInNotebooks(ref string searchingFor, AutoSuggestBox sender)
        {
            if (string.IsNullOrWhiteSpace(sender.Text))
            {
                searchingFor = "";
                sender.ItemsSource = null;
                return null;
            }

            searchingFor = sender.Text;
            List<NotebookSearchResult> matches = new List<NotebookSearchResult>();
            string[] searches = searchingFor.Split(" ");
            for (int i = 0; i < searches.Length; ++i)
                searches[i] = searches[i].Trim().ToLower();

            int currentPage = 0;
            foreach (SearchableNotebook nb in App.SearchableNotebooks)
            {
                currentPage = 0;
                foreach (SearchableNotebookPage page in nb.pages)
                {
                    ++currentPage;
                    foreach (RecognizedText text in page.recTextCol.recText)
                    {
                        foreach (string str in searches)
                        {
                            bool matchAlreadyExists = false;
                            foreach (NotebookSearchResult match in matches)
                            {
                                if (match.notebookFolder.Path == nb.notebookFolder.Path && match.pageId == page.pageId)
                                {
                                    ++match.rating;
                                    matchAlreadyExists = true;
                                    break;
                                }
                            }
                            if (text.text.ToLower().Contains(str) && !matchAlreadyExists)
                                matches.Add(new NotebookSearchResult(nb.notebookFolder, Utils.GetNotebookPathFromFolder(nb.notebookFolder), currentPage, page.pageId, text));
                        }
                    }
                }
            }

            return matches;
        }
    }
}
