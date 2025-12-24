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
    public class PageMargin
    {
        public float left, top, right, bottom;

        public PageMargin()
        {
            left = top = right = bottom = -1f;
        }
    }

    public abstract class PageTemplatePattern
    {
        protected Windows.UI.Color objectColor { get; } = Windows.UI.Colors.Gray;
        public PageMargin margin;

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
            margin = new PageMargin();
        }

        public abstract void DrawOnCanvas(CanvasControl c, CanvasDrawEventArgs args);

        protected float CalculateVerticalOffset(double pageHeight, int objectsToDrawHorizontally)
        {
            float distanceToMargin = (float)(pageHeight - objectsToDrawHorizontally * desiredSpacing);
            return distanceToMargin / 2f;
        }
    }

    public class LinesPagePattern : PageTemplatePattern
    {
        public LinesPagePattern(double spacing) : base(spacing) { }

        public override void DrawOnCanvas(CanvasControl c, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

            int linesToDrawHorizontally = (int)(c.ActualHeight / desiredSpacing);

            float lineWidth = (float)c.ActualHeight * 0.001f;
            float offset = CalculateVerticalOffset(c.ActualHeight, linesToDrawHorizontally);

            float actualWidthFloat = (float)c.ActualWidth;

            for (double i = 0; i < linesToDrawHorizontally; ++i)
            {
                float yPos = (float)(desiredSpacing * (i + 1));
                args.DrawingSession.DrawLine(
                    new System.Numerics.Vector2(0, yPos + offset),
                    new System.Numerics.Vector2(actualWidthFloat, yPos + offset),
                    objectColor,
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
            float yOffset = CalculateVerticalOffset(c.ActualHeight, linesToDrawHorizontally);

            float actualWidthFloat = (float)c.ActualWidth;
            float actualHeightFloat = (float)c.ActualHeight;

            for (double i = 0; i < linesToDrawHorizontally; ++i)
            {
                float yPos = (float)(desiredSpacing * (i + 1));
                args.DrawingSession.DrawLine(
                    new System.Numerics.Vector2(0, yPos + yOffset),
                    new System.Numerics.Vector2(actualWidthFloat, yPos + yOffset),
                    objectColor,
                    lineWidth
                    );

                float xPos = (float)(desiredSpacing * (i + 1));
                args.DrawingSession.DrawLine(
                    new System.Numerics.Vector2(xPos, 0),
                    new System.Numerics.Vector2(xPos, actualHeightFloat),
                    objectColor,
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

            int dotsToDrawVertically = (int)(c.ActualHeight / desiredSpacing);
            int dotsToDrawHorizontally = (int)(c.ActualWidth / desiredSpacing);

            float yOffset = CalculateVerticalOffset(c.ActualHeight, dotsToDrawVertically);

            float actualHeightFloat = (float)c.ActualHeight;
            float actualWidthFloat = (float)c.ActualWidth;

            for (float i = 0; i < dotsToDrawVertically; ++i)
            {
                float yPos = i * (float)desiredSpacing;
                for (float j = 0; j < dotsToDrawHorizontally; ++j)
                {
                    float xPos = j * (float)desiredSpacing;
                    args.DrawingSession.DrawCircle(
                        new System.Numerics.Vector2(xPos, yPos + yOffset),
                        dotRadius,
                        objectColor
                        );
                }
            }
        }
    }
}
