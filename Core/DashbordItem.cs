using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public enum ItemType {  Video = 0, Mp3 = 1, Podcast = 2 };

    public enum ListType { featured, popular, latest, trending, albums }

    public enum PodcastType { featured, dance, popular, shows};

    [DataContract]
    public class BaseItem
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "photo_player")]
        public string PhotoPlayer { get; set; }

        [DataMember(Name = "artist")]
        public string Artist { get; set; }

        [DataMember(Name = "song")]
        public string Song { get; set; }

        [DataMember(Name = "permlink")]
        public string PremLink { get; set; }

        [DataMember(Name = "created_at")]
        public string CreatedAt { get; set; }

        [DataMember(Name = "likes")]
        public string Likes { get; set; }

        [DataMember(Name = "dislikes")]
        public string Dislikes { get; set; }

        [DataMember(Name = "type")]
        public ItemType Type { get; set; }

        [DataMember(Name = "photo")]
        public string Photo { get; set; }

        [IgnoreDataMember()]
        public string DiplayName { 
            get 
            {
                var retStr = Song;
                if (string.IsNullOrWhiteSpace(Song))
                    retStr = Title;
                if (retStr.Length > 17 && !(retStr.Length <= 19))
                    retStr = retStr.Substring(0, 17) + "...";
                return retStr;
            } 
        }
    }

    [DataContract]
    public class VideoItem : BaseItem
    {
        public VideoItem() { Related = new List<VideoItem>(); }

        public VideoItem(BaseItem _base) 
        {
            this.Title = _base.Title;
            this.Id = _base.Id;
            this.Artist = _base.Artist;
            this.Song = _base.Song;
            this.Likes = _base.Likes;
            this.Dislikes = _base.Dislikes;
            this.Photo = _base.Photo;
            this.PhotoPlayer = _base.PhotoPlayer;
            this.PremLink = _base.PremLink;
            this.Type = _base.Type;
            this.CreatedAt = _base.CreatedAt;
            Related = new List<VideoItem>();
        }

        [DataMember(Name = "low")]
        public string LowQ { get; set; }

        [DataMember(Name = "high")]
        public string HighQ { get; set; }

        [DataMember(Name = "photo_large")]
        public string LargePhoto { get; set; }

        [DataMember(Name = "views")]
        public string Views { get; set; }

        [DataMember(Name = "low_web")]
        public string LowWeb { get; set; }

        [DataMember(Name = "high_web")]
        public string HighWeb { get; set; }

        [DataMember(Name = "related")]
        public List<VideoItem> Related { get; set; }

        [IgnoreDataMember()]
        public Windows.UI.Xaml.Controls.Symbol Icon { get { return Windows.UI.Xaml.Controls.Symbol.SlideShow; } }

        public void ConvertToVideo(BaseItem _base)
        {
            this.Title = _base.Title;
            this.Id = _base.Id;
            this.Artist = _base.Artist;
            this.Song = _base.Song;
            this.Likes = _base.Likes;
            this.Dislikes = _base.Dislikes;
            this.Photo = _base.Photo;
            this.PhotoPlayer = _base.PhotoPlayer;
            this.PremLink = _base.PremLink;
            this.Type = _base.Type;
            this.CreatedAt = _base.CreatedAt;
        }
    }

    [DataContract]
    public class MP3Item : BaseItem
    {
        public MP3Item() 
        {
            Related = new List<MP3Item>();
        }

        public MP3Item(BaseItem _base) 
        {
            this.Title = _base.Title;
            this.Id = _base.Id;
            this.Artist = _base.Artist;
            this.Song = _base.Song;
            this.Likes = _base.Likes;
            this.Dislikes = _base.Dislikes;
            this.Photo = _base.Photo;
            this.PhotoPlayer = _base.PhotoPlayer;
            this.PremLink = _base.PremLink;
            this.Type = _base.Type;
            this.CreatedAt = _base.CreatedAt;
            Related = new List<MP3Item>();
        }

        public MP3Item(Podcast _base)
        {
            this.Title = _base.Title;
            this.Id = _base.Id;
            this.Artist = _base.Artist;
            this.Song = _base.Song;
            this.Likes = _base.Likes;
            this.Dislikes = _base.Dislikes;
            this.Photo = _base.Photo;
            this.PhotoPlayer = _base.PhotoPlayer;
            this.PremLink = _base.PremLink;
            this.Type = _base.Type;
            this.CreatedAt = _base.CreatedAt;
            this.Thumbnail = _base.Thumbnail;
            this.Photo_240 = _base.Photo_240;
            this.Link = _base.Link;
            this.Plays = _base.Plays;
            this.DownloadsCount = _base.DownloadsCount;
            this.Type = ItemType.Mp3;
            this.Related = new List<MP3Item>();
        }

        [DataMember(Name = "link")]
        public string Link { get; set; }

        [DataMember(Name = "photo_240")]
        public string Photo_240 { get; set; }

        [DataMember(Name = "thumbnail")]
        public string Thumbnail { get; set; }

        [DataMember(Name = "plays")]
        public string Plays { get; set; }

        [DataMember(Name = "downloads")]
        public string DownloadsCount { get; set; }

        [DataMember(Name = "related")]
        public List<MP3Item> Related { get; set; }

        [DataMember(Name = "lyric")]
        public string Lyric { get; set; }

        [IgnoreDataMember()]
        public Windows.UI.Xaml.Controls.Symbol Icon { get { return Windows.UI.Xaml.Controls.Symbol.Audio; } }

        public void ConvertToMP3(BaseItem _base)
        {
            this.Title = _base.Title;
            this.Id = _base.Id;
            this.Artist = _base.Artist;
            this.Song = _base.Song;
            this.Likes = _base.Likes;
            this.Dislikes = _base.Dislikes;
            this.Photo = _base.Photo;
            this.PhotoPlayer = _base.PhotoPlayer;
            this.PremLink = _base.PremLink;
            this.Type = _base.Type;
            this.CreatedAt = _base.CreatedAt;
        }
        public void ConvertPodcastToMp3(Podcast _base)
        {
            this.Title = _base.Title;
            this.Id = _base.Id;
            this.Artist = _base.Artist;
            this.Song = _base.Song;
            this.Likes = _base.Likes;
            this.Dislikes = _base.Dislikes;
            this.Photo = _base.Photo;
            this.PhotoPlayer = _base.PhotoPlayer;
            this.PremLink = _base.PremLink;
            this.Type = _base.Type;
            this.CreatedAt = _base.CreatedAt;
            this.Thumbnail = _base.Thumbnail;
            this.Photo_240 = _base.Photo_240;
            this.Link = _base.Link;
            this.Plays = _base.Plays;
            this.DownloadsCount = _base.DownloadsCount;
            this.Type = ItemType.Mp3;
        }
    }

    [DataContract]
    public class Podcast : MP3Item
    {
        public Podcast() 
        { 
            this.Related = new List<MP3Item>();
        }

        public Podcast(MP3Item _base) 
        {
            this.Title = _base.Title;
            this.Id = _base.Id;
            this.Artist = _base.Artist;
            this.Song = _base.Song;
            this.Likes = _base.Likes;
            this.Dislikes = _base.Dislikes;
            this.Photo = _base.Photo;
            this.PhotoPlayer = _base.PhotoPlayer;
            this.PremLink = _base.PremLink;
            this.Type = _base.Type;
            this.CreatedAt = _base.CreatedAt;
            this.Thumbnail = _base.Thumbnail;
            this.Photo_240 = _base.Photo_240;
            this.Link = _base.Link;
            this.Plays = _base.Plays;
            this.DownloadsCount = _base.DownloadsCount;
            this.Related = new List<MP3Item>();
            this.Lyric = _base.Lyric;
        }

        [DataMember(Name = "date")]
        public string Date { get; set; }

        [DataMember(Name = "short_date")]
        public string ShortDate { get; set; }

        [DataMember(Name = "photo_large")]
        public string PhotoLarge { get; set; }

        [IgnoreDataMember()]
        public new Windows.UI.Xaml.Controls.Symbol Icon { get { return Windows.UI.Xaml.Controls.Symbol.Memo; } }

        public void ConvertToPodcast(MP3Item _base)
        {
            this.Title = _base.Title;
            this.Id = _base.Id;
            this.Artist = _base.Artist;
            this.Song = _base.Song;
            this.Likes = _base.Likes;
            this.Dislikes = _base.Dislikes;
            this.Photo = _base.Photo;
            this.PhotoPlayer = _base.PhotoPlayer;
            this.PremLink = _base.PremLink;
            this.Type = _base.Type;
            this.CreatedAt = _base.CreatedAt;
            this.Thumbnail = _base.Thumbnail;
            this.Photo_240 = _base.Photo_240;
            this.Link = _base.Link;
            this.Plays = _base.Plays;
            this.DownloadsCount = "0";
            this.Lyric = _base.Lyric;

            if (this.Related == null)
                Related = new List<MP3Item>();
            else
                this.Related = _base.Related;
        }
    }

    [DataContract]
    public class Dashbord
    {
        public Dashbord()
        {
            Items = new List<BaseItem>();
        }

        [DataMember(Name = "DashbordItems")]
        public List<BaseItem> Items { get; set; }
    }

    [DataContract]
    public class DashbordSliderItem
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "subtitle")]
        public string Subtitle { get; set; }

        [DataMember(Name = "line")]
        public string Line { get; set; }

        [DataMember(Name = "photo")]
        public string Photo { get; set; }

        [DataMember(Name = "artist")]
        public string Artist { get; set; }

        [DataMember(Name = "song")]
        public string Song { get; set; }

        [DataMember(Name = "type")]
        public ItemType Type { get; set; }

        [IgnoreDataMember()]
        public Windows.UI.Xaml.Media.Imaging.BitmapImage Image { get { return new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(Photo)); } }
    }

    [DataContract]
    public class DashbordSlider
    {
        public DashbordSlider()
        {
            Items = new List<DashbordSliderItem>();
        }

        [DataMember (Name = "sliderItems")]
        public List<DashbordSliderItem> Items { get; set; }
    }

    [DataContract]
    public class FeaturedPlaylistItem
    {
        public FeaturedPlaylistItem() { Thumbs = new List<string>(); }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "thumbs")]
        public List<string> Thumbs { get; set; }

        [DataMember(Name = "count")]
        public int Count { get; set; }

        [DataMember(Name = "type")]
        public ItemType Type { get; set; }

        [DataMember(Name = "photo")]
        public string Photo { get; set; }
    }

    [DataContract]
    public class Featurelaylist
    {
        public Featurelaylist()
        {
            Items = new List<FeaturedPlaylistItem>();
        }
        [DataMember(Name = "featuredMp3List")]
        public List<FeaturedPlaylistItem> Items { get; set; }
    }

    [DataContract]
    public class RadioNowPlayingItem
    {
        [DataMember(Name = "photo")]
        public string Photo { get; set; }

        [DataMember(Name = "artist")]
        public string Artist { get; set; }

        [DataMember(Name = "song")]
        public string Song { get; set; }

        [DataMember(Name = "like")]
        public string Likes { get; set; }

        [DataMember(Name = "dislike")]
        public string Dislikes { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "thumb")]
        public string Thumb { get; set; }

        [DataMember(Name = "mp3id")]
        public string Mp3Id { get; set; }
    }

    [DataContract]
    public class RadioNowPlaying
    {
        public RadioNowPlaying()
        {
            Items = new List<RadioNowPlayingItem>();
        }

        [DataMember(Name = "nowplaying")]
        public List<RadioNowPlayingItem> Items { get; set; }
    }

    [DataContract]
    public class SearchArtistItem
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "photo")]
        public string Photo { get; set; }
    }

    public class SearchResult
    {
        public SearchResult()
        {
            Artists = new List<SearchArtistItem>();
            Videos = new List<VideoItem>();
            Mp3s = new List<MP3Item>();
            Podcasts = new List<Podcast>();
        }

        public List<SearchArtistItem> Artists { get; set; }

        public List<VideoItem> Videos { get; set; }

        public List<MP3Item> Mp3s { get; set; }

        public List<Podcast> Podcasts { get; set; }
    }

    public class StreamAddress
    {
        public string LowQ { get; set; }

        public string HighQ { get; set; }
    }

    [DataContract]
    public class CustomPlaylist
    {
        public CustomPlaylist()
        {
            Items = new List<BaseItem>();
        }

        [DataMember(Name = "data")]
        public List<BaseItem> Items { get; private set; }

        public void AddToList(MP3Item item)
        {
            if (Items.Contains(item) == false)
                Items.Add(item);
        }
    }
}
