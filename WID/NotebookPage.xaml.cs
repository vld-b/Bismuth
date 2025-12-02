using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Printing;
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
        public WriteableBitmap? bgImage { get; private set; }
        public List<OnPageText> textBoxes { get; private set; } = new List<OnPageText>();
        public Canvas contentCanvas { get; private set; }
        public InkCanvas canvas { get; private set; }
        public InkPresenter inkPres { get; private set; }
        public InkPresenterRuler ruler { get; private set; }
        public InkPresenterProtractor protractor { get; private set; }

        private PageTemplatePattern? currentPattern;

        public NotebookPage()
        {
            this.InitializeComponent();
            this.hasBg = false;
            contentCanvas = pageContent;
            canvas = inkCanvas;
            inkPres = inkCanvas.InkPresenter;
            ruler = new InkPresenterRuler(inkPres);
            protractor = new InkPresenterProtractor(inkPres);
        }

        public NotebookPage(int id) :this()
        {
            this.id = id;
        }


        public NotebookPage(int id, WriteableBitmap bg) : this(id)
        {
            LoadBackground(bg);
        }

        public NotebookPage(int id, double width, double height) : this(id)
        {
            this.Width = width;
            this.Height = height;
        }

        public void LoadBackground(WriteableBitmap bg)
        {
            this.Width = bg.PixelWidth;
            this.Height = bg.PixelHeight;
            this.bgImage = bg;
            this.hasBg = true;

            ccTemplateCanvas.Invalidate();
        }

        public void SetupForDrawing(bool shouldErase, InkToolbar inkToolbar)
        {
            inkPres.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Mouse;
            if (shouldErase)
                inkPres.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
            inkPres.UpdateDefaultDrawingAttributes(inkToolbar.InkDrawingAttributes);
        }

        internal async Task LoadLastPageFromConfig(NotebookConfig notebookConfig, StorageFolder notebookDir)
        {
            this.Width = notebookConfig.pageMapping.Last().width;
            this.Height = notebookConfig.pageMapping.Last().height;
            StorageFile ink = await notebookDir.GetFileAsync(notebookConfig.pageMapping.Last().fileName);
            using (IInputStream ipStream = await ink.OpenAsync(FileAccessMode.Read))
                await this.inkCanvas.InkPresenter.StrokeContainer.LoadAsync(ipStream);

            if (notebookConfig.pageMapping.Last().hasBg)
            {
                WriteableBitmap bgImg = await Utils.GetWBMPFromFileWithWidth(
                    await notebookDir.GetFileAsync(notebookConfig.pageMapping.Last().GetBgName()),
                    (int)notebookConfig.pageMapping.Last().width
                    );
                this.LoadBackground(bgImg);
            }
        }

        public void AddTextToPage(OnPageText text)
        {
            textBoxes.Add(text);
            contentCanvas.Children.Add(text);
        }

        public void ReDrawTemplate(PageTemplatePattern pattern)
        {
            currentPattern = pattern;
            ccTemplateCanvas.Invalidate();
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

        private void DrawBg(CanvasControl sender, CanvasDrawEventArgs args)
        {
            SetNewBackground(sender, args);
        }

        private void SetNewBackground(CanvasControl canvas, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Clear(Colors.White);
            if (hasBg)
            {
                using (Stream pixelStream = bgImage!.PixelBuffer.AsStream())
                {
                    byte[] pixels = new byte[pixelStream.Length];
                    pixelStream.ReadExactly(pixels);

                    CanvasBitmap cbmp = CanvasBitmap.CreateFromBytes(
                        args.DrawingSession.Device,
                        pixels,
                        bgImage!.PixelWidth,
                        bgImage!.PixelHeight,
                        Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized
                        );

                    args.DrawingSession.DrawImage(cbmp);
                }
            }
            else
            {
                currentPattern?.DrawOnCanvas(canvas, args);
            }
        }
    }
}
