using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OnPageImage : Grid
    {
        public int id { get; private set; }

        double widthToHeight;
        private Point? mousePos;
        NotebookPage containingPage;
        private ScrollViewer pageContainer;

        public OnPageImage(int id, double top, double left, BitmapImage imgSource, NotebookPage containingPage, ScrollViewer pageContainer)
        {
            this.InitializeComponent();

            this.id = id;
            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, left);

            this.img.Source = imgSource;
            widthToHeight = (double)imgSource.PixelWidth / (double)imgSource.PixelHeight;
            this.Height = 100d;
            this.Width = 100d * widthToHeight;

            this.containingPage = containingPage;
            this.pageContainer = pageContainer;

            img.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(StartDraggingImage), true);
            img.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(ContinueDraggingImage), true);
            img.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(StopDraggingImage), true);

            btResize.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(StartResizeImage), true);
            btResize.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(ContinueResizeImage), true);
            btResize.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(StopResizeImage), true);
        }

        private void FocusImage(object sender, RoutedEventArgs e)
        {

        }

        private void LoseFocus(object sender, RoutedEventArgs e)
        {

        }

        private void StartDraggingImage(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            pageContainer.HorizontalScrollMode = ScrollMode.Disabled;
            pageContainer.VerticalScrollMode = ScrollMode.Disabled;

            mousePos = e.GetCurrentPoint(containingPage).Position;
            ((UIElement)sender).CapturePointer(e.Pointer);
        }

        private void ContinueDraggingImage(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                e.Handled = true;

                double oldY = Canvas.GetTop(this);

                Canvas.SetTop(this, Math.Max(0, Math.Min(containingPage.Height - this.Height, Canvas.GetTop(this) + e.GetCurrentPoint(containingPage).Position.Y - mousePos.Value.Y)));
                Canvas.SetLeft(this, Canvas.GetLeft(this) + e.GetCurrentPoint(containingPage).Position.X - mousePos.Value.X);

                if (oldY != Canvas.GetTop(this))
                {
                    mousePos = e.GetCurrentPoint(containingPage).Position;
                }
                else
                {
                    mousePos = new Point(e.GetCurrentPoint(containingPage).Position.X, mousePos.Value.Y);
                }

            }
        }

        private void StopDraggingImage(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            pageContainer.HorizontalScrollMode = ScrollMode.Enabled;
            pageContainer.VerticalScrollMode = ScrollMode.Enabled;
            mousePos = null;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        }

        private void StartResizeImage(object sender, PointerRoutedEventArgs e)
        {
            mousePos = e.GetCurrentPoint(containingPage).Position;
            ((UIElement)sender).CapturePointer(e.Pointer);
        }

        private void ContinueResizeImage(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                double oldHeight = this.Height;

                this.Height = Math.Max(50, Math.Min(containingPage.Height - Canvas.GetTop(this), this.Height + e.GetCurrentPoint(containingPage).Position.Y - mousePos.Value.Y));
                this.Width = this.Height * widthToHeight;

                if (oldHeight != this.Height)
                {
                    mousePos = new Point(mousePos.Value.X, e.GetCurrentPoint(containingPage).Position.Y);
                }
            }
        }

        private void StopResizeImage(object sender, PointerRoutedEventArgs e)
        {
            mousePos = null;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        }
    }
}
