using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OnPageText : Grid
    {
        private Point? mousePos;
        NotebookPage containingPage;

        public OnPageText(double width, double height, NotebookPage containingPage)
        {
            this.InitializeComponent();
            this.Width = width;
            this.Height = height;
            this.containingPage = containingPage;

            btMove.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(StartDraggingText), true);
            btMove.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(ContinueDraggingText), true);
            btMove.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(StopDraggingText), true);

            btResize.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(StartResizeText), true);
            btResize.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(ContinueResizeText), true);
            btResize.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(StopResizeText), true);
        }

        private void ShowToolPopup(object sender, RoutedEventArgs e)
        {
        }

        private void HideToolPopup(object sender, RoutedEventArgs e)
        {
        }

        private void StartDraggingText(object sender, PointerRoutedEventArgs e)
        {
            mousePos = e.GetCurrentPoint(containingPage).Position;
            ((UIElement)sender).CapturePointer(e.Pointer);
        }

        private void ContinueDraggingText(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                double oldY = Canvas.GetTop(this);

                Canvas.SetTop(this, Math.Max(0, Math.Min(containingPage.Height-this.Height, Canvas.GetTop(this) + e.GetCurrentPoint(containingPage).Position.Y - mousePos.Value.Y)));
                Canvas.SetLeft(this, Canvas.GetLeft(this) + e.GetCurrentPoint(containingPage).Position.X - mousePos.Value.X);

                if (oldY != Canvas.GetTop(this))
                {
                    mousePos = e.GetCurrentPoint(containingPage).Position;
                } else
                {
                    mousePos = new Point(e.GetCurrentPoint(containingPage).Position.X, mousePos.Value.Y);
                }

            }
        }

        private void StopDraggingText(object sender, PointerRoutedEventArgs e)
        {
            mousePos = null;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        }

        private void StartResizeText(object sender, PointerRoutedEventArgs e)
        {
            mousePos = e.GetCurrentPoint(containingPage).Position;
            ((UIElement)sender).CapturePointer(e.Pointer);
        }

        private void ContinueResizeText(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                double oldHeight = this.Height;
                double oldWidth = this.Width;

                this.Height = Math.Max(50, Math.Min(containingPage.Height - Canvas.GetTop(this), this.Height + e.GetCurrentPoint(containingPage).Position.Y - mousePos.Value.Y));
                this.Width = Math.Max(50, this.Width + e.GetCurrentPoint(containingPage).Position.X - mousePos.Value.X);

                if (oldWidth != this.Width)
                {
                    mousePos = new Point(e.GetCurrentPoint(containingPage).Position.X, mousePos.Value.Y);
                }
                if (oldHeight != this.Height)
                {
                    mousePos = new Point(mousePos.Value.X, e.GetCurrentPoint(containingPage).Position.Y);
                }
            }
        }

        private void StopResizeText(object sender, PointerRoutedEventArgs e)
        {
            mousePos = null;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        }

        private void StartWriting(object sender, RoutedEventArgs e)
        {

        }

        private void StopWriting(object sender, RoutedEventArgs e)
        {

        }
    }
}
