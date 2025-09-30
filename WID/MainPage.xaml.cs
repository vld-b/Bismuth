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
            lvNotebooks.Items.Clear();

            IReadOnlyList<StorageFolder> folders = await notes.GetFoldersAsync();
            foreach (StorageFolder folder in folders)
            {
                lvNotebooks.Items.Add(new NotebookItem(folder.Name, true));
            }

            IReadOnlyList<StorageFile> notebooks = await notes.GetFilesAsync();
            foreach (StorageFile nb in notebooks)
            {
                lvNotebooks.Items.Add(new NotebookItem(nb.DisplayName, false));
            }
        }

        private async void CreateNewNotebook(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            //StorageFile newNotebook = await notes.CreateFileAsync("Test.gif", CreationCollisionOption.GenerateUniqueName);
            //lvNotebooks.Items.Add(new NotebookItem(newNotebook.DisplayName, false));
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

            await dialog.ShowAsync();

            try
            {
                StorageFile newNotebook = await notes.CreateFileAsync(txtbox.Text, CreationCollisionOption.FailIfExists);
            } catch
            {
                ContentDialog dialogFailed = new ContentDialog
                {
                    Title = "Failed to create file",
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
            lvNotebooks.Items.Insert(index, new NotebookItem(newFolder.DisplayName, true));
        }

        private void DeleteFolderOrFile(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            NotebookItem? nbItem = ((Button)sender).DataContext as NotebookItem;
            if (nbItem == null) return;
            if (nbItem.IsFolder)
            {
                Directory.Delete(notes.Path + "\\" + nbItem.Name.Replace("(Folder) ", ""), true);
                lvNotebooks.Items.Remove(nbItem);
            } else
            {
                File.Delete(notes.Path + "\\" + nbItem.fileName);
                lvNotebooks.Items.Remove(nbItem);
            }
        }

        private async void OpenNote(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            NotebookItem? nbItem = ((Button)sender).DataContext as NotebookItem;
            if (nbItem == null) return;
            Frame.Navigate(typeof(CanvasPage), await notes.GetFileAsync(nbItem.fileName), new DrillInNavigationTransitionInfo());
        }
    }
}
