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
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Hashtag_Music
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        bool byPages = false;

        public LoginPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if ((e.NavigationParameter as string) == "Relogin")
            {
                try
                {
                    byPages = false;
                    var data = await Settings.GetUserData();
                    txtPassword.Password = data.password;
                    txtUsername.Text = data.username;

                    Login(this, null);
                }
                catch (Exception ex)
                {
                    Message(ex.Message);
                }
            }
            //byPages
            if ((e.NavigationParameter as string) == "byPages")
                byPages = true;

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

        private async void Skip_Login(object sender, RoutedEventArgs e)
        {
            var msg = new MessageDialog("آیا می خواهید این صفحه در هنگام ورود باز نشود؟");
            msg.Commands.Add(new UICommand("بلی", (IUICommand) =>
            {
                Core.Settings.RememberLogin = true;
            }));

            msg.Commands.Add(new UICommand("خیر"));

            var result = await msg.ShowAsync();

            Core.Settings.Login = false;

            Frame.Navigate(typeof(MainPage));
        }

        private void Login(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                loading.IsActive = false;
                IsShowing = false;
                animationLoginButton(false);
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }
        }

        public bool IsShowing { get; set; }

        private async void Message(string content)
        {
            await new MessageDialog(content).ShowAsync();
        }

        private void animationLoginButton(bool show)
        {
            skipLogin.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            Storyboard s = new Storyboard();

            DoubleAnimation da = new DoubleAnimation();
            da.Duration = new Duration(TimeSpan.FromMilliseconds(100));

            if (show == false)
            {
                da.To = 400;
            }
            else
            {
                da.To = 0;
            }

            Storyboard.SetTarget(da, tranformLogin);
            Storyboard.SetTargetProperty(da, "TranslateX");

            s.Children.Add(da);

            s.Completed += s_Completed;
            s.Begin();
        }

        private async void s_Completed(object sender, object e)
        {
            if (IsShowing == true)
            {
                loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                loading.IsActive = false;
                IsShowing = false;
                return;
            }

            loading.Visibility = Windows.UI.Xaml.Visibility.Visible;
            loading.IsActive = true;
            IsShowing = true;

            try
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Password.ToString()))
                {
                    Message("لطفا فیلد نام کاربری یا رمزعبورتان را پر کنید");
                    animationLoginButton(true);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error msg: " + ex.Message);
            }

            try
            {
                bool result = await Core.DataGetter.Login(txtUsername.Text, txtPassword.Password.ToString());

                if (result == true)
                {
                    UserDef de = new UserDef();
                    de.password = txtPassword.Password.ToString();
                    de.username = txtUsername.Text;
                    await Core.Settings.SaveUserData(de);

                    Core.Settings.RememberLogin = true;
                    Core.Settings.Login = true;

                    if (byPages == false)
                    {
                        Frame.Navigate(typeof(MainPage));
                    }
                    else
                    {
                        if (Frame.CanGoBack)
                        {
                            Frame.GoBack();
                        }
                        else
                        {
                            Frame.Navigate(typeof(MainPage));
                        }
                    }
                }
                else
                {
                    Message("نام کاربری یا رمز ورود اشتباه است");
                    loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    loading.IsActive = false;
                    tranformLogin.TranslateX = 0;
                }
            }
            catch (Exception ex)
            {
                Message(ex.Message);
                //animationLoginButton(true);
                loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                loading.IsActive = false;
                tranformLogin.TranslateX = 0;
            }
            IsShowing = false;
            skipLogin.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void SignUp(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(RegisterPage));
        }

        private void LoginClick(object sender, RoutedEventArgs e)
        {
            Login(this, null);
        }

        private void SignupClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RegisterPage));
        }
    }
}
