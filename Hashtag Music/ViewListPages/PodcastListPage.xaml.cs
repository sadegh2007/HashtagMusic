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
    public sealed partial class PodcastListPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private Grid lastItem = null;

        PodcastType pdt = PodcastType.featured;

        public PodcastListPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            headerPanel.SetMenuPanel(menuPanel);
            menuPanel.SetCurrentPage(PageType.PodcastListPage);
            lastItem = gr1;
            headerPanel.RefreshButton.Tapped += RefreshButton_Tapped;
        }

        void RefreshButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (headerPanel.ShowLoading == false)
            {
                headerPanel.RefreshButtonVisibility = false;
                addItems(pdt);
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
            addItems(PodcastType.featured);
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
            Frame.Navigate(typeof(MusicPlayerPage), e.ClickedItem as MP3Item);
        }

        private async void addItems(PodcastType p)
        {
            headerPanel.RefreshButtonVisibility = false;
            headerPanel.ShowLoading = true;
            try
            {
                (pivot.Items[pivot.SelectedIndex] as PivotItem).DataContext = await DataGetter.GetPodcastByType(p, "0");
            }
            catch (Exception ex)
            {
                headerPanel.ShowLoading = false;
                Message(ex.Message);
                headerPanel.RefreshButtonVisibility = true;
            }
            headerPanel.ShowLoading = false;
        }

        private async void Message(string p)
        {
            await new Windows.UI.Popups.MessageDialog(p).ShowAsync();
        }

        private void searchType_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var t = sender as Grid;
            var tag = t.Tag.ToString();
            try
            {
                if (tag == "1")
                {
                    pivot.SelectedIndex = 3;
                }
                else if (tag == "2")
                {
                    pivot.SelectedIndex = 2;
                }
                else if (tag == "3")
                {
                    pivot.SelectedIndex = 1;
                }
                else if (tag == "4")
                {
                    pivot.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                headerPanel.ShowLoading = false;
                Message(ex.Message);
                headerPanel.RefreshButtonVisibility = true;
            }
        }

        private void SetColor(Grid current)
        {
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
            headerPanel.RefreshButtonVisibility = false;
            if (pivot.SelectedIndex == 0)
            {
                pdt = PodcastType.shows;
                SetColor(gr4);
                addItems(pdt);
            }
            else if (pivot.SelectedIndex == 1)
            {
                pdt = PodcastType.dance;
                SetColor(gr3);
                addItems(pdt);
            }
            else if (pivot.SelectedIndex == 2)
            {
                pdt = PodcastType.popular;
                SetColor(gr2);
                addItems(pdt);
            }
            else
            {
                pdt = PodcastType.featured;
                SetColor(gr1);
                addItems(pdt);
            }
        }

        private void lstVideo_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            menuPanel.Hide();
        }
    }
}
