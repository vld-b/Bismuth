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

        protected Button undoBtn;
        protected Button redoBtn;

        private int strokeCount = 0;

        public UndoRedoSystem(Button undoBtn, Button redoBtn)
        {
            undoStack = new Stack<UndoObject>();
            redoStack = new Stack<UndoObject>();
            this.undoBtn = undoBtn;
            this.redoBtn = redoBtn;
        }

        private void AddDriedStrokeToUndoStack(InkPresenter inkPres, InkStrokesCollectedEventArgs e)
        {
            redoStack.Clear();
            undoStack.Push(new UndoAddStroke((List<InkStroke>)e.Strokes, inkPres));
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
        }

        public void AddToUndoStack(UndoObject undoObject)
        {
            undoStack.Push(undoObject);
            undoBtn.IsEnabled = true;
        }
    }
}
