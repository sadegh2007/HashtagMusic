using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Web.Http;

namespace Core
{
    public class DataGetter
    {

        // http://198.98.182.149
        public static string server = Settings.GetServer();

        public static async Task<Dashbord> GetDashbord(int page)
        {
            Dashbord lst = new Dashbord();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/iphone_dashboard?page=" + page.ToString();
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonArray jsonArray = JsonArray.Parse(jsonText);

                foreach (JsonValue groupValue in jsonArray)
                {
                    JsonObject groupObject = groupValue.GetObject();
                    JsonObject itemObject = groupObject.GetObject();

                    string type = itemObject["type"].GetString();

                    BaseItem _base = new BaseItem();

                    _base.Id = itemObject["id"].GetNumber().ToString();
                    _base.Title = itemObject["title"].GetString();
                    _base.Photo = itemObject["photo"].GetString();
                    _base.PhotoPlayer = itemObject["photo_player"].GetString();

                    if (type != "podcast")
                    {
                        _base.Artist = itemObject["artist"].GetString();
                        _base.Song = itemObject["song"].GetString();
                    }
                    _base.PremLink = itemObject["permlink"].GetString();
                    _base.CreatedAt = itemObject["created_at"].GetString();
                    _base.Likes = itemObject["likes"].GetString();
                    _base.Dislikes = itemObject["dislikes"].GetString();

                    if (type == "mp3" || type == "podcast" || type == "mp3s" || type == "podcasts")
                    {
                        MP3Item mp3 = new MP3Item();
                        mp3.Thumbnail = itemObject["thumbnail"].GetString();
                        mp3.Link = itemObject["link"].GetString();
                        mp3.Photo_240 = itemObject["photo_240"].GetString();
                        mp3.Plays = itemObject["plays"].GetString();

                        mp3.ConvertToMP3(_base);

                        if (type == "podcast")
                        {
                            var pod = new Podcast(mp3);
                            pod.Type = ItemType.Podcast;

                            pod.PhotoLarge = itemObject["photo_large"].GetString();
                            pod.Date = itemObject["date"].GetString();
                            pod.ShortDate = itemObject["short_date"].GetString();

                            lst.Items.Add(pod);
                        }
                        else
                        {
                            mp3.DownloadsCount = itemObject["downloads"].GetString();
                            mp3.Type = ItemType.Mp3;
                            lst.Items.Add(mp3);
                        }
                    }
                    else
                    {
                        VideoItem video = new VideoItem(_base);
                        video.Type = ItemType.Video;

                        video.LowQ = itemObject["low"].GetString();
                        video.LowWeb = itemObject["low_web"].GetString();
                        video.HighQ = itemObject["high"].GetString();
                        video.HighWeb = itemObject["high_web"].GetString();
                        video.LargePhoto = itemObject["photo_large"].GetString();
                        video.Views = itemObject["views"].GetString();

                        lst.Items.Add(video);
                    }
                }
                client.Dispose();
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<DashbordSlider> GetDashbordSlider()
        {
            DashbordSlider lst = new DashbordSlider();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url =  server + "/api2/iphone_slider";
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonArray jsonArray = JsonArray.Parse(jsonText);

                foreach (JsonValue groupValue in jsonArray)
                {
                    JsonObject groupObject = groupValue.GetObject();
                    JsonObject itemObject = groupObject.GetObject();

                    DashbordSliderItem item = new DashbordSliderItem();

                    item.Artist = itemObject["artist"].GetString();
                    item.Id = itemObject["id"].GetNumber().ToString();

                    if (itemObject["line"].ValueType != JsonValueType.Null && itemObject["line"].ValueType == JsonValueType.String)
                        item.Line = itemObject["line"].GetString();

                    item.Photo = itemObject["photo"].GetString();
                    item.Song = itemObject["song"].GetString();
                    item.Subtitle = itemObject["subtitle"].GetString();
                    item.Title = itemObject["title"].GetString();
                    var type = itemObject["type"].GetString();

                    if (type == "mp3" || type == "mp3s")
                        item.Type = ItemType.Mp3;
                    else if (type == "podcast" || type == "podcasts")
                        item.Type = ItemType.Podcast;
                    else
                        item.Type = ItemType.Video;

                    lst.Items.Add(item);
                }
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<Featurelaylist> GetFeaturedMp3Playlist()
        {
            Featurelaylist lst = new Featurelaylist();


            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/mp3_playlists_featured";

            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonArray jsonArray = JsonArray.Parse(jsonText);

                foreach (JsonValue groupValue in jsonArray)
                {
                    JsonObject groupObject = groupValue.GetObject();
                    JsonObject itemObject = groupObject.GetObject();

                    FeaturedPlaylistItem item = new FeaturedPlaylistItem();

                    item.Id = itemObject["id"].GetString();
                    item.Name = itemObject["name"].GetString();
                    item.Count = (int)itemObject["count"].GetNumber();
                    item.Photo = itemObject["photo"].GetString();
                    item.Type = ItemType.Mp3;
                    var array2 = itemObject["thumbs"].GetArray();
                    foreach (var t in array2)
                    {
                        item.Thumbs.Add(t.GetString());
                    }
                    lst.Items.Add(item);
                }
                client.Dispose();
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<Featurelaylist> GetFeaturedVideoPlaylist()
        {
            Featurelaylist lst = new Featurelaylist();

            //Debug.WriteLine("0");
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/video_playlists_featured";

            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonArray jsonArray = JsonArray.Parse(jsonText);

                foreach (JsonValue groupValue in jsonArray)
                {
                    JsonObject groupObject = groupValue.GetObject();
                    JsonObject itemObject = groupObject.GetObject();

                    FeaturedPlaylistItem item = new FeaturedPlaylistItem();
                    item.Id = itemObject["id"].GetString();
                    item.Name = itemObject["name"].GetString();
                    item.Count = (int)itemObject["count"].GetNumber();
                    item.Photo = itemObject["photo"].GetString();
                    item.Type = ItemType.Video;

                    var array2 = itemObject["thumbs"].GetArray();

                    foreach (var t in array2)
                    {
                        if (t.ValueType == JsonValueType.String)
                            item.Thumbs.Add(t.GetString());
                    }
                    lst.Items.Add(item);
                }
                client.Dispose();
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<MP3Item>> GetMP3PlayList(string featuredId)
        {
            List<MP3Item> lst = new List<MP3Item>();

            string url = server + "api2/mp3_playlist?id=" + featuredId;
            try
            {
                string jsonText = await DownloadPage(url);

                JsonArray jsonArray = JsonArray.Parse(jsonText);

                foreach (JsonValue groupValue in jsonArray)
                {
                    JsonObject groupObject = groupValue.GetObject();
                    JsonObject itemObject = groupObject.GetObject();

                    lst.Add(GetMp3Item(itemObject));
                }
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<List<VideoItem>> GetVideoPlayList(string featuredId)
        {
            List<VideoItem> lst = new List<VideoItem>();

            string url = server + "api2/video_playlist?id=" + featuredId;
            try
            {
                string jsonText = await DownloadPage(url);

                JsonArray jsonArray = JsonArray.Parse(jsonText);

                foreach (JsonValue groupValue in jsonArray)
                {
                    JsonObject groupObject = groupValue.GetObject();
                    JsonObject itemObject = groupObject.GetObject();

                    lst.Add(GetVideoItem(itemObject));
                }
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<MP3Item> GetMp3ById(string id)
        {
            try
            {
                string url = server + "api2/mp3?id=" + id;

                string jsonText = await DownloadPage(url);

                JsonObject groupObject = JsonObject.Parse(jsonText);
                JsonObject itemObject = groupObject.GetObject();

                var mp3 = GetMp3Item(itemObject);

                var t = itemObject["related"];
                if (t.ValueType == JsonValueType.Array)
                {
                    foreach (var mp3Item in t.GetArray())
                    {
                        mp3.Related.Add(GetMp3Item(mp3Item.GetObject()));
                    }
                }

                if (itemObject.ContainsKey("lyric"))
                {
                    try
                    {
                        mp3.Lyric = itemObject["lyric"].GetString();
                    }
                    catch (Exception) { mp3.Lyric = string.Empty; }
                }
                else
                {
                    mp3.Lyric = string.Empty;
                }

                return mp3;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<VideoItem> GetVideoById(string id)
        {
            try
            {
                string url = server + "api2/video?id=" + id;

                string jsonText = await DownloadPage(url); // await client.GetStringAsync(new Uri(url));

                JsonObject groupObject = JsonObject.Parse(jsonText);
                JsonObject itemObject = groupObject.GetObject();

                VideoItem video = GetVideoItem(itemObject);

                var t = itemObject["related"];
                if (t.ValueType == JsonValueType.Array)
                {
                    foreach (var videoItem in t.GetArray())
                    {
                        video.Related.Add(GetVideoItem(videoItem.GetObject()));
                    }
                }
                return video;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<Podcast> GetPodcastById(string id)
        {
            try
            {
                string url = server + "api2/podcast?id=" + id;

                string jsonText = await DownloadPage(url);

                JsonObject groupObject = JsonObject.Parse(jsonText);
                JsonObject itemObject = groupObject.GetObject();

                Podcast pod = GetPodcastItem(itemObject);

                var t = itemObject["related"];
                if (t.ValueType == JsonValueType.Array)
                {
                    foreach (var podItem in t.GetArray())
                    {
                        pod.Related.Add(GetPodcastItem(podItem.GetObject()));
                    }
                }
                return pod;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<StreamAddress> GetStream()
        {
            StreamAddress lst = new StreamAddress();

            //Debug.WriteLine("0");
            HttpClient client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
                string url = server + "api2/streams";
                // Debug.WriteLine("Url: " + url);
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonObject groupObject = JsonObject.Parse(jsonText);
                JsonObject itemObject = groupObject.GetObject();

                lst.LowQ = itemObject["lq"].GetString();
                lst.HighQ = itemObject["hq"].GetString();
                client.Dispose();
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<RadioNowPlaying> GetRadioNowPlaying()
        {
            RadioNowPlaying lst = new RadioNowPlaying();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/radio_nowplaying";
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonArray jsonArray = JsonArray.Parse(jsonText);

                foreach (JsonValue groupValue in jsonArray)
                {
                    JsonObject groupObject = groupValue.GetObject();
                    JsonObject itemObject = groupObject.GetObject();

                    RadioNowPlayingItem item = new RadioNowPlayingItem();

                    item.Id = itemObject["id"].GetString();
                    item.Song = itemObject["song"].GetString();
                    item.Artist = itemObject["artist"].GetString();
                    item.Dislikes = itemObject["dislikes"].GetString();
                    item.Likes = itemObject["likes"].GetString();
                    item.Photo = itemObject["photo"].GetString();
                    item.Thumb = itemObject["thumb"].GetString();

                    if (itemObject.ContainsKey("mp3id"))
                        item.Mp3Id = itemObject["mp3id"].GetString();
                    else
                        item.Mp3Id = string.Empty;

                    lst.Items.Add(item);
                }
                client.Dispose();
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<SearchResult> GetSearchResult(string searchText)
        {
            SearchResult lst = new SearchResult();

            string url = server + "api2/search?query=" + WebUtility.UrlEncode(searchText);
            try
            {
                string jsonText = await DownloadPage(url);

                JsonObject js = JsonObject.Parse(jsonText);

                if (js["artists"].ValueType == JsonValueType.Array)
                {
                    JsonArray jsonArray = js["artists"].GetArray();

                    foreach (JsonValue groupValue in jsonArray)
                    {
                        JsonObject groupObject = groupValue.GetObject();
                        JsonObject itemObject = groupObject.GetObject();

                        SearchArtistItem item = new SearchArtistItem();
                        item.Name = itemObject["name"].GetString();
                        item.Photo = itemObject["photo"].GetString();

                        lst.Artists.Add(item);
                    }
                }

                if (js["videos"].ValueType == JsonValueType.Array)
                {
                    JsonArray jsonArray = js["videos"].GetArray();

                    foreach (JsonValue groupValue in jsonArray)
                    {
                        JsonObject groupObject = groupValue.GetObject();
                        JsonObject itemObject = groupObject.GetObject();

                        lst.Videos.Add(GetVideoItem(itemObject));
                    }
                }

                if (js["mp3s"].ValueType == JsonValueType.Array)
                {
                    JsonArray jsonArray = js["mp3s"].GetArray();

                    foreach (JsonValue groupValue in jsonArray)
                    {
                        JsonObject groupObject = groupValue.GetObject();
                        JsonObject itemObject = groupObject.GetObject();

                        lst.Mp3s.Add(GetMp3Item(itemObject));
                    }
                }

                if (js["podcasts"].ValueType == JsonValueType.Array)
                {
                    JsonArray jsonArray = js["podcasts"].GetArray();

                    foreach (JsonValue groupValue in jsonArray)
                    {
                        JsonObject groupObject = groupValue.GetObject();
                        JsonObject itemObject = groupObject.GetObject();

                        lst.Podcasts.Add(GetPodcastItem(itemObject));
                    }
                }
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static MP3Item GetMp3Item(JsonObject itemObject)
        {
            BaseItem _base = new BaseItem();

            _base.Id = itemObject["id"].GetNumber().ToString();
            _base.Title = itemObject["title"].GetString();
            _base.Photo = itemObject["photo"].GetString();
            _base.PhotoPlayer = itemObject["photo_player"].GetString();
            _base.Artist = itemObject["artist"].GetString();
            _base.Song = itemObject["song"].GetString();

            _base.PremLink = itemObject["permlink"].GetString();
            _base.CreatedAt = itemObject["created_at"].GetString();
            _base.Likes = itemObject["likes"].GetString();
            _base.Dislikes = itemObject["dislikes"].GetString();
            _base.Type = ItemType.Mp3;

            MP3Item mp3 = new MP3Item();
            mp3.Thumbnail = itemObject["thumbnail"].GetString();
            mp3.Link = itemObject["link"].GetString();
            mp3.Photo_240 = itemObject["photo_240"].GetString();
            mp3.Plays = itemObject["plays"].GetString();
            mp3.DownloadsCount = itemObject["downloads"].GetString();

            mp3.ConvertToMP3(_base);
            return mp3;
        }
        private static VideoItem GetVideoItem(JsonObject itemObject)
        {
            BaseItem _base = new BaseItem();

            _base.Id = itemObject["id"].GetNumber().ToString();
            _base.Title = itemObject["title"].GetString();
            _base.Photo = itemObject["photo"].GetString();
            _base.PhotoPlayer = itemObject["photo_player"].GetString();
            _base.Artist = itemObject["artist"].GetString();
            _base.Song = itemObject["song"].GetString();

            _base.PremLink = itemObject["permlink"].GetString();
            _base.CreatedAt = itemObject["created_at"].GetString();
            _base.Likes = itemObject["likes"].GetString();
            _base.Dislikes = itemObject["dislikes"].GetString();
            _base.Type = ItemType.Video;

            VideoItem video = new VideoItem();
            video.LowQ = itemObject["low"].GetString();
            video.LowWeb = itemObject["low_web"].GetString();
            video.HighQ = itemObject["high"].GetString();
            video.HighWeb = itemObject["high_web"].GetString();
            video.LargePhoto = itemObject["photo_large"].GetString();
            video.Views = itemObject["views"].GetString();

            video.ConvertToVideo(_base);

            return video;
        }
        private static Podcast GetPodcastItem(JsonObject itemObject)
        {
            Podcast pod = new Podcast();
            pod.Id = itemObject["id"].GetNumber().ToString();
            pod.Title = itemObject["title"].GetString();
            pod.Photo = itemObject["photo"].GetString();
            pod.PhotoPlayer = itemObject["photo_player"].GetString();

            pod.PremLink = itemObject["permlink"].GetString();
            pod.CreatedAt = itemObject["created_at"].GetString();
            pod.Likes = itemObject["likes"].GetString();
            pod.Dislikes = itemObject["dislikes"].GetString();
            pod.Type = ItemType.Podcast;

            pod.Thumbnail = itemObject["thumbnail"].GetString();
            pod.Link = itemObject["link"].GetString();
            pod.Photo_240 = itemObject["photo_240"].GetString();
            pod.Plays = itemObject["plays"].GetString();

            pod.PhotoLarge = itemObject["photo_large"].GetString();
            pod.Date = itemObject["date"].GetString();
            pod.ShortDate = itemObject["short_date"].GetString();

            return pod;
        }

        public static async Task<bool> Login(string email, string pass)
        {
            Settings.client = new HttpClient();
            Settings.client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/login?login_email=" + email + "&login_password=" + pass;
            try
            {
                string jsonText = await Settings.client.GetStringAsync(new Uri(url));

                JsonObject obj = JsonObject.Parse(jsonText);

                if (obj["success"].GetBoolean() == true)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static async Task<bool> LogOut(HttpClient client)
        {
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/logout";
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonObject obj = JsonObject.Parse(jsonText);

                if (obj["success"].GetBoolean() == true)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static async Task<string> SignUp(string firstName, string lastName, string email, string emailConfirmation, string pass)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/signup?firstname={0}&lastname={1}&email={2}&email_confirm={3}&password={4}";
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(string.Format(url, firstName, lastName, email, emailConfirmation, pass)));

                JsonObject obj = JsonObject.Parse(jsonText);

                if (obj["success"].GetBoolean() == true)
                {
                    return "BO::FINISHED";
                }

                if (obj.ContainsKey("msg"))
                {
                    return obj["msg"].GetString();
                }

                return "BO::INVALID";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static async Task<bool> VoteMp3(HttpClient client, string id, bool like)
        {
            string vote = like ? "1" : "5";

            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/mp3_vote?id=" + id + "&vote=" + vote;
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonObject obj = JsonObject.Parse(jsonText);

                if (obj["success"].GetBoolean() == true)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static async Task<bool> VoteVideo(HttpClient client, string id, bool like)
        {
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string vote = like ? "1" : "5";
            string url = server + "api2/video_vote?id=" + id + "&vote=" + vote;
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonObject obj = JsonObject.Parse(jsonText);

                if (obj["success"].GetBoolean() == true)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static async Task<bool> VotePodcast(HttpClient client, string id, bool like)
        {
            string vote = like ? "1" : "5";
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/podcast_vote?id=" + id + "&vote=" + vote;
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonObject obj = JsonObject.Parse(jsonText);

                if (obj["success"].GetBoolean() == true)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static async Task<bool> VoteRadio(HttpClient client, string id, bool like)
        {
            string vote = like ? "1" : "5";
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/radio_vote?song=" + id + "&vote=" + vote;
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonObject obj = JsonObject.Parse(jsonText);

                if (obj["success"].GetBoolean() == true)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<List<VideoItem>> GetVideosByType(ListType type, string page)
        {
            List<VideoItem> lst = new List<VideoItem>();
            
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/videos?type=" + type.ToString() + "&page=" + page;
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonArray jsonArray = JsonArray.Parse(jsonText);

                foreach (JsonValue groupValue in jsonArray)
                {
                    JsonObject groupObject = groupValue.GetObject();
                    JsonObject itemObject = groupObject.GetObject();

                    lst.Add(GetVideoItem(itemObject));
                }
                client.Dispose();
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<List<MP3Item>> GetMP3sByType(ListType type, string page)
        {
            List<MP3Item> lst = new List<MP3Item>();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/mp3s?type=" + type.ToString() + "&page=" + page;
            try
            {
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonArray jsonArray = JsonArray.Parse(jsonText);

                foreach (JsonValue groupValue in jsonArray)
                {
                    JsonObject groupObject = groupValue.GetObject();
                    JsonObject itemObject = groupObject.GetObject();

                    lst.Add(GetMp3Item(itemObject));
                }
                client.Dispose();
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<List<Podcast>> GetPodcastByType(PodcastType type, string page)
        {
            List<Podcast> lst = new List<Podcast>();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string url = server + "api2/podcasts?type=" + type.ToString() + "&page=" + page;
            try
            {
                Debug.WriteLine("url: " + url);
                string jsonText = await client.GetStringAsync(new Uri(url));

                JsonArray jsonArray = JsonArray.Parse(jsonText);

                foreach (JsonValue groupValue in jsonArray)
                {
                    JsonObject groupObject = groupValue.GetObject();
                    JsonObject itemObject = groupObject.GetObject();

                    lst.Add(GetPodcastItem(itemObject));
                }
                client.Dispose();
                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static async Task<string> DownloadPage(string url)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Headers["Accept-Encoding"] = "gzip";
                req.Headers["Accept-Language"] = "en-us";
                req.Credentials = CredentialCache.DefaultNetworkCredentials;

                using (WebResponse response = await req.GetResponseAsync())
                {
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(response.GetResponseStream()))
                    {
                        return reader.ReadToEnd();
                    }
                }

            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
