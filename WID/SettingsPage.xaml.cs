using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
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

        public SettingsPage()
        {
            this.InitializeComponent();

            cbInputMouse.IsChecked = (App.AppSettings.inputDevices & CoreInputDeviceTypes.Mouse) != 0;
            cbInputPen.IsChecked = (App.AppSettings.inputDevices & CoreInputDeviceTypes.Pen) != 0;
            cbInputTouch.IsChecked = (App.AppSettings.inputDevices & CoreInputDeviceTypes.Touch) != 0;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            App.AppSettings.SaveSettingsSafe();
        }

        private void InputDeviceChecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            App.AppSettings.inputDevices |= inputDeviceTypes[(string)cb.Content];
        }

        private void InputDeviceUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            App.AppSettings.inputDevices &= ~(inputDeviceTypes[(string)cb.Content]);
        }
    }
}
