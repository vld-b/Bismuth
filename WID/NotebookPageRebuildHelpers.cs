using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace WID
{
    public sealed partial class NotebookPage : Grid, IDisposable // Rebuilders that reconfigure the Notebookpage without replacing it used in OnNavigatedTo of CanvasPage
    {
        public void Rebuild()
        {
            this.InitializeComponent();
            this.undoRedoSystem = new UndoRedoSystem();
            this.pageState = new PageState(null);
            contentCanvas = pageContent;
            canvas = inkCanvas;
            inkPres = inkCanvas.InkPresenter;
            canvas.InkPresenter.InputProcessingConfiguration.RightDragAction = App.AppSettings.selectWithRightClick;
            currentPattern = null;

            SetInkLanguage(App.AppSettings.defaultInkLanguage);
        }

        public void Rebuild(int id, UndoRedoSystem undoRedoSystem, PageState pageState)
        {
            Rebuild();
            this.id = id;
            this.undoRedoSystem = undoRedoSystem;
            this.pageState = pageState;
        }

        public void Rebuild(int id, BitmapImage bg, UndoRedoSystem undoRedoSystem, PageState pageState)
        {
            Rebuild(id, undoRedoSystem, pageState);
            LoadBackground(bg);
        }

        public void Rebuild(int id, double width, double height, UndoRedoSystem undoRedoSystem, PageState pageState)
        {
            Rebuild(id, undoRedoSystem, pageState);
            this.Width = width;
            this.Height = height;
        }

        public void Rebuild(int id, double width, double height, PageTemplatePattern? pattern, bool hasPattern, UndoRedoSystem undoRedoSystem, PageState pageState)
        {
            Rebuild(id, undoRedoSystem, pageState);
            this.Width = width;
            this.Height = height;
            currentPattern = pattern;
            this.hasPattern = hasPattern;
        }
    }
}
