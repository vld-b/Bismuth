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
        private Vector2? originalPos;
        private NotebookPage containingPage;
        private List<MovedStroke> selectedStrokes;
        private float oldX, oldY;

        private UndoRedoSystem undoRedoSystem;

        private bool hasMoved = false;

        public ManipulateInkRect(double x, double y, double width, double height, NotebookPage containingPage, List<InkStroke> selectedStrokes, UndoRedoSystem undoRedoSystem)
        {
            this.InitializeComponent();

            Canvas.SetLeft(this, x);
            Canvas.SetTop(this, y);
            oldX = (float)x;
            oldY = (float)y;
            this.Width = width;
            this.Height = height;
            this.containingPage = containingPage;

            this.selectedStrokes = new List<MovedStroke>();
            foreach (InkStroke stroke in selectedStrokes)
                this.selectedStrokes.Add(new MovedStroke(stroke, stroke.PointTransform, Matrix3x2.Identity));

            this.undoRedoSystem = undoRedoSystem;

            AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(StartDraggingInk), true);
            AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(ContinueDraggingInk), true);
            AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(StopDraggingInk), true);
        }

        public ManipulateInkRect(Rect rect, NotebookPage containingPage, List<InkStroke> selectedStrokes, UndoRedoSystem undoRedoSystem)
            : this(rect.X, rect.Y, rect.Width, rect.Height, containingPage, selectedStrokes, undoRedoSystem)
        {
        }

        private void StartDraggingInk(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            //pageContainer.HorizontalScrollMode = ScrollMode.Disabled;
            //pageContainer.VerticalScrollMode = ScrollMode.Disabled;

            mousePos = e.GetCurrentPoint(containingPage).Position;
            originalPos = new Vector2((float)Canvas.GetLeft(this), (float)Canvas.GetTop(this));
            ((UIElement)sender).CapturePointer(e.Pointer);
            containingPage.hasBeenModifiedSinceSave = true;
        }

        private void ContinueDraggingInk(object sender, PointerRoutedEventArgs e)
        {
            if (mousePos is not null)
            {
                e.Handled = true;

                Point oldMousePos = mousePos.Value;

                Canvas.SetTop(this, Math.Max(0, Math.Min(containingPage.Height - this.Height, originalPos!.Value.Y + e.GetCurrentPoint(containingPage).Position.Y - mousePos.Value.Y)));
                Canvas.SetLeft(this, originalPos.Value.X + e.GetCurrentPoint(containingPage).Position.X - mousePos.Value.X);

                //if (oldY != Canvas.GetTop(this))
                //{
                //    mousePos = e.GetCurrentPoint(containingPage).Position;
                //}
                //else
                //{
                //    mousePos = new Point(e.GetCurrentPoint(containingPage).Position.X, mousePos.Value.Y);
                //}

                foreach (MovedStroke stroke in selectedStrokes)
                {
                    stroke.stroke.PointTransform = stroke.oldTransform * Matrix3x2.CreateTranslation(new Vector2((float)Canvas.GetLeft(this) - oldX, (float)Canvas.GetTop(this) - oldY));
                }
            }
        }

        private void StopDraggingInk(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            //pageContainer.HorizontalScrollMode = ScrollMode.Enabled;
            //pageContainer.VerticalScrollMode = ScrollMode.Enabled;
            if (mousePos != e.GetCurrentPoint(containingPage).Position)
            {
                hasMoved = true;
                foreach (MovedStroke stroke in selectedStrokes)
                    stroke.newTransform = stroke.stroke.PointTransform;
                undoRedoSystem.AddToUndoStack(new UndoMoveStrokes(selectedStrokes, undoRedoSystem));
            }

            mousePos = null;
            originalPos = null;

            foreach (MovedStroke stroke in selectedStrokes)
                stroke.newTransform = stroke.stroke.PointTransform;

            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        }
    }
}
