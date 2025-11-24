using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;

namespace WID
{
    public abstract class PageTemplatePattern
    {
        public List<ItemsRepeater> patterns { get; private set; }

        public PageTemplatePattern()
        {
            patterns = new List<ItemsRepeater>();
        }
    }

    public class LinesPagePattern : PageTemplatePattern
    {
        public LinesPagePattern(double pageHeight, double spacing) : base()
        {
            List<Line> lines = new List<Line>();
            StackLayout layout = new StackLayout()
            {
                Orientation = Windows.UI.Xaml.Controls.Orientation.Vertical,
                Spacing = spacing,
            };
            ItemsRepeater linesContainer = new ItemsRepeater()
            {
                ItemsSource = lines,
                Margin = new Windows.UI.Xaml.Thickness(0d, spacing * 2d, 0d, 0d),
                Layout = layout,
            };

            for (int i = 0; i < pageHeight/spacing; ++i)
            {
            }
        }
    }
}
