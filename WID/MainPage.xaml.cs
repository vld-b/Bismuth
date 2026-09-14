using Shared;
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
        private bool readyToChangePage = false;
        private string searchingFor = "";
        private object? notebookData;

        public MainPage()
        {
            InitializeComponent();
            SetTitlebar();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.Back)
            {
                frMainMenu.Navigate(
                    typeof(NotebookList),
                    new FolderNavigationData(null, Frame),
                    new SuppressNavigationTransitionInfo()
                    );
                nvMainNavigation.SelectedItem = nvMainNavigation.MenuItems[0];
                readyToChangePage = true;
            }
            else if (e.Parameter is LanguageReloadData reloadData)
            {
                notebookData = reloadData.notebookData;
                frMainMenu.Navigate(
                    typeof(SettingsPage),
                    new SettingsNavigationData(LanguageChangeReloadPage, reloadData.settingsPageVerticalOffset),
                    new SuppressNavigationTransitionInfo()
                    );
                if (nvMainNavigation.IsLoaded)
                {
                    nvMainNavigation.SelectedItem = nvMainNavigation.SettingsItem;
                    readyToChangePage = true;
                }
                else
                    nvMainNavigation.Loaded += SetSettingsHighlighted;
            }
            else
            {
                notebookData = e.Parameter;
                frMainMenu.Navigate(
                    typeof(NotebookList),
                    e.Parameter,
                    new SuppressNavigationTransitionInfo()
                    );
                nvMainNavigation.SelectedItem = nvMainNavigation.MenuItems[0];
                readyToChangePage = true;
            }
        }

        private void SetTitlebar()
        {
            Window.Current.SetTitleBar(TitleBar);
        }

        private void SetSettingsHighlighted(object sender, RoutedEventArgs e)
        {
            nvMainNavigation.Loaded -= SetSettingsHighlighted;
            nvMainNavigation.SelectedItem = nvMainNavigation.SettingsItem;
            readyToChangePage = true;
        }

        private void SwitchPage(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is Microsoft.UI.Xaml.Controls.NavigationViewItem item && readyToChangePage)
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
                            new SettingsNavigationData(LanguageChangeReloadPage, 0.0d),
                            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight }
                            );
                        break;
                }
            }
        }

        private void SearchNotebooks(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(sender.Text))
            {
                searchingFor = "";
                sender.ItemsSource = null;
                return;
            }

            if (args.Reason == AutoSuggestionBoxTextChangeReason.SuggestionChosen || args.Reason == AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
                return;

            searchingFor = sender.Text;
            List<NotebookSearchResult> matches = new List<NotebookSearchResult>();
            string[] searches = searchingFor.Split(" ");
            for (int i = 0; i < searches.Length; ++i)
                searches[i] = searches[i].Trim().ToLower();

            int currentPage = 0;
            foreach (SearchableNotebook nb in App.SearchableNotebooks)
            {
                currentPage = 0;
                foreach (SearchableNotebookPage page in nb.pages)
                {
                    ++currentPage;
                    foreach (RecognizedText text in page.recTextCol.recText)
                    {
                        foreach (string str in searches)
                        {
                            bool matchAlreadyExists = false;
                            foreach (NotebookSearchResult match in matches)
                            {
                                if (match.notebookFolder.Path == nb.notebookFolder.Path && match.pageId == page.pageId)
                                {
                                    ++match.rating;
                                    matchAlreadyExists = true;
                                    break;
                                }
                            }
                            if (text.text.ToLower().Contains(str) && !matchAlreadyExists)
                                matches.Add(new NotebookSearchResult(nb.notebookFolder, Utils.GetNotebookPathFromFolder(nb.notebookFolder), currentPage, page.pageId, text));
                        }
                    }
                }
            }

            matches.Sort();

            sender.ItemsSource = matches;
        }

        private void SelectItem(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            NotebookSearchResult selItem = (NotebookSearchResult)args.SelectedItem;
            sender.Text = selItem.notebookName;
        }

        private async void NavigateToSelectedItem(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is null)
                return;

            NotebookSearchResult selItem = (NotebookSearchResult)args.ChosenSuggestion;

            SearchNavigation objectToPass = new SearchNavigation(selItem.notebookFolder, searchingFor, selItem.pageId, selItem.recText);

            Frame.Navigate(
                typeof(CanvasPage),
                objectToPass,
                new DrillInNavigationTransitionInfo()
                );
        }

        private void LanguageChangeReloadPage(double verticalOffset)
        {
            Frame.Navigate(typeof(MainPage), new LanguageReloadData(notebookData, verticalOffset), new DrillInNavigationTransitionInfo());
        }
    }

    public class SearchNavigation
    {
        public StorageFolder notebookFolder;
        public string searchKeyword;
        public int pageId;
        public RecognizedText recText;

        public SearchNavigation(StorageFolder notebookFolder, string searchKeyword, int pageId, RecognizedText recText)
        {
            this.notebookFolder = notebookFolder;
            this.searchKeyword = searchKeyword;
            this.pageId = pageId;
            this.recText = recText;
        }
    }

    public class LanguageReloadData
    {
        public object? notebookData;
        public double settingsPageVerticalOffset;

        public LanguageReloadData(object? notebookData, double verticalOffset)
        {
            this.notebookData = notebookData;
            this.settingsPageVerticalOffset = verticalOffset;
        }
    }
}
