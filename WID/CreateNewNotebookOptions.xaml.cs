using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRT.WIDVtableClasses;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CreateNewNotebookOptions : Grid
    {
        public string notebookName => tbNotebookName.Text;

        public PageTemplatePattern? chosenPattern;

        public CreateNewNotebookOptions()
        {
            this.InitializeComponent();
        }

        private void ChoosePagePattern(object sender, SelectionChangedEventArgs e)
        {
            string selectedItem = (string)e.AddedItems[0];

            switch (selectedItem)
            {
                case "Empty":
                    spSpacingOptions.Opacity = 0d;
                    spSpacingOptions.IsHitTestVisible = false;
                    chosenPattern = null;
                    npTemplatePreview.currentPattern = null;
                    return;
                case "Lines":
                    tbSpacingLabel.Text = "Line spacing";
                    chosenPattern = new PageTemplatePattern(PatternType.Lines, slTemplateSpacing.Value);
                    break;
                case "Grid":
                    tbSpacingLabel.Text = "Grid spacing";
                    chosenPattern = new PageTemplatePattern(PatternType.Grid, slTemplateSpacing.Value);
                    break;
                case "Dots":
                    tbSpacingLabel.Text = "Dot spacing";
                    chosenPattern = new PageTemplatePattern(PatternType.Dots, slTemplateSpacing.Value);
                    break;
            }
            npTemplatePreview.currentPattern = chosenPattern;

            spSpacingOptions.Opacity = 1d;
            spSpacingOptions.IsHitTestVisible = true;
        }

        private void TemplateSpacingChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (npTemplatePreview.currentPattern != null)
            {
                npTemplatePreview.currentPattern.desiredSpacing = e.NewValue;
            }
        }

        private void ToggleMargins(Microsoft.UI.Xaml.Controls.ToggleSplitButton sender, Microsoft.UI.Xaml.Controls.ToggleSplitButtonIsCheckedChangedEventArgs args)
        {
            if (sender.IsChecked)
            {
                spMarginOptions.Opacity = 1d;
                spMarginOptions.IsHitTestVisible = true;
            }
            else
            {
                spMarginOptions.Opacity = 0d;
                spMarginOptions.IsHitTestVisible = false;
            }
            npTemplatePreview.currentPattern!.margin = new PageMarginReactive(sender.IsChecked);
        }

        private void TemplateMarginsChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (slMarginTop is not null) // Theoretically any other slider could be null too, but checking one is enough
            {
                slMarginLeft.Value = slMarginTop.Value = slMarginRight.Value = slMarginBottom.Value = e.NewValue;
                float newMargin = (float)e.NewValue / 100f;
                npTemplatePreview.currentPattern!.margin.left = newMargin;
                npTemplatePreview.currentPattern!.margin.top = newMargin;
                npTemplatePreview.currentPattern!.margin.right = newMargin;
                npTemplatePreview.currentPattern!.margin.bottom = newMargin;
            }
        }

        private void TemplateMarginToggled(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            switch (cb.Content)
            {
                case "Left":
                    npTemplatePreview.currentPattern!.margin.hasLeft = cb.IsChecked ?? true;
                    break;
                case "Top":
                    npTemplatePreview.currentPattern!.margin.hasTop = cb.IsChecked ?? true;
                    break;
                case "Right":
                    npTemplatePreview.currentPattern!.margin.hasRight = cb.IsChecked ?? true;
                    break;
                case "Bottom":
                    npTemplatePreview.currentPattern!.margin.hasBottom = cb.IsChecked ?? true;
                    break;
            }
        }

        private void TemplateMarginChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!npTemplatePreview.IsLoaded)
                return;
            Slider sl = (Slider)sender;
            float newMargin = (float)e.NewValue / 100f;
            switch (sl.Name)
            {
                case "slMarginLeft":
                    npTemplatePreview.currentPattern!.margin.left = newMargin;
                    break;
                case "slMarginTop":
                    npTemplatePreview.currentPattern!.margin.top = newMargin;
                    break;
                case "slMarginRight":
                    npTemplatePreview.currentPattern!.margin.right = newMargin;
                    break;
                case "slMarginBottom":
                    npTemplatePreview.currentPattern!.margin.bottom = newMargin;
                    break;
            }
        }
    }
}
