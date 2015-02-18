using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Playback;

namespace Core
{
    /// <summary>
    /// Manage playlist information. For simplicity of this sample, we allow only one playlist
    /// </summary>
    public sealed class MyPlaylistManager
    {
        #region Private members
        private static MyPlaylist instance;
        #endregion

        #region Playlist management methods/properties
        public MyPlaylist Current
        {
            get
            {
                if (instance == null)
                {
                    instance = new MyPlaylist();
                }
                return instance;
            }
        }

        /// <summary>
        /// Clears playlist for re-initialization
        /// </summary>
        public void ClearPlaylist()
        {
            instance = null;
        }
        #endregion
    }

    /// <summary>
    /// Implement a playlist of tracks. 
    /// If instantiated in background task, it will keep on playing once app is suspended
    /// </summary>
    public sealed class MyPlaylist
    {
        #region Private members
        static List<string> tracks = new List<string>();

        int CurrentTrackId = -1;
        private MediaPlayer mediaPlayer;
        private TimeSpan startPosition = TimeSpan.FromSeconds(0);
        internal MyPlaylist()
        {
            mediaPlayer = BackgroundMediaPlayer.Current;
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.CurrentStateChanged += mediaPlayer_CurrentStateChanged;
            mediaPlayer.MediaFailed += mediaPlayer_MediaFailed;
        }

        #endregion

        #region Public properties, events and handlers
        /// <summary>
        /// Get the current track name
        /// </summary>
        public string CurrentTrackName
        {
            get
            {
                if (CurrentTrackId == -1)
                {
                    return String.Empty;
                }
                if (CurrentTrackId < tracks.Count)
                {
                    string fullUrl = tracks[CurrentTrackId];

                    return fullUrl.Split('/')[fullUrl.Split('/').Length - 1];
                }
                else
                    throw new ArgumentOutOfRangeException("Track Id is higher than total number of tracks");
            }
        }

        private string GetCurrentTrackNameFromLink(string fullUrl) 
        {
            return fullUrl.Split('/')[fullUrl.Split('/').Length - 1];
        }

        /// <summary>
        /// Invoked when the media player is ready to move to next track
        /// </summary>
        public event TypedEventHandler<MyPlaylist, object> TrackChanged;

        public string CurrentLink
        {
            get
            {
                if (CurrentTrackId == -1)
                {
                    return String.Empty;
                }
                return tracks[CurrentTrackId];
            }
        }
        #endregion

        #region MediaPlayer Handlers
        /// <summary>
        /// Handler for state changed event of Media Player
        /// </summary>
        void mediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {

            if (sender.CurrentState == MediaPlayerState.Playing && startPosition != TimeSpan.FromSeconds(0))
            {
                // if the start position is other than 0, then set it now
                sender.Position = startPosition;
                sender.Volume = 1.0;
                startPosition = TimeSpan.FromSeconds(0);
                sender.PlaybackMediaMarkers.Clear();
            }
        }

        /// <summary>
        /// Fired when MediaPlayer is ready to play the track
        /// </summary>
        void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            // wait for media to be ready
            sender.Play();
            Debug.WriteLine("New Track" + this.CurrentTrackName);
            TrackChanged.Invoke(this, CurrentTrackName);
        }

        /// <summary>
        /// Handler for MediaPlayer Media Ended
        /// </summary>
        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            SkipToNext();
        }

        private void mediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Debug.WriteLine("Failed with error code " + args.ExtendedErrorCode.ToString());
        }
        #endregion

        #region Playlist command handlers
        /// <summary>
        /// Starts track at given position in the track list
        /// </summary>
        private void StartTrackAt(int id)
        {
            string source = tracks[id];
            CurrentTrackId = id;
            mediaPlayer.AutoPlay = false;
            mediaPlayer.SetUriSource(new Uri(source));
        }

        /// <summary>
        /// Starts a given track by finding its name
        /// </summary>
        public void StartTrackAt(string TrackName)
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].Contains(TrackName))
                {
                    string source = tracks[i];
                    CurrentTrackId = i;
                    mediaPlayer.AutoPlay = false;
                    mediaPlayer.SetUriSource(new Uri(source));
                }
            }
        }

        /// <summary>
        /// Starts a given track by finding its name and at desired position
        /// </summary>
        public void StartTrackAt(string TrackName, TimeSpan position)
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].Contains(TrackName))
                {
                    CurrentTrackId = i;
                    break;
                }
            }

            mediaPlayer.AutoPlay = false;

            // Set the start position, we set the position once the state changes to playing, 
            // it can be possible for a fraction of second, playback can start before we are 
            // able to seek to new start position
            mediaPlayer.Volume = 1;
            startPosition = position;
            mediaPlayer.SetUriSource(new Uri(tracks[CurrentTrackId]));
        }

        /// <summary>
        /// Play all tracks in the list starting with 0 
        /// </summary>
        public void PlayAllTracks()
        {
            StartTrackAt(0);
        }

        /// <summary>
        /// Skip to next track
        /// </summary>
        public void SkipToNext()
        {
            StartTrackAt((CurrentTrackId + 1) % tracks.Count);
        }

        /// <summary>
        /// Skip to next track
        /// </summary>
        public void SkipToPrevious()
        {
            if (CurrentTrackId == 0)
            {
                StartTrackAt(CurrentTrackId);
            }
            else
            {
                StartTrackAt(CurrentTrackId - 1);
            }
        }

        /// <summary>
        /// Add song to end of playlist
        /// </summary>
        /// <param name="link"></param>
        public void AddToPlaylist(string link)
        {
            Debug.WriteLine("Add: " + link);
            if (tracks.Contains(link) == false)
            {
                tracks.Add(link);
            }
        }

        /// <summary>
        /// Add song to first of playlist
        /// </summary>
        /// <param name="link">Media Url</param>
        public void InsertToFirst(string link)
        {
            Debug.WriteLine("Insert: " + link);

            // IS currentlly playing
            if (tracks.Count > 0)
                if (tracks[0] == link) return;

            if (tracks.Contains(link))
            {
                tracks.RemoveAt(tracks.IndexOf(link));
            }
            tracks.Insert(0, link);
            StartTrackAt(0);
        }

        /// <summary>
        /// Count of playlist
        /// </summary>
        public int PlaylistCount { get { return tracks.Count; } }

        /// <summary>
        /// Remove all song from playlist
        /// </summary>
        public void ClearList()
        {
            try
            {
                tracks.Clear();
            }
            catch (Exception) { }
        }

        public List<string> SongList { get { return tracks; } }

        /// <summary>
        /// Remove music from playlist
        /// </summary>
        /// <param name="link"></param>
        public void RemoveFromList(string link)
        {
            if (tracks.Contains(link))
            {
                bool change = false;
                if (CurrentTrackName == GetCurrentTrackNameFromLink(link) && BackgroundMediaPlayer.IsMediaPlaying())
                {
                    BackgroundMediaPlayer.Current.Pause();
                    change = true;
                }

                tracks.Remove(link);

                if (change)
                    StartTrackAt(0);
            }
        }



        #endregion

    }
}
