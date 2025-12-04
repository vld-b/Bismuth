using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace WID
{
    public abstract class PageTemplatePattern
    {
        public int desiredSpacing { get; private set; }

        public PageTemplatePattern(int spacing)
        {
            desiredSpacing = spacing;
        }

        public abstract void DrawOnCanvas(CanvasVirtualControl c, CanvasDrawingSession ds);
    }

    public class LinesPagePattern : PageTemplatePattern
    {
        public LinesPagePattern(int spacing) : base(spacing) { }

        public override void DrawOnCanvas(CanvasVirtualControl c, CanvasDrawingSession ds)
        {
            ds.Antialiasing = CanvasAntialiasing.Antialiased;
            int linesToDrawHorizontally = (int)c.ActualHeight / desiredSpacing;
            float lineWidth = (float)c.ActualHeight * 0.001f;

            for (int i = 0; i < linesToDrawHorizontally; ++i)
            {
                float yPos = desiredSpacing * (i + 1);
                ds.DrawLine(
                    new System.Numerics.Vector2(0, yPos),
                    new System.Numerics.Vector2((float)c.ActualWidth, yPos),
                    new CanvasSolidColorBrush(c, Windows.UI.Colors.Black),
                    lineWidth
                    );
            }
        }
    }

    //public class GridPagePattern : PageTemplatePattern
    //{
    //    public GridPagePattern(int spacing): base(spacing) { }

    //    public override void DrawOnCanvas(CanvasControl c, CanvasDrawEventArgs args)
    //    {
    //        args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;
    //        int linesToDrawHorizontally = (int)c.ActualHeight / desiredSpacing;
    //        float lineWidth = (float)c.ActualHeight * 0.001f;
    //    }
    //}
}
