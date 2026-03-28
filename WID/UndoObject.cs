using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;

namespace WID
{
    public delegate void NewRefsTrigger(List<NewStrokereference> newRefs);

    public abstract class UndoObject
    {

        public abstract void Undo();
        public abstract void Redo();

        public abstract void UpdateReferences(List<NewStrokereference> newStrokeReferences);
        protected void UpdateStrokeReferences(List<InkStroke> strokes, List<NewStrokereference> newStrokeReferences)
        {
            for (int i = 0; i < strokes.Count; ++i)
                for (int j = 0; j < newStrokeReferences.Count; ++j)
                    if (newStrokeReferences[j].oldStroke == strokes[i])
                        strokes[i] = newStrokeReferences[j].newStroke;
        }

        protected List<InkStroke> CloneStrokesAndUpdateReferences(List<InkStroke> strokes, EventHandler<InkStroke>? strokeAction)
        {
            List<InkStroke> clonedStrokes = new List<InkStroke>();
            List<NewStrokereference> newRefs = new List<NewStrokereference>();
            foreach (InkStroke s in strokes)
            {
                InkStroke clone = s.Clone();
                clonedStrokes.Add(clone);
                newRefs.Add(new NewStrokereference(s, clone));
                strokeAction?.Invoke(this, s);
            }
            containingSystem.UpdateStrokeReferences(newRefs);

            return clonedStrokes;
        }

        protected UndoRedoSystem containingSystem;

        public UndoObject(UndoRedoSystem containingSystem)
        {
            this.containingSystem = containingSystem;
        }
    }

    public class NewStrokereference
    {
        public InkStroke oldStroke;
        public InkStroke newStroke;

        public NewStrokereference(InkStroke oldStroke, InkStroke newStroke)
        {
            this.oldStroke = oldStroke;
            this.newStroke = newStroke;
        }
    }

    public class MovedStroke
    {
        public InkStroke stroke;
        public Matrix3x2 oldTransform;
        public Matrix3x2 newTransform;

        public MovedStroke(InkStroke stroke, Matrix3x2 oldTransform, Matrix3x2 newTransform)
        {
            this.stroke = stroke;
            this.oldTransform = oldTransform;
            this.newTransform = newTransform;
        }
    }

    public class RecoloredStroke
    {
        public InkStroke stroke;
        public Color oldColor;

        public RecoloredStroke(InkStroke stroke, Color oldColor)
        {
            this.stroke = stroke;
            this.oldColor = oldColor;
        }
    }

    public sealed class UndoAddStroke : UndoObject
    {
        public List<InkStroke> strokes { get; private set; }
        public InkPresenter inkPres { get; private set; }

        public override void Undo()
        {
            foreach (InkStroke s in inkPres.StrokeContainer.GetStrokes())
                s.Selected = false;

            strokes = CloneStrokesAndUpdateReferences(strokes, (sender, s) => s.Selected = true);

            inkPres.StrokeContainer.DeleteSelected();
        }

        public override void Redo()
        {
            inkPres.StrokeContainer.AddStrokes(strokes);
        }

        public override void UpdateReferences(List<NewStrokereference> newStrokeReferences)
        {
            UpdateStrokeReferences(strokes, newStrokeReferences);
        }

        public UndoAddStroke(List<InkStroke> strokes, InkPresenter inkPres, UndoRedoSystem containingSystem) : base(containingSystem)
        {
            this.strokes = strokes;
            this.inkPres = inkPres;
        }
    }

    public sealed class UndoDeleteStroke : UndoObject
    {
        public List<InkStroke> strokes { get; private set; }
        public InkPresenter inkPres { get; private set; }

        public override void Undo()
        {
            inkPres.StrokeContainer.AddStrokes(strokes);
        }

        public override void Redo()
        {
            foreach (InkStroke s in inkPres.StrokeContainer.GetStrokes())
                s.Selected = false;

            strokes = CloneStrokesAndUpdateReferences(strokes, (sender, s) => s.Selected = true);

            inkPres.StrokeContainer.DeleteSelected();
        }

        public override void UpdateReferences(List<NewStrokereference> newStrokeReferences)
        {
            UpdateStrokeReferences(strokes, newStrokeReferences);
        }

        public UndoDeleteStroke(List<InkStroke> strokes, InkPresenter inkPres, UndoRedoSystem containingSystem) : base(containingSystem)
        {
            this.strokes = CloneStrokesAndUpdateReferences(strokes, null);
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

        public override void UpdateReferences(List<NewStrokereference> newStrokeReferences)
        {
            throw new NotImplementedException();
        }

        public UndoAddPages(IList<NotebookPage> pages, Panel parent, UndoRedoSystem containingSystem) : base(containingSystem)
        {
            this.pages = pages;
            this.parent = parent;
        }
    }

    public sealed class UndoMoveStrokes : UndoObject
    {
        public List<MovedStroke> movedStrokes { get; private set; }

        public override void Undo()
        {
            foreach (MovedStroke stroke in movedStrokes)
                stroke.stroke.PointTransform = stroke.oldTransform;
        }

        public override void Redo()
        {
            foreach (MovedStroke stroke in movedStrokes)
                stroke.stroke.PointTransform = stroke.newTransform;
        }

        public override void UpdateReferences(List<NewStrokereference> newStrokeReferences)
        {
            for (int i = 0; i < movedStrokes.Count; ++i)
                for (int j = 0; j < newStrokeReferences.Count; ++j)
                    if (newStrokeReferences[j].oldStroke == movedStrokes[i].stroke)
                        movedStrokes[i].stroke = newStrokeReferences[j].newStroke;
        }

        public UndoMoveStrokes(List<MovedStroke> movedStrokes, UndoRedoSystem containingSystem) : base(containingSystem)
        {
            this.movedStrokes = movedStrokes;
        }
    }

    public sealed class UndoRecolorStrokes : UndoObject
    {
        List<RecoloredStroke> recoloredStrokes;
        Color newColor;

        public override void Undo()
        {
            foreach (RecoloredStroke s in recoloredStrokes)
            {
                InkDrawingAttributes attrs = s.stroke.DrawingAttributes;
                attrs.Color = s.oldColor;
                s.stroke.DrawingAttributes = attrs;
            }
        }

        public override void Redo()
        {
            foreach (RecoloredStroke s in recoloredStrokes)
            {
                InkDrawingAttributes attrs = s.stroke.DrawingAttributes;
                attrs.Color = newColor;
                s.stroke.DrawingAttributes = attrs;
            }
        }

        public override void UpdateReferences(List<NewStrokereference> newStrokeReferences)
        {
            for (int i = 0; i < recoloredStrokes.Count; ++i)
                for (int j = 0; j < newStrokeReferences.Count; ++j)
                    if (newStrokeReferences[j].oldStroke == recoloredStrokes[i].stroke)
                        recoloredStrokes[i].stroke = newStrokeReferences[j].newStroke;
        }

        public UndoRecolorStrokes(List<RecoloredStroke> recoloredStrokes, Color newColor, UndoRedoSystem containingSystem) : base(containingSystem)
        {
            this.recoloredStrokes = recoloredStrokes;
            this.newColor = newColor;
        }
    }
}