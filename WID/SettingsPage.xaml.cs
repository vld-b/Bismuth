using AppSettings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private readonly Dictionary<string, CoreInputDeviceTypes> inputDeviceTypes = new Dictionary<string, CoreInputDeviceTypes>
        {
            ["Mouse"] = CoreInputDeviceTypes.Mouse,
            ["Pen"] = CoreInputDeviceTypes.Pen,
            ["Touch"] = CoreInputDeviceTypes.Touch,
        };

        private readonly Dictionary<string, HomeScreenThumbnailSize> homescreenThumbnailSizes = new Dictionary<string, HomeScreenThumbnailSize>
        {
            ["Small"] = HomeScreenThumbnailSize.Small,
            ["Medium"] = HomeScreenThumbnailSize.Medium,
            ["Large"] = HomeScreenThumbnailSize.Large,
        };

        private readonly Dictionary<string, UndoRedoButtonsPlacement> undoRedoButtonsPlacement = new Dictionary<string, UndoRedoButtonsPlacement>
        {
            ["Top left"] = UndoRedoButtonsPlacement.TopLeft,
            ["Top right"] = UndoRedoButtonsPlacement.TopRight,
            ["Bottom left"] = UndoRedoButtonsPlacement.BottomLeft,
            ["Bottom right"] = UndoRedoButtonsPlacement.BottomRight,
        };

        private readonly Dictionary<string, InkToolbarPlacement> inkToolbarPlacement = new Dictionary<string, InkToolbarPlacement>
        {
            ["Top"] = InkToolbarPlacement.Top,
            ["Bottom"] = InkToolbarPlacement.Bottom,
        };

        private readonly List<string> inkLanguageRecognizers = new List<string>();

        public SettingsPage()
        {
            this.InitializeComponent();

            cbInputMouse.IsChecked = (App.AppSettings.inputDevices & CoreInputDeviceTypes.Mouse) != 0;
            cbInputPen.IsChecked = (App.AppSettings.inputDevices & CoreInputDeviceTypes.Pen) != 0;
            cbInputTouch.IsChecked = (App.AppSettings.inputDevices & CoreInputDeviceTypes.Touch) != 0;

            string homescreenThumbnailSize = homescreenThumbnailSizes.FirstOrDefault(x => x.Value == App.AppSettings.homescreenThumbnailSize).Key;
            foreach (ComboBoxItem item in cbxHomeScreenThumbnailSize.Items)
                if ((string)item.Tag == homescreenThumbnailSize)
                    cbxHomeScreenThumbnailSize.SelectedItem = item;

            cbxUndoRedoButtonsPlacement.SelectedItem = undoRedoButtonsPlacement.FirstOrDefault(x => x.Value == App.AppSettings.undoRedoButtonsPlacement).Key;
            cbxInkToolbarPlacement.SelectedItem = inkToolbarPlacement.FirstOrDefault(x => x.Value == App.AppSettings.inkToolbarPlacement).Key;

            foreach (InkRecognizer rec in (new InkRecognizerContainer()).GetRecognizers())
                inkLanguageRecognizers.Add(rec.Name);
            cbxDefaultInkLanguage.ItemsSource = inkLanguageRecognizers;
            cbxDefaultInkLanguage.SelectedItem = App.AppSettings.defaultInkLanguage;

            tsSelectWithRightClick.IsOn = App.AppSettings.selectWithRightClick == InkInputRightDragAction.LeaveUnprocessed;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            App.AppSettings.RequestSave();
        }

        private void InputDeviceChecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            App.AppSettings.inputDevices |= inputDeviceTypes[(string)cb.Tag];
        }

        private void InputDeviceUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            App.AppSettings.inputDevices &= ~(inputDeviceTypes[(string)cb.Content]);
        }

        private void ChangeHomescreenThumbnailSize(object sender, SelectionChangedEventArgs e)
        {
            string chosenOption = (string)((ComboBoxItem)e.AddedItems[0]).Tag;
            if (chosenOption != homescreenThumbnailSizes.FirstOrDefault(x => x.Value == App.AppSettings.homescreenThumbnailSize).Key)
                App.AppSettings.homescreenThumbnailSize = homescreenThumbnailSizes[chosenOption];
        }

        private void ChangeUndoRedoButtonsPlacement(object sender, SelectionChangedEventArgs e)
        {
            App.AppSettings.undoRedoButtonsPlacement = undoRedoButtonsPlacement[(string)e.AddedItems[0]];
        }

        private void ChangeInkToolbarPlacement(object sender, SelectionChangedEventArgs e)
        {
            App.AppSettings.inkToolbarPlacement = inkToolbarPlacement[(string)e.AddedItems[0]];
        }

        private void ChangeDefaultInkLanguage(object sender, SelectionChangedEventArgs e)
        {
            App.AppSettings.defaultInkLanguage = (string)e.AddedItems[0];
        }

        private async void GetMoreInkLanguages(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:regionlanguage"));
        }

        private void ChangeSelectWithRightClick(object sender, RoutedEventArgs e)
        {
            App.AppSettings.selectWithRightClick = tsSelectWithRightClick.IsOn ? InkInputRightDragAction.LeaveUnprocessed : InkInputRightDragAction.AllowProcessing;
        }
    }
}
