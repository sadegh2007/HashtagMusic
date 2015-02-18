using Core;
using Hashtag_Music.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Hashtag_Music
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicPlayerPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        MP3Item data = null;
        string id = string.Empty;
        bool byClass = false;
        DispatcherTimer timer = null;

        bool isPodcast = false;

        private System.Threading.AutoResetEvent SererInitialized;
        private bool isMyBackgroundTaskRunning = false;

        private bool IsMyBackgroundTaskRunning
        {
            get
            {
                if (isMyBackgroundTaskRunning)
                    return true;

                object value = ApplicationSettingsHelper.ReadResetSettingsValue(Constants.BackgroundTaskState);
                if (value == null)
                {
                    return false;
                }
                else
                {
                    isMyBackgroundTaskRunning = ((String)value).Equals(Constants.BackgroundTaskRunning);
                    return isMyBackgroundTaskRunning;
                }
            }
        }

        public string CurrentTrackName { get; set; }

        public MusicPlayerPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            headerPanel.SetMenuPanel(menuPanel);
            menuPanel.SetCurrentPage(PageType.MusicPlayer);

            headerPanel.RefreshButton.Tapped += RefreshButton_Tapped;

            SererInitialized = new System.Threading.AutoResetEvent(false);

            IsCurrentSong = false;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += timer_Tick;
            timer.Stop();
            Portatio.Visibility = Windows.UI.Xaml.Visibility.Visible;
            UpdateOrienation(DisplayInformation.GetForCurrentView().CurrentOrientation);
            DisplayInformation.GetForCurrentView().OrientationChanged += MusicPlayerPage_OrientationChanged;

            //this.NavigationCacheMode = NavigationCacheMode.Required;
            //HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (menuPanel.IsDisplayed || lyricViewer.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                menuPanel.Hide();
                lyricViewer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                e.Handled = true;
                return;
            }

            if (loading.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                Message("Please wait until loading complete");
                e.Handled = true;
                return;
            }

            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        void MusicPlayerPage_OrientationChanged(DisplayInformation sender, object args)
        {
            UpdateOrienation(sender.CurrentOrientation);
        }

        private void UpdateOrienation(DisplayOrientations currentOrientation)
        {
            if (currentOrientation == DisplayOrientations.Portrait || currentOrientation == DisplayOrientations.PortraitFlipped)
            {
                Landscape.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Portatio.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else if (currentOrientation == DisplayOrientations.Landscape || currentOrientation == DisplayOrientations.LandscapeFlipped)
            {
                Portatio.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Landscape.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
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

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            CheckIsCurrentSong();
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("0");
            if (e.Parameter.GetType() == typeof(MP3Item))
            {
                isPodcast = false;
                AddItem(e.Parameter as MP3Item);
            }
            else if (e.Parameter.GetType() == typeof(Podcast))
            {
                isPodcast = true;
                AddItem(new MP3Item(e.Parameter as Podcast));
            }
            else
            {
                byClass = false;
                id = e.Parameter as string;
                AddItem();
            }
            Debug.WriteLine("1");
            try
            {
                CheckIsCurrentSong();
            }
            catch (Exception) { }
            Debug.WriteLine("2");
            BackgroundMediaPlayer.Current.MediaOpened += Current_MediaOpened;
            App.Current.Suspending += Current_Suspending;
            App.Current.Resuming += Current_Resuming;
            ApplicationSettingsHelper.SaveSettingsValue(Constants.AppState, Constants.ForegroundAppActive);
            Debug.WriteLine("3");
            UpdateOrienation(DisplayInformation.GetForCurrentView().CurrentOrientation);

            //var message = new ValueSet();
            //message.Add(Constants.SongLink, "");
            //BackgroundMediaPlayer.SendMessageToBackground(message);

            //Debug.WriteLine("photo: " + data.Photo);
            //Debug.WriteLine("photo240: " + data.Photo_240);
            //Debug.WriteLine("photoplayer: " + data.PhotoPlayer);

            try
            {
                string link = data.PhotoPlayer;
                if (string.IsNullOrWhiteSpace(link))
                {
                    link = data.Photo;
                }
                if (string.IsNullOrWhiteSpace(link))
                {
                    link = data.Photo_240;
                }
                if (string.IsNullOrWhiteSpace(link) == false)
                {
                    var img = new ImageBrush() { Opacity = 0.2, AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Center, ImageSource = new BitmapImage() { UriSource = new Uri(link) }, Stretch = Stretch.UniformToFill };
                    this.Background = img;
                }
            }
            catch (Exception) { }

            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        #region Foreground App Lifecycle Handlers

        void Current_Resuming(object sender, object e)
        {
            ApplicationSettingsHelper.SaveSettingsValue(Constants.AppState, Constants.ForegroundAppActive);
            timer.Start();
            // Verify if the task was running before
            if (IsMyBackgroundTaskRunning)
            {
                //if yes, reconnect to media play handlers
                AddMediaPlayerEventHandlers();

                //send message to background task that app is resumed, so it can start sending notifications
                ValueSet messageDictionary = new ValueSet();
                messageDictionary.Add(Constants.AppResumed, DateTime.Now.ToString());
                messageDictionary.Add(Constants.SongLink, "");
                BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);

                if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
                {
                    playSymbole.Symbol = Symbol.Pause;     // Change to pause button
                }
                else
                {
                    playSymbole.Symbol = Symbol.Play;     // Change to play button
                }
                //txtCurrentTrack.Text = CurrentTrack;
            }
            else
            {
                playSymbole.Symbol = Symbol.Play;     // Change to play button
                //txtCurrentTrack.Text = "";
            }
        }

        void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            timer.Stop();
            ValueSet messageDictionary = new ValueSet();
            messageDictionary.Add(Constants.AppSuspended, DateTime.Now.ToString());
            BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);
            RemoveMediaPlayerEventHandlers();
            ApplicationSettingsHelper.SaveSettingsValue(Constants.AppState, Constants.ForegroundAppSuspended);
            deferral.Complete();
        }

        #endregion

        #region UI Methods

        private async void Download(object sender, TappedRoutedEventArgs e)
        {
            if (Settings.AskIEDownload)
            {
                var msg = new MessageDialog("Do you want download with IE?" + Environment.NewLine + "this option for who have problem to download with this app. if you don't want to see this message again go to settings and off this option.");
                msg.Commands.Add(new UICommand("Yes", async (a) =>
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(data.Link));
                }));
                msg.Commands.Add(new UICommand("No", (a) =>
                {
                    Frame.Navigate(typeof(DownloadPage), data.Link);
                }));
                await msg.ShowAsync();
            }
            else
            {
                Frame.Navigate(typeof(DownloadPage), data.Link);
            }
        }

        private void Share(object sender, TappedRoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += dataTransferManager_DataRequested;

            DataTransferManager.ShowShareUI();
        }

        private void Related(object sender, TappedRoutedEventArgs e)
        {
            if (byClass == false)
                Frame.Navigate(typeof(RelatedPage), data);
            else
                Frame.Navigate(typeof(RelatedPage), "M::" + id);
        }

        private void AddToPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AddToPlaylist();
        }

        private async void AddToPlaylist()
        {
            if (IsMyBackgroundTaskRunning == false)
            {
                StartBackgroundAudioTask();
                Settings.CurrentPlaying = data;
                await Settings.SaveLastPlayed(data);
                IsCurrentSong = true;
                return;
            }

            Debug.WriteLine("Add playlist tapped from app");

            var message = new ValueSet();
            message.Add(Constants.AddToPlaylist, data.Link);
            BackgroundMediaPlayer.SendMessageToBackground(message);

            if (Settings.CurrentPlaylist == null)
                Settings.CurrentPlaylist = new CustomPlaylist();

            //Settings.CurrentPlaylist.AddToList(data);
            //await Settings.SavePlaylist(Core.Settings.CurrentPlaylist);
        }

        void RefreshButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (headerPanel.ShowLoading == true) return;

            headerPanel.RefreshButtonVisibility = false;
            AddItem();
        }

        void timer_Tick(object sender, object e)
        {
            if (data.Link.Contains(CurrentTrack) == false) return; else IsCurrentSong = true;
            try
            {
                UpdateValues();
            }
            catch (Exception) { }
        }

        private void UpdateValues()
        {
            if (BackgroundMediaPlayer.IsMediaPlaying())
            { 
                var mediaPlayer = BackgroundMediaPlayer.Current;
                pb.Maximum = pb_land.Maximum = slider.Maximum = mediaPlayer.NaturalDuration.TotalMilliseconds;

                pb.Value = BackgroundMediaPlayer.Current.Position.TotalMilliseconds;

                var val = Subtract(mediaPlayer.NaturalDuration, mediaPlayer.Position);
                txtElapsedTime.Text = txtElapsedTime_land.Text = "-" + changeTo(val.Minutes) + ":" + changeTo(val.Seconds);
                playTime.Text = playTime_land.Text = changeTo(mediaPlayer.Position.Minutes) + ":" + changeTo(mediaPlayer.Position.Seconds);
            }
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

        private void AddItem(MP3Item d)
        {
            try
            {
                Debug.WriteLine("New data: " + d.Type);
                byClass = true;
                data = d;
                id = data.Id;
                this.DataContext = data;
                headerPanel.MP3DetailPanel = data;
            }
            catch (Exception ex) { Message(ex.Message); }
            try
            {
                txtSong.Text = data.Song;
                txtArtist.Text = data.Artist;
            }
            catch (Exception) { }
        }

        private async void AddItem()
        {
            headerPanel.ShowLoading = true;
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    data = await DataGetter.GetMp3ById(id);

                    if (data == null)
                    {
                        Message("There is problem to get data please tray again later");
                        Frame.GoBack();
                    }
                    data.DownloadsCount = "Downloaded: " + data.DownloadsCount;
                    this.DataContext = data;
                    headerPanel.MP3DetailPanel = data;
                });
            }
            catch (Exception ex)
            {
                headerPanel.ShowLoading = false;
                Message(ex.Message);
                headerPanel.RefreshButtonVisibility = true;
            }
            try
            {
                txtSong.Text = data.Song;
                txtArtist.Text = data.Artist;
            }
            catch (Exception) { }
            headerPanel.ShowLoading = false;
        }

        private void CheckIsCurrentSong()
        {
            Debug.WriteLine("is in 0-4");

            if (Settings.CurrentPlaying == null || data == null) { IsCurrentSong = true; return; }

            Debug.WriteLine("is in 0-5");

            var _artist = Core.Settings.CurrentPlaying.Artist;
            var _song = Core.Settings.CurrentPlaying.Song;

            Debug.WriteLine("is in 0-6");

            //if (data.Artist == _artist && _song == data.Song)
            if (data.Id == Core.Settings.CurrentSongId)
            {
                Debug.WriteLine("is in 0-6-0");
                IsCurrentSong = true;
                Debug.WriteLine("is in 0-6-1");
                //UpdateValues();
                timer.Start();
                Debug.WriteLine("is in 0-6-2");

                if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
                    playSymbole.Symbol = Symbol.Pause;
                else
                    playSymbole.Symbol = Symbol.Play;

                Debug.WriteLine("is in 0-7 - finish");
                return;
            }

            Debug.WriteLine("is in 0-7");

            IsCurrentSong = false;
        }

        public bool IsCurrentSong { get; set; }

        private void dataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            request.Data.Properties.Title = "Download " + data.Title + " on Radio Javan" + Environment.NewLine + "https://radiojavan.com/mp3s/mp3/" + data.PremLink;

            request.Data.SetText(Environment.NewLine + "Share By #Music - Windows Phone");
        }

        private async void Message(string p)
        {
            await new MessageDialog(p).ShowAsync();
        }

        #endregion

        #region Playback Buttons

        private void Previous_Song(object sender, TappedRoutedEventArgs e)
        {
            var value = new ValueSet();
            value.Add(Constants.SkipPrevious, "");
            BackgroundMediaPlayer.SendMessageToBackground(value);

            EnablePrevButton(false);
        }

        private async void PlayPause(object sender, TappedRoutedEventArgs e)
        {
            Debug.WriteLine("Play button pressed from App");
            Settings.CurrentPlaying = data;
            Settings.CurrentSongId = data.Id;
            await Settings.SaveLastPlayed(data);

            try
            {
                if (IsMyBackgroundTaskRunning)
                {
                    if (MediaPlayerState.Playing == BackgroundMediaPlayer.Current.CurrentState)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                        {
                            BackgroundMediaPlayer.Current.Pause();
                        });
                    }
                    else if (MediaPlayerState.Paused == BackgroundMediaPlayer.Current.CurrentState)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                        {
                            BackgroundMediaPlayer.Current.Play();
                        });
                    }
                    else if (MediaPlayerState.Closed == BackgroundMediaPlayer.Current.CurrentState)
                    {
                        StartBackgroundAudioTask();
                    }
                }
                else
                {
                    StartBackgroundAudioTask();
                }

                pb.IsIndeterminate = pb_land.IsIndeterminate = true;
                
                IsCurrentSong = true;
            }
            catch (Exception) { }
        }

        private void Next_Song(object sender, TappedRoutedEventArgs e)
        {
            var value = new ValueSet();
            value.Add(Constants.SkipNext, "");
            BackgroundMediaPlayer.SendMessageToBackground(value);

            EnableNextButton(false);
        }

        #endregion

        #region Media Playback Helper methods

        private void RemoveMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged -= Current_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        private void AddMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        async void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case Constants.Trackchanged:
                        //When foreground app is active change track based on background message
                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            CurrentTrackName = (string)e.Data[key];
                        }
                        );
                        break;
                    case Constants.BackgroundTaskStarted:
                        //Wait for Background Task to be initialized before starting playback
                        Debug.WriteLine("Background Task started");
                        SererInitialized.Set();
                        break;
                    case Constants.AddedToPlaylistComplete:
                        Debug.WriteLine("Song added");
                        Core.Settings.CurrentPlaylist.AddToList(data);
                        break;
                    case Constants.PlaylistCount:
                        Debug.WriteLine("Count updated from playlist");
                        CheckButtons(e.Data[key] as string);
                        break;
                    case Constants.SongLink:
                        Debug.WriteLine("song link get: " + e.Data[key].ToString() + "\ndata link: " + data.Link);
                        try
                        {
                            if (e.Data[key].ToString() == data.Link)
                            {
                                IsCurrentSong = false;
                                Debug.WriteLine("is in 0-1");
                                Core.Settings.CurrentPlaying = data;
                                Debug.WriteLine("is in 0-2");
                                CheckIsCurrentSong();
                                Debug.WriteLine("is in 0-3");
                            }
                        } catch (Exception) { }
                        break;
                }
            }
        }

        private void CheckButtons(string co)
        {
            try
            {
                int count = int.Parse(co);
                if (count > 1)
                {
                    EnableNextButton(true);
                }
                else
                {
                    EnableNextButton(false);
                    EnablePrevButton(false);
                }
            }
            catch (Exception) { }
        }

        async void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    switch (sender.CurrentState) 
                    {
                        case MediaPlayerState.Paused:
                            Core.Settings.IsPlaying = false;
                            playSymbole.Symbol = playSymbole_land.Symbol = Symbol.Play;
                            timer.Stop();
                            pb.IsIndeterminate = pb_land.IsIndeterminate = false;
                            break;
                        case MediaPlayerState.Playing:
                            pb.Maximum = slider.Maximum = slider.Maximum = sender.NaturalDuration.TotalMilliseconds;
                            timer.Start();
                            Core.Settings.IsPlaying = true;
                            playSymbole.Symbol = playSymbole_land.Symbol = Symbol.Pause;
                            EnableNextButton(true);
                            EnablePrevButton(true);
                            pb.IsIndeterminate = pb_land.IsIndeterminate = false;
                            break;
                        case MediaPlayerState.Buffering:
                            
                            //pb.IsIndeterminate = pb_land.IsIndeterminate = true;
                            break;
                    }
                });
        }

        private void EnableNextButton(bool enable)
        {
            if (enable)
            {
                next_symbol.Foreground = App.Current.Resources["lightForground"] as SolidColorBrush;
                NextButton.IsTapEnabled = true;
            }
            else
            {
                next_symbol.Foreground = App.Current.Resources["medForground"] as SolidColorBrush;
                NextButton.IsTapEnabled = false;
            }
        }

        private void EnablePrevButton(bool enable)
        {
            if (enable)
            {
                prev_symbol.Foreground = App.Current.Resources["lightForground"] as SolidColorBrush;
                prevButton.IsTapEnabled = true;
            }
            else
            {
                prev_symbol.Foreground = App.Current.Resources["medForground"] as SolidColorBrush;
                prevButton.IsTapEnabled = false;
            }
        }

        private void StartBackgroundAudioTask()
        {
            AddMediaPlayerEventHandlers();
            var backgroundtaskinitializationresult = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                try {
                    var message = new ValueSet();
                    message.Add(Constants.StartPlayback, data.Link);
                    BackgroundMediaPlayer.SendMessageToBackground(message);

                    Core.Settings.CurrentPlaylist.AddToList(data);
                } catch (Exception) { }
            });
            backgroundtaskinitializationresult.Completed = new AsyncActionCompletedHandler(BackgroundTaskInitializationCompleted);
        }

        private void BackgroundTaskInitializationCompleted(IAsyncAction action, AsyncStatus status)
        {
            if (status == AsyncStatus.Completed)
            {
                Debug.WriteLine("Background Audio Task initialized");
            }
            else if (status == AsyncStatus.Error)
            {
                Debug.WriteLine("Background Audio Task could not initialized due to an error ::" + action.ErrorCode.ToString());
            }
        }

        void Current_MediaOpened(MediaPlayer sender, object args)
        {
            try
            {
                pb.Maximum = slider.Maximum = sender.NaturalDuration.TotalMilliseconds;
            } catch (Exception) { }
        }

        private void SeekToSliderPosition(object sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                if (BackgroundMediaPlayer.Current.CanSeek)
                {
                    timer.Stop();
                    int sliderValue = (int)(sender as Slider).Value;
                    TimeSpan ts = new TimeSpan(0, 0, 0, 0, sliderValue);
                    BackgroundMediaPlayer.Current.Position = ts;
                    pb.Value = sliderValue;
                    playTime.Text = playTime_land.Text = changeTo(ts.Minutes) + ":" + changeTo(ts.Seconds);
                }
            }
            catch (Exception) { }
        }

        #endregion

        private string CurrentTrack
        {
            get
            {
                object value = ApplicationSettingsHelper.ReadResetSettingsValue(Constants.CurrentTrack);
                if (value != null)
                {
                    return (String)value;
                }
                else
                    return String.Empty;
            }
        }

        private async void likeButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            loading.Visibility = Windows.UI.Xaml.Visibility.Visible;
            pbLoading.IsActive = true;
            try
            {
                if (Settings.Login)
                {
                    bool result;
                    if (isPodcast)
                        result = await DataGetter.VotePodcast(Settings.client, data.Id, true);
                    else
                        result = await DataGetter.VoteMp3(Settings.client, data.Id, true);

                    if (result == false)
                    {
                        Message("There is problem to like this song please check your logined before or not");
                    }
                    else
                    {
                        Message("Thank you for voting " + data.Title);
                    }
                }
                else
                {
                    Message("You must login first.");
                }
            }
            catch (Exception) { }
            pbLoading.IsActive = false;
            loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private async void dislikeButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            loading.Visibility = Windows.UI.Xaml.Visibility.Visible;
            pbLoading.IsActive = true;
            try
            {
                if (Settings.Login)
                {
                    bool result;
                    if (isPodcast)
                        result = await DataGetter.VotePodcast(Settings.client, data.Id, false);
                    else
                        result = await DataGetter.VoteMp3(Settings.client, data.Id, false);
                    if (result == false)
                    {
                        Message("There is problem to like this song please check your logined before or not");
                    }
                    else
                    {
                        Message("Thank you for voting " + data.Title);
                    }
                }
                else
                {
                    Message("You must login first.");
                }
            }
            catch (Exception) { }
            pbLoading.IsActive = false;
            loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private async void Lyric_Tapped(object sender, TappedRoutedEventArgs e)
        {
            loading.Visibility = Windows.UI.Xaml.Visibility.Visible;
            pbLoading.IsActive = true;

            try
            {
                var t = await DataGetter.GetMp3ById(data.Id);
                txtLyric.Text = t.Lyric;
                pbLoading.IsActive = false;
                loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                lyricViewer.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }

            pbLoading.IsActive = false;
            loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void lyricViewer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            (sender as Grid).Visibility = Visibility.Collapsed;
        }
    }
}
