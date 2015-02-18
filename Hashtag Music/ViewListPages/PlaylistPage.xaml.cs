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
using Windows.UI;
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
    public sealed partial class PlaylistPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        bool isMp3Search = false;

        Grid lastItem = null;

        public PlaylistPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            headerPanel.SetMenuPanel(menuPanel);
            menuPanel.SetCurrentPage(PageType.PlayListPage);
            headerPanel.RefreshButton.Tapped += RefreshButton_Tapped;
            lastItem = vi;
        }

        void RefreshButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (headerPanel.ShowLoading == false)
            {
                headerPanel.RefreshButtonVisibility = false;
                getData(!isMp3Search);
            }
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
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
            getData(true);
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

        private void searchType_Video(object sender, TappedRoutedEventArgs e)
        {
            SetColor(sender as Grid);
            pivot.SelectedIndex = 1;
        }

        private void searchType_MP3(object sender, TappedRoutedEventArgs e)
        {
            SetColor(sender as Grid);
            pivot.SelectedIndex = 0;
        }

        private void SetColor(Grid current)
        {
            if (current.Name == lastItem.Name) return;

            lastItem.Background = new SolidColorBrush(Colors.Transparent);
            (lastItem.Children[0] as SymbolIcon).Foreground = App.Current.Resources["darkForground"] as SolidColorBrush;
            (lastItem.Children[1] as TextBlock).Foreground = App.Current.Resources["darkForground"] as SolidColorBrush;

            current.Background = App.Current.Resources["TitlebarColor"] as SolidColorBrush;
            (current.Children[0] as SymbolIcon).Foreground = App.Current.Resources["lightForground"] as SolidColorBrush;
            (current.Children[1] as TextBlock).Foreground = App.Current.Resources["lightForground"] as SolidColorBrush;

            lastItem = current;
        }

        private async void getData(bool video)
        {
            headerPanel.ShowLoading = true;
            try
            {
                if (video)
                {
                    isMp3Search = false;
                    var t = await Core.DataGetter.GetFeaturedVideoPlaylist();
                    Debug.WriteLine("count: " + t.Items.Count);
                    lstVideo.ItemsSource = t.Items;
                }
                else
                {
                    isMp3Search = true;
                    lstMusic.ItemsSource = (await Core.DataGetter.GetFeaturedMp3Playlist()).Items;
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
                headerPanel.RefreshButtonVisibility = true;
            }
            headerPanel.ShowLoading = false;
        }

        private async void Message(string content)
        {
            await new Windows.UI.Popups.MessageDialog(content).ShowAsync();
        }

        private void lstVideo_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as FeaturedPlaylistItem;
            if (item.Type == ItemType.Video)
            {
                Frame.Navigate(typeof(VMListPage), "PL::Video::" + item.Id);
            }
            else if (item.Type == ItemType.Mp3)
            {
                Frame.Navigate(typeof(VMListPage), "PL::MP3::" + item.Id);
            }
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            menuPanel.Hide();
            if (pivot.SelectedIndex == 0)
            {
                SetColor(mp);
                getData(false);
            }
            else
            {
                SetColor(vi);
                getData(true);
            }
        }

        private void lstMusic_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            menuPanel.Hide();
        }
    }
}
