using Core;
using Hashtag_Music.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
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
    public sealed partial class RelatedPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        string id = string.Empty;
        string type = "mp3";

        public RelatedPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            headerPanel.SetMenuPanel(menuPanel);
            menuPanel.SetCurrentPage(PageType.Other);
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
            var g = e.NavigationParameter.GetType();
            if (g != typeof(string))
            {
                var t = e.NavigationParameter as BaseItem;
                if (t.Type == ItemType.Mp3 || t.Type == ItemType.Podcast)
                {
                    lstVideo.ItemTemplate = App.Current.Resources["RelatedMP3Item"] as DataTemplate;
                    lstVideo.ItemsSource = (e.NavigationParameter as MP3Item).Related;
                }
                else
                {
                    lstVideo.ItemTemplate = App.Current.Resources["RelatedItem"] as DataTemplate;
                    lstVideo.ItemsSource = (e.NavigationParameter as VideoItem).Related;
                }
            }
            else
            {
                id = e.NavigationParameter as string;
                headerPanel.RefreshButton.Tapped += RefreshButton_Tapped;
                if (id.StartsWith("M::"))
                {
                    type = "mp3";
                    id = id.Replace("M::", string.Empty);
                    lstVideo.ItemTemplate = App.Current.Resources["RelatedMP3Item"] as DataTemplate;
                }
                else
                {
                    type = "video";
                    id = id.Replace("V::", string.Empty);
                    lstVideo.ItemTemplate = App.Current.Resources["RelatedItem"] as DataTemplate;
                }
                GetData();
            }
        }

        void RefreshButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (headerPanel.ShowLoading == true) return;

            headerPanel.RefreshButtonVisibility = false;
            GetData();
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void lstVideo_ItemClick(object sender, ItemClickEventArgs e)
        {
            var t = e.ClickedItem as BaseItem;

            if (t.Type == ItemType.Mp3)
            {
                Frame.Navigate(typeof(MusicPlayerPage), t.Id);
            }
            else if (t.Type == ItemType.Video)
            {
                Frame.Navigate(typeof(VideoViewPage), t.Id);
            }
        }

        private async void GetData()
        {
            headerPanel.ShowLoading = true;
            try
            {
                if (type == "mp3")
                {
                    var data = (await DataGetter.GetMp3ById(id));
                    lstVideo.ItemsSource = (await DataGetter.GetMp3ById(id)).Related;


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
                            var img = new ImageBrush() { Opacity = 0.2, AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Center, ImageSource = new Windows.UI.Xaml.Media.Imaging.BitmapImage() { UriSource = new Uri(link) }, Stretch = Stretch.UniformToFill };
                            this.Background = img;
                        }
                    }
                    catch (Exception) { }

                }
                else
                {
                    lstVideo.ItemsSource = (await DataGetter.GetVideoById(id)).Related;
                }
            }
            catch
            {
                headerPanel.RefreshButtonVisibility = true;
            }
            headerPanel.ShowLoading = false;
        }

        private void lstVideo_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            menuPanel.Hide();
        }
    }
}
