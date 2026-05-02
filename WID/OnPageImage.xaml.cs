using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public sealed partial class OnPageImage : Grid, IOnPageItem
    {
        public int id { get; private set; }

        private double widthToHeight;
        private Point? mousePos;
        private double oldX = -1d, oldY = -1d;
        private double oldWidth = -1d, oldHeight = -1d;
        public NotebookPage containingPage { get; private set;  }
        public WriteableBitmap wbmp;
        private ScrollViewer pageContainer;
        public bool isNewImage { get; set; }

        public EventHandler? ImageGotFocus;
        public EventHandler? ImageLostFocus;

        public OnPageImage(int id, double top, double left, WriteableBitmap imgSource, NotebookPage containingPage, ScrollViewer pageContainer, bool isNewImage)
        {
            this.InitializeComponent();

            this.id = id;
            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, left);

            this.wbmp = imgSource;
            this.img.Source = imgSource;
            widthToHeight = (double)imgSource.PixelWidth / (double)imgSource.PixelHeight;
            this.Height = 500d;
            this.Width = this.Height * widthToHeight;

            this.containingPage = containingPage;
            this.pageContainer = pageContainer;

            this.isNewImage = isNewImage;

            img.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(StartDraggingImage), true);
            img.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(ContinueDraggingImage), true);
            img.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(StopDraggingImage), true);

            btResize.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(StartResizeImage), true);
            btResize.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(ContinueResizeImage), true);
            btResize.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(StopResizeImage), true);
        }

        public double Top { get => Canvas.GetTop(this); }

        public double Left { get => Canvas.GetLeft(this); }


        public void SetPos(double left, double top)
        {
            Canvas.SetLeft(this, left);
            Canvas.SetTop(this, top);
        }

        public void SetDimensions(double width, double height)
        {
            this.Width = width;
            this.Height = height;
        }

        public void SetIsSelectable(bool isSelectable)
        {
            IsHitTestVisible = isSelectable;
            btResize.Visibility = isSelectable ? Visibility.Visible : Visibility.Collapsed;
            bdImage.BorderBrush = isSelectable ? (SolidColorBrush)Application.Current.Resources["SystemControlHighlightAccentBrush"] : new SolidColorBrush(Colors.Transparent);
        }

        public NotebookPage RemoveImageFromPage()
        {
            containingPage.RemoveOnPageItemFromPage(this);
            containingPage.hasBeenModifiedSinceSave = true;
            return containingPage;
        }

        public string FileName { get => "img" + (id == 0 ? "" : (" (" + id + ")")) + ".jpg"; }

        public bool HasBeenModified { set { } }

        private void FocusImage(object sender, RoutedEventArgs e)
        {
            ImageGotFocus?.Invoke(this, EventArgs.Empty);
        }

        private void LoseFocus(object sender, RoutedEventArgs e)
        {
            ImageLostFocus?.Invoke(this, EventArgs.Empty);
        }

        private void StartDraggingImage(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            pageContainer.HorizontalScrollMode = ScrollMode.Disabled;
            pageContainer.VerticalScrollMode = ScrollMode.Disabled;
            btResize.Focus(FocusState.Pointer); // Needed to redirect focus to the OnPageImage

            oldX = Left;
            oldY = Top;
            mousePos = e.GetCurrentPoint(containingPage).Position;
            ((UIElement)sender).CapturePointer(e.Pointer);
        }

        private void ContinueDraggingImage(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                e.Handled = true;

                double oldX = Left;
                double oldY = Top;
                Point currentPos = e.GetCurrentPoint(containingPage).Position;

                Canvas.SetTop(this, Math.Max(0, Math.Min(containingPage.Height - this.Height, Top + currentPos.Y - mousePos.Value.Y)));
                Canvas.SetLeft(this, Math.Max(0, Math.Min(containingPage.Width - this.Width, Left + currentPos.X - mousePos.Value.X)));

                if (oldY != Top)
                    mousePos = new Point(mousePos.Value.X, currentPos.Y);
                if (oldX != Left)
                    mousePos = new Point(currentPos.X, mousePos.Value.Y);
            }
        }

        private void StopDraggingImage(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            pageContainer.HorizontalScrollMode = ScrollMode.Enabled;
            pageContainer.VerticalScrollMode = ScrollMode.Enabled;
            containingPage.undoRedoSystem.AddToUndoStack(new UndoMoveOnPageElement(this, oldX, oldY, containingPage.undoRedoSystem));
            oldX = -1d;
            oldY = -1d;
            mousePos = null;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        }

        private void StartResizeImage(object sender, PointerRoutedEventArgs e)
        {
            oldWidth = this.Width;
            oldHeight = this.Height;
            mousePos = e.GetCurrentPoint(containingPage).Position;
            ((UIElement)sender).CapturePointer(e.Pointer);
        }

        private void ContinueResizeImage(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                double oldHeight = this.Height;
                Point currentPos = e.GetCurrentPoint(containingPage).Position;

                double newHeight = this.Height + currentPos.Y - mousePos.Value.Y;
                double newWidth = newHeight * widthToHeight;
                double maxHeight = containingPage.Height - Top;
                double maxWidth = containingPage.Width - Left;

                if (50d <= newHeight && newHeight <= maxHeight && newWidth <= maxWidth)
                {
                    this.Height = newHeight;
                    this.Width = this.Height * widthToHeight;
                }

                if (oldHeight != this.Height)
                    mousePos = new Point(mousePos.Value.X, currentPos.Y);
            }
        }

        private void StopResizeImage(object sender, PointerRoutedEventArgs e)
        {
            mousePos = null;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
            containingPage.undoRedoSystem.AddToUndoStack(new UndoResizeOnPageElement(this, oldWidth, oldHeight, containingPage.undoRedoSystem));
            oldWidth = -1d;
            oldHeight = -1d;
        }
    }
}
