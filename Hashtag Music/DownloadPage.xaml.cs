using Core;
using Hashtag_Music.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Hashtag_Music
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DownloadPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private BackgroundTransferGroup notificationGroup;
        DispatcherTimer _timer;

        public DownloadPage()
        {
            this.InitializeComponent();

            if (Settings.activeDownloads == null)
                Settings.activeDownloads = new List<DownloadOperation>();

            if (Settings.ctses == null)
                Settings.ctses = new List<CancellationTokenSource>();

            notificationGroup = BackgroundTransferGroup.CreateGroup("{296628BF-5AE6-48CE-AA36-86A85A726B6A}");
            notificationGroup.TransferBehavior = BackgroundTransferBehavior.Parallel;

            if (Settings.downloader == null)
            {
                Settings.downloader = new BackgroundDownloader();
                Settings.downloader.TransferGroup = notificationGroup;
            }

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += timer_Tick;
            _timer.Start();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            if (Settings.IsShowDownloadHelp == false)
                th.Visibility = Windows.UI.Xaml.Visibility.Visible;
            else
                th.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
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
            if (e.NavigationParameter == null) return;

            string link = (e.NavigationParameter as string);
            try
            {
                if (string.Empty == link) return;
                else
                    addDownload(link);
            }
            catch { }
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            await discoveryDownloads();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void timer_Tick(object sender, object e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                updateList2();
            });
        }

        private async Task discoveryDownloads()
        {
            try
            {
                Settings.ctses = null;
                Settings.ctses = new List<CancellationTokenSource>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("disc top error: " + ex.Message);
            }

            Debug.WriteLine("Discovery - 0");
            IReadOnlyList<DownloadOperation> downloads = null;
            try
            {
                // Note that we only enumerate transfers that belong to the transfer group used by this sample
                // scenario. We'll not enumerate transfers started by other sample scenarios in this app.
                downloads = await BackgroundDownloader.GetCurrentDownloadsForTransferGroupAsync(notificationGroup);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("discovery error: " + ex.Message);
                if (!IsExceptionHandled("Discovery error", ex))
                {
                    throw;
                }
                return;
            }
            Debug.WriteLine("Discovery - 1");

            if (downloads.Count > 0)
            {
                Debug.WriteLine("Discovery - 2");
                List<Task> tasks = new List<Task>();
                int i = 0;
                try
                {
                    foreach (DownloadOperation download in downloads)
                    {
                        double percent = 0;

                        if (download.Progress.TotalBytesToReceive > 0)
                            percent = download.Progress.BytesReceived * 100 / download.Progress.TotalBytesToReceive;

                        string downloadedSize = "0.0";

                        if (download.Progress.BytesReceived > 0)
                            downloadedSize = GetFileSize(download.Progress.BytesReceived);

                        var di = new DownloadItem(i++, GetFileName(download.RequestedUri), download,
                            GetFileSize(download.Progress.TotalBytesToReceive), percent, download.Progress.Status.ToString(), downloadedSize);

                        //grp.Items.Add(di);
                        downloadList.Items.Add(di);

                        Settings.ctses.Add(new CancellationTokenSource());
                        // Attach progress and completion handlers.
                        tasks.Add(HandleDownloadAsync(Settings.ctses.LastOrDefault(), download, false));
                        //tasks.Add(HandleDownloadAsync(Settings.ctses.LastOrDefault(), download, false));
                    }
                    Debug.WriteLine("Downloads Count: " + downloads.Count);
                    // Don't await HandleDownloadAsync() in the foreach loop since we would attach to the second
                    // download only when the first one completed; attach to the third download when the second one
                    // completes etc. We want to attach to all downloads immediately.
                    // If there are actions that need to be taken once downloads complete, await tasks here, outside
                    // the loop.
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("disc bot: " + ex.Message);
                }
            }
            Debug.WriteLine("Discovery - 3");
            if (_timer != null)
                _timer.Start();
        }

        private string GetFileSize(ulong TotalBytesToReceive)
        {
            try
            {
                var MB = Math.Round(((TotalBytesToReceive / 1024f) / 1024f), 3);
                return string.Format("{0} MB", MB.ToString().Replace("/", "."));
            }
            catch
            {
                return "0.0 MB";
            }
        }

        private string GetFileName(Uri uri)
        {
            return uri.Segments[uri.Segments.Length - 1];
        }

        private async void Message(string content)
        {
            await new MessageDialog(content, "اخطار").ShowAsync();
        }

        private async Task HandleDownloadAsync(CancellationTokenSource cancellationTokenSource, DownloadOperation download, bool start)
        {
            try
            {
                
                /*
                var progressCallback = new Progress<DownloadOperation>(x =>
                {
                    if (download.Progress.TotalBytesToReceive > 0)
                    {
                        var progress = download.Progress.BytesReceived * 100.0 / download.Progress.TotalBytesToReceive;
                        pb.progressValue = progress;
                        pb.FileSize = GetFileSize(download.Progress.TotalBytesToReceive);
                    }

                    pb.Status = download.Progress.Status.ToString();
                    pb.Operation = download;
                    pb.FileNme = download.ResultFile.Name;
                });
                */
                var progressCallback = new Progress<DownloadOperation>(DownloadProgress);
                if (start)
                {
                    await download.StartAsync().AsTask(cancellationTokenSource.Token, progressCallback);
                }
                else
                {
                    await download.AttachAsync().AsTask(cancellationTokenSource.Token, progressCallback);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HandleDownloadAsync error: " + ex.Message);
            }
        }

        private void DownloadProgress(DownloadOperation download)
        {
        }

        void downloadList_ItemClick(object sender, ItemClickEventArgs e)
        {
        }

        private async Task RunDownloadsAsync(BackgroundDownloader downloader, string link)
        {
            // Validating the URI is required since it was received from an untrusted source (user input).
            // The URI is validated by calling Uri.TryCreate() that will return 'false' for strings that are not valid URIs.
            // Note that when enabling the text box users may provide URIs to machines on the intrAnet that require
            // the "Home or Work Networking" capability.

            Uri baseUri;

            if (!Uri.TryCreate(link, UriKind.Absolute, out baseUri))
            {
                //rootPage.NotifyUser("Invalid URI.", NotifyType.ErrorMessage);
                Message("Invalid URI.");
                return;
            }

            // Use a unique ID for every button click, to help the user associate downloads of the same run
            // in the logs.
            //runId++;

            DownloadOperation downloads;

            try
            {
                // First we create three download operations: Note that we don't start downloads immediately. It is
                // important to first create all operations that should participate in the toast/tile update. Once all
                // operations have been created, we can start them.
                // If we start a download and create a second one afterwards, there is a race where the first download
                // may complete before we were able to create the second one. This would result in the toast/tile being
                // shown before we even create the second download.
                downloads = await CreateDownload(downloader, 1, GetFileName(baseUri), link);
            }
            catch (FileNotFoundException ex)
            {
                // We were unable to create the destination file.
                Debug.WriteLine("down: " + ex.Message);
                return;
            }

            // Once all downloads participating in the toast/tile update have been created, start them.
            try
            {
                var di = new DownloadItem(downloadList.Items.Count,
                    GetFileName(baseUri), downloads,
                    GetFileSize(downloads.Progress.TotalBytesToReceive), 0, downloads.Progress.Status.ToString(), "0.0");

                //grp.Items.Add(di);

                //downloadList.DataContext = grp;
                downloadList.Items.Add(di);

                //downloadTasks = DownloadAsync(downloads);
                Settings.ctses.Add(new CancellationTokenSource());
                Task downloadTasks = HandleDownloadAsync(Settings.ctses.LastOrDefault(), downloads, true);
                try
                {
                    //downloads.Pause();
                }
                catch { }
                await Task.WhenAll(downloadTasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RunDownloadAsync error: " + ex.Message);
            }
        }

        private async Task<DownloadOperation> CreateDownload(BackgroundDownloader downloader, int delaySeconds, string fileName, string baseUri)
        {
            Uri source = new Uri(baseUri, UriKind.Absolute);

            StorageFile destinationFile;
            try
            {
                var folder = await KnownFolders.SavedPictures.CreateFolderAsync("BOMUSIC", CreationCollisionOption.OpenIfExists);
                destinationFile = await folder.CreateFileAsync(
                    fileName, CreationCollisionOption.GenerateUniqueName);
            }
            catch (FileNotFoundException ex)
            {
                Debug.WriteLine("create download error: " + ex.Message);
                throw;
            }

            return downloader.CreateDownload(source, destinationFile);
        }

        private async Task DownloadAsync(DownloadOperation download)
        {
            //Log(String.Format(CultureInfo.CurrentCulture, "Downloading {0}", download.ResultFile.Name));

            try
            {
                await download.StartAsync();
                //LogStatus(String.Format(CultureInfo.CurrentCulture, "Downloading {0} completed.", download.ResultFile.Name), NotifyType.StatusMessage);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine("downloadAsync error: " + ex.Message);
                if (!IsExceptionHandled("Execution error", ex, download))
                {
                    throw;
                }
            }
        }

        private bool IsExceptionHandled(string title, Exception ex, DownloadOperation download = null)
        {
            WebErrorStatus error = BackgroundTransferError.GetStatus(ex.HResult);
            Debug.WriteLine("error handled: " + error.ToString());
            if (error == WebErrorStatus.Unknown)
            {
                return false;
            }

            if (download == null)
            {
                //LogStatus(String.Format(CultureInfo.CurrentCulture, "Error: {0}: {1}", title, error), NotifyType.ErrorMessage);
            }
            else
            {
                //LogStatus(String.Format(CultureInfo.CurrentCulture, "Error: {0} - {1}: {2}", download.ResultFile.Name, title, error), NotifyType.ErrorMessage);
            }

            return true;
        }

        private async void addDownload(string link)
        {
            /*
            try
            {
                Windows.Data.Xml.Dom.XmlDocument successToastXml = Windows.UI.Notifications.ToastNotificationManager.GetTemplateContent(Windows.UI.Notifications.ToastTemplateType.ToastText01);
                successToastXml.GetElementsByTagName("text").Item(0).InnerText =
                    "Download completed successfully.";
                Windows.UI.Notifications.ToastNotification successToast = new Windows.UI.Notifications.ToastNotification(successToastXml);
                Settings.downloader.SuccessToastNotification = successToast;
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine("notification error: " + ex.Message); 
            }
            */
            downloadList.DataContext = null;
            bool isExists = false;
            /*
            try
            {
                foreach (DownloadItem t in grp.Items)
                {
                    if (t.Operation.RequestedUri.AbsoluteUri.ToLower() == link.ToLower())
                    {
                        Message("این فایل در صف دانلودها وجود دارد");
                        isExists = true;
                        break;
                    }
                }
            }
            catch (Exception ex) 
            {
                Debug.WriteLine("add error: " + ex.Message);
                isExists = false; 
            }
            */
            try
            {
                //link = link.Replace("https://", "http://");
                if (isExists == false)
                {
                    if (Settings.downloader == null)
                        Settings.downloader = new BackgroundDownloader();
                    await RunDownloadsAsync(Settings.downloader, link);
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }

            //downloadList.DataContext = grp;
        }

        private Border getItem(int index)
        {
            try
            {
                var border = downloadList.ContainerFromIndex(index) as ContentControl;
                return border.ContentTemplateRoot as Border;
            }
            catch (Exception ex) { Debug.WriteLine("error get item: " + ex.Message); }
            return null;
        }

        private void updateList2()
        {
            if (downloadList.Items.Count > 0)
            {
                for (int i = 0; i < downloadList.Items.Count; ++i)
                {
                    try
                    {
                        var border = getItem(i);
                        if (border == null) continue;

                        var t = border.DataContext as DownloadItem;
                        var panel = (border.Child as Grid).Children[0] as StackPanel;

                        if (t.Operation.Progress.Status == BackgroundTransferStatus.Canceled || t.Operation.Progress.Status == BackgroundTransferStatus.Completed)
                        {
                            try
                            {
                                downloadList.Items.RemoveAt(i);
                                Settings.ctses.RemoveAt(i);
                            }
                            catch (Exception ex) { Debug.WriteLine("error finished: " + ex.Message); }
                            border.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                            continue;
                        }

                        var stpanel = panel.FindName("gridStatus") as Grid;

                        if (t.Operation.Progress.Status == BackgroundTransferStatus.PausedByApplication ||
                            t.Operation.Progress.Status == BackgroundTransferStatus.PausedCostedNetwork ||
                            t.Operation.Progress.Status == BackgroundTransferStatus.PausedNoNetwork ||
                            t.Operation.Progress.Status == BackgroundTransferStatus.PausedSystemPolicy ||
                            t.Operation.Progress.Status == BackgroundTransferStatus.Error ||
                            t.Operation.Progress.Status == BackgroundTransferStatus.Idle ||
                            t.Operation.Progress.Status == BackgroundTransferStatus.Canceled)
                        {
                            
                            //Debug.WriteLine("TStatus: " + t.Status);
                            if (t.Status != "waiting...")
                            {
                                (stpanel.FindName("statusIcon") as SymbolIcon).Symbol = Symbol.Pause;
                                (stpanel.FindName("txtStatus") as TextBlock).Text = t.Operation.Progress.Status.ToString();
                            }
                            else
                            {
                                (stpanel.FindName("statusIcon") as SymbolIcon).Symbol = Symbol.Refresh;
                                (stpanel.FindName("txtStatus") as TextBlock).Text = "conecting to server...";
                            }
                            //continue;
                        }

                        if (t.Operation.Progress.Status == BackgroundTransferStatus.Running)
                        {
                            (stpanel.FindName("statusIcon") as SymbolIcon).Symbol = Symbol.Play;
                            (stpanel.FindName("txtStatus") as TextBlock).Text = "Downloading...";
                        }

                        var grid = panel.FindName("gridSize") as Grid;

                        if (t.Operation.Progress.TotalBytesToReceive > 0)
                        {
                            var progress = t.Operation.Progress.BytesReceived * 100.0 / t.Operation.Progress.TotalBytesToReceive;
                            (panel.FindName("pb") as ProgressBar).Value = progress;
                            (grid.FindName("txtFileSize") as TextBlock).Text = "File Size: " + GetFileSize(t.Operation.Progress.TotalBytesToReceive);
                        }

                        if (t.Operation.Progress.BytesReceived > 0)
                        {
                            (grid.FindName("txtDownloadedSize") as TextBlock).Text = "Down: " + GetFileSize(t.Operation.Progress.BytesReceived);
                        }

                        t.Id = i;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Update error: " + ex.Message);
                        continue;
                    }
                }
            }
        }

        private async void finishedDownload_Click(object sender, ItemClickEventArgs e)
        {
            try
            {
                var data = (e.ClickedItem as DownloadItem);
                if (data.Status == "Running") return;
                StorageFile file = await (await KnownFolders.SavedPictures.GetFolderAsync("BOMUSIC")).GetFileAsync(data.FileNme);
                
                if (file.FileType == ".mp3")
                    Frame.Navigate(typeof(OfflineMusicPlayer), file);
                else
                    await Launcher.LaunchFileAsync(file);
            }
            catch (FileNotFoundException)
            {
                Message("File not found");
            }
        }

        private void PivotItem_Loaded(object sender, RoutedEventArgs e)
        {
            //UpdateDownloadedList();
        }

        private void pausefl_click(object sender, RoutedEventArgs e)
        {
            var s = sender as FrameworkElement;
            var t = s.DataContext as DownloadItem;
            try
            {
                t.Operation.Pause();
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }
        }

        private void resumefl_click(object sender, RoutedEventArgs e)
        {
            _timer.Start();
            var s = sender as FrameworkElement;
            var t = s.DataContext as DownloadItem;
            try
            {
                t.Operation.Resume();
                t.Status = "waiting...";
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }
        }

        private void cancelfl_click(object sender, RoutedEventArgs e)
        {
            var s = sender as FrameworkElement;
            var t = s.DataContext as DownloadItem;
            try
            {
                try
                {
                    Debug.WriteLine("id: " + t.Id);
                    int index = t.Id;

                    if (downloadList.Items.Count == 1)
                        index = 0;

                    Settings.ctses[index].Cancel();
                    Settings.ctses[index].Dispose();
                    Settings.ctses[index] = new CancellationTokenSource();
                    Settings.activeDownloads.RemoveAt(index);
                    //grp.Items.RemoveAt(index);
                    //downloadList.DataContext = grp;
                    downloadList.Items.RemoveAt(index);
                }
                catch { }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }
        }

        private void Border_Holding(object sender, HoldingRoutedEventArgs e)
        {
            MenuFlyout flbase = (MenuFlyout)FlyoutBase.GetAttachedFlyout(sender as FrameworkElement);
            flbase.ShowAt(sender as FrameworkElement);
        }

        private async void playAll_tapped(object sender, TappedRoutedEventArgs e)
        {
            _timer.Stop();
            //if (grp == null) return;
            try
            {
                var mygrp = new DownloadItemGroup();
                for (int i = 0; i < downloadList.Items.Count; ++i)
                {
                    var doit = getItem(i).DataContext as DownloadItem;

                    if (doit.Operation.Progress.Status == BackgroundTransferStatus.PausedByApplication ||
                        doit.Operation.Progress.Status == BackgroundTransferStatus.PausedNoNetwork)
                    {
                        doit.Operation.Resume();
                    }
                    else if (doit.Operation.Progress.Status == BackgroundTransferStatus.Idle)
                    {
                        await doit.Operation.StartAsync();
                    }
                    doit.Status = "resuming...";

                    mygrp.Items.Add(doit);
                }
                downloadList.DataContext = mygrp;
            }
            catch { }
            _timer.Start();
        }

        private void pauseAll_tapped(object sender, TappedRoutedEventArgs e)
        {
            var mygrp = new DownloadItemGroup();

            for (int i = 0; i < downloadList.Items.Count; ++i)
            {
                var doit = getItem(i).DataContext as DownloadItem;
                try
                {
                    doit.Operation.Pause();
                }
                catch { }
                doit.Status = BackgroundTransferStatus.PausedByApplication.ToString();

                mygrp.Items.Add(doit);
            }
            //downloadList.DataContext = mygrp;
        }

        private async void UpdateDownloadedList()
        {
            await StatusBar.GetForCurrentView().ProgressIndicator.ShowAsync();
            lstFinishedDownload.Items.Clear();
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    int index = 0;
                    StorageFolder folder = await KnownFolders.SavedPictures.CreateFolderAsync("BOMUSIC", CreationCollisionOption.OpenIfExists);
                    //DownloadItemGroup grp = new DownloadItemGroup();
                    foreach (var file in await folder.GetFilesAsync())
                    {
                        var prop = await file.GetBasicPropertiesAsync();

                        if (prop.Size == 0) continue;

                        DownloadItem item = new DownloadItem(index++, file.Name, null, GetFileSize(prop.Size), 100, "Tap to open file", string.Empty);
                        item.ModifiedDateTime = "Downloaded: " + prop.DateModified.UtcDateTime.ToString();
                        
                        //grp.Items.Add(item);
                        lstFinishedDownload.Items.Add(item);
                    }
                    //lstFinishedDownload.DataContext = grp;
                });
            }
            catch (Exception ex)
            {
                Message("error: " + ex.Message);
            }
            await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
        }

        private void refreshList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            UpdateDownloadedList();
        }

        private async void deleteFile_click(object sender, RoutedEventArgs e)
        {
            try
            {
                var s = sender as FrameworkElement;

                if (s != null)
                {
                    if (s.DataContext == null)
                    {
                        Message("1217 - There is problem with deleting this file please check back later.");
                    }
                    else
                    {
                        Debug.WriteLine("type: " + s.DataContext.GetType());
                        var down = s.DataContext as DownloadItem;
                        string fileName = down.FileNme;

                        var msg = new MessageDialog("آیا مایل به پاک کردن این فایل هستید؟");
                        msg.Commands.Add(new UICommand("بلی"));
                        msg.Commands.Add(new UICommand("خیر"));
                        var t = await msg.ShowAsync();

                        if (t.Label == "بلی")
                        {
                            Debug.WriteLine(0);
                            StorageFolder folder = await KnownFolders.SavedPictures.GetFolderAsync("BOMUSIC");
                            Debug.WriteLine("0-1");

                            Debug.WriteLine("FileName: " + fileName);

                            StorageFile file = await folder.GetFileAsync(fileName);
                            Debug.WriteLine(1);
                            await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                            Debug.WriteLine(2);
                            await StatusBar.GetForCurrentView().ProgressIndicator.ShowAsync();
                            UpdateDownloadedList();
                            await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
                            Debug.WriteLine(3);
                        }
                    }
                }
                else
                {
                    Message("1228 - There is problem with deleting this file please check back later.");
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }
        }

        private void downloadedFile_Holding(object sender, HoldingRoutedEventArgs e)
        {
            MenuFlyout flbase = (MenuFlyout)FlyoutBase.GetAttachedFlyout(sender as FrameworkElement);
            flbase.ShowAt(sender as FrameworkElement);
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as Pivot).SelectedIndex == 1)
            {
                UpdateDownloadedList();
            }
        }

        private void Help_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Settings.IsShowDownloadHelp = true;
            (sender as Grid).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
    }

    [DataContract]
    public class DownloadItem
    {
        private string downloadedSize = string.Empty;
        private string _fileSize = string.Empty;
        public DownloadItem(int id, string fileName, DownloadOperation operation)
        {
            this.Id = id;
            this.FileNme = fileName;
            this.Operation = operation;
            FileSize = string.Empty;
            progressValue = 0;
        }
        public DownloadItem(int id, string fileName, DownloadOperation operation, string fileszie, double pbValue, string status, string downloadedSize)
        {
            this.Id = id;
            this.FileNme = fileName;
            this.Operation = operation;
            this.FileSize = fileszie;
            this.progressValue = pbValue;
            this.Status = status;
            this.DownloadedSize = downloadedSize;
        }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "filename")]
        public string FileNme { get; set; }
        [DataMember(Name = "filesize")]
        public string FileSize
        {
            get { return "File Size: " + _fileSize; }
            set { _fileSize = value; }
        }

        [DataMember(Name = "status")]
        public string Status { get; set; }
        [DataMember(Name = "progressvalue")]
        public double progressValue { get; set; }
        [IgnoreDataMember()]
        public DownloadOperation Operation { get; set; }

        [DataMember(Name = "lastEdit")]
        public string ModifiedDateTime { get; set; }

        [DataMember(Name = "downloadedSize")]
        public string DownloadedSize
        {
            get { return "Down: " + downloadedSize; }
            set { downloadedSize = value; }
        }
    }

    [DataContract]
    public class DownloadItemGroup
    {
        public DownloadItemGroup()
        {
            Items = new List<DownloadItem>();
        }
        [DataMember(Name = "finisheddownload")]
        public List<DownloadItem> Items { get; set; }
    }
}
