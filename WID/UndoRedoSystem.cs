using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace WID
{
    public class UndoRedoSystem
    {
        protected Stack<UndoObject> undoStack = new Stack<UndoObject>();
        protected Stack<UndoObject> redoStack = new Stack<UndoObject>();

        protected readonly List<Control> undoBtns = new List<Control>();
        protected readonly List<Control> redoBtns = new List<Control>();

        private int strokeCount = 0;

        public UndoRedoSystem()
        {
        }

        private void AddDriedStrokeToUndoStack(InkPresenter inkPres, InkStrokesCollectedEventArgs e)
        {
            redoStack.Clear();
            List<InkStroke> strokes = new List<InkStroke>();
            foreach (InkStroke stroke in e.Strokes)
            {
                strokes.Add(stroke);
            }
            AddToUndoStack(new UndoAddStroke(strokes, inkPres));
            SetUndoState(true);
        }

        private void AddDeletedStrokeToUndoStack(InkPresenter inkPres, InkStrokesErasedEventArgs e)
        {
            redoStack.Clear();
            List<InkStroke> strokes = new List<InkStroke>();
            foreach (InkStroke stroke in e.Strokes)
                strokes.Add(stroke);
            AddToUndoStack(new UndoDeleteStroke(strokes, inkPres));
        }

        public void RegisterPageToSystem(NotebookPage page, Panel parent)
        {
            page.canvas.InkPresenter.StrokesCollected += AddDriedStrokeToUndoStack;
            page.canvas.InkPresenter.StrokesErased += AddDeletedStrokeToUndoStack;

            List<NotebookPage> pages = new List<NotebookPage>();
            pages.Add(page);
            undoStack.Push(new UndoAddPages(pages, parent));
        }

        public void RegisterUndoButton(Control activator)
        {
            undoBtns.Add(activator);
        }

        public void RegisterRedoButton(Control activator)
        {
            redoBtns.Add(activator);
        }

        public void FlushStacks()
        {
            undoStack.Clear();
            redoStack.Clear();
            SetUndoState(false);
            SetRedoState(false);
        }

        public void AddToUndoStack(UndoObject undoObject)
        {
            undoStack.Push(undoObject);
            SetUndoState(true);
            redoStack.Clear();
            SetRedoState(false);
        }

        public void Undo()
        {
            undoStack.Peek().Undo();
            redoStack.Push(undoStack.Pop());
            if (undoStack.Count == 0)
                SetUndoState(false);
            SetRedoState(true);
        }

        public void Redo()
        {
            redoStack.Peek().Redo();
            undoStack.Push(redoStack.Pop());
            if (redoStack.Count == 0)
                SetRedoState(false);
            SetUndoState(true);
        }

        private void SetUndoState(bool state)
        {
            foreach (Control ctrl in undoBtns)
                ctrl.IsEnabled = state;
        }

        private void SetRedoState(bool state)
        {
            foreach (Control ctrl in redoBtns)
                ctrl.IsEnabled = state;
        }
    }
}
