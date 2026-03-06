using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input.Inking;
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
    public sealed partial class ManipulateInkRect : Grid
    {
        private Point? mousePos;
        private NotebookPage containingPage;
        List<InkStroke> selectedStrokes;

        public ManipulateInkRect(double x, double y, double width, double height, NotebookPage containingPage, List<InkStroke> selectedStrokes)
        {
            this.InitializeComponent();

            Canvas.SetLeft(this, x);
            Canvas.SetTop(this, y);
            this.Width = width;
            this.Height = height;
            this.containingPage = containingPage;
            this.selectedStrokes = selectedStrokes;

            AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(StartDraggingInk), true);
            AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(ContinueDraggingInk), true);
            AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(StopDraggingInk), true);
        }

        public ManipulateInkRect(Rect rect, NotebookPage containingPage, List<InkStroke> selectedStrokes) : this(rect.X, rect.Y, rect.Width, rect.Height, containingPage, selectedStrokes)
        {
        }

        private void StartDraggingInk(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            //pageContainer.HorizontalScrollMode = ScrollMode.Disabled;
            //pageContainer.VerticalScrollMode = ScrollMode.Disabled;

            mousePos = e.GetCurrentPoint(containingPage).Position;
            ((UIElement)sender).CapturePointer(e.Pointer);
            containingPage.hasBeenModifiedSinceSave = true;
        }

        private void ContinueDraggingInk(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                e.Handled = true;

                double oldY = Canvas.GetTop(this);
                Point oldMousePos = mousePos.Value;

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

                foreach (InkStroke stroke in selectedStrokes)
                    stroke.PointTransform *= Matrix3x2.CreateTranslation(mousePos.Value.ToVector2() - oldMousePos.ToVector2());
            }
        }

        private void StopDraggingInk(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            //pageContainer.HorizontalScrollMode = ScrollMode.Enabled;
            //pageContainer.VerticalScrollMode = ScrollMode.Enabled;
            mousePos = null;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        }
    }
}
