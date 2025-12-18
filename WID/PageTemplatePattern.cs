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
        private double _desSpacing;
        public double desiredSpacing
        {
            get => _desSpacing;
            set
            {
                if (_desSpacing != value)
                {
                    _desSpacing = value;
                    SpacingChanged?.Invoke(this, _desSpacing);
                }
            }
        }

        public EventHandler<double>? SpacingChanged { get; set; }

        public PageTemplatePattern(double spacing)
        {
            desiredSpacing = spacing;
        }

        public abstract void DrawOnCanvas(CanvasControl c, CanvasDrawEventArgs args);
    }

    public class LinesPagePattern : PageTemplatePattern
    {
        public LinesPagePattern(double spacing) : base(spacing) { }

        public override void DrawOnCanvas(CanvasControl c, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

            int linesToDrawHorizontally = (int)(c.ActualHeight / desiredSpacing);

            float lineWidth = (float)c.ActualHeight * 0.001f;

            float actualWidthFloat = (float)c.ActualWidth;

            for (double i = 0; i < linesToDrawHorizontally; ++i)
            {
                float yPos = (float)(desiredSpacing * (i + 1));
                args.DrawingSession.DrawLine(
                    new System.Numerics.Vector2(0, yPos),
                    new System.Numerics.Vector2(actualWidthFloat, yPos),
                    new CanvasSolidColorBrush(c, Windows.UI.Colors.Black),
                    lineWidth
                    );
            }
        }
    }

    public class GridPagePattern : PageTemplatePattern
    {
        public GridPagePattern(double spacing) : base(spacing) { }

        public override void DrawOnCanvas(CanvasControl c, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

            int linesToDrawHorizontally = (int)(c.ActualHeight / desiredSpacing);
            int linesToDrawVertically = (int)(c.ActualWidth / desiredSpacing);

            float lineWidth = (float)c.ActualHeight * 0.001f;

            float actualWidthFloat = (float)c.ActualWidth;
            float actualHeightFloat = (float)c.ActualHeight;

            for (double i = 0; i < linesToDrawHorizontally; ++i)
            {
                float yPos = (float)(desiredSpacing * (i + 1));
                args.DrawingSession.DrawLine(
                    new System.Numerics.Vector2(0, yPos),
                    new System.Numerics.Vector2(actualWidthFloat, yPos),
                    new CanvasSolidColorBrush(c, Windows.UI.Colors.Black),
                    lineWidth
                    );

                float xPos = (float)(desiredSpacing * (i + 1));
                args.DrawingSession.DrawLine(
                    new System.Numerics.Vector2(xPos, 0),
                    new System.Numerics.Vector2(xPos, actualHeightFloat),
                    new CanvasSolidColorBrush(c, Windows.UI.Colors.Black),
                    lineWidth
                    );
            }
        }
    }

    public class DotsPagePattern : PageTemplatePattern
    {
        public DotsPagePattern(double spacing) : base(spacing) { }

        public override void DrawOnCanvas(CanvasControl c, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

            float dotRadius = (float)((c.ActualWidth + c.ActualHeight) * 0.001f);

            int dotsToDrawHorizontally = (int)(c.ActualHeight / desiredSpacing);
            int dotsToDrawVertically = (int)(c.ActualWidth / desiredSpacing);

            float actualHeightFloat = (float)c.ActualHeight;
            float actualWidthFloat = (float)c.ActualWidth;

            for (float i = 0; i < dotsToDrawHorizontally; ++i)
            {
                float yPos = i * (float)desiredSpacing;
                for (float j = 0; j < dotsToDrawVertically; ++j)
                {
                    float xPos = j * (float)desiredSpacing;
                    args.DrawingSession.DrawCircle(
                        new System.Numerics.Vector2(xPos, yPos),
                        dotRadius,
                        new CanvasSolidColorBrush(c, Windows.UI.Colors.Black)
                        );
                }
            }
        }
    }
}
