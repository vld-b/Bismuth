using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
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
    public sealed partial class NotebookPage : Grid
    {
        public int id { get; private set; }
        public bool hasBg { get; private set; }
        public InkCanvas canvas { get; private set; }
        public InkPresenter inkPres { get; private set; }
        public InkPresenterRuler ruler { get; private set; }
        public InkPresenterProtractor protractor { get; private set; }

        public NotebookPage(int id)
        {
            this.InitializeComponent();
            this.id = id;
            this.hasBg = false;
            canvas = inkCanvas;
            inkPres = inkCanvas.InkPresenter;
            ruler = new InkPresenterRuler(inkPres);
            protractor = new InkPresenterProtractor(inkPres);
        }

        public NotebookPage(int id, BitmapImage bg) : this(id)
        {
            Image bgImage = new Image();
            bgImage.HorizontalAlignment = HorizontalAlignment.Stretch;
            bgImage.VerticalAlignment = VerticalAlignment.Stretch;
            bgImage.Source = bg;
            this.Width = bg.PixelWidth;
            this.Height = bg.PixelHeight;
            this.Children.Insert(0, bgImage);
            this.hasBg = true;
        }

        public NotebookPage(int id, double width, double height) : this(id)
        {
            this.Width = width;
            this.Height = height;
        }

        public void SetupForDrawing(bool shouldErase, InkToolbar inkToolbar)
        {
            inkPres.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Mouse;
            if (shouldErase)
                inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
            inkPres.UpdateDefaultDrawingAttributes(inkToolbar.InkDrawingAttributes);
        }

        public void AnimateIn()
        {
            this.RenderTransformOrigin = new Point(0.5f, 0f);
            this.RenderTransform = new ScaleTransform { ScaleX = 0.8f, ScaleY = 0.8f };
            this.Opacity = 0f;

            DoubleAnimation xAnim = new DoubleAnimation
            {
                From = 0.8f,
                To = 1f,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            };
            DoubleAnimation yAnim = new DoubleAnimation
            {
                From = 0.8f,
                To = 1f,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            };
            DoubleAnimation opacityAnim = new DoubleAnimation
            {
                From = 0f,
                To = 1f,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            };

            Storyboard sb = new Storyboard();
            Storyboard.SetTarget(xAnim, this.RenderTransform);
            Storyboard.SetTargetProperty(xAnim, "ScaleX");
            Storyboard.SetTarget(yAnim, this.RenderTransform);
            Storyboard.SetTargetProperty(yAnim, "ScaleY");
            Storyboard.SetTarget(opacityAnim, this);
            Storyboard.SetTargetProperty(opacityAnim, "Opacity");

            sb.Children.Add(xAnim);
            sb.Children.Add(yAnim);
            sb.Children.Add(opacityAnim);

            sb.Begin();
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
    }
}
