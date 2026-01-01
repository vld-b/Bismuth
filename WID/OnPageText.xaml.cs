using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Text;
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
        public int id { get; private set; }
        public RichEditBox TextBox;
        private Point? mousePos;
        private NotebookPage containingPage;
        public bool hasBeenModifiedSinceSave { get; set; } = false;
        public EventHandler? TextBoxGotFocus;
        public EventHandler? TextBoxLostFocus;
        public ScrollViewer pageContainer { get; private set; }

        public OnPageText(int id, double width, double height, double top, double left, NotebookPage containingPage, ScrollViewer pageContainer)
        {
            this.InitializeComponent();
            this.id = id;
            this.TextBox = reb;
            this.Width = width;
            this.Height = height;
            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, left);
            this.containingPage = containingPage;
            this.pageContainer = pageContainer;

            btMove.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(StartDraggingText), true);
            btMove.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(ContinueDraggingText), true);
            btMove.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(StopDraggingText), true);

            btResize.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(StartResizeText), true);
            btResize.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(ContinueResizeText), true);
            btResize.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(StopResizeText), true);
        }

        public void SaveToStream(IRandomAccessStream stream)
        {
            reb.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, stream);
        }

        public void LoadFromStream(IRandomAccessStream stream)
        {
            reb.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, stream);
        }

        public void RemoveTextFromPage()
        {
            containingPage.RemoveTextFromPage(this);
            containingPage.hasBeenModifiedSinceSave = true;
        }

        private void StartDraggingText(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            pageContainer.HorizontalScrollMode = ScrollMode.Disabled;
            pageContainer.VerticalScrollMode = ScrollMode.Disabled;

            mousePos = e.GetCurrentPoint(containingPage).Position;
            ((UIElement)sender).CapturePointer(e.Pointer);
        }

        private void ContinueDraggingText(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                e.Handled = true;

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
            e.Handled = true;
            pageContainer.HorizontalScrollMode = ScrollMode.Enabled;
            pageContainer.VerticalScrollMode = ScrollMode.Enabled;
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
            TextBoxGotFocus?.Invoke(this, new EventArgs());
        }

        private void StopWriting(object sender, RoutedEventArgs e)
        {
            TextBoxLostFocus?.Invoke(this, new EventArgs());
        }

        private void ReceivedTextInput(object sender, RoutedEventArgs e)
        {
            this.hasBeenModifiedSinceSave = true;
            containingPage.hasBeenModifiedSinceSave = true;
        }
    }
}
