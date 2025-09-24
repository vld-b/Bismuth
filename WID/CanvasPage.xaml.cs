using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Preview.Notes;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CanvasPage : Page
    {
        private StorageFolder notes => ApplicationData.Current.LocalFolder;

        private readonly InkPresenter inkPres;
        private readonly InkRecognizerContainer inkRec;

        private string? fileName;

        public CanvasPage()
        {
            InitializeComponent();
            SetTitlebar();
            inkPres = inkMain.InkPresenter;
            inkRec = new InkRecognizerContainer();
            SetupInk();
        }

        private void SetupInk()
        {
            inkPres.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen;
            inkPres.StrokesCollected += RecognizeStroke;
        }
        private void SetTitlebar()
        {
            Window.Current.SetTitleBar(TitleBar);
            tbAppTitle.Text = AppInfo.Current.DisplayInfo.DisplayName;
        }

        private async void RecognizeStroke(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            //foreach (InkRecognizer recognizer in inkRec.GetRecognizers())
            //{
            //    if (recognizer.Name.Equals("Microsoft English (US) Handwriting Recognizer"))
            //    {
            //        inkRec.SetDefaultRecognizer(recognizer);
            //        break;
            //    }
            //}
            //inkRec.RecognizeAsync(inkPres.StrokeContainer, InkRecognitionTarget.Recent).Completed = (resAsync, status) => {
            //    IReadOnlyList<InkRecognitionResult> res = resAsync.GetResults();
            //    if (res.Count > 0)
            //    {
            //        txtTest.Text = string.Empty;
            //        foreach (InkRecognitionResult result in res)
            //        {
            //            txtTest.Text += result.GetTextCandidates().FirstOrDefault() + " ";
            //        }
            //    }
            //};
            if (!inkPres.StrokeContainer.GetStrokes().Any())
                return;

            IReadOnlyList<InkRecognitionResult> results = await inkRec.RecognizeAsync(inkPres.StrokeContainer, InkRecognitionTarget.All);
            if (results.Count > 0)
            {
                txtTest.Text = string.Empty;
                foreach (InkRecognitionResult result in results)
                {
                    txtTest.Text += result.GetTextCandidates().FirstOrDefault() + " ";
                }
            }
        }

        private void SaveFile(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!inkPres.StrokeContainer.GetStrokes().Any())
                return;


        }

        private async void PageBack(object sender, RoutedEventArgs e)
        {
            if (fileName != null)
            {
                StorageFile f = await notes.GetFileAsync(fileName);
                await inkPres.StrokeContainer.SaveAsync((await f.OpenStreamForWriteAsync()).AsOutputStream());
            }

            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            fileName = e.Parameter as string;
            if (inkPres == null || fileName == null)
                return;

            Console.WriteLine("Starting to load file");
            StorageFile f = await notes.GetFileAsync(fileName);
            Console.WriteLine("Loaded file");
            if ((new FileInfo(f.Path)).Length >= 0)
                await inkPres.StrokeContainer.LoadAsync((await f.OpenStreamForReadAsync()).AsInputStream());
        }
    }
}
