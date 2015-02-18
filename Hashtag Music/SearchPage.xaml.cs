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
    public sealed partial class SearchPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private SearchResult result = null;

        private Grid lastItem = null;

        public SearchPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            headerPanel.GoSearchPage = false;
            headerPanel.SetMenuPanel(menuPanel);
            headerPanel.SearchTextBox.KeyUp += SearchTextBox_KeyUp;
            lastItem = gr1;
            headerPanel.RefreshButton.Tapped += RefreshButton_Tapped;
        }

        void RefreshButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (headerPanel.ShowLoading == true) return;
            
            headerPanel.RefreshButtonVisibility = false;
            AddItems();
        }

        void SearchTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AddItems();
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
            headerPanel.SearchText = e.NavigationParameter as string;
            AddItems();
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

        private void searchType_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (result == null || sender == null)
                return;

            var tag = int.Parse((sender as Grid).Tag.ToString()) - 1;
            pivot.SelectedIndex = tag;

        }

        private void setButtonColor(Grid s)
        {
            // is current item
            if (s.Name == lastItem.Name) return;

            // set button colors
            (lastItem.Children[0] as SymbolIcon).Foreground = App.Current.Resources["darkForground"] as SolidColorBrush;
            (lastItem.Children[1] as TextBlock).Foreground = App.Current.Resources["darkForground"] as SolidColorBrush;
            lastItem.Background = new SolidColorBrush(Colors.Transparent);

            (s.Children[0] as SymbolIcon).Foreground = App.Current.Resources["lightForground"] as SolidColorBrush;
            (s.Children[1] as TextBlock).Foreground = App.Current.Resources["lightForground"] as SolidColorBrush;
            s.Background = App.Current.Resources["TitlebarColor"] as SolidColorBrush;
            lastItem = s;
        }

        private async void Message(string content)
        {
            await new Windows.UI.Popups.MessageDialog(content).ShowAsync();
        }

        private async void AddItems()
        {
            headerPanel.ShowLoading = true;
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    result = await Core.DataGetter.GetSearchResult(headerPanel.SearchText);
                    p1.DataContext = result.Videos;
                    p2.DataContext = result.Mp3s;
                    p3.DataContext = result.Podcasts;
                    artistItem.ItemsSource = result.Artists;
                });
            }
            catch (Exception ex)
            {
                result = null;
                Message(ex.Message);
                headerPanel.ShowLoading = false;
                headerPanel.RefreshButtonVisibility = true;
            }
            headerPanel.ShowLoading = false;
        }

        private void lstVideo_ItemClick(object sender, ItemClickEventArgs e)
        {
            var t = e.ClickedItem;
            if (t.GetType() == typeof(VideoItem))
            {
                Frame.Navigate(typeof(VideoViewPage), (t as VideoItem));
            }
            else if (t.GetType() == typeof(MP3Item))
            {
                Frame.Navigate(typeof(MusicPlayerPage), (t as MP3Item));
            }
            else if (t.GetType() == typeof(Podcast))
            {
                Frame.Navigate(typeof(MusicPlayerPage), (t as Podcast));
            }
        }

        private void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            menuPanel.Hide();
            var tag = pivot.SelectedIndex + 1;
            if (tag == 1)
            {
                setButtonColor(gr1);
            }
            else if (tag == 2)
            {
                setButtonColor(gr2);
            }
            else if (tag == 3)
            {
                setButtonColor(gr3);
            }
            else if (tag == 4)
            {
                setButtonColor(gr4);
            }

        }

        private void lstVideo_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            menuPanel.Hide();
        }
    }
}
