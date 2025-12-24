using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                    chosenPattern = new LinesPagePattern(slTemplateSpacing.Value);
                    break;
                case "Grid":
                    tbSpacingLabel.Text = "Grid spacing";
                    chosenPattern = new GridPagePattern(slTemplateSpacing.Value);
                    break;
                case "Dots":
                    tbSpacingLabel.Text = "Dot spacing";
                    chosenPattern = new DotsPagePattern(slTemplateSpacing.Value);
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

        private void MarginsChecked(object sender, RoutedEventArgs e)
        {
            spMarginOptions.Opacity = 1d;
            spMarginOptions.IsHitTestVisible = true;
        }

        private void MarginsUnchecked(object sender, RoutedEventArgs e)
        {
            spMarginOptions.Opacity = 0d;
            spMarginOptions.IsHitTestVisible = false;
        }
    }
}
