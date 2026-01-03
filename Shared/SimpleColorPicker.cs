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
}
