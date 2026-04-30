using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI;
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
    public sealed partial class OnPageText : Grid, IOnPageItem
    {
        public int id { get; private set; }
        public RichEditBox TextBox;
        private Point? mousePos;
        private NotebookPage containingPage;
        private double oldX, oldY = -1d;
        private double oldWidth, oldHeight = -1d;
        public bool hasBeenModifiedSinceSave { get; set; } = false;
        private bool hasLoadedFromFile = false;
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

        public NotebookPage RemoveTextFromPage()
        {
            containingPage.RemoveOnPageItemFromPage(this);
            return containingPage;
        }

        public double GetTop() => Canvas.GetTop(this);

        public double GetLeft() => Canvas.GetLeft(this);

        public void SetPos(double left, double top)
        {
            Canvas.SetLeft(this, left);
            Canvas.SetTop(this, top);
        }

        public double GetWidth() => this.Width;

        public double GetHeight() => this.Height;

        public void SetDimensions(double width, double height)
        {
            this.Width = width;
            this.Height = height;
        }

        public void SetIsSelectable(bool isSelectable)
        {
            IsHitTestVisible = isSelectable;
            btResize.Visibility = isSelectable ? Visibility.Visible : Visibility.Collapsed;
            btMove.Visibility = btResize.Visibility;
            bdText.BorderBrush = isSelectable ? (SolidColorBrush)Application.Current.Resources["SystemControlHighlightAccentBrush"] : new SolidColorBrush(Colors.Transparent);
        }

        public string GetFileName() => "text" + (id == 0 ? "" : (" (" + id + ")")) + ".rtf";

        public void SetHasBeenModified(bool value)
        {
            hasBeenModifiedSinceSave = value;
        }

        private void StartDraggingText(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            pageContainer.HorizontalScrollMode = ScrollMode.Disabled;
            pageContainer.VerticalScrollMode = ScrollMode.Disabled;

            oldX = GetLeft();
            oldY = GetTop();
            mousePos = e.GetCurrentPoint(containingPage).Position;
            ((UIElement)sender).CapturePointer(e.Pointer);
            hasBeenModifiedSinceSave = true;
        }

        private void ContinueDraggingText(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                e.Handled = true;

                double oldX = GetLeft();
                double oldY = GetTop();
                Point currentPos = e.GetCurrentPoint(containingPage).Position;

                Canvas.SetTop(this, Math.Max(0, Math.Min(containingPage.Height - this.Height, GetTop() + currentPos.Y - mousePos.Value.Y)));
                Canvas.SetLeft(this, Math.Max(0, Math.Min(containingPage.Width - this.Width, GetLeft() + currentPos.X - mousePos.Value.X)));

                if (oldY != GetTop())
                    mousePos = new Point(mousePos.Value.X, currentPos.Y);
                if (oldX != GetLeft())
                    mousePos = new Point(currentPos.X, mousePos.Value.Y);
            }
        }

        private void StopDraggingText(object sender, PointerRoutedEventArgs e)
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

        private void StartResizeText(object sender, PointerRoutedEventArgs e)
        {
            oldWidth = this.Width;
            oldHeight = this.Height;
            mousePos = e.GetCurrentPoint(containingPage).Position;
            ((UIElement)sender).CapturePointer(e.Pointer);
            hasBeenModifiedSinceSave = true;
        }

        private void ContinueResizeText(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                double oldHeight = this.Height;
                double oldWidth = this.Width;
                Point currentPos = e.GetCurrentPoint(containingPage).Position;

                this.Height = Math.Max(50, Math.Min(containingPage.Height - GetTop(), this.Height + currentPos.Y - mousePos.Value.Y));
                this.Width = Math.Max(50, Math.Min(containingPage.Width - GetLeft(), this.Width + currentPos.X - mousePos.Value.X));

                if (oldWidth != this.Width)
                    mousePos = new Point(currentPos.X, mousePos.Value.Y);
                if (oldHeight != this.Height)
                    mousePos = new Point(mousePos.Value.X, currentPos.Y);
            }
        }

        private void StopResizeText(object sender, PointerRoutedEventArgs e)
        {
            mousePos = null;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
            containingPage.undoRedoSystem.AddToUndoStack(new UndoResizeOnPageElement(this, oldWidth, oldHeight, containingPage.undoRedoSystem));
            oldWidth = -1d;
            oldHeight = -1d;
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
            if (hasLoadedFromFile)
                hasBeenModifiedSinceSave = true;
            else
                hasLoadedFromFile = true;
        }
    }
}
