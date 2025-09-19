using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;

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
            LoadNotebooks();
        }

        private async void LoadNotebooks()
        {
            IReadOnlyList<StorageFile> notebooks = await notes.GetFilesAsync();
            foreach (StorageFile nb in notebooks)
            {
                lvNotebooks.Items.Add(new NotebookItem(nb.Name));
            }
        }

        private async void CreateNewNotebook(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            StorageFile newNotebook = await notes.CreateFileAsync("Test", CreationCollisionOption.GenerateUniqueName);
            lvNotebooks.Items.Add(new NotebookItem(newNotebook.Name));
        }
    }
}
