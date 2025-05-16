using Windows.Storage;
using Windows.UI.Xaml.Controls;
using System;

namespace GAP
{
    public sealed partial class MainPage2 : Page
    {
        public MainPage2()
        {
            this.InitializeComponent();

            // Load the HTML file into the WebView
            LoadHtmlFile();

        }

        private void LoadHtmlFile()
        {
            // Construire l'URI avec le schéma ms-appx://
            string filePath = "ms-appx-web:///map.html";
            MyWebView.Navigate(new Uri(filePath));
        }

    }
}
