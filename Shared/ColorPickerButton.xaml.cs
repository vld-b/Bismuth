using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Shared
{
    public delegate void RemoveColorEvent(ColorPickerButton button, EventArgs args);
    public delegate void ChangeColorEvent(ColorPickerButton sender, Color color);

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

        private readonly InkToolbar inkToolbar;

        public RemoveColorEvent? RemoveColor;
        public ChangeColorEvent? ChangeColor;

        SimpleColorPicker parent;

        public bool hasBeenSelected = false;

        public ColorPickerButton(SolidColorBrush Fill, InkToolbar inkToolbar, SimpleColorPicker parent)
        {
            this.InitializeComponent();
            this.Fill = Fill;
            colorPicker.Color = Fill.Color;
            this.inkToolbar = inkToolbar;
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
                bt.BorderThickness = new Thickness(0);
                if (bt != this)
                    bt.hasBeenSelected = false;
            }
            BorderThickness = new Thickness(4);
            if (!hasBeenSelected)
            {
                flyout.ShowAt(this);
                flyout.Hide();
                hasBeenSelected = true;
            }

            ChangeColor?.Invoke(this, Fill.Color);
        }

        private void ChooseNewColor(Microsoft.UI.Xaml.Controls.ColorPicker sender, Microsoft.UI.Xaml.Controls.ColorChangedEventArgs args)
        {
            this.Fill.Color = args.NewColor;
            ChangeColor?.Invoke(this, args.NewColor);
        }
    }
}
