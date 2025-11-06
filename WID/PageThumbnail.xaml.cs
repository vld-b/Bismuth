using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PageThumbnail : Grid
    {
        public NotebookPage page { get; private set; }
        public EventHandler<DeletePageArgs>? RequestPageDelete;

        public PageThumbnail(int id, double width, double height)
        {
            this.InitializeComponent();
            page = new NotebookPage(id, width, height);
            page.canvas.InkPresenter.InputProcessingConfiguration.Mode = Windows.UI.Input.Inking.InkInputProcessingMode.None;
            page.canvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.None;
            Grid.SetRow(page, 0);
            this.Children.Insert(0, page);
        }

        public PageThumbnail(int id, double width, double height, BitmapImage bg)
        {
            this.InitializeComponent();
            page = new NotebookPage(id, bg);
            page.canvas.InkPresenter.InputProcessingConfiguration.Mode = Windows.UI.Input.Inking.InkInputProcessingMode.None;
            page.canvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.None;
            Grid.SetRow(page, 0);
            this.Children.Insert(0, page);
        }

        private void DeleteNote(object sender, RoutedEventArgs e)
        {
            RequestPageDelete?.Invoke(sender, new DeletePageArgs(this.page.id));
        }
    }

    public class DeletePageArgs : EventArgs
    {
        public int id { get; private set; }

        public DeletePageArgs(int id)
        {
            this.id = id;
        }
    }
}
