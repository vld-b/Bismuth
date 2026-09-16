using Shared;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FirstTour : Grid
    {
        public readonly string[] Captions = new string[5];
        public readonly MediaPlayerElement[] players = new MediaPlayerElement[5];

        public int currentVideo = 0;

        public FirstTour()
        {
            this.InitializeComponent();

            for (int i = 0; i < 5; ++i)
            {
                Captions[i] = Loc.GetLocalizedString("MainPageFirstTourCaption" + i+1);
            }

            int currentPlayer = 0;
            foreach (StackPanel sp in fvFirstTourVideos.Items)
            {
                players[currentPlayer] = (MediaPlayerElement)sp.Children[0];
                ++currentPlayer;
            }

            foreach (MediaPlayerElement el in players)
                el.MediaPlayer.IsLoopingEnabled = true;

            ((MediaPlayerElement)players[0]).MediaPlayer.Play();
        }

        public void NextVideo()
        {
            if (currentVideo >= players.Length - 1)
                return;

            ((MediaPlayerElement)players[currentVideo]).MediaPlayer.Pause();
            currentVideo = ++fvFirstTourVideos.SelectedIndex;
            ((MediaPlayerElement)players[currentVideo]).MediaPlayer.Play();
        }

        public void PreviousVideo()
        {
            if (currentVideo <= 0)
                return;

            ((MediaPlayerElement)players[currentVideo]).MediaPlayer.Pause();
            currentVideo = --fvFirstTourVideos.SelectedIndex;
            ((MediaPlayerElement)players[currentVideo]).MediaPlayer.Play();
        }
    }
}
