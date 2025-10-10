using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MenuItem : Grid
    {
        public bool isFolder { get; private set; }
        public string itemName { get; private set;  }
        public StorageFolder folder { get; private set; }
        public Frame mainPage { get; private set; }
        public Button btOpen { get; private set; }

        public MenuItem(bool isFolder, string itemName, StorageFolder folder, Frame mainPage)
        {
            this.InitializeComponent();
            this.isFolder = isFolder;
            this.itemName = itemName;
            this.folder = folder;
            this.mainPage = mainPage;
            btOpen = btClick;
            if (!isFolder)
            {
                grItemPreview.Children.Clear();
            }
        }

        private void OpenItem(object sender, RoutedEventArgs e)
        {
            if (isFolder)
            {

            } else
            {
                Debug.WriteLine(Dispatcher.HasThreadAccess);
                Debug.WriteLine(this.folder.Name);
                Debug.WriteLine(mainPage.Name);
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => mainPage.Navigate(typeof(CanvasPage), folder, new DrillInNavigationTransitionInfo()));
            }
        }
    }
}
