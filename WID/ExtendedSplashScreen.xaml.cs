using CommunityToolkit.WinUI.Lottie;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExtendedSplashScreen : Page
    {
        private Task loadingTask;
        private List<NotebookData>? notebookData;
        private bool canNavigate = false;

        public ExtendedSplashScreen()
        {
            this.InitializeComponent();

            loadingTask = LoadUserData();
        }

        private async Task LoadUserData()
        {
            List<NotebookData> notebooks = new List<NotebookData>();

            List<MenuElement> organizationFolders = new List<MenuElement>();
            List<MenuElement> notebookElements = new List<MenuElement>();
            foreach (StorageFolder folder in await ApplicationData.Current.LocalFolder.GetFoldersAsync())
            {
                if (folder.Name.EndsWith(".notebook"))
                    notebookElements.Add(new MenuElement(folder.Name[..(folder.Name.Length - 9)], false));
                else
                    organizationFolders.Add(new MenuElement(folder.Name, true));
            }

            foreach (MenuElement folder in organizationFolders)
            {
                notebooks.Add(new NotebookData(folder, null, null));
            }

            foreach (MenuElement notebook in notebookElements)
            {
                NotebookPage currentPage = new NotebookPage();
                StorageFolder notebookDir = await ApplicationData.Current.LocalFolder.GetFolderAsync(notebook.itemName + ".notebook");
                StorageFile configFile = await notebookDir.GetFileAsync("config.json");
                NotebookConfig? config;
                using (Stream ipStream = await configFile.OpenStreamForReadAsync())
                    config = JsonSerializer.Deserialize(ipStream, NotebookConfigJsonContext.Default.NotebookConfig);

                await currentPage.LoadLastPageFromConfig(config!, notebookDir);

                if (currentPage.hasBg)
                    notebooks.Add(new NotebookData(notebook, currentPage.bgImage, currentPage.canvas.InkPresenter.StrokeContainer));
                else
                    notebooks.Add(new NotebookData(notebook, currentPage.canvas.InkPresenter.StrokeContainer, currentPage.Width, currentPage.Height));
            }

            this.notebookData = notebooks;

            Frame.Navigate(typeof(MainPage), notebookData, new DrillInNavigationTransitionInfo());
        }
    }

    public class NotebookData
    {
        public MenuElement notebook { get; private set; }
        public BitmapImage? bg { get; private set; }
        public InkStrokeContainer? ink { get; private set; }
        public double width { get; private set; }
        public double height { get; private set; }

        public NotebookData(MenuElement notebook, BitmapImage? bg, InkStrokeContainer? ink)
        {
            this.notebook = notebook;
            this.bg = bg;
            this.ink = ink;
        }

        public NotebookData(MenuElement notebook, InkStrokeContainer? ink, double width, double height)
        {
            this.notebook = notebook;
            this.ink = ink;
            this.width = width;
            this.height = height;
        }
    }
}
