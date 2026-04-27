using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Shared
{
    public delegate void RemoveColorEvent(ColorPickerButton button, EventArgs args);
    public delegate void ChangeColorEvent(ColorPickerButton sender, ChangeColorData changeColorData);

    public partial class ColorPickerButton : Button
    {
        public SolidColorBrush Fill
        {
            get => (SolidColorBrush)rect.Fill;
            set
            {
                if (rect.Fill != value)
                {
                    rect.Fill = value;
                }
            }
        }

        public RemoveColorEvent? RemoveColor;
        public ChangeColorEvent? ChangeColor;

        SimpleColorPicker parent;

        public bool isSelected
        {
            get => BorderThickness == new Thickness(0);
            set
            {
                if (value)
                    BorderThickness = new Thickness(4);
                else
                    BorderThickness = new Thickness(0);
            }
        }

        public int btIndex = 0;

        Storyboard? animBoard;
        ScaleTransform scaleTrans = new ScaleTransform { ScaleX = 0d, ScaleY = 0d };

        public ColorPickerButton(SolidColorBrush Fill, SimpleColorPicker parent)
        {
            this.InitializeComponent();
            RenderTransform = scaleTrans;
            this.Fill = Fill;
            colorPicker.Color = Fill.Color;
            this.parent = parent;
        }

        private void RemoveCurrentColor(object sender, RoutedEventArgs e)
        {
            RemoveColor?.Invoke(this, new EventArgs());
        }

        private void ChangeToCurrentColor(object sender, RoutedEventArgs e)
        {
            foreach (ColorPickerButton bt in parent.Children)
            {
                if (bt != this)
                    bt.isSelected = false;
            }
            if (isSelected)
            {
                flyout.ShowAt(this);
                flyout.Hide();
            }
            isSelected = true;

            ChangeColor?.Invoke(this, new ChangeColorData(Fill.Color, btIndex, false));
        }

        private void ChooseNewColor(Microsoft.UI.Xaml.Controls.ColorPicker sender, Microsoft.UI.Xaml.Controls.ColorChangedEventArgs args)
        {
            this.Fill.Color = args.NewColor;
            ChangeColor?.Invoke(this, new ChangeColorData(args.NewColor, btIndex, true));
        }

        public void AnimateScale(float scale)
        {
            animBoard = new Storyboard();

            DoubleAnimation scaleXAnim = new DoubleAnimation
            {
                From = scaleTrans.ScaleX,
                To = scale,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
            };
            Storyboard.SetTarget(scaleXAnim, scaleTrans);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");

            DoubleAnimation scaleYAnim = new DoubleAnimation
            {
                From = scaleTrans.ScaleX,
                To = scale,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
            };
            Storyboard.SetTarget(scaleYAnim, scaleTrans);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

            animBoard.Children.Add(scaleXAnim);
            animBoard.Children.Add(scaleYAnim);

            animBoard.Begin();
        }
    }

    public class ChangeColorData
    {
        public Color color;
        public int buttonIndex;
        public bool shouldSave;

        public ChangeColorData(Color color, int buttonIndex, bool shouldSave)
        {
            this.color = color;
            this.buttonIndex = buttonIndex;
            this.shouldSave = shouldSave;
        }
    }
}
