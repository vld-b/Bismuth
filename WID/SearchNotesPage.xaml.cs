using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas.Effects;
using Windows.Media.Audio;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.Capture;
using Microsoft.Graphics.Canvas.Brushes;

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

        public SearchNotesPage()
        {
            this.InitializeComponent();
        }

        private async void CreateBackgroundBlurResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            pointRadius = (float)sender.Size.Height / 1.2f;

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
    }
}
