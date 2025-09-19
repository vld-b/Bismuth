using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a <see cref="Frame">.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private InkPresenter inkPres;
        private InkRecognizerContainer inkRec;

        public MainPage()
        {
            InitializeComponent();
            inkPres = inkMain.InkPresenter;
            inkPres.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen;
            inkPres.StrokesCollected += RecoginzeStroke;
            inkRec = new InkRecognizerContainer();
        }

        private void RecognizeStroke(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            foreach (InkRecognizer recognizer in inkRec.GetRecognizers())
            {
                if (recognizer.Name.Equals("Microsoft English (US) Handwriting Recognizer"))
                {
                    inkRec.SetDefaultRecognizer(recognizer);
                    break;
                }
            }
            inkRec.RecognizeAsync(inkPres.StrokeContainer, InkRecognitionTarget.Recent).Completed = (resAsync, status) => {
                IReadOnlyList<InkRecognitionResult> res = resAsync.GetResults();
                if (res.Count > 0)
                {
                    txtTest.Text = string.Empty;
                    foreach (InkRecognitionResult result in res)
                    {
                        txtTest.Text += result.GetTextCandidates().FirstOrDefault() + " ";
                    }
                }
            };
        }
    }
}
