using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Popups;
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
    public sealed partial class HeaderPanel : UserControl
    {
        MenuPanel _panel;
        Core.VideoItem _video;
        Core.MP3Item _mp3;
        Storyboard lo;
        Storyboard tray;
        bool _isLoading = false;

        public HeaderPanel()
        {
            this.InitializeComponent();
            GoSearchPage = true;
            _mp3 = null;
            _video = null;
            refreshButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            _ref.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            _isLoading = false;
            _panel = null;
            lo = new Storyboard();
            tray = new Storyboard();
            refreshButton.Tapped += refreshButton_Tapped;
        }

        void refreshButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.ShowLoading = false;
        }

        private void Search(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (GoSearchPage)
                    (Window.Current.Content as Frame).Navigate(typeof(SearchPage), (sender as TextBox).Text);
                this.Focus(FocusState.Programmatic);
            }
        }

        private async void Menu(object sender, TappedRoutedEventArgs e)
        {
            if (_panel == null)
            {
                await new MessageDialog("Developer please set menu panel first. if you not developer of this app please send and report to this app developer.").ShowAsync();
                return;
            }
            else
            {
                if (_panel.IsDisplayed == false)
                {
                    _panel.Display();
                }
                else
                {
                    _panel.Hide();
                }
            }
        }

        public void SetMenuPanel(MenuPanel panel)
        {
            _panel = panel;
        }

        public string SearchText { get { return txtSearch.Text; } set { txtSearch.Text = value; } }

        public TextBox SearchTextBox { get { return txtSearch; } }

        public bool GoSearchPage { get; set; }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_panel.IsDisplayed)
                _panel.Hide();
        }

        public bool HideSearch 
        {
            get
            {
                return txtSearch.Visibility == Windows.UI.Xaml.Visibility.Visible;
            }
        }

        public Core.VideoItem VideoDetailPanel 
        {
            get
            {
                return _video;
            }
            set
            {
                if (value == null)
                {
                    detailPan.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    txtSearch.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    _mp3 = null;
                    txtSearch.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    detailPan.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    txtTitle.Text = value.Title;
                    txtLikes.Text = value.Likes + " likes";
                    txtDislikes.Text = value.Dislikes + " dislikes";
                    txtViews.Text = value.Views + " views";
                    _video = value;
                }
            }
        }

        public Core.MP3Item MP3DetailPanel
        {
            get
            {
                return _mp3;
            }
            set
            {
                if (value == null)
                {
                    detailPan.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    txtSearch.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    _video = null;
                    txtSearch.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    detailPan.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    txtTitle.Text = value.Title;
                    txtLikes.Text = value.Likes + " likes";
                    txtDislikes.Text = value.Dislikes + " dislikes";
                    txtViews.Text = value.Plays + " plays";
                    _mp3 = value;
                }
            }
        }

        public Grid RefreshButton { get { return refreshButton; } }

        public bool RefreshButtonVisibility 
        {
            get
            {
                return refreshButton.Visibility == Windows.UI.Xaml.Visibility.Visible;
            }
            set
            {
                if (RefreshButtonVisibility == value) return;
                lo.Stop();
                tray.Stop();

                if (value == true)
                {
                    _ref.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                    refreshButton.Opacity = 0;
                    refreshButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                try
                {
                    RefreshButtonAnimation(value, refreshButton);
                }
                catch (Exception) 
                {
                    if (value)
                    {
                        refreshButton.Opacity = 1;
                        refreshButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    }
                    else
                    {
                        refreshButton.Opacity = 0;
                        refreshButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    }
                }
            }
        }

        public bool CurrentGo { get; set; }

        private void RefreshButtonAnimation(bool show, UIElement el)
        {
            tray.Stop();
            lo.Stop();
            tray = new Storyboard();

            CurrentGo = show;
            DoubleAnimation da = new DoubleAnimation();
            if (show)
                da.To = 1.0;
            else
                da.To = 0.0;
            da.Duration = new Duration(TimeSpan.FromMilliseconds(100));

            Storyboard.SetTarget(da, el);
            Storyboard.SetTargetProperty(da, "Opacity");

            tray.Children.Add(da);

            tray.Completed += s_Completed;
            tray.Begin();
        }

        void s_Completed(object sender, object e)
        {
            if (refreshButton.Opacity == 0)
            {
                refreshButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            } 
            else
            {
                refreshButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        public bool ShowLoading 
        {
            get
            {
                return _isLoading;
            }
            set
            {
                if (_isLoading == value) return;
                tray.Stop();
                lo.Stop();
                refreshButton.Opacity = 0;
                RefreshButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                try
                {
                    if (value)
                    {
                        _ref.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        LoadingAnimation();
                    }
                    else
                    {
                        _ref.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    }
                }
                catch (Exception) { }
                _isLoading = value;
            }
        }

        private void LoadingAnimation()
        {
            tray.Stop();
            lo.Stop();
            lo = new Storyboard();

            _ref.Visibility = Windows.UI.Xaml.Visibility.Visible;
            _ref.Opacity = 1;

            DoubleAnimation da = new DoubleAnimation();
            da.To = 360;
            da.Duration = new Duration(TimeSpan.FromMilliseconds(300));

            SineEase ease = new SineEase() { EasingMode = EasingMode.EaseOut };
            da.EasingFunction = ease;

            Storyboard.SetTarget(da, rotAn);
            Storyboard.SetTargetProperty(da, "Rotation");

            lo.Children.Add(da);
            lo.RepeatBehavior = RepeatBehavior.Forever;
            lo.Completed += lo_Completed;
            lo.Begin();
        }

        void lo_Completed(object sender, object e)
        {
            rotAn.Rotation = 0;
        }
    }
}
