using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [ContentProperty(Name = "Children")]
    public sealed partial class CustomInkToolbarTool : Button
    {
        public static readonly DependencyProperty ChildrenProperty = DependencyProperty.Register(
            nameof(Children),
            typeof(ObservableCollection<UIElement>),
            typeof(CustomInkToolbarTool),
            new PropertyMetadata(null)
            );

        public ObservableCollection<UIElement> Children
        {
            get => (ObservableCollection<UIElement>)GetValue(ChildrenProperty);
            set => SetValue(ChildrenProperty, value);
        }

        private bool _isSelected = false;
        public bool isSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                if (_isSelected)
                {
                    gdContent.Translation = new System.Numerics.Vector3(0f, -4f, 0f);
                    Background = (SolidColorBrush)Application.Current.Resources["SystemControlHighlightAccentBrush"];
                } else
                {
                    gdContent.Translation = new System.Numerics.Vector3(0f, 0f, 0f);
                    Background = null;
                }
            }
        }

        public CustomInkToolbarTool()
        {
            this.InitializeComponent();

            Children = new ObservableCollection<UIElement>();
        }
    }
}
