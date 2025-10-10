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

        private async void LoadNotebooks()
        {
            gvNotebooks.Items.Clear();

            IReadOnlyList<StorageFolder> folders = await notes.GetFoldersAsync();
            foreach (StorageFolder folder in folders)
            {
                MenuItem newItem;

                if (folder.Name.EndsWith(".notebook"))
                    newItem = new MenuItem(false, folder.Name[0..(folder.Name.Length - 9)], folder.Name, Frame);
                else
                    newItem = new MenuItem(true, folder.Name, folder.Name, Frame);

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
                gvNotebooks.Items.Add(new MenuItem(false, newNotebook.DisplayName[..(newNotebook.DisplayName.Length-9)], newNotebook.Name, Frame));
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
            StorageFolder newFolder = await notes.CreateFolderAsync("Test", CreationCollisionOption.GenerateUniqueName);
            int index = (await notes.GetFoldersAsync()).Count - 1;
            gvNotebooks.Items.Insert(index, new MenuItem(true, newFolder.Name, newFolder.Name, Frame));
        }

        //private void DeleteFolder(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        //{
        //    NotebookItem? nbItem = ((Button)sender).DataContext as NotebookItem;
        //    if (nbItem == null) return;
        //    Directory.Delete(notes.Path + "\\" + nbItem.fileName, true);
        //    gvNotebooks.Items.Remove(nbItem);
        //}
    }
}
