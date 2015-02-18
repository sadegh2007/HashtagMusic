using Core;
using Hashtag_Music.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media.Playback;
using Windows.UI;
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

    public sealed partial class Playlist : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public Playlist()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            headerPanel.SetMenuPanel(menuPanel);
            menuPanel.SetCurrentPage(PageType.MyPlaylistPage);
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
            GetData();
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

        private void lstVideo_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            menuPanel.Hide();
        }

        private void GetData()
        {
            UpdatePlaylist();
        }

        private async void UpdatePlaylist()
        {
            try
            {
                if (Core.Settings.CurrentPlaylist != null)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        var val = Core.Settings.CurrentPlaylist.Items;
                        foreach (var t in val)
                        {
                            lstPlaylist.Items.Add(t);
                        }
                        //UpdateCurrentPlaying(CurrentTrack);
                        BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
                    });

                }
                else
                {
                    Debug.WriteLine("null returned from playlist");
                }
            }
            catch (Exception ex) { Message(ex.Message); }
        }

        private async void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case Constants.Trackchanged:
                        //When foreground app is active change track based on background message
                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            var CurrentTrackName = (string)e.Data[key];
                            // TODO : UPDATE PLAYLIST VIEW
                            UpdateCurrentPlaying(CurrentTrackName);
                            //UpdatePlaylist();
                        }
                        );
                        break;
                }
            }
        }

        private void UpdateCurrentPlaying(string trackName)
        {
            for (int i = 0; i < lstPlaylist.Items.Count; ++i)
            {
                MP3Item d = lstPlaylist.Items[i] as MP3Item;

                if (d == null) continue;

                if (d.Link.Contains(trackName) == false)
                {
                    try
                    {
                        Settings.CurrentPlaying = d;
                        // Move Current Playing to top of the playlist
                        lstPlaylist.Items.RemoveAt(i);
                        lstPlaylist.Items.Insert(0, d);
                        break;
                    }
                    catch (Exception) { }
                }
            }
        }

        private async void Message(string p)
        {
            await new MessageDialog(p).ShowAsync();
        }

        private void RemoveFromPlaylist(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                var d = (sender as FrameworkElement).DataContext as MP3Item;
                int index = Core.Settings.CurrentPlaylist.Items.IndexOf(d); 
                ValueSet message = new ValueSet();
                message.Add(Constants.RemoveFromPlaylist, d.Link);
                BackgroundMediaPlayer.SendMessageToBackground(message);
                Core.Settings.CurrentPlaylist.Items.RemoveAt(index);
                lstPlaylist.Items.RemoveAt(index);
                lstPlaylist.Focus(FocusState.Programmatic);
            }
            catch (Exception ex) { Message(ex.Message); }
        }

        private Grid GetItem(int index)
        {
            try
            {
                var t = lstPlaylist.ContainerFromIndex(index) as ContentControl;
                return t.ContentTemplateRoot as Grid;
            }
            catch (Exception) { Debug.WriteLine("Item not found at index " + index); }
            return null;
        }

        /// <summary>
        /// Read current track information from application settings
        /// </summary>
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
        
    }
}
