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
        protected Stack<UndoObject> undoStack;
        protected Stack<UndoObject> redoStack;

        protected Control btUndo;
        protected Control btRedo;

        private int strokeCount = 0;

        public UndoRedoSystem(Control btUndo, Control btRedo)
        {
            undoStack = new Stack<UndoObject>();
            redoStack = new Stack<UndoObject>();
            this.btUndo = btUndo;
            this.btRedo = btRedo;
        }

        private void AddDriedStrokeToUndoStack(InkPresenter inkPres, InkStrokesCollectedEventArgs e)
        {
            redoStack.Clear();
            List<InkStroke> strokes = new List<InkStroke>();
            foreach (InkStroke stroke in e.Strokes)
            {
                strokes.Add(stroke);
            }
            undoStack.Push(new UndoAddStroke(strokes, inkPres));
            btUndo.IsEnabled = true;
        }

        private void AddDeletedStrokeToUndoStack(InkPresenter inkPres, InkStrokesErasedEventArgs e)
        {
            redoStack.Clear();
            undoStack.Push(new UndoDeleteStroke((List<InkStroke>)e.Strokes, inkPres));
        }

        public void RegisterPageToSystem(NotebookPage page, Panel parent)
        {
            page.canvas.InkPresenter.StrokesCollected += AddDriedStrokeToUndoStack;
            page.canvas.InkPresenter.StrokesErased += AddDeletedStrokeToUndoStack;

            List<NotebookPage> pages = new List<NotebookPage>();
            pages.Add(page);
            undoStack.Push(new UndoAddPages(pages, parent));
        }

        public void FlushStacks()
        {
            undoStack.Clear();
            redoStack.Clear();
            btUndo.IsEnabled = false;
            btRedo.IsEnabled = false;
        }

        public void AddToUndoStack(UndoObject undoObject)
        {
            undoStack.Push(undoObject);
            btUndo.IsEnabled = true;
            redoStack.Clear();
            btRedo.IsEnabled = false;
        }

        public void Undo()
        {
            undoStack.Peek().Undo();
            redoStack.Push(undoStack.Pop());
            if (undoStack.Count == 0)
                btUndo.IsEnabled = false;
            btRedo.IsEnabled = true;
        }

        public void Redo()
        {
            redoStack.Peek().Redo();
            undoStack.Push(redoStack.Pop());
            if (redoStack.Count == 0)
                btRedo.IsEnabled = false;
            btUndo.IsEnabled = true;
        }
    }
}
