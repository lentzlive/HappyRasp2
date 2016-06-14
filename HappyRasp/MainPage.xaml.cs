using HappyRasp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;



// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HappyRasp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private WebcamHelper webcam;
        private StorageFile currentIdPhotoFile;

        public MainPage()
        {

            this.InitializeComponent();

            //If user has set the DisableLiveCameraFeed within Constants.cs to true, disable the feed:
            if (GeneralConstants.DisableLiveCameraFeed)
            {
                WebcamFeed.Visibility = Visibility.Collapsed;
                DisabledFeedGrid.Visibility = Visibility.Visible;
            }
            else
            {
                WebcamFeed.Visibility = Visibility.Visible;
                DisabledFeedGrid.Visibility = Visibility.Collapsed;
            }

        }





        /// <summary>
        /// Triggered when the webcam feed control is loaded. Sets up the live webcam feed.
        /// </summary>
        private async void WebcamFeed_Loaded(object sender, RoutedEventArgs e)
        {

            if (webcam == null || !webcam.IsInitialized())
            {
                // Initialize Webcam Helper
                webcam = new WebcamHelper();
                await webcam.InitializeCameraAsync();

                // Set source of WebcamFeed on MainPage.xaml
                WebcamFeed.Source = webcam.mediaCapture;

                // Check to make sure MediaCapture isn't null before attempting to start preview. Will be null if no camera is attached.
                if (WebcamFeed.Source != null)
                {
                    // Start the live feed
                    await webcam.StartCameraPreview();
                }
            }
            else if (webcam.IsInitialized())
            {
                WebcamFeed.Source = webcam.mediaCapture;

                // Check to make sure MediaCapture isn't null before attempting to start preview. Will be null if no camera is attached.
                if (WebcamFeed.Source != null)
                {
                    await webcam.StartCameraPreview();
                }
            }

        }

        /// <summary>
        /// Triggered when the Capture Photo button is clicked by the user
        /// </summary>
        private async void Capture_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;

            // Hide the capture photo button
            //CaptureButton.Visibility = Visibility.Collapsed;

            // Capture current frame from webcam, store it in temporary storage and set the source of a BitmapImage to said photo
            currentIdPhotoFile = await webcam.CapturePhoto();
            var photoStream = await currentIdPhotoFile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapImage idPhotoImage = new BitmapImage();
            await idPhotoImage.SetSourceAsync(photoStream);


            #region Prima converto in Byte[] e poi in stream
            var reader = new DataReader(photoStream.GetInputStreamAt(0));
            var bytes = new byte[photoStream.Size];
            await reader.LoadAsync((uint)photoStream.Size);
            reader.ReadBytes(bytes); 

            var stream = new MemoryStream(bytes);

            #endregion

            // Set the soruce of the photo control the new BitmapImage and make the photo control visible
            IdPhotoControl.Source = idPhotoImage;
            IdPhotoControl.Visibility = Visibility.Visible;

            // Collapse the webcam feed or disabled feed grid. Make the enter user name grid visible.
            WebcamFeed.Visibility = Visibility.Collapsed;
            DisabledFeedGrid.Visibility = Visibility.Collapsed;

            //UserNameGrid.Visibility = Visibility.Visible;


            string subscriptionKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxx";
            EmotionServiceClient emotionServiceClient = new EmotionServiceClient(subscriptionKey);

            try
            {
                Emotion[] emotionResult;
                emotionResult = await emotionServiceClient.RecognizeAsync(stream);

                if (emotionResult != null)
                {

                    string score= "Felicità: " + emotionResult[0].Scores.Happiness.ToString("0.0000") + "\n";
                    score+= "Paura: " + emotionResult[0].Scores.Fear.ToString("0.0000") + "\n";
                    score += "Rabbia: " + emotionResult[0].Scores.Anger.ToString("0.0000") + "\n";
                    score += "Disprezzo: " + emotionResult[0].Scores.Contempt.ToString("0.0000") + "\n";
                    score += "Disgusto: " + emotionResult[0].Scores.Disgust.ToString("0.0000") + "\n";
                    score += "Neutrale: " + emotionResult[0].Scores.Neutral.ToString("0.0000") + "\n";
                    score += "Tristezza: " + emotionResult[0].Scores.Sadness.ToString("0.0000") + "\n";
                    score += "Sorpresa: " + emotionResult[0].Scores.Surprise.ToString("0.0000") + "\n";
                    

                    txtText.Text = score;// "Happiness: " +emotionResult[0].Scores.Happiness.ToString()+"\n";
                }
            }
            catch (Exception exception)
            {
                //window.Log(exception.ToString());
                //return null;
            }

            // Dispose photo stream
            photoStream.Dispose();
            progressBar.Visibility = Visibility.Collapsed;

        }


        /// <summary>
        /// Uploads the image to Project Oxford and detect emotions.
        /// </summary>
        /// <param name="imageFilePath">The image file path.</param>
        /// <returns></returns>
        private async Task<Emotion[]> UploadAndDetectEmotions(string imageFilePath)
        {

            string subscriptionKey = "3963f888b7374cbb8b78f492758bbb02";

            //window.Log("EmotionServiceClient is created");

            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE STARTS HERE
            // -----------------------------------------------------------------------

            //
            // Create Project Oxford Emotion API Service client
            //
            EmotionServiceClient emotionServiceClient = new EmotionServiceClient(subscriptionKey);

            //window.Log("Calling EmotionServiceClient.RecognizeAsync()...");
            try
            {
                Emotion[] emotionResult;
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    //
                    // Detect the emotions in the URL
                    //
                    emotionResult = await emotionServiceClient.RecognizeAsync(imageFileStream);
                    return emotionResult;
                }
            }
            catch (Exception exception)
            {
                //window.Log(exception.ToString());
                return null;
            }
            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE ENDS HERE
            // -----------------------------------------------------------------------

        }

        /// <summary>
        /// Triggered when the Confirm photo button is clicked by the user. Stores the captured photo to storage and navigates back to MainPage.
        /// </summary>
        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UserNameBox.Text))
            {
                // Create or open the folder in which the Whitelist is stored
                StorageFolder whitelistFolder = await KnownFolders.PicturesLibrary.CreateFolderAsync(GeneralConstants.WhiteListFolderName, CreationCollisionOption.OpenIfExists);
                // Create a folder to store this specific user's photos
                StorageFolder currentFolder = await whitelistFolder.CreateFolderAsync(UserNameBox.Text, CreationCollisionOption.ReplaceExisting);
                // Move the already captured photo the user's folder
                await currentIdPhotoFile.MoveAsync(currentFolder);

                // Add user to Oxford database
                //OxfordFaceAPIHelper.AddUserToWhitelist(UserNameBox.Text, currentFolder);

                // Stop live camera feed
                await webcam.StopCameraPreview();
                // Navigate back to MainPage
                Frame.Navigate(typeof(MainPage));
            }
        }

        /// <summary>
        /// Triggered when the Cancel Photo button is clicked by the user. Resets page.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Collapse the confirm photo buttons and open the capture photo button.
            CaptureButton.Visibility = Visibility.Visible;
            UserNameGrid.Visibility = Visibility.Collapsed;
            UserNameBox.Text = "";

            // Open the webcam feed or disabled camera feed
            if (GeneralConstants.DisableLiveCameraFeed)
            {
                DisabledFeedGrid.Visibility = Visibility.Visible;
            }
            else
            {
                WebcamFeed.Visibility = Visibility.Visible;
            }

            // Collapse the photo control:
            IdPhotoControl.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Triggered when the "Back" button is clicked by the user
        /// </summary>
        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop the camera preview
            await webcam.StopCameraPreview();
            // Navigate back to the MainPage
            Frame.Navigate(typeof(MainPage));
        }


    }
}
