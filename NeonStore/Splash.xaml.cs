using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Navigation;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NeonStore
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Splash : Page
    {
        private string[] messages =
        {
            "Did you know this was called GeekHub aka NanoStore in the first place?",
            "Special thanks to some of the GDCR staff for contributing to this app!",
            "Assets from 8.1, too lazy to edit..",
            "Ok...?",
            "This is another page, I swear!",
            "Our JSON uses GitHub and Gist technology, open but fast!"
        };

        public Splash()
        {
            this.InitializeComponent();
            SetRandomLoadingText();
            StartTimer();
        }

        private void SetRandomLoadingText()
        {
            Random rnd = new Random();
            int index = rnd.Next(messages.Length);

            LoadingText.Text = messages[index];
        }

        private void StartTimer()
        {
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);

            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;

            timer.Tick += (s, e) =>
            {
                timer.Stop();

                // default = NOT new version unless missing
                bool isNewVersion = !settings.Values.ContainsKey("HasOpened");

                if (isNewVersion)
                {
                    settings.Values["HasOpened"] = true;
                    Frame.Navigate(typeof(Welcome));
                }
                else
                {
                    Frame.Navigate(typeof(MainPage));
                }
            };

            timer.Start();
        }
    }
}
