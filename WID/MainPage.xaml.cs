using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using WinRT;

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a <see cref="Frame">.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public StorageFolder notes = ApplicationData.Current.LocalFolder;
        public MainPage()
        {
            InitializeComponent();
            SetTitlebar();

            nvMainNavigation.SelectedItem = nvMainNavigation.MenuItems[0];
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            frMainMenu.Navigate(
                typeof(NotebookList),
                e.Parameter,
                new SuppressNavigationTransitionInfo()
                );
        }

        private void SetTitlebar()
        {
            Window.Current.SetTitleBar(TitleBar);
        }

        private void SwitchPage(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is Microsoft.UI.Xaml.Controls.NavigationViewItem item)
            {
                switch (item.Tag)
                {
                    case "notebooksPage":
                        frMainMenu.Navigate(
                            typeof(NotebookList),
                            new FolderNavigationData(null, Frame),
                            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight }
                            );
                        break;
                    case "Settings":
                        frMainMenu.Navigate(
                            typeof(SettingsPage),
                            null,
                            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight }
                            );
                        break;
                }
            }
        }
    }
}
