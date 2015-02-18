using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using System.IO;
using System.Diagnostics;

namespace Core
{
    public class Settings
    {
        private static bool _isPlaying = false;
        private static bool _isOpenApp = false;
        private static MP3Item _currentplaying = null;


        public static bool RememberLogin
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey("_REMEMBER"))
                {
                    RememberLogin = false;
                }
                string retVal = localSettings.Values["_REMEMBER"].ToString();
                if (string.IsNullOrEmpty(retVal))
                    return false;
                return retVal == "0" ? false : true;
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["_REMEMBER"] = value == true ? "1" : "0";
            }
        }

        public static bool Login
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey("_LOGIN"))
                {
                    Login = false;
                }
                string retVal = localSettings.Values["_LOGIN"].ToString();
                if (string.IsNullOrEmpty(retVal))
                    return false;
                return retVal == "0" ? false : true;
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["_LOGIN"] = value == true ? "1" : "0";
            }
        }

        public static bool UseLowAudioQ
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey("_ULAQ"))
                {
                    UseLowAudioQ = false;
                }
                string retVal = localSettings.Values["_ULAQ"].ToString();
                if (string.IsNullOrEmpty(retVal))
                    return false;
                return retVal == "0" ? false : true;
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["_ULAQ"] = value == true ? "1" : "0";
            }
        }

        public static bool UseLowVideoQ
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey("_ULVQ"))
                {
                    UseLowVideoQ = false;
                }
                string retVal = localSettings.Values["_ULVQ"].ToString();
                if (string.IsNullOrEmpty(retVal))
                    return false;
                return retVal == "0" ? false : true;
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["_ULVQ"] = value == true ? "1" : "0";
            }
        }

        public static bool AskIEDownload
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey("_ASKIEDOWN"))
                {
                    AskIEDownload = true;
                }
                string retVal = localSettings.Values["_ASKIEDOWN"].ToString();
                if (string.IsNullOrEmpty(retVal))
                    return true;
                return retVal == "0" ? false : true;
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["_ASKIEDOWN"] = value == true ? "1" : "0";
            }
        }

        public static int WitchServerSaved
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey("_WSERVER"))
                {
                    WitchServerSaved = 0;
                }
                string retVal = localSettings.Values["_WSERVER"].ToString();
                if (string.IsNullOrEmpty(retVal))
                    return 0;
                return 0;
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["_WSERVER"] = value;
            }
        }

        public static string GetServer()
        {
            if (WitchServerSaved == 0)
            {
                return "https://www.radiojavan.com/";
            }
            else
            {
                return "http://198.98.182.149";
            }
        }

        public static int OpenCount
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey("_count"))
                {
                    OpenCount = 0;
                }
                string retVal = localSettings.Values["_count"].ToString();
                if (string.IsNullOrEmpty(retVal))
                    return 0;
                return int.Parse(retVal);
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["_count"] = value.ToString();
            }
        }

        public static string CurrentSongId
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey("_CID"))
                {
                    AskIEDownload = true;
                }
                string retVal = localSettings.Values["_CID"].ToString();
                if (string.IsNullOrEmpty(retVal))
                    return "";
                return retVal;
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["_CID"] = value;
            }
        }

        public async static Task<UserDef> GetUserData()
        {
            var jsonSer = new DataContractJsonSerializer(typeof(UserDef));
            var folder = await Package.Current.InstalledLocation.GetFolderAsync("User");
            var myStream = await folder.OpenStreamForReadAsync("userdata.json");
            return (UserDef)jsonSer.ReadObject(myStream);
        }

        public async static Task SaveUserData(UserDef data)
        {
            var ser = new DataContractJsonSerializer(typeof(UserDef));
            var folder = await Package.Current.InstalledLocation.CreateFolderAsync("User", CreationCollisionOption.OpenIfExists);
            using (var stream = await folder.OpenStreamForWriteAsync(
                "userdata.json",
                CreationCollisionOption.ReplaceExisting))
            {
                ser.WriteObject(stream, data);
            }
        }

        public static StreamAddress StreamAd { get; set; }

        public static Windows.Networking.BackgroundTransfer.BackgroundDownloader downloader { get; set; }

        public static List<Windows.Networking.BackgroundTransfer.DownloadOperation> activeDownloads { get; set; }

        public static List<System.Threading.CancellationTokenSource> ctses { get; set; }

        public static MP3Item CurrentPlaying
        {
            get
            {
                return _currentplaying;
            }
            set
            {
                _currentplaying = value;
            }
        }

        public static bool IsPlaying { get { return _isPlaying; } set { _isPlaying = value;} }

        public static bool IsOpenApp { get { return _isOpenApp; } set { _isOpenApp = value; } }

        public static bool IsShowDownloadHelp
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (!localSettings.Values.ContainsKey("_ftd"))
                {
                    IsShowDownloadHelp = false;
                }
                string retVal = localSettings.Values["_ftd"].ToString();
                if (string.IsNullOrEmpty(retVal))
                    return false;
                return int.Parse(retVal) == 1 ? true : false;
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["_ftd"] = value == true ? "1" : "0";
            }
        }

        public async static Task<CustomPlaylist> GetPlaylist()
        {
            try
            {
                var jsonSer = new DataContractJsonSerializer(typeof(CustomPlaylist));
                var myStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync("playlists.json");
                return (CustomPlaylist)jsonSer.ReadObject(myStream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("playlist load error: " + ex.Message);
                return new CustomPlaylist();
            }
        }

        public async static Task SavePlaylist(CustomPlaylist data)
        {
            var ser = new DataContractJsonSerializer(typeof(CustomPlaylist));
            using (var stream = await ApplicationData.Current.LocalFolder.OpenStreamForWriteAsync(
                "playlists.json",
                CreationCollisionOption.ReplaceExisting))
            {
                ser.WriteObject(stream, data);
            }
        }

        public static CustomPlaylist CurrentPlaylist { get; set; }

        public static Windows.Web.Http.HttpClient client { get; set; }

        public static string AppTitle { get { return "HashtagMusic"; } }

        public async static Task<MP3Item> GetLastPlayed()
        {
            try
            {
                var jsonSer = new DataContractJsonSerializer(typeof(MP3Item));
                var myStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync("lastplay.json");
                return (MP3Item)jsonSer.ReadObject(myStream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("last played load error: " + ex.Message);
                return null;
            }
        }

        public async static Task SaveLastPlayed(MP3Item data)
        {
            try
            {
                var ser = new DataContractJsonSerializer(typeof(MP3Item));
                using (var stream = await ApplicationData.Current.LocalFolder.OpenStreamForWriteAsync(
                    "lastplay.json",
                    CreationCollisionOption.ReplaceExisting))
                {
                    ser.WriteObject(stream, data);
                }
            } catch (Exception) { }
        }
    }


    [DataContract]
    public class UserDef
    {
        [DataMember(Name = "username")]
        public string username { get; set; }

        [DataMember(Name = "pass")]
        public string password { get; set; }
    }
}
