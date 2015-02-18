using Core;
using Hashtag_Music.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Hashtag_Music
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideoViewPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private VideoItem data = null;
        string id = string.Empty;

        bool byClass = false;

        public VideoViewPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            headerPanel.SetMenuPanel(menuPanel);
            menuPanel.SetCurrentPage(PageType.Other);
            headerPanel.RefreshButton.Tapped += RefreshButton_Tapped;

            DisplayInformation disp = DisplayInformation.GetForCurrentView();
            disp.OrientationChanged += disp_OrientationChanged;
        }

        void RefreshButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            headerPanel.RefreshButtonVisibility = false;
            AddItem();
        }

        void disp_OrientationChanged(DisplayInformation sender, object args)
        {
            var or = sender.CurrentOrientation;
            if (or == DisplayOrientations.Portrait || or == DisplayOrientations.PortraitFlipped)
            {
                videoPlayer.IsFullWindow = false;
            }
            else if (or == DisplayOrientations.Landscape || or == DisplayOrientations.LandscapeFlipped)
            {
                videoPlayer.IsFullWindow = true;
            }
            else
            {
                videoPlayer.IsFullWindow = false;
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
            if (e.NavigationParameter.GetType() == typeof(VideoItem))
            {
                byClass = true;
                data = e.NavigationParameter as VideoItem;
                id = data.Id;
                data.Views = "Views: " + data.Views;
                data.Likes = "Likes: " + data.Likes;
                data.Dislikes = "Dislikes: " + data.Dislikes;
                this.DataContext = data;
                videoPlayer.PosterSource = new BitmapImage(new Uri(data.PhotoPlayer));
                if (Core.Settings.UseLowVideoQ == false)
                    videoPlayer.Source = new Uri(getLink(false));
                else
                    videoPlayer.Source = new Uri(getLink(true));
            }
            else
            {
                byClass = false;
                id = e.NavigationParameter as string;
                AddItem();
            }
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void AddItem()
        {
            loading.Visibility = Windows.UI.Xaml.Visibility.Visible;
            (loading.Children[0] as ProgressRing).IsActive = true;
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    data = await DataGetter.GetVideoById(id);
                    if (data == null)
                    {
                        Message("There is problem to get data please tray again later");
                        Frame.GoBack();
                    }
                    data.Views = "Views: " + data.Views;
                    data.Likes = "Likes: " + data.Likes;
                    data.Dislikes = "Dislikes: " + data.Dislikes;

                    this.DataContext = data;
                    videoPlayer.PosterSource = new BitmapImage(new Uri(data.PhotoPlayer));
                    if (Core.Settings.UseLowVideoQ == false)
                        videoPlayer.Source = new Uri(getLink(false));
                    else
                        videoPlayer.Source = new Uri(getLink(true));
                });
            }
            catch (Exception ex)
            {
                Message(ex.Message);
                headerPanel.RefreshButtonVisibility = true;
            }
            loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            (loading.Children[0] as ProgressRing).IsActive = false;
        }

        private string getLink(bool low)
        {
            var str = data.LowQ;

            if (low)
                str = str.Replace("/hls", "/lq");
            else
            {
                str = data.HighQ;
                str = str.Replace("/hls", "/hq");
            }

            str = str.Replace(".m3u8", ".mp4");
            return str;
        }

        private async void Message(string p)
        {
            await new MessageDialog(p).ShowAsync();
        }

        private void lstVideo_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(VideoViewPage), (e.ClickedItem as BaseItem).Id);
        }

        private async void LowQ_Download(object sender, TappedRoutedEventArgs e)
        {
            if (Settings.AskIEDownload)
            {
                var msg = new MessageDialog("Do you want download with IE?" + Environment.NewLine + "this option for who have problem to download with this app. if you don't want to see this message again go to settings and off this option.");
                msg.Commands.Add(new UICommand("Yes", async (a) =>
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(getLink(true)));
                }));
                msg.Commands.Add(new UICommand("No", (a) =>
                {
                    Frame.Navigate(typeof(DownloadPage), getLink(true));
                }));
                await msg.ShowAsync();
            }
            else
            {
                Frame.Navigate(typeof(DownloadPage), getLink(true));
            }
        }

        private async void HighQ_Download(object sender, TappedRoutedEventArgs e)
        {
            if (Settings.AskIEDownload)
            {
                var msg = new MessageDialog("Do you want download with IE?" + Environment.NewLine + "this option for who have problem to download with this app. if you don't want to see this message again go to settings and off this option.");
                msg.Commands.Add(new UICommand("Yes", async (a) =>
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(getLink(false)));
                }));
                msg.Commands.Add(new UICommand("No", (a) =>
                {
                    Frame.Navigate(typeof(DownloadPage), getLink(false));
                }));
                await msg.ShowAsync();
            }
            else
            {
                Frame.Navigate(typeof(DownloadPage), getLink(false));
            }
        }

        private void Share(object sender, TappedRoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += dataTransferManager_DataRequested;

            DataTransferManager.ShowShareUI();
        }

        private void dataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            request.Data.Properties.Title = "Download " + data.Title + " on Radio Javan" + Environment.NewLine + "https://radiojavan.com/videos/video/" + data.PremLink;

            request.Data.SetText(Environment.NewLine + "Share By #Music - Windows Phone");
        }

        private void Related(object sender, TappedRoutedEventArgs e)
        {
            if (byClass)
            {
                Frame.Navigate(typeof(RelatedPage), "V::" + data.Id);
            }
            else
            {
                Frame.Navigate(typeof(RelatedPage), data);
            }
        }

        private async void Like_Tapped(object sender, TappedRoutedEventArgs e)
        {
            loading.Visibility = Windows.UI.Xaml.Visibility.Visible;
            pbLoading.IsActive = true;
            try
            {
                if (Settings.Login)
                {
                    bool result = await DataGetter.VoteVideo(Settings.client, data.Id, true);
                    if (result == false)
                    {
                        Message("There is problem to like this song please check your logined before or not");
                    }
                    else
                    {
                        Message("Thank you for voting " + data.Title);
                    }
                }
                else
                {
                    Message("You must login first.");
                }
            }
            catch (Exception) { }
            pbLoading.IsActive = false;
            loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private async void dislike_Tapped(object sender, TappedRoutedEventArgs e)
        {
            loading.Visibility = Windows.UI.Xaml.Visibility.Visible;
            pbLoading.IsActive = true;
            try
            {
                if (Settings.Login)
                {
                    bool result = await DataGetter.VoteVideo(Settings.client, data.Id, false);
                    if (result == false)
                    {
                        Message("There is problem to like this song please check your logined before or not");
                    }
                    else
                    {
                        Message("Thank you for voting " + data.Title);
                    }
                }
                else
                {
                    Message("You must login first.");
                }
            }
            catch (Exception) { }
            pbLoading.IsActive = false;
            loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
    }
}
