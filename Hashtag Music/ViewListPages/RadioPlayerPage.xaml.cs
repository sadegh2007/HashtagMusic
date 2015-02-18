using Core;
using Hashtag_Music.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media.Playback;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Hashtag_Music
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RadioPlayerPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private MediaPlayer _mediaPlayer = null;
        DispatcherTimer timer = null;

        public RadioPlayerPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            headerPanel.SetMenuPanel(menuPanel);
            menuPanel.SetCurrentPage(PageType.RadioPage);

            headerPanel.RefreshButton.Tapped += RefreshButton_Tapped;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            addItems();
            _mediaPlayer = BackgroundMediaPlayer.Current;
            _mediaPlayer.CurrentStateChanged += _mediaPlayer_CurrentStateChanged;
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        #region UI Data 

        async void _mediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (sender.CurrentState == MediaPlayerState.Playing)
                {
                    s.Symbol = Symbol.Pause;
                }
                else if (sender.CurrentState == MediaPlayerState.Paused || sender.CurrentState == MediaPlayerState.Stopped)
                {
                    s.Symbol = Symbol.Play;
                }
            });
        }

        void RefreshButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (headerPanel.ShowLoading == false)
            {
                headerPanel.RefreshButtonVisibility = false;
                addItems();
            }
        }

        void timer_Tick(object sender, object e)
        {
            addItems();
        }

        private async void addItems()
        {
            menuPanel.Hide();
            headerPanel.ShowLoading = true;
            try
            {
                var lst = await DataGetter.GetRadioNowPlaying();
                if (lst.Items.Count > 0)
                {
                    var item = lst.Items.FirstOrDefault();
                    currentImage.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(item.Thumb));
                    txtArtist.Text = item.Artist;
                    txtSong.Text = item.Song;
                }
                lstVideo.ItemsSource = lst.Items;
                timer.Start();
            }
            catch (Exception ex)
            {
                Message(ex.Message);
                headerPanel.RefreshButtonVisibility = true;
            }
            headerPanel.ShowLoading = false;
        }

        private async void Message(string content)
        {
            menuPanel.Hide();
            await new MessageDialog(content).ShowAsync();
        }

        private async void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            menuPanel.Hide();
            try
            {
                if (Core.Settings.StreamAd == null)
                    Core.Settings.StreamAd = await Core.DataGetter.GetStream();

                var link = "";
                if (Core.Settings.UseLowAudioQ)
                    link = Core.Settings.StreamAd.LowQ;
                else
                    link = Core.Settings.StreamAd.HighQ;

                //link += "::" + txtSong.Text + "::" + txtArtist.Text;

                if (s.Symbol == Symbol.Play)
                {
                    var message = new ValueSet
                    {
                        {
                            Constants.StartPlayback,
                            link
                        }
                    };
                    BackgroundMediaPlayer.SendMessageToBackground(message);
                }
                else
                {
                    BackgroundMediaPlayer.Current.Pause();
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }
        }

        #endregion

        private void lstVideo_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            menuPanel.Hide();
        }
    }
}
