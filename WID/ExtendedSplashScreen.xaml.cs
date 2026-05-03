using CommunityToolkit.WinUI.Lottie;
using Microsoft.Graphics.Canvas.UI.Xaml;
using PdfSharpCore.Pdf.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.HumanInterfaceDevice;
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
        private Task? loadingTask;

        private LoadedNotebooks? notebookData;

        public ExtendedSplashScreen()
        {
            this.InitializeComponent();
            this.Loaded += StartAnimation;
        }

        private async Task<List<SearchableNotebook>> LoadSearchableNotebooks(StorageFolder searchingFolder)
        {
            List<SearchableNotebook> notebooks = new List<SearchableNotebook>();

            IReadOnlyList<StorageFolder> folders = await searchingFolder.GetFoldersAsync();
            foreach (StorageFolder folder in folders)
            {
                if (folder.Name.EndsWith(".notebook"))
                {
                    NotebookConfig config = NotebookUpgrader.UpgradeToLastVersion((await NotebookConfig.DeserializeFile(folder))!);
                    notebooks.Add(await SearchableNotebook.FromConfig(config, folder));
                }else
                {
                    notebooks.Add(await LoadSearchableNotebooks(folder));
                }
            }

            return notebooks;
        }

        private async Task LoadUserData()
        {
            LoadedNotebooks noteData = new LoadedNotebooks(ApplicationData.Current.LocalFolder, Frame);

            App.SearchableNotebooks = await LoadSearchableNotebooks(ApplicationData.Current.LocalFolder);

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
                noteData.notebooks.Add(new NotebookData(folder, null, null));
            }

            foreach (MenuElement notebook in notebookElements)
            {
                NotebookPage currentPage = new NotebookPage();
                StorageFolder notebookDir = await ApplicationData.Current.LocalFolder.GetFolderAsync(notebook.itemName + ".notebook");
                NotebookConfig? config;
                config = NotebookUpgrader.UpgradeToLastVersion((await NotebookConfig.DeserializeFile(notebookDir))!);

                await currentPage.LoadLastPageFromConfig(config!, notebookDir);

                if (currentPage.hasBg)
                    noteData.notebooks.Add(
                        new NotebookData(
                            notebook,
                            currentPage.bgImage,
                            currentPage.canvas.InkPresenter.StrokeContainer
                            )
                        );
                else
                {
                    noteData.notebooks.Add(
                        new NotebookData(
                            notebook,
                            currentPage.canvas.InkPresenter.StrokeContainer,
                            currentPage.templateCanvas,
                            currentPage.Width,
                            currentPage.Height
                            )
                        );
                    currentPage.templateCanvas = null;
                }
            }

            notebookData = noteData;
        }

        private async void StartAnimation(object sender, RoutedEventArgs e)
        {
            loadingTask = LoadUserData();
            await apStartupAnim.PlayAsync(0, 0, false);
#if !DEBUG
            bool animationHasPlayedOnce = false;
            while (!(loadingTask.IsCompleted && animationHasPlayedOnce))
            { // !loadingTask.IsCompleted || !animationHasPlayedOnce becomes
                //!(loadingTask.IsCompleted && animationHasPlayedOnce) by De Morgan's laws
                animationHasPlayedOnce = true;
                await apStartupAnim.PlayAsync(0, 1, false);
                if (!loadingTask.IsCompleted)
                    await Task.Delay(1000);
                else
                    await Task.Delay(200);
            }
#endif
            await loadingTask;
            Frame.Navigate(typeof(MainPage), notebookData, new DrillInNavigationTransitionInfo());
            notebookData = null;
            Frame.BackStack.Clear();
        }
    }

    public class LoadedNotebooks
    {
        public List<NotebookData> notebooks { get; private set; } = new List<NotebookData>();
        public StorageFolder notesFolder { get; private set; }
        public Frame mainFrame { get; private set; }

        public LoadedNotebooks(StorageFolder notesFolder, Frame mainFrame)
        {
            this.notesFolder = notesFolder;
            this.mainFrame = mainFrame;
        }
    }

    public class NotebookData
    {
        public MenuElement notebook { get; private set; }
        public BitmapImage? bg { get; private set; }
        public InkStrokeContainer? ink { get; private set; }
        public CanvasControl? pattern { get; private set; }
        public double width { get; private set; }
        public double height { get; private set; }

        public NotebookData(MenuElement notebook, BitmapImage? bg, InkStrokeContainer? ink)
        {
            this.notebook = notebook;
            this.bg = bg;
            this.ink = ink;
        }

        public NotebookData(MenuElement notebook, InkStrokeContainer? ink, CanvasControl? pattern, double width, double height)
        {
            this.notebook = notebook;
            this.ink = ink;
            this.pattern = pattern;
            this.width = width;
            this.height = height;
        }
    }

    public class SearchableNotebook
    {
        public List<SearchableNotebookPage> pages;
        public StorageFolder notebookFolder;

        public SearchableNotebook(List<SearchableNotebookPage> pages, StorageFolder notebookFolder)
        {
            this.pages = pages;
            this.notebookFolder = notebookFolder;
        }

        public static async Task<SearchableNotebook> FromConfig(NotebookConfig config, StorageFolder folder)
        {
            List<SearchableNotebookPage> pages = new List<SearchableNotebookPage>();

            foreach (PageConfig pageConfig in config.pageMapping)
            {
                RecognizedTextCollection recTextCollection = new RecognizedTextCollection();

                string possibleTextFileName = "recText" + (pageConfig.id == 0 ? "" : (" (" + pageConfig.id + ")")) + ".json";
                if (File.Exists(folder.Path + "\\" + possibleTextFileName))
                {
                    recTextCollection = (await RecognizedTextCollection.DeserializeFile(await folder.GetFileAsync(possibleTextFileName)))!;
                }
                pages.Add(new SearchableNotebookPage(recTextCollection, pageConfig.id));
            }

            return new SearchableNotebook(pages, folder);
        }
    }

    public class SearchableNotebookPage
    {
        public RecognizedTextCollection recTextCol;
        public int pageId;

        public SearchableNotebookPage(RecognizedTextCollection recTextCol, int pageId)
        {
            this.recTextCol = recTextCol;
            this.pageId = pageId;
        }
    }
}
