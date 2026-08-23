using Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Input.Inking;
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
        public string? chosenInkLanguage = null;

        private const string defaultInkLanguageOption = "Default setting";
        private List<string> possibleInkLanguages = new List<string>();

        public CreateNewNotebookOptions()
        {
            this.InitializeComponent();

            AssignMethods();

            possibleInkLanguages.Add(defaultInkLanguageOption);
            foreach (InkRecognizer rec in (new InkRecognizerContainer()).GetRecognizers())
                possibleInkLanguages.Add(rec.Name);
            cbxNotebookInkLanguage.ItemsSource = possibleInkLanguages;
            cbxNotebookInkLanguage.SelectedIndex = 0;
        }

        public void LoadFromConfig(NotebookConfig config, StorageFolder containingFolder)
        {
            UnassignMethods();

            tbNotebookName.IsReadOnly = true;
            tbNotebookName.Text = Utils.GetNotebookNameFromFolder(containingFolder);

            if (config.inkRecognizerLanguage is null)
                cbxNotebookInkLanguage.SelectedItem = defaultInkLanguageOption;
            else
                cbxNotebookInkLanguage.SelectedItem = config.inkRecognizerLanguage;

            chosenPattern = config.defaultTemplate.pattern?.Clone() ?? null;
            npTemplatePreview.currentPattern = chosenPattern;

            if (chosenPattern is null)
                cbxConfigPattern.SelectedItem = "Empty";
            else
            {
                switch (chosenPattern.type)
                {
                    case PatternType.Empty:
                        cbxConfigPattern.SelectedItem = "Empty";
                        break;
                    case PatternType.Lines:
                        cbxConfigPattern.SelectedItem = "Lines";
                        spSpacingOptions.Opacity = 1d;
                        spSpacingOptions.IsHitTestVisible = true;
                        break;
                    case PatternType.Grid:
                        cbxConfigPattern.SelectedItem = "Grid";
                        spSpacingOptions.Opacity = 1d;
                        spSpacingOptions.IsHitTestVisible = true;
                        break;
                    case PatternType.Dots:
                        cbxConfigPattern.SelectedItem = "Dots";
                        spSpacingOptions.Opacity = 1d;
                        spSpacingOptions.IsHitTestVisible = true;
                        break;
                }
                slTemplateSpacing.Value = chosenPattern.desiredSpacing;

                cbMarginTop.IsChecked = chosenPattern.margin.hasTop;
                cbMarginBottom.IsChecked = chosenPattern.margin.hasBottom;
                cbMarginLeft.IsChecked = chosenPattern.margin.hasLeft;
                cbMarginRight.IsChecked = chosenPattern.margin.hasRight;

                tsbHasMargins.IsChecked = (bool)cbMarginTop.IsChecked || (bool)cbMarginBottom.IsChecked || (bool)cbMarginLeft.IsChecked || (bool)cbMarginRight.IsChecked;
                if (tsbHasMargins.IsChecked)
                {
                    spMarginOptions.Opacity = 1d;
                    spMarginOptions.IsHitTestVisible = true;

                    slMarginTop.Value = chosenPattern.margin.top * 100f;
                    slMarginBottom.Value = chosenPattern.margin.bottom * 100f;
                    slMarginLeft.Value = chosenPattern.margin.left * 100f;
                    slMarginRight.Value = chosenPattern.margin.right * 100f;
                }
            }

            AssignMethods();
        }

        public async Task UpdatePreviewTemplateBackground()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, async () =>
            {
                npTemplatePreview.UpdateTemplateBackground();
            });
        }

        private void AssignMethods()
        {
            cbxNotebookInkLanguage.SelectionChanged += ChooseInkLanguage;
            cbxConfigPattern.SelectionChanged += ChoosePagePattern;
            slTemplateSpacing.ValueChanged += TemplateSpacingChanged;
            tsbHasMargins.IsCheckedChanged += ToggleMargins;
            cbMarginTop.Click += TemplateMarginToggled;
            cbMarginBottom.Click += TemplateMarginToggled;
            cbMarginLeft.Click += TemplateMarginToggled;
            cbMarginRight.Click += TemplateMarginToggled;
            slSimpleMargins.ValueChanged += TemplateMarginsChanged;
            slMarginTop.ValueChanged += TemplateMarginChanged;
            slMarginBottom.ValueChanged += TemplateMarginChanged;
            slMarginLeft.ValueChanged += TemplateMarginChanged;
            slMarginRight.ValueChanged += TemplateMarginChanged;
        }

        private void UnassignMethods()
        {
            cbxNotebookInkLanguage.SelectionChanged -= ChooseInkLanguage;
            cbxConfigPattern.SelectionChanged -= ChoosePagePattern;
            slTemplateSpacing.ValueChanged -= TemplateSpacingChanged;
            tsbHasMargins.IsCheckedChanged -= ToggleMargins;
            cbMarginTop.Click -= TemplateMarginToggled;
            cbMarginBottom.Click -= TemplateMarginToggled;
            cbMarginLeft.Click -= TemplateMarginToggled;
            cbMarginRight.Click -= TemplateMarginToggled;
            slSimpleMargins.ValueChanged -= TemplateMarginsChanged;
            slMarginTop.ValueChanged -= TemplateMarginChanged;
            slMarginBottom.ValueChanged -= TemplateMarginChanged;
            slMarginLeft.ValueChanged -= TemplateMarginChanged;
            slMarginRight.ValueChanged -= TemplateMarginChanged;
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
                    if (chosenPattern is null)
                        chosenPattern = new PageTemplatePattern(PatternType.Lines, slTemplateSpacing.Value);
                    chosenPattern.type = PatternType.Lines;
                    break;
                case "Grid":
                    tbSpacingLabel.Text = "Grid spacing";
                    if (chosenPattern is null)
                        chosenPattern = new PageTemplatePattern(PatternType.Grid, slTemplateSpacing.Value);
                    chosenPattern.type = PatternType.Grid;
                    break;
                case "Dots":
                    tbSpacingLabel.Text = "Dot spacing";
                    if (chosenPattern is null)
                        chosenPattern = new PageTemplatePattern(PatternType.Dots, slTemplateSpacing.Value);
                    chosenPattern.type = PatternType.Dots;
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

            if (!sender.IsChecked)
                return;

            npTemplatePreview.currentPattern!.margin.hasTop = (bool)cbMarginTop.IsChecked!;
            npTemplatePreview.currentPattern!.margin.hasBottom = (bool)cbMarginBottom.IsChecked!;
            npTemplatePreview.currentPattern!.margin.hasLeft = (bool)cbMarginLeft.IsChecked!;
            npTemplatePreview.currentPattern!.margin.hasRight = (bool)cbMarginRight.IsChecked!;
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

        private void ChooseInkLanguage(object sender, SelectionChangedEventArgs e)
        {
            string chosenItem = (string)e.AddedItems[0];
            if (chosenItem == defaultInkLanguageOption)
                chosenInkLanguage = null;
            else
                chosenInkLanguage = chosenItem;
        }

        private async void UpdateTemplatePreview(object sender, RoutedEventArgs e)
        {
            await UpdatePreviewTemplateBackground();
        }
    }
}
