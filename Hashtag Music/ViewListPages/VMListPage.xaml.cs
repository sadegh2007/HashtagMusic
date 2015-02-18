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
    public sealed partial class VMListPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        string itemType = string.Empty;
        string id = string.Empty;
        bool plItems = false;
        ListType type = ListType.featured;

        Grid lastItem = null;

        public VMListPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            headerPanel.SetMenuPanel(menuPanel);
            headerPanel.RefreshButton.Tapped += RefreshButton_Tapped;
            lastItem = gr1;
        }

        void RefreshButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (headerPanel.ShowLoading == true) return;
            headerPanel.RefreshButtonVisibility = false;
            if (plItems)
            {
                AddPlaylistItem();
            }
            else
            {
                AddItem(pivot.Items[pivot.SelectedIndex] as PivotItem);
            }
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
            var str = (e.NavigationParameter as string).ToLower();

            if (str.StartsWith("pl") == false)
            {
                pivot.Visibility = Windows.UI.Xaml.Visibility.Visible;
                lstVideo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                plItems = false;
                itemType = str;
                id = str;
                pivot.SelectedIndex = 3;
                gridLst.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                pivot.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                lstVideo.Visibility = Windows.UI.Xaml.Visibility.Visible;
                gridLst.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                var t = str.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                plItems = true;
                itemType = t[1];
                id = t[2];
                AddPlaylistItem();
            }
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
            BaseItem it = e.ClickedItem as BaseItem;
            if (e.ClickedItem.GetType() == typeof(VideoItem))
            {
                Frame.Navigate(typeof(VideoViewPage), it as VideoItem);
            }
            else
            {
                Frame.Navigate(typeof(MusicPlayerPage), it as MP3Item);
            }
        }

        private async void AddItem(PivotItem pv)
        {
            headerPanel.ShowLoading = true;
            try
            {
                if (itemType == "video")
                {
                    c4.Width = GridLength.Auto;
                    gr4.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    (gr3.Children[0] as TextBlock).Text = "Latest";
                    pv.DataContext = await DataGetter.GetVideosByType(type, "0");
                    menuPanel.SetCurrentPage(PageType.VideoListPage);
                }
                else if (itemType == "mp3")
                {
                    c4.Width = new GridLength(95);
                    (gr3.Children[0] as TextBlock).Text = "Trending";
                    gr4.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    pv.DataContext = await DataGetter.GetMP3sByType(type, "0");
                    menuPanel.SetCurrentPage(PageType.MP3ListPage);
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
                headerPanel.RefreshButtonVisibility = true;
            }
            headerPanel.ShowLoading = false;
        }

        private async void AddPlaylistItem()
        {
            headerPanel.ShowLoading = false;
            headerPanel.ShowLoading = true;
            try
            {
                if (itemType == "video")
                {
                    lstVideo.ItemsSource = await DataGetter.GetVideoPlayList(id);
                    menuPanel.SetCurrentPage(PageType.VideoListPage);
                }
                else if (itemType == "mp3")
                {
                    lstVideo.ItemsSource = await DataGetter.GetMP3PlayList(id);
                    menuPanel.SetCurrentPage(PageType.MP3ListPage);
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
                headerPanel.ShowLoading = false;
                headerPanel.RefreshButtonVisibility = true;
            }
            headerPanel.ShowLoading = false;
        }

        private async void Message(string p)
        {
            menuPanel.Hide();
            await new Windows.UI.Popups.MessageDialog(p).ShowAsync();
        }

        private void searchType_Video(object sender, TappedRoutedEventArgs e)
        {
            var t = sender as Grid;

            try
            {
                var tag = t.Tag.ToString();

                if (tag == "1") 
                {
                    type = ListType.featured;
                }
                else if (tag == "2")
                {
                    type = ListType.popular;
                }
                else if (tag == "3")
                {
                    if (itemType == "video")
                        type = ListType.latest;
                    else
                        type = ListType.trending;
                }
                else if (tag == "4")
                {
                    type = ListType.albums;
                }
                
            }
            catch (Exception ex)
            {
                Message(ex.Message);
                headerPanel.RefreshButtonVisibility = true;
            }            
        }

        private void SetColor(Grid current)
        {
            if (current.Name == lastItem.Name) return;

            (lastItem.Children[0] as TextBlock).Foreground = App.Current.Resources["darkForground"] as SolidColorBrush;
            lastItem.Background = new SolidColorBrush(Colors.Transparent);

            (current.Children[0] as TextBlock).Foreground = App.Current.Resources["lightForground"] as SolidColorBrush;
            current.Background = App.Current.Resources["TitlebarColor"] as SolidColorBrush;
            lastItem = current;
        }

        private void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            menuPanel.Hide();
            headerPanel.ShowLoading = false;

            if (pivot.SelectedIndex == 0)
            {
                type = ListType.albums;
                SetColor(gr4);
            }
            else if (pivot.SelectedIndex == 1)
            {
                if (itemType == "video")
                    type = ListType.latest;
                else
                    type = ListType.trending;
                SetColor(gr3);
            }
            else if (pivot.SelectedIndex == 2)
            {
                type = ListType.popular;
                SetColor(gr2);
            }
            else
            {
                type = ListType.featured;
                SetColor(gr1);
            }
            AddItem(pivot.Items[pivot.SelectedIndex] as PivotItem);
        }

        private void lstVideo_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            menuPanel.Hide();
        }
    }
}
