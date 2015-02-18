using Core;
using Hashtag_Music.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media.Playback;
using Windows.Media.SpeechRecognition;
using Windows.Phone.UI.Input;
using Windows.Storage;
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
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

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

        public MainPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            this.Loaded += MainPage_Loaded;

            try
            {
                headerPanel.SetMenuPanel(menuPanel);
                menuPanel.SetCurrentPage(PageType.MainPage);
                headerPanel.RefreshButton.Tapped += RefreshButton_Tapped;
            }
            catch (Exception ex) { Message(ex.Message); }
            try
            {
                if (Core.Settings.CurrentPlaylist == null)
                    Core.Settings.CurrentPlaylist = new CustomPlaylist();
            }
            catch (Exception) { }

            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;

            //HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
        }

        private async void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (sender.CurrentState == MediaPlayerState.Playing)
                {
                    symbpp.Symbol = Symbol.Pause;
                    pblv.IsIndeterminate = false;
                    pblv.Visibility = Visibility.Collapsed;
                }
                else if (sender.CurrentState == MediaPlayerState.Paused)
                {
                    symbpp.Symbol = Symbol.Play;
                    pblv.IsIndeterminate = false;
                    pblv.Visibility = Visibility.Collapsed;
                }
                else if (sender.CurrentState == MediaPlayerState.Buffering)
                {
                    pblv.Visibility = Visibility.Visible;
                    pblv.IsIndeterminate = true;
                }
            });
        }

        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            try
            {
                if (menuPanel.IsDisplayed)
                {
                    menuPanel.Hide();
                    e.Handled = true;
                    return;
                }
            }
            catch (Exception) { }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Core.Settings.IsOpenApp == false) // don't counting when page navigation
                {
                    Core.Settings.OpenCount = Core.Settings.OpenCount + 1;
                    Core.Settings.IsOpenApp = true;
                }
            }
            catch (Exception ex) { Debug.WriteLine("error mainpage loaded: " + ex.Message); }

            StartBackgroundAudioTask();
        }

        private async void RegisterVoiceCommends()
        {
            try
            {
                Uri userVoiceCommands = new Uri("ms-appx:///vcd.xml", UriKind.Absolute);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(userVoiceCommands);
                await VoiceCommandManager.InstallCommandSetsFromStorageFileAsync(file);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("register voice command error: " + ex.Message); }
        }

        void RefreshButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (headerPanel.ShowLoading == true) return;

            headerPanel.RefreshButtonVisibility = false;
            addToList();
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
            try
            {
                addToList();
            }
            catch (Exception) { headerPanel.RefreshButtonVisibility = true; }
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                if (e.NavigationMode == NavigationMode.New)
                {
                    System.Threading.Tasks.Task.Run(() => RegisterVoiceCommends());
                }
                if (Frame.CanGoBack)
                    Frame.BackStack.Clear();
            }
            catch (Exception) { }

            if (Settings.CurrentPlaying != null)
            {
                lastplay.DataContext = Settings.CurrentPlaying;
                lastplay.Visibility = Visibility.Visible;
            }
            else
            {
                lastplay.Visibility = Visibility.Collapsed;
            }

            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //slider.StopAnimation();
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void Message(string content)
        {
            await new MessageDialog(content).ShowAsync();
        }

        private async void addToList()
        {
            if (lstVideo.ItemsSource != null) return;

            Debug.WriteLine("2");
            headerPanel.ShowLoading = true;
            try
            {
                var t = await DataGetter.GetDashbord(0);
                lstVideo.ItemsSource = t.Items;
                slider.Items = await DataGetter.GetDashbordSlider();
                slider.StartAnimation();
            }
            catch (Exception)
            {
                headerPanel.ShowLoading = false;
                Message("Please check your internet and tray again with Blue refresh button");
                headerPanel.RefreshButtonVisibility = true;
            }
            headerPanel.ShowLoading = false;
            Debug.WriteLine("3");
        }

        private void lstVideo_ItemClick(object sender, ItemClickEventArgs e)
        {
            BaseItem it = e.ClickedItem as BaseItem;
            if (e.ClickedItem.GetType() == typeof(VideoItem))
            {
                Debug.WriteLine("Video");
                Frame.Navigate(typeof(VideoViewPage), it);
            }
            else
            {
                if (it.Type == ItemType.Podcast)
                    Debug.WriteLine("T: " + it.Title);

                Debug.WriteLine("Song");
                Frame.Navigate(typeof(MusicPlayerPage), e.ClickedItem);
            }
        }

        private void lstVideo_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                menuPanel.Hide();
            }
            catch (Exception) { }
        }

        private void StartBackgroundAudioTask()
        {
            //AddMediaPlayerEventHandlers();
            var backgroundtaskinitializationresult = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                try
                {
                    var message = new ValueSet();
                    message.Add(Constants.AppState, "");
                    Windows.Media.Playback.BackgroundMediaPlayer.SendMessageToBackground(message);
                }
                catch (Exception) { }
            });
            backgroundtaskinitializationresult.Completed = new AsyncActionCompletedHandler(BackgroundTaskInitializationCompleted);
        }

        private void StartBackgroundAudioTaskPlay()
        {
            //AddMediaPlayerEventHandlers();
            var backgroundtaskinitializationresult = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                try
                {
                    var message = new ValueSet();
                    message.Add(Constants.StartPlayback, Core.Settings.CurrentPlaying.Link);
                    Windows.Media.Playback.BackgroundMediaPlayer.SendMessageToBackground(message);
                }
                catch (Exception) { }
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

        private async void PlayPauseLast(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                if (IsMyBackgroundTaskRunning)
                {
                    if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal , () =>
                        {
                            BackgroundMediaPlayer.Current.Pause();
                            //symbpp.Symbol = Symbol.Play;
                        });
                    }
                    else if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Paused)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            BackgroundMediaPlayer.Current.Play();
                            //symbpp.Symbol = Symbol.Pause;
                        });
                    }
                    else if (MediaPlayerState.Closed == BackgroundMediaPlayer.Current.CurrentState)
                    {
                        StartBackgroundAudioTaskPlay();
                        pblv.IsIndeterminate = true;
                    }
                }
                else
                {
                    StartBackgroundAudioTaskPlay();
                    pblv.IsIndeterminate = true;
                }

                pblv.Visibility = Visibility.Visible;
            }
            catch (Exception) { }
        }

        private void OpenCurrentPlaying(object sender, TappedRoutedEventArgs e)
        {
            Core.Settings.CurrentSongId = Core.Settings.CurrentPlaying.Id;
            Frame.Navigate(typeof(MusicPlayerPage), Core.Settings.CurrentPlaying);
        }
    }
}
