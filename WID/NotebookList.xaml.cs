using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input.ForceFeedback;
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
    public sealed partial class NotebookList : Page
    {
        public StorageFolder notes = ApplicationData.Current.LocalFolder;
        private FlyoutBase? currentFlyout;
        private Frame? mainFrame;

        private LoadedNotebooks? userNotebooks;
        private int numberOfFolders;
        private int noteCounter = 0;
        private List<NotebookPage> templates = new List<NotebookPage>();

        public NotebookList()
        {
            InitializeComponent();
        }

        ~NotebookList()
        {
            RemoveTemplates();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is FolderNavigation folderInfo)
            {
                this.mainFrame = folderInfo.mainFrame;
                if (notes != null)
                {
                    btBack.Visibility = Visibility.Visible;
                    this.notes = folderInfo.folder;
                }
                else
                    notes = ApplicationData.Current.LocalFolder;
                LoadNotebooks();
            }
            else if (e.Parameter is LoadedNotebooks notebookData)
            {
                this.notes = notebookData.notesFolder;
                this.mainFrame = notebookData.mainFrame;
                this.userNotebooks = notebookData;
                numberOfFolders = 0;
                while (numberOfFolders < notebookData.notebooks.Count && notebookData.notebooks[numberOfFolders].notebook.isFolder)
                    ++numberOfFolders;

                foreach (NotebookData data in this.userNotebooks.notebooks)
                {
                    gvNotebooks.Items.Add(data.notebook);
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            RemoveTemplates();
        }

        public void RemoveTemplates()
        {
            for (int i = 0; i < templates.Count; ++i)
            {
                templates[i].templateCanvas = null;
            }
        }

        private void ResizeMenuElements(object sender, SizeChangedEventArgs e)
        {
            if (gvNotebooks.ItemsPanelRoot is ItemsWrapGrid panel)
            {
                double minWidth = 432d;
                double maxWidth = 720d;

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
            List<MenuElement> organizationFolders = new List<MenuElement>();
            List<MenuElement> notebooks = new List<MenuElement>();
            foreach (StorageFolder folder in folders)
            {
                if (folder.Name.EndsWith(".notebook"))
                    notebooks.Add(new MenuElement(folder.Name[..(folder.Name.Length - 9)], false));
                else
                    organizationFolders.Add(new MenuElement(folder.Name, true));
            }
            for (int i = 0; i < organizationFolders.Count; ++i)
            {
                gvNotebooks.Items.Add(organizationFolders[i]);
            }
            for (int i = 0; i < notebooks.Count; ++i)
            {
                gvNotebooks.Items.Add(notebooks[i]);
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
            CreateNewNotebookOptions options = new CreateNewNotebookOptions();

            ContentDialog dialog = new ContentDialog
            {
                Title = "Create new notebook",
                Content = options,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            };

            ContentDialogResult res = await dialog.ShowAsync();

            if (res == ContentDialogResult.None)
                return;
            else if (res == ContentDialogResult.Primary && options.notebookName == string.Empty)
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
                StorageFolder newNotebook = await notes.CreateFolderAsync(options.notebookName + ".notebook", CreationCollisionOption.FailIfExists);
                StorageFile file = await newNotebook.CreateFileAsync("config.json", CreationCollisionOption.ReplaceExisting);
                NotebookConfig config = new NotebookConfig(
                    1L,
                    new System.Collections.ObjectModel.ObservableCollection<PageConfig>(),
                    -1,
                    new List<int>(),
                    new LastNotebookState(),
                    -1,
                    new List<int>(),
                    new DefaultTemplate(options.chosenPattern)
                    );
                await config.SerializeToFile(file);
                LoadNotebooks();
                //gvNotebooks.Items.Add(new MenuElement(newNotebook.DisplayName[..(newNotebook.DisplayName.Length - 9)], false));
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
                StorageFolder newFolder = await notes.CreateFolderAsync(txtbox.Text, CreationCollisionOption.FailIfExists);
                LoadNotebooks();
                //gvNotebooks.Items.Add(new MenuElement(newFolder.Name, true));
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
                    Title = "Rename " + (element.isFolder ? "folder" : "notebook"),
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
                    await (await notes.GetFolderAsync(element.itemName + (element.isFolder ? "" : ".notebook"))).RenameAsync(txtbox.Text + (element.isFolder ? "" : ".notebook"));
                    LoadNotebooks();
                    //element.itemName = txtbox.Text;
                }
                catch
                {
                    ContentDialog dialogFailed = new ContentDialog
                    {
                        Title = "Failed to rename " + (element.isFolder ? "folder" : "notebook"),
                        Content = "A " + (element.isFolder ? "folder" : "notebook") + " with the same name already exists",
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
                    await (await notes.GetFolderAsync(element.itemName + (element.isFolder ? "" : ".notebook"))).DeleteAsync();
                    LoadNotebooks();
                    //gvNotebooks.Items.Remove(element);
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

        private async void OpenNotebook(object sender, ItemClickEventArgs e)
        {
            MenuElement item = (MenuElement)e.ClickedItem;
            if (item.isFolder)
                Frame.Navigate(
                    typeof(NotebookList),
                    new FolderNavigation(await notes.GetFolderAsync(item.itemName), mainFrame!),
                    new SlideNavigationTransitionInfo()
                    {
                        Effect = SlideNavigationTransitionEffect.FromRight
                    });
            else
            {
                GridViewItem gvItem = (GridViewItem)((GridView)sender).ContainerFromItem(e.ClickedItem);
                FrameworkElement root = (FrameworkElement)gvItem.ContentTemplateRoot;
                NotebookPage origin = (NotebookPage)root.FindName("npPagePreview");

                //ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(new BasicConnectedAnimationConfiguration());
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("OpenNotebook", origin).Configuration = new BasicConnectedAnimationConfiguration();

                mainFrame!.Navigate(typeof(CanvasPage),
                    await notes.GetFolderAsync(item.itemName + ".notebook"),
                    new DrillInNavigationTransitionInfo()
                    );
            }
        }

        private void GoBackFolder(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        private async void LoadPagePreview(object sender, RoutedEventArgs e)
        {
            NotebookPage preview = (NotebookPage)sender;
            if (userNotebooks is null)
            {
                try
                {
                    StorageFolder configDir = await notes.GetFolderAsync(((MenuElement)preview.DataContext).itemName + ".notebook");
                    NotebookConfig? config;
                    config = await NotebookConfig.DeserializeFile(configDir);
                    if (config is not null)
                        await preview.LoadLastPageFromConfig(config, configDir);
                }
                catch { }
            }
            else
            {
                int currentIndex = numberOfFolders + noteCounter;
                NotebookData currentData = userNotebooks.notebooks[currentIndex];
                preview.inkPres.StrokeContainer = currentData.ink;
                if (currentData.bg is not null)
                    preview.LoadBackground(currentData.bg!);
                else
                {
                    preview.Width = currentData.width;
                    preview.Height = currentData.height;
                    preview.templateCanvas = currentData.pattern;
                    templates.Add(preview);
                }

                ++noteCounter;

                if (currentIndex == (await notes.GetFoldersAsync()).Count - 1) // Free userNoteboos so that it's only used once, on load
                    this.userNotebooks = null;
            }
        }
    }

    public class FolderNavigation
    {
        public StorageFolder folder { get; private set; }
        public Frame mainFrame { get; private set; }

        public FolderNavigation(StorageFolder folder, Frame mainFrame)
        {
            this.folder = folder;
            this.mainFrame = mainFrame;
        }
    }
}