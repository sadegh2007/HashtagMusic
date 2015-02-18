using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Hashtag_Music
{
    public sealed partial class SliderPanel : UserControl
    {

        DispatcherTimer timer;
        int index = -1;
        Core.DashbordSlider _items;

        private int MoveSpeed = 300;
        private int OpacityChangeSpeed = 150;

        private int lastIndex = 0;

        public SliderPanel()
        {
            this.InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        public Core.DashbordSlider Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
                if (value != null)
                {
                    index = 0;
                    UpdateCurrentValues();
                }
            }
        }

        public void StopAnimation()
        {
            try
            {
                //timer.Stop();
                Debug.WriteLine("Slider animation stoped");
            }
            catch (Exception) { }
        }

        public void StartAnimation()
        {
            try
            {
                //timer.Start();
            }
            catch (Exception) { }
        }

        public int CurrentIndex { get { return index; } }

        async void timer_Tick(object sender, object e)
        {
            if (Items == null || Items.Items.Count == 0) return;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                //FirstChangeAnimation();
            });
        }

        private void UpdateCurrentValues()
        {
            --index;

            UpdateToNextBanner();
        }

        private void UpdateToNextBanner()
        {
            try
            {
                // TODO : Create Privot Items
                for (int i = 0; i < Items.Items.Count; ++i)
                {
                    PivotItem pv = new PivotItem();
                    pv.Margin = new Thickness(0);

                    pv.ContentTemplate = this.Resources["contentTemplate"] as DataTemplate;

                    Windows.UI.Xaml.Shapes.Ellipse ellipse = new Windows.UI.Xaml.Shapes.Ellipse();
                    ellipse.Width = ellipse.Height = 10;
                    ellipse.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                    ellipse.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center;

                    if (i == 0)
                        ellipse.Fill = App.Current.Resources["darkForground"] as SolidColorBrush;
                    else 
                        ellipse.Fill = App.Current.Resources["medForground"] as SolidColorBrush;

                    //ellipse.Tag = i;
                    ellipse.SetValue(Grid.ColumnProperty, i);
                    ellipse.Margin = new Thickness(2);

                    var _item = Items.Items[i];

                    string song = _item.Song;
                    if (song.Length > 18)
                        song = song.Substring(0, 18) + "...";

                    _item.Song = song;

                    pv.DataContext = _item;
                    pvitems.Items.Add(pv);
                    pvitemChange.Children.Add(ellipse);
                }

                //if (CurrentIndex + 1 == Items.Items.Count)
                //    index = -1;
            }
            catch (Exception ex) { Debug.WriteLine("banner update error: " + ex.Message); }
        }

        // Banner Clicks
        private void RootBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (pvitems.SelectedItem as PivotItem).DataContext as Core.DashbordSliderItem;
            var rootFrame = Window.Current.Content as Frame;

            if (rootFrame != null)
            {
                if (item.Type == Core.ItemType.Video)
                {
                    rootFrame.Navigate(typeof(VideoViewPage), item.Id);
                }
                else
                {
                    rootFrame.Navigate(typeof(MusicPlayerPage), item.Id);
                }
            }
        }

        private void pvitems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                (pvitemChange.Children[lastIndex] as Windows.UI.Xaml.Shapes.Ellipse).Fill = App.Current.Resources["medForground"] as SolidColorBrush;
                (pvitemChange.Children[pvitems.SelectedIndex] as Windows.UI.Xaml.Shapes.Ellipse).Fill = App.Current.Resources["darkForground"] as SolidColorBrush;
                lastIndex = pvitems.SelectedIndex;
            }
            catch (Exception) { }
        }

    }
}
