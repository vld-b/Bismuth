using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Animation;
using WinRT;

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a <see cref="Frame">.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public StorageFolder notes => ApplicationData.Current.LocalFolder;
        public MainPage()
        {
            InitializeComponent();
            SetTitlebar();
            LoadNotebooks();
        }

        private void SetTitlebar()
        {
            Window.Current.SetTitleBar(TitleBar);
            tbAppTitle.Text = AppInfo.Current.DisplayInfo.DisplayName;
        }

        private void ResizeMenuElements(object sender, SizeChangedEventArgs e)
        {
            if (gvNotebooks.ItemsPanelRoot is ItemsWrapGrid panel)
            {
                double minWidth = 288;
                double maxWidth = 480;

                int columns = Math.Max(1, (int)(e.NewSize.Width / minWidth));

                double itemWidth = e.NewSize.Width / columns;

                itemWidth = Math.Max(minWidth, Math.Min(maxWidth, itemWidth));

                panel.ItemWidth = itemWidth;
                panel.ItemHeight = double.NaN;
            }
        }

        private async Task LoadNotebooks()
        {
            gvNotebooks.Items.Clear();

            IReadOnlyList<StorageFolder> folders = await notes.GetFoldersAsync();
            foreach (StorageFolder folder in folders)
            {
                MenuElement newItem;

                if (folder.Name.EndsWith(".notebook"))
                    newItem = new MenuElement(folder.Name[..(folder.Name.Length-9)], false);
                else
                    newItem = new MenuElement(folder.Name, true);

                gvNotebooks.Items.Add(newItem);
            }
        }

        private async void CreateNewNotebook(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            TextBox txtbox = new TextBox
            {
                PlaceholderText = "Enter name for notebook",
                AcceptsReturn = false,
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = "Create new notebook",
                Content = txtbox,
                PrimaryButtonText = "Create",
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
                    Content = "Empty file names are not supported",
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                };
                await dialogNoName.ShowAsync();
                return;
            }

            try
            {
                StorageFolder newNotebook = await notes.CreateFolderAsync(txtbox.Text + ".notebook", CreationCollisionOption.FailIfExists);
                gvNotebooks.Items.Add(new MenuItem(false, newNotebook.DisplayName[..(newNotebook.DisplayName.Length-9)], newNotebook, Frame));
            }
            catch
            {
                ContentDialog dialogFailed = new ContentDialog
                {
                    Title = "Failed to create notebook",
                    Content = "A notebook with the same name already exists",
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                };
                await dialogFailed.ShowAsync();
            }
        }

        private async void CreateNewFolder(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            TextBox txtbox = new TextBox
            {
                PlaceholderText = "Enter name for folder",
                AcceptsReturn = false,
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = "Create new folder",
                Content = txtbox,
                PrimaryButtonText = "Create",
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
            } else if (res == ContentDialogResult.Primary && txtbox.Text.EndsWith(".notebook"))
            {
                ContentDialog dialogInvalidEnding = new ContentDialog
                {
                    Title = "Invalid ending entered",
                    Content = "Folders with the ending '.notebook' are considered notebooks",
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                };
                await dialogInvalidEnding.ShowAsync();
                return;
            }

            try
            {
                StorageFolder newFolder = await notes.CreateFolderAsync(txtbox.Text, CreationCollisionOption.FailIfExists);
                gvNotebooks.Items.Add(new MenuItem(true, newFolder.Name, newFolder, Frame));
            }
            catch
            {
                ContentDialog dialogFailed = new ContentDialog
                {
                    Title = "Failed to create notebook",
                    Content = "A notebook with the same name already exists",
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                };
                await dialogFailed.ShowAsync();
            }
        }
    }
}
