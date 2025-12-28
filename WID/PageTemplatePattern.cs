using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Media.Protection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace WID
{
    public class PageMarginReactive
    {
        private float _left, _top, _right, _bottom;
        private bool _hasLeft, _hasTop, _hasRight, _hasBottom;

        public float left
        {
            get => _left;
            set
            {
                if (_left != value)
                {
                    _left = value;
                    MarginChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public float top
        {
            get => _top;
            set
            {
                if (_top != value)
                {
                    _top = value;
                    MarginChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public float right
        {
            get => _right;
            set
            {
                if (_right != value)
                {
                    _right = value;
                    MarginChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public float bottom
        {
            get => _bottom;
            set
            {
                if (_bottom != value)
                {
                    _bottom = value;
                    MarginChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public bool hasLeft
        {
            get => _hasLeft;
            set
            {
                if (_hasLeft != value)
                {
                    _hasLeft = value;
                    MarginChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public bool hasTop
        {
            get => _hasTop;
            set
            {
                if (_hasTop != value)
                {
                    _hasTop = value;
                    MarginChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public bool hasRight
        {
            get => _hasRight;
            set
            {
                if (_hasRight != value)
                {
                    _hasRight = value;
                    MarginChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public bool hasBottom
        {
            get => _hasBottom;
            set
            {
                if (_hasBottom != value)
                {
                    _hasBottom = value;
                    MarginChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [JsonIgnore]
        public EventHandler? MarginChanged;

        public PageMarginReactive()
        {
            hasLeft = hasTop = hasRight = hasBottom = false;
            left = top = right = bottom = 0.2f;
        }

        public PageMarginReactive(bool hasMargins)
        {
            hasLeft = hasTop = hasRight = hasBottom = hasMargins;
            left = top = right = bottom = 0.2f;
        }

        public PageMarginReactive(float margin)
        {
            hasLeft = hasTop = hasRight = hasBottom = true;
            left = top = right = bottom = margin;
        }
    }

    public class PageTemplatePattern
    {
        protected Windows.UI.Color objectColor { get; } = Windows.UI.Colors.Gray;
        private PageMarginReactive _margin;
        public PageMarginReactive margin
        {
            get => _margin;
            set
            {
                if (_margin != value)
                {
                    _margin = value;
                    _margin.MarginChanged += (s, e) => TemplatePropertiesChanged?.Invoke(this, EventArgs.Empty);
                    TemplatePropertiesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private double _desSpacing;
        public double desiredSpacing
        {
            get => _desSpacing;
            set
            {
                if (_desSpacing != value)
                {
                    _desSpacing = value;
                    TemplatePropertiesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private PatternType _type;
        public PatternType type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    TemplatePropertiesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [JsonIgnore]
        public EventHandler? TemplatePropertiesChanged { get; set; }

        public PageTemplatePattern()
        {
            this.type = PatternType.Empty;
            desiredSpacing = float.PositiveInfinity;
            margin = new PageMarginReactive(false);
            _margin = margin;
        }

        public PageTemplatePattern(PatternType type, double spacing)
        {
            this.type = type;
            desiredSpacing = spacing;
            margin = new PageMarginReactive(false);
            _margin = margin;
        }

        public void DrawOnCanvas(CanvasControl c, CanvasDrawEventArgs args)
        {
            switch (_type) {
                case PatternType.Lines:
                    DrawLinesOnCanvas(c, args);
                    break;
                case PatternType.Grid:
                    DrawGridOnCanvas(c, args);
                    break;
                case PatternType.Dots:
                    DrawDotsOnCanvas(c, args);
                    break;
            }
            DrawMargins(c, args);
        }

        protected void DrawMargins(CanvasControl c, CanvasDrawEventArgs args)
        {
            float lineWidth = (float)c.ActualHeight * 0.001f;

            float actualWidthFloat = (float)c.ActualWidth;
            float actualHeightFloat = (float)c.ActualHeight;

            if (margin.hasLeft)
            {
                float xPos = actualWidthFloat * margin.left;
                args.DrawingSession.DrawLine(
                    new System.Numerics.Vector2(xPos, 0f),
                    new System.Numerics.Vector2(xPos, actualHeightFloat),
                    Windows.UI.Colors.Red,
                    lineWidth
                    );
            }
            if (margin.hasTop)
            {
                float yPos = actualHeightFloat * margin.top;
                args.DrawingSession.DrawLine(
                    new System.Numerics.Vector2(0f, yPos),
                    new System.Numerics.Vector2(actualWidthFloat, yPos),
                    Windows.UI.Colors.Red,
                    lineWidth
                    );
            }
            if (margin.hasRight)
            {
                float xPos = actualWidthFloat * (1f - margin.right);
                args.DrawingSession.DrawLine(
                    new System.Numerics.Vector2(xPos, 0f),
                    new System.Numerics.Vector2(xPos, actualHeightFloat),
                    Windows.UI.Colors.Red,
                    lineWidth
                    );
            } if (margin.hasBottom)
            {
                float yPos = actualHeightFloat * (1f - margin.bottom);
                args.DrawingSession.DrawLine(
                    new System.Numerics.Vector2(0f, yPos),
                    new System.Numerics.Vector2(actualWidthFloat, yPos),
                    Windows.UI.Colors.Red,
                    lineWidth
                    );
            }
        }

        protected float CalculateVerticalOffset(double pageHeight, int objectsToDrawHorizontally)
        {
            float distanceToMargin = (float)(pageHeight - objectsToDrawHorizontally * desiredSpacing);
            return distanceToMargin / 2f;
        }

        public void DrawLinesOnCanvas(CanvasControl c, CanvasDrawEventArgs args)
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

        public void DrawGridOnCanvas(CanvasControl c, CanvasDrawEventArgs args)
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

        public void DrawDotsOnCanvas(CanvasControl c, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Antialiasing = CanvasAntialiasing.Antialiased;

            float dotRadius = (float)((c.ActualWidth + c.ActualHeight) * 0.0005f);

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
                    args.DrawingSession.FillCircle(
                        new System.Numerics.Vector2(xPos, yPos + yOffset),
                        dotRadius,
                        objectColor
                        );
                }
            }
        }
    }

    public enum PatternType
    {
        Empty,
        Lines,
        Grid,
        Dots,
    }
}
