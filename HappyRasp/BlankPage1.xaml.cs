using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HappyRasp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BlankPage1 : Page
    {
        public BlankPage1()
        {
            this.InitializeComponent();
        }
        private async void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            var camera = new CameraCaptureUI();
            var img = await camera.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (img != null)
            {
                using (var stream = await img.OpenAsync(FileAccessMode.Read))
                {
                    var bitmap = new BitmapImage();
                    bitmap.SetSource(stream);
                    takenImage.Source = bitmap;
                }
            }
            else
            {
                var dialog = new MessageDialog("The user has not taken a photo");
                dialog.ShowAsync();
            }
        }
    }
}
