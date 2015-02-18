using Core;
using Hashtag_Music.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Hashtag_Music
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OfflineMusicPlayer : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        MP3Item data = null;
        DispatcherTimer timer = null;
        StorageFile fileData = null;

        public OfflineMusicPlayer()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            player.CurrentStateChanged += player_CurrentStateChanged;
            player.MediaEnded += player_MediaEnded;
            player.MediaOpened += player_MediaOpened;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += timer_Tick;
            timer.Stop();

            headerPanel.SetMenuPanel(menuPanel);
            menuPanel.SetCurrentPage(PageType.MusicPlayer);

            Portrait.Visibility = Windows.UI.Xaml.Visibility.Visible;
            updateOrientation(DisplayInformation.GetForCurrentView().CurrentOrientation);
            DisplayInformation.GetForCurrentView().OrientationChanged += OfflineMusicPlayer_OrientationChanged;
        }

        void player_MediaOpened(object sender, RoutedEventArgs e)
        {
            pb.Maximum = pb_land.Maximum = slider.Maximum = player.NaturalDuration.TimeSpan.TotalMilliseconds;
        }

        void OfflineMusicPlayer_OrientationChanged(DisplayInformation sender, object args)
        {
            var orientation = sender.CurrentOrientation;
            updateOrientation(orientation);
        }

        private void updateOrientation(DisplayOrientations orientation)
        {
            if (orientation == DisplayOrientations.Portrait || orientation == DisplayOrientations.PortraitFlipped)
            {
                Landscape.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Portrait.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else if (orientation == DisplayOrientations.Landscape || orientation == DisplayOrientations.LandscapeFlipped)
            {
                Portrait.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Landscape.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        void player_MediaEnded(object sender, RoutedEventArgs e)
        {
            timer.Stop();   
        }

        async void player_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                switch(player.CurrentState)
                {
                    case MediaElementState.Paused:
                        playSymbole.Symbol = Symbol.Play;
                        timer.Stop();
                        break;
                    case MediaElementState.Playing:
                        playSymbole.Symbol = Symbol.Pause;
                        timer.Start();
                        break;
                    case MediaElementState.Stopped:
                        playSymbole.Symbol = Symbol.Play;
                        timer.Stop();
                        break;
                }
            });
        }

        void timer_Tick(object sender, object e)
        {
            UpdateValues();
        }

        private void UpdateValues()
        {
            var mediaPlayer = player;
            pb.Value = pb_land.Value = player.Position.TotalMilliseconds;
            var val = Subtract(mediaPlayer.NaturalDuration.TimeSpan, mediaPlayer.Position);
            txtElapsedTime.Text = txtElapsedTime_land.Text = "-" + changeTo(val.Minutes) + ":" + changeTo(val.Seconds);
            playTime.Text = playTime_land.Text = changeTo(mediaPlayer.Position.Minutes) + ":" + changeTo(mediaPlayer.Position.Seconds);
        }

        private string changeTo(int t)
        {
            if (t < 10)
                return "0" + t;
            return t.ToString();
        }

        static TimeSpan Subtract(TimeSpan dt1, TimeSpan dt2)
        {
            TimeSpan span = dt1 - dt2;
            return span;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var file = e.NavigationParameter as StorageFile;
            MusicProperties prop = await file.Properties.GetMusicPropertiesAsync();

            data = new MP3Item();
            data.Song = prop.Album;
            data.Artist = prop.Artist;
            data.Title = data.Song + " (" + data.Artist + ")";

            data.PremLink = file.Name.Replace(file.FileType, string.Empty);

            var image = new BitmapImage();
            image.SetSource(await file.GetThumbnailAsync(ThumbnailMode.MusicView));
            imgArt_kand.ImageSource = imgArt.ImageSource = image;
            
            player.SetSource((await file.OpenStreamForReadAsync()).AsRandomAccessStream(), ".mp3");
            //PlayBackground(file);

            headerPanel.MP3DetailPanel = data;
            fileData = file;
            txtArtist.Text = data.Artist;
            txtSong.Text = data.Song;
        }

        private async void Message(string p)
        {
            await new MessageDialog(p).ShowAsync();
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void Previous_Song(object sender, TappedRoutedEventArgs e)
        {

        }

        private void PlayPause(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                if (playSymbole.Symbol == Symbol.Pause)
                {
                    player.Pause();
                    //BackgroundMediaPlayer.Current.Pause();
                    playSymbole.Symbol = playSymbole_land.Symbol = Symbol.Play;
                }
                else
                {
                    player.Play();
                    //BackgroundMediaPlayer.Current.Play();
                    playSymbole.Symbol = playSymbole_land.Symbol = Symbol.Pause;
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }
        }

        private void Next_Song(object sender, TappedRoutedEventArgs e)
        {

        }

        private void Share(object sender, TappedRoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += dataTransferManager_DataRequested;

            DataTransferManager.ShowShareUI();
        }

        private void dataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            request.Data.SetStorageItems(new List<IStorageFile>() { fileData });
            request.Data.Properties.Title = "Download " + data.Title + " on Radio Javan" + Environment.NewLine + "https://radiojavan.com/mp3s/mp3/" + data.PremLink;
            request.Data.SetText(Environment.NewLine + "Share By #Music - Windows Phone");
        }

        private void SkipToPostionMedia(object sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                if (player.CanSeek)
                {
                    timer.Stop();
                    int sliderValue = (int)(sender as Slider).Value;
                    TimeSpan ts = new TimeSpan(0, 0, 0, 0, sliderValue);
                    player.Position = ts;
                    pb.Value = sliderValue;
                }
            }
            catch (Exception ex) { Debug.WriteLine("SkipError: " + ex.Message); }
        }
    }
}
