using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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
        private FlyoutBase? currentFlyout;
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

        private void FileOptionsFlyoutOpened(object sender, object e)
        {
            currentFlyout = (FlyoutBase)sender;
        }

        private void FileOptionsFlyoutClosed(object sender, object e)
        {
            currentFlyout = null;
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
                gvNotebooks.Items.Add(new MenuElement(newNotebook.DisplayName[..(newNotebook.DisplayName.Length - 9)], false));
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
                gvNotebooks.Items.Add(new MenuElement(newFolder.Name, true));
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

        private async void OpenNotebook(object sender, ItemClickEventArgs e)
        {
            MenuElement item = (MenuElement)e.ClickedItem;
            Frame.Navigate(typeof(CanvasPage), await notes.GetFolderAsync(item.itemName+".notebook"), new DrillInNavigationTransitionInfo());
        }

        private async void RenameItem(object sender, RoutedEventArgs e)
        {
            currentFlyout?.Hide();

            Button btSender = (Button)sender;

            if (btSender.DataContext is MenuElement element)
            {
                TextBox txtbox = new TextBox
                {
                    PlaceholderText = "Enter new name",
                    AcceptsReturn = false,
                };

                ContentDialog dialog = new ContentDialog
                {
                    Title = "Rename "+(element.isFolder ? "folder" : "notebook"),
                    Content = txtbox,
                    PrimaryButtonText = "Rename",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                };

                ContentDialogResult res = await dialog.ShowAsync();

                if (res == ContentDialogResult.None)
                    return;
                else if (res == ContentDialogResult.Primary && txtbox.Text == element.itemName)
                {
                    ContentDialog dialogNoName = new ContentDialog
                    {
                        Title = "Name equals current name",
                        Content = "Rename to another name",
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
                        Content = "Folders with the ending '.notebook' are considered notebooks",
                        PrimaryButtonText = "Ok",
                        DefaultButton = ContentDialogButton.Primary,
                    };
                    await dialogInvalidEnding.ShowAsync();
                    return;
                }

                try
                {
                    await (await notes.GetFolderAsync(element.itemName+(element.isFolder ? "" : ".notebook"))).RenameAsync(txtbox.Text+(element.isFolder ? "" : ".notebook"));
                    element.itemName = txtbox.Text;
                }
                catch
                {
                    ContentDialog dialogFailed = new ContentDialog
                    {
                        Title = "Failed to rename "+(element.isFolder ? "folder" : "notebook"),
                        Content = "A "+(element.isFolder ? "folder" : "notebook")+" with the same name already exists",
                        PrimaryButtonText = "Ok",
                        DefaultButton = ContentDialogButton.Primary,
                    };
                    await dialogFailed.ShowAsync();
                }
            }
        }

        private async void DeleteItem(object sender, RoutedEventArgs e)
        {
            currentFlyout?.Hide();

            Button btSender = (Button)sender;

            if (btSender.DataContext is MenuElement element)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Delete " + (element.isFolder ? "folder" : "notebook") + "?",
                    Content = "This action cannot be undone",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                };

                ContentDialogResult res = await dialog.ShowAsync();

                if (res == ContentDialogResult.None)
                    return;

                try
                {
                    await(await notes.GetFolderAsync(element.itemName + (element.isFolder ? "" : ".notebook"))).DeleteAsync();
                    gvNotebooks.Items.Remove(element);
                }
                catch
                {
                    ContentDialog dialogFailed = new ContentDialog
                    {
                        Title = "Failed to delete " + (element.isFolder ? "folder" : "notebook"),
                        Content = "An error occured",
                        PrimaryButtonText = "Ok",
                        DefaultButton = ContentDialogButton.Primary,
                    };
                    await dialogFailed.ShowAsync();
                }
            }
        }
    }
}
