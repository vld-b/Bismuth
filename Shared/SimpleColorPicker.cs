using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Shared
{
    public partial class SimpleColorPicker : StackPanel
    {
        public SimpleColorPicker()
        {
            this.Orientation = Orientation.Horizontal;
            this.Spacing = 8d;
        }

        public SimpleColorPicker(List<ColorPickerButton> buttons) : this()
        {
            foreach (ColorPickerButton button in buttons)
            {
                this.Children.Add(button);
            }
        }
    }

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

        private Rectangle rect;

        public ColorPickerButton()
        {
            this.Padding = new Windows.UI.Xaml.Thickness(0);

            rect = new Rectangle
            {
                Width = 32d,
                Height = 32d,
            };
            Fill = new SolidColorBrush(Colors.Transparent);
            this.Content = rect;
        }

        public ColorPickerButton(SolidColorBrush Fill) : this()
        {
            this.Fill = Fill;
        }
    }
}
