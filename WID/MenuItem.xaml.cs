using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public sealed partial class MenuItem : Grid, INotifyPropertyChanged
    {
        public bool isFolder { get; private set; }
        private string _itemName;
        public string ItemName
        {
            get => _itemName;
            set
            {
                if (_itemName != value)
                {
                    _itemName = value;
                    OnPropertyChanged(nameof(ItemName));
                }
            }
        }
        public EventHandler? RequestDeleteItem { get; set; }
        public StorageFolder representingFolder { get; private set; }
        public Frame mainPage { get; private set; }
        public Button btOpen { get; private set; }

        public MenuItem(bool isFolder, string itemName, StorageFolder representingFolder, Frame mainPage)
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.isFolder = isFolder;
            this.ItemName = itemName;
            this.representingFolder = representingFolder;
            this.mainPage = mainPage;
            btOpen = btClick;
            if (!isFolder)
            {
                grItemPreview.Children.Clear();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void OpenItem(object sender, RoutedEventArgs e)
        {
            if (isFolder)
            {

            } else
            {
                Debug.WriteLine(Dispatcher.HasThreadAccess);
                Debug.WriteLine(this.representingFolder.Name);
                Debug.WriteLine(mainPage.Name);
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => mainPage.Navigate(typeof(CanvasPage), representingFolder, new DrillInNavigationTransitionInfo()));
            }
        }

        private async void DeleteItem(object sender, RoutedEventArgs e)
        {
            flButton.Hide();
            ContentDialog dialog = Utils.ShowLoadingPopup("Deleting item");
            await representingFolder.DeleteAsync();
            RequestDeleteItem?.Invoke(sender, new EventArgs());
            dialog.Hide();
        }

        private async void RenameItem(object sender, RoutedEventArgs e)
        {
            flButton.Hide();
            string fileType = isFolder ? "folder" : "notebook";

            TextBox txtbox = new TextBox
            {
                PlaceholderText = "Enter new name for "+fileType,
                AcceptsReturn = false,
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = "Rename "+fileType,
                Content = txtbox,
                PrimaryButtonText = "Rename",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            };

            ContentDialogResult res = await dialog.ShowAsync();

            if (res == ContentDialogResult.None)
                return;
            else if (res == ContentDialogResult.Primary && txtbox.Text == string.Empty)
            {
                ContentDialog dialogNoName = new ContentDialog
                {
                    Title = "No name entered",
                    Content = "Empty folder names are not supported",
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                };
                await dialogNoName.ShowAsync();
                return;
            }
            else if (res == ContentDialogResult.Primary && txtbox.Text.EndsWith(".notebook"))
            {
                ContentDialog dialogInvalidEnding = new ContentDialog
                {
                    Title = "Invalid ending entered",
                    Content = "Renaming items with ending '.notebook' is not safe",
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                };
                await dialogInvalidEnding.ShowAsync();
                return;
            }

            try
            {
                await representingFolder.RenameAsync(isFolder ? txtbox.Text : (txtbox.Text+".notebook"));
                this.ItemName = txtbox.Text;
            }
            catch
            {
                ContentDialog dialogFailed = new ContentDialog
                {
                    Title = "Failed to rename "+fileType,
                    Content = "A " + fileType + " with the same name already exists",
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                };
                await dialogFailed.ShowAsync();
            }
        }
    }
}
