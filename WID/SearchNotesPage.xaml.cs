using Shaders;
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
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas.Effects;
using Windows.Media.Audio;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.Capture;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class SearchNotesPage : Page
    {
        private PixelShaderEffect? bgShader;
        private readonly byte[] bgShaderByteCode = ShaderStorage.SearchNotebooksBackgroundShader;

        public SearchNotesPage()
        {
            this.InitializeComponent();

        }

        private async void CreateBackgroundBlurResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            bgShader = new PixelShaderEffect(bgShaderByteCode);
            //bgShader.Properties["p1"] = new System.Numerics.Vector2(0.5f, 0.5f);
            //bgShader.Properties["p2"] = new System.Numerics.Vector2(0.2f, 0.4f);
            //bgShader.Properties["p3"] = new System.Numerics.Vector2(0.9f, 0.75f);
            //bgShader.Properties["aspectRatio"] = (float)(sender.Size.Width / sender.Size.Height);
            //Windows.UI.Color accentColor = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"];
            //bgShader.Properties["color"] = new System.Numerics.Vector3((float)accentColor.R / 255.0f, (float)accentColor.G / 255.0f, (float)accentColor.B / 255.0f);
            //bgShader.Properties["glowStrength"] = 5.0f;
        }

        private void DrawBackgroundBlur(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            if (bgShader is null)
                return;

            args.DrawingSession.DrawImage(bgShader);
        }
    }
}
