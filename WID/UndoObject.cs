using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;

namespace WID
{
    public abstract class UndoObject
    {

        public abstract void Undo();
        public abstract void Redo();

        public UndoObject() { }
    }

    public sealed class UndoAddStroke : UndoObject
    {
        public List<InkStroke> strokes { get; private set; }
        public InkPresenter inkPres { get; private set; }

        public override void Undo()
        {
            foreach (InkStroke s in inkPres.StrokeContainer.GetStrokes())
                s.Selected = false;
            List<InkStroke> clonedStrokes = new List<InkStroke>();
            foreach (InkStroke s in strokes)
            {
                s.Selected = true;
                clonedStrokes.Add(s.Clone());
            }
            strokes = clonedStrokes;
            inkPres.StrokeContainer.DeleteSelected();
        }

        public override void Redo()
        {
            inkPres.StrokeContainer.AddStrokes(strokes);
        }

        public UndoAddStroke(List<InkStroke> strokes, InkPresenter inkPres)
        {
            this.strokes = strokes;
            this.inkPres = inkPres;
        }
    }

    public sealed class UndoDeleteStroke : UndoObject
    {
        public IList<InkStroke> strokes { get; private set; }
        public InkPresenter inkPres { get; private set; }

        public override void Undo()
        {
            inkPres.StrokeContainer.AddStrokes(strokes);
        }

        public override void Redo()
        {
            foreach (InkStroke s in inkPres.StrokeContainer.GetStrokes())
            {
                s.Selected = false;
            }
            foreach (InkStroke s in strokes)
            {
                s.Selected = true;
            }
            inkPres.StrokeContainer.DeleteSelected();
        }

        public UndoDeleteStroke(IList<InkStroke> strokes, InkPresenter inkPres)
        {
            this.strokes = strokes;
            this.inkPres = inkPres;
        }
    }

    public sealed class UndoAddPages : UndoObject
    {
        public IList<NotebookPage> pages { get; private set; }
        public Panel parent { get; private set; }

        private int pageIndex = -1;

        public override void Undo()
        {
            pageIndex = parent.Children.IndexOf(pages[0]);
            foreach (NotebookPage page in pages)
                parent.Children.Remove(page);
        }

        public override void Redo()
        {
            for (int i = 0; i < pages.Count; ++i)
                parent.Children.Insert(pageIndex+i, pages[i]);
        }

        public UndoAddPages(IList<NotebookPage> pages, Panel parent)
        {
            this.pages = pages;
            this.parent = parent;
        }
    }
}