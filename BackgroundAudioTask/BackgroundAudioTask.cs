﻿using Core;
using Hashtag_Music;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;


namespace BackgroundAudioTask
{
    /// <summary>
    /// Enum to identify foreground app state
    /// </summary>
    enum ForegroundAppStatus
    {
        Active,
        Suspended,
        Unknown
    }

    /// <summary>
    /// Impletements IBackgroundTask to provide an entry point for app code to be run in background. 
    /// Also takes care of handling UVC and communication channel with foreground
    /// </summary>
    public sealed class MediaPlayerAudioTask : IBackgroundTask
    {

        #region Private fields, properties
        private SystemMediaTransportControls systemmediatransportcontrol;
        private MyPlaylistManager playlistManager;
        private BackgroundTaskDeferral deferral; // Used to keep task alive
        private ForegroundAppStatus foregroundAppState = ForegroundAppStatus.Unknown;
        private AutoResetEvent BackgroundTaskStarted = new AutoResetEvent(false);
        private bool backgroundtaskrunning = false;

        /// <summary>
        /// Property to hold current playlist
        /// </summary>
        private MyPlaylist Playlist
        {
            get
            {
                if (null == playlistManager)
                {
                    playlistManager = new MyPlaylistManager();
                }
                return playlistManager.Current;
            }
        }
        #endregion

        #region IBackgroundTask and IBackgroundTaskInstance Interface Members and handlers
        /// <summary>
        /// The Run method is the entry point of a background task. 
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background Audio Task " + taskInstance.Task.Name + " starting...");
            // Initialize SMTC object to talk with UVC. 
            //Note that, this is intended to run after app is paused and 
            //hence all the logic must be written to run in background process
            systemmediatransportcontrol = SystemMediaTransportControls.GetForCurrentView();
            systemmediatransportcontrol.ButtonPressed += systemmediatransportcontrol_ButtonPressed;
            systemmediatransportcontrol.PropertyChanged += systemmediatransportcontrol_PropertyChanged;
            systemmediatransportcontrol.IsEnabled = true;
            systemmediatransportcontrol.IsPauseEnabled = true;
            systemmediatransportcontrol.IsPlayEnabled = true;

            // Associate a cancellation and completed handlers with the background task.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
            taskInstance.Task.Completed += Taskcompleted;

            var value = ApplicationSettingsHelper.ReadResetSettingsValue(Constants.AppState);
            if (value == null)
                foregroundAppState = ForegroundAppStatus.Unknown;
            else
                foregroundAppState = (ForegroundAppStatus)Enum.Parse(typeof(ForegroundAppStatus), value.ToString());

            //Add handlers for MediaPlayer
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;

            //Add handlers for playlist trackchanged
            Playlist.TrackChanged += playList_TrackChanged;

            //Initialize message channel 
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;

            //Send information to foreground that background task has been started if app is active
            if (foregroundAppState != ForegroundAppStatus.Suspended)
            {
                ValueSet message = new ValueSet();
                message.Add(Constants.BackgroundTaskStarted, "");
                BackgroundMediaPlayer.SendMessageToForeground(message);
            }
            BackgroundTaskStarted.Set();
            backgroundtaskrunning = true;

            ApplicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskRunning);
            deferral = taskInstance.GetDeferral();
        }

        /// <summary>
        /// Indicate that the background task is completed.
        /// </summary>       
        void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Debug.WriteLine("MyBackgroundAudioTask " + sender.TaskId + " Completed...");
            deferral.Complete();
        }

        /// <summary>
        /// Handles background task cancellation. Task cancellation happens due to :
        /// 1. Another Media app comes into foreground and starts playing music 
        /// 2. Resource pressure. Your task is consuming more CPU and memory than allowed.
        /// In either case, save state so that if foreground app resumes it can know where to start.
        /// </summary>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            // You get some time here to save your state before process and resources are reclaimed
            Debug.WriteLine("MyBackgroundAudioTask " + sender.Task.TaskId + " Cancel Requested...");
            try
            {
                //save state
                ApplicationSettingsHelper.SaveSettingsValue(Constants.CurrentTrack, Playlist.CurrentTrackName);
                ApplicationSettingsHelper.SaveSettingsValue(Constants.Position, BackgroundMediaPlayer.Current.Position.ToString());
                ApplicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskCancelled);
                ApplicationSettingsHelper.SaveSettingsValue(Constants.AppState, Enum.GetName(typeof(ForegroundAppStatus), foregroundAppState));
                backgroundtaskrunning = false;
                //unsubscribe event handlers
                systemmediatransportcontrol.ButtonPressed -= systemmediatransportcontrol_ButtonPressed;
                systemmediatransportcontrol.PropertyChanged -= systemmediatransportcontrol_PropertyChanged;
                Playlist.TrackChanged -= playList_TrackChanged;

                //clear objects task cancellation can happen uninterrupted
                playlistManager.ClearPlaylist();
                playlistManager = null;
                BackgroundMediaPlayer.Shutdown(); // shutdown media pipeline
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            deferral.Complete(); // signals task completion. 
            Debug.WriteLine("MyBackgroundAudioTask Cancel complete...");
        }
        #endregion

        #region SysteMediaTransportControls related functions and handlers
        /// <summary>
        /// Update UVC using SystemMediaTransPortControl apis
        /// </summary>
        private void UpdateUVCOnNewTrack()
        {
            if (Playlist.PlaylistCount > 1)
            {
                systemmediatransportcontrol.IsNextEnabled = true;
                systemmediatransportcontrol.IsPreviousEnabled = true;
            }
            else
            {
                systemmediatransportcontrol.IsNextEnabled = false;
                systemmediatransportcontrol.IsPreviousEnabled = false;
            }
            systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Playing;
            systemmediatransportcontrol.DisplayUpdater.Type = MediaPlaybackType.Music;
            systemmediatransportcontrol.DisplayUpdater.MusicProperties.Title = Playlist.CurrentTrackName;
            systemmediatransportcontrol.DisplayUpdater.Update();
        }

        /// <summary>
        /// Fires when any SystemMediaTransportControl property is changed by system or user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void systemmediatransportcontrol_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            //TODO: If soundlevel turns to muted, app can choose to pause the music
        }

        /// <summary>
        /// This function controls the button events from UVC.
        /// This code if not run in background process, will not be able to handle button pressed events when app is suspended.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void systemmediatransportcontrol_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Debug.WriteLine("UVC play button pressed");
                    // If music is in paused state, for a period of more than 5 minutes, 
                    //app will get task cancellation and it cannot run code. 
                    //However, user can still play music by pressing play via UVC unless a new app comes in clears UVC.
                    //When this happens, the task gets re-initialized and that is asynchronous and hence the wait
                    if (!backgroundtaskrunning)
                    {
                        bool result = BackgroundTaskStarted.WaitOne(2000);
                        if (!result)
                            throw new Exception("Background Task didnt initialize in time");
                    }
                    StartPlayback();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Debug.WriteLine("UVC pause button pressed");
                    try
                    {
                        BackgroundMediaPlayer.Current.Pause();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("error pause: " + ex.ToString());
                    }
                    break;
                case SystemMediaTransportControlsButton.Next:
                    Debug.WriteLine("UVC next button pressed");
                    SkipToNext();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    Debug.WriteLine("UVC previous button pressed");
                    SkipToPrevious();
                    break;
            }
        }


        #endregion

        #region Playlist management functions and handlers
        /// <summary>
        /// Start playlist and change UVC state
        /// </summary>

        private void StartPlayback()
        {
            try
            {
                if (Playlist.CurrentTrackName == string.Empty)
                {
                    //If the task was cancelled we would have saved the current track and its position. We will try playback from there
                    var currenttrackname = ApplicationSettingsHelper.ReadResetSettingsValue(Constants.CurrentTrack);
                    var currenttrackposition = ApplicationSettingsHelper.ReadResetSettingsValue(Constants.Position);
                    if (currenttrackname != null)
                    {

                        if (currenttrackposition == null)
                        {
                            // play from start if we dont have position
                            Playlist.StartTrackAt((string)currenttrackname);
                        }
                        else
                        {
                            // play from exact position otherwise
                            Playlist.StartTrackAt((string)currenttrackname, TimeSpan.Parse((string)currenttrackposition));
                        }
                    }
                    else
                    {
                        //If we dont have anything, play from beginning of playlist.
                        Playlist.PlayAllTracks(); //start playback
                    }
                }
                else
                {
                    BackgroundMediaPlayer.Current.Play();
                }

                sendCurrentLink();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error start playback: " + ex.ToString());
            }
        }

        /// <summary>
        /// Fires when playlist changes to a new track
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void playList_TrackChanged(MyPlaylist sender, object args)
        {
            UpdateUVCOnNewTrack();
            ApplicationSettingsHelper.SaveSettingsValue(Constants.CurrentTrack, sender.CurrentTrackName);

            if (foregroundAppState == ForegroundAppStatus.Active)
            {
                //Message channel that can be used to send messages to foreground
                //ValueSet message = new ValueSet();
                //message.Add(Constants.Trackchanged, sender.CurrentTrackName);
                //BackgroundMediaPlayer.SendMessageToForeground(message);
                sendCurrentLink();
            }

        }

        /// <summary>
        /// Skip track and update UVC via SMTC
        /// </summary>
        private void SkipToPrevious()
        {
            systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Changing;
            Playlist.SkipToPrevious();
        }

        /// <summary>
        /// Skip track and update UVC via SMTC
        /// </summary>
        private void SkipToNext()
        {
            systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Changing;
            Playlist.SkipToNext();
        }

        #endregion

        #region Background Media Player Handlers
        void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
            else if (sender.CurrentState == MediaPlayerState.Paused)
            {
                systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
        }


        /// <summary>
        /// Fires when a message is recieved from the foreground app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                Debug.WriteLine("Key: " + key);
                switch (key.ToLower())
                {
                    case Constants.AppSuspended:
                        Debug.WriteLine("App suspending"); // App is suspended, you can save your task state at this point
                        foregroundAppState = ForegroundAppStatus.Suspended;
                        ApplicationSettingsHelper.SaveSettingsValue(Constants.CurrentTrack, Playlist.CurrentTrackName);
                        break;
                    case Constants.AppResumed:
                        Debug.WriteLine("App resuming"); // App is resumed, now subscribe to message channel
                        foregroundAppState = ForegroundAppStatus.Active;
                        break;
                    case Constants.StartPlayback: //Foreground App process has signalled that it is ready for playback
                        Debug.WriteLine("Starting Playback");
                        Playlist.InsertToFirst(e.Data[key] as string);
                        SendUpdate();
                        StartPlayback();
                        break;
                    case Constants.SkipNext: // User has chosen to skip track from app context.
                        Debug.WriteLine("Skipping to next");
                        SkipToNext();
                        break;
                    case Constants.SkipPrevious: // User has chosen to skip track from app context.
                        Debug.WriteLine("Skipping to previous");
                        SkipToPrevious();
                        break;
                    case Constants.RadioPlayback:
                        Debug.WriteLine("Starting Radio");
                        PlayRadio(e.Data[key] as string, true);
                        break;
                    case Constants.RadioPause:
                        Debug.WriteLine("Starting Radio");
                        PlayRadio(e.Data[key] as string, false);
                        break;
                    case Constants.PlayStreamSource:
                        Debug.WriteLine("Starting stream playback");
                        PlayStream(e.Data[key] as string);
                        break;
                    case Constants.AddToPlaylist:
                        Debug.WriteLine("Add new song to playlist from background");
                        AddSongToPlayList(e.Data[key] as string);
                        break;
                    case Constants.RemoveFromPlaylist:
                        Debug.WriteLine("Removeing song from playlist");
                        Playlist.RemoveFromList(e.Data[key] as string);
                        break;
                    case Constants.SongLink:
                        Debug.WriteLine("Song List Request");
                        sendCurrentLink();
                        break;
                }
            }
        }

        private void sendCurrentLink()
        {
            var vs = new ValueSet();
            if (playlistManager != null && playlistManager.Current != null && playlistManager.Current.PlaylistCount > 0)
            {
                vs.Add(Constants.SongLink, playlistManager.Current.CurrentLink);
                Debug.WriteLine("Avaliable: " + playlistManager.Current.CurrentLink);
            }
            else
            {
                vs.Add(Constants.SongLink, "");
                Debug.WriteLine("Not Avaliable");
            }

            BackgroundMediaPlayer.SendMessageToForeground(vs);
        }

        private void AddSongToPlayList(string link)
        {
            Playlist.AddToPlaylist(link);

            if (Playlist.PlaylistCount > 1)
            {
                systemmediatransportcontrol.IsNextEnabled = true;
                systemmediatransportcontrol.IsPreviousEnabled = true;
            }
            else
            {
                systemmediatransportcontrol.IsNextEnabled = false;
                systemmediatransportcontrol.IsPreviousEnabled = false;
            }

            systemmediatransportcontrol.DisplayUpdater.Type = MediaPlaybackType.Music;
            systemmediatransportcontrol.DisplayUpdater.MusicProperties.Title = Playlist.CurrentTrackName;
            systemmediatransportcontrol.DisplayUpdater.Update();

            ValueSet mesage = new ValueSet {
                {
                    Constants.AddedToPlaylistComplete, 
                    "0"
                }
            };
            BackgroundMediaPlayer.SendMessageToForeground(mesage);

            SendUpdate();
        }

        private void SendUpdate()
        {
            Debug.WriteLine("Update send to forground");
            ValueSet mesage2 = new ValueSet {
                {
                    Constants.PlaylistCount, 
                    Playlist.PlaylistCount
                }
            };
            BackgroundMediaPlayer.SendMessageToForeground(mesage2);
        }

        private void PlayRadio(string link, bool play)
        {
            if (play == false)
            {
                BackgroundMediaPlayer.Current.Pause();
                return;
            }

            var lnk = link.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var url = new Uri(lnk[0]);
            BackgroundMediaPlayer.Current.SetUriSource(url);

            systemmediatransportcontrol.IsPlayEnabled = true;
            systemmediatransportcontrol.IsPauseEnabled = true;
            systemmediatransportcontrol.IsNextEnabled = false;
            systemmediatransportcontrol.IsPreviousEnabled = false;
            systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Playing;
            systemmediatransportcontrol.DisplayUpdater.Type = MediaPlaybackType.Music;
            systemmediatransportcontrol.DisplayUpdater.MusicProperties.Title = lnk[1];
            systemmediatransportcontrol.DisplayUpdater.MusicProperties.Artist = lnk[2];
            systemmediatransportcontrol.DisplayUpdater.Update();

            BackgroundMediaPlayer.Current.Play();
        }

        private async void PlayStream(string fileName)
        {
            try
            {
                Debug.WriteLine("Streaming: " + fileName);
                //await new Windows.UI.Popups.MessageDialog("Stream: " + fileName).ShowAsync();
                var player = BackgroundMediaPlayer.Current;

                var file = await (await Windows.Storage.KnownFolders.SavedPictures.GetFolderAsync("BOMUSIC")).GetFileAsync(fileName);
                var prop = await file.Properties.GetMusicPropertiesAsync();
                player.AutoPlay = false;

                player.SetStreamSource((await file.OpenStreamForReadAsync()).AsRandomAccessStream());

                systemmediatransportcontrol.IsNextEnabled = false;
                systemmediatransportcontrol.IsPreviousEnabled = false;

                systemmediatransportcontrol.IsPlayEnabled = true;
                systemmediatransportcontrol.IsPauseEnabled = true;

                systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Playing;
                systemmediatransportcontrol.DisplayUpdater.Type = MediaPlaybackType.Music;
                systemmediatransportcontrol.DisplayUpdater.MusicProperties.Title = prop.Title;
                systemmediatransportcontrol.DisplayUpdater.MusicProperties.Artist = prop.Artist;
                systemmediatransportcontrol.DisplayUpdater.Update();

                player.Play();
            }
            catch
            {
                Debug.WriteLine("Error reading stream file");
            }
        }

        #endregion

    }
}
