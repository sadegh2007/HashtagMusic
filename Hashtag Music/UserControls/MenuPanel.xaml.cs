using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Hashtag_Music
{

    public enum PageType { MainPage, SearchPage, PlayListPage, MyPlaylistPage, VideoListPage, MP3ListPage, PodcastListPage , RadioPage, Other , MusicPlayer };

    public sealed partial class MenuPanel : UserControl
    {

        private PageType _currentPage = PageType.MainPage;

        public MenuPanel()
        {
            this.InitializeComponent();
            IsDisplayed = false;
            HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
            this.Width = 250;
        }

        public PageType CurrentPage { get { return _currentPage; } }

        public void Display()
        {
            if (Core.Settings.Login)
            {
                txtSign.Text = "SignOut";
            }
            else
            {
                txtSign.Text = "SignIn";
            }

            this.Visibility = Windows.UI.Xaml.Visibility.Visible;

            try
            {
                SlideAnimation(false);
            }
            catch (Exception) { }
        }

        public void Hide()
        {
            try
            {
                SlideAnimation(true);
            }
            catch (Exception) { }
        }

        public void SetCurrentPage(PageType pt) 
        {
            _currentPage = pt;
        }

        public bool IsDisplayed { get; set; }

        private void SlideAnimation(bool hide)
        {
            Storyboard s = new Storyboard();

            DoubleAnimation da = new DoubleAnimation();
            da.Duration = new Duration(TimeSpan.FromMilliseconds(200));

            if (hide)
            {
                da.To = -300;
            }
            else
                da.To = 0;

            s.Children.Add(da);

            Storyboard.SetTarget(da, transform);
            Storyboard.SetTargetProperty(da, "TranslateX");

            s.Completed += s_Completed;
            s.Begin();
        }

        private void SlideAnimation2(bool hide)
        {
            Storyboard s = new Storyboard();

            DoubleAnimation da = new DoubleAnimation();
            da.Duration = new Duration(TimeSpan.FromMilliseconds(200));

            if (hide)
            {
                da.To = -300;
            }
            else
                da.To = 0;

            s.Children.Add(da);

            DoubleAnimation datr = new DoubleAnimation();
            datr.Duration = new Duration(TimeSpan.FromMilliseconds(200));

            if (hide)
            {
                datr.To = -300;
            }
            else
                datr.To = 0;

            s.Children.Add(da);

            Storyboard.SetTarget(da, transform);
            Storyboard.SetTargetProperty(da, "TranslateX");

            Storyboard.SetTarget(datr, transform);
            Storyboard.SetTargetProperty(datr, "TranslateX");

            s.Completed += s_Completed;
            s.Begin();
        }

        void s_Completed(object sender, object e)
        {
            IsDisplayed = !IsDisplayed;
        }

        private async void Sign_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (txtSign.Text == "SignIn")
            {
                (Window.Current.Content as Frame).Navigate(typeof(LoginPage), "byPages");
            }
            else
            {
                var t = await Core.DataGetter.LogOut(Core.Settings.client);
                if (t == true)
                {
                    Core.Settings.Login = false;
                    Core.Settings.RememberLogin = false;
                    txtSign.Text = "SignIn";
                }
            }
            Hide();
        }

        private void Home_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_currentPage != PageType.MainPage)
                (Window.Current.Content as Frame).Navigate(typeof(MainPage));
            else
                Hide();
        }

        private void Video_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_currentPage != PageType.VideoListPage)
                (Window.Current.Content as Frame).Navigate(typeof(VMListPage), "Video");
            else
                Hide();
        }

        private void Mp3s_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_currentPage != PageType.MP3ListPage)
                (Window.Current.Content as Frame).Navigate(typeof(VMListPage), "Mp3");
            else
                Hide();
        }

        private void Podcast_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_currentPage != PageType.PodcastListPage)
                (Window.Current.Content as Frame).Navigate(typeof(PodcastListPage));
            else
                Hide();
        }

        private void PlayList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_currentPage != PageType.PlayListPage)
                (Window.Current.Content as Frame).Navigate(typeof(PlaylistPage));
            else
                Hide();
        }

        private void Radio_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_currentPage != PageType.RadioPage)
            {
                (Window.Current.Content as Frame).Navigate(typeof(RadioPlayerPage));
            }
            else
                Hide();
        }

        private void Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            (Window.Current.Content as Frame).Navigate(typeof(SettingsPage));
        }

        private void About_Tapped(object sender, TappedRoutedEventArgs e)
        {
            (Window.Current.Content as Frame).Navigate(typeof(AboutPage));
        }

        private void Downloads_Tapped(object sender, TappedRoutedEventArgs e)
        {
            (Window.Current.Content as Frame).Navigate(typeof(DownloadPage));
        }

        private void MyPlayList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (CurrentPage != PageType.MyPlaylistPage)
                (Window.Current.Content as Frame).Navigate(typeof(Playlist));
        }

    }
}
