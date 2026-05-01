using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [ContentProperty(Name = "Children")]
    public sealed partial class CustomInkToolbarTool : Button
    {
        public static readonly DependencyProperty ChildrenProperty = DependencyProperty.Register(
            nameof(Children),
            typeof(ObservableCollection<UIElement>),
            typeof(CustomInkToolbarTool),
            new PropertyMetadata(null)
            );

        public ObservableCollection<UIElement> Children
        {
            get => (ObservableCollection<UIElement>)GetValue(ChildrenProperty);
            set => SetValue(ChildrenProperty, value);
        }

        private bool _isSelected = false;
        public bool isSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                if (_isSelected)
                {
                    gdContent.Translation = new System.Numerics.Vector3(0f, -4f, 0f);
                    Background = bg;
                } else
                {
                    gdContent.Translation = new System.Numerics.Vector3(0f, 0f, 0f);
                    Background = null;
                }
            }
        }

        LinearGradientBrush bg;

        public CustomInkToolbarTool()
        {
            this.InitializeComponent();

            Children = new ObservableCollection<UIElement>();

            bg = new LinearGradientBrush();
            bg.StartPoint = new Point(.3f, 0);
            bg.EndPoint = new Point(.7f, 1);
            bg.ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation;

            Color accentColor = (Color)Application.Current.Resources["SystemAccentColor"];
            Color startColor = CreateClampedColor(255, accentColor.R + 30, accentColor.G + 30, accentColor.B + 30);
            Color endColor = CreateClampedColor(255, accentColor.R - 10, accentColor.G - 10, accentColor.B - 10);

            bg.GradientStops.Add(new GradientStop { Color = startColor, Offset = 0.0d });
            bg.GradientStops.Add(new GradientStop { Color = endColor, Offset = 1.0d });
        }

        private Color CreateClampedColor(int a, int r, int g, int b)
        {
            return Color.FromArgb((byte)Math.Clamp(a, 0, 255), (byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255));
        }
    }
}
