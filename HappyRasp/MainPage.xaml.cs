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
using Windows.Networking.Sockets;
using System.Collections.ObjectModel;
using System.Threading;
using Windows.System.Threading;
using Windows.Devices.Enumeration;
using System.Text;
using Windows.Devices.Bluetooth.Rfcomm;



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
        //
        DispatcherTimer timer;
        //
        private Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceService _service;
        private StreamSocket _socket;
        private DataWriter dataWriterObject;
        private DataReader dataReaderObject;
        ObservableCollection<PairedDeviceInfo> _pairedDevices;
        private CancellationTokenSource ReadCancellationTokenSource;

        string recvdtxt;
        private static ThreadPoolTimer timerDataProcess;


        /// <summary>
        ///  Class to hold all paired device information
        /// </summary>
        public class PairedDeviceInfo
        {
            internal PairedDeviceInfo(DeviceInformation deviceInfo)
            {
                this.DeviceInfo = deviceInfo;
                this.ID = this.DeviceInfo.Id;
                this.Name = this.DeviceInfo.Name;
            }

            public string Name { get; private set; }
            public string ID { get; private set; }
            public DeviceInformation DeviceInfo { get; private set; }
        }

        public MainPage()
        {

            this.InitializeComponent();
            InitializeRfcommDeviceService();
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

            timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 10, 0) };// (0, 0, 0, 10, 0) };
            timer.Tick += OnTimerTick;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            timer.Stop();

        }



        async void InitializeRfcommDeviceService()
        {
            try
            {
                DeviceInformationCollection DeviceInfoCollection = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort));

                var numDevices = DeviceInfoCollection.Count();

                // By clearing the backing data, we are effectively clearing the ListBox
                _pairedDevices = new ObservableCollection<PairedDeviceInfo>();
                _pairedDevices.Clear();

                if (numDevices == 0)
                {
                    //MessageDialog md = new MessageDialog("No paired devices found", "Title");
                    //await md.ShowAsync();
                    System.Diagnostics.Debug.WriteLine("InitializeRfcommDeviceService: No paired devices found.");
                }
                else
                {
                    // Found paired devices.
                    foreach (var deviceInfo in DeviceInfoCollection)
                    {
                        _pairedDevices.Add(new PairedDeviceInfo(deviceInfo));
                    }
                }
                PairedDevices.Source = _pairedDevices;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InitializeRfcommDeviceService: " + ex.Message);
            }
        }

        #region Connessione Dispositivo

        async private void ConnectDevice_Click(object sender, RoutedEventArgs e)
        {
            //Revision: No need to requery for Device Information as we alraedy have it:
            DeviceInformation DeviceInfo; // = await DeviceInformation.CreateFromIdAsync(this.TxtBlock_SelectedID.Text);
            PairedDeviceInfo pairedDevice = (PairedDeviceInfo)ConnectDevices.SelectedItem;
            DeviceInfo = pairedDevice.DeviceInfo;

            bool success = true;
            try
            {
                _service = await RfcommDeviceService.FromIdAsync(DeviceInfo.Id);

                if (_socket != null)
                {
                    // Disposing the socket with close it and release all resources associated with the socket
                    _socket.Dispose();
                }

                _socket = new StreamSocket();
                try
                {
                    // Note: If either parameter is null or empty, the call will throw an exception
                    await _socket.ConnectAsync(_service.ConnectionHostName, _service.ConnectionServiceName);
                }
                catch (Exception ex)
                {
                    success = false;
                    System.Diagnostics.Debug.WriteLine("Connect:" + ex.Message);
                }
                // If the connection was successful, the RemoteAddress field will be populated
                if (success)
                {
                    this.buttonDisconnect.IsEnabled = true;

                    this.StartStopReceive.IsEnabled = true;

                    string msg = String.Format("Connected to {0}!", _socket.Information.RemoteAddress.DisplayName);
                        }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Overall Connect: " + ex.Message);
                _socket.Dispose();
                _socket = null;
            }
        }

        private void ConnectDevices_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            PairedDeviceInfo pairedDevice = (PairedDeviceInfo)ConnectDevices.SelectedItem;
            ConnectDevice_Click(sender, e);
        }

        #endregion

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            //OutBuff = new Windows.Storage.Streams.Buffer(100);
            Button button = (Button)sender;
            if (button != null)
            {
                switch ((string)button.Content)
                {
                    case "Disconnect":
                        await this._socket.CancelIOAsync();
                        _socket.Dispose();
                        _socket = null;
                        this.buttonDisconnect.IsEnabled = false;
                        this.StartStopReceive.IsEnabled = false;
                        break;
                    case "Refresh":
                        InitializeRfcommDeviceService();
                        break;
                }
            }
        }


        public async void Send(string msg)
        {
            try
            {
                if (_socket.OutputStream != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriterObject = new DataWriter(_socket.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteAsync(msg);
                }
                else
                {

                }

            }
            catch (Exception ex)
            {
                //status.Text = "Send(): " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Send(): " + ex.Message);
            }
            finally
            {
                // Cleanup once complete
                if (dataWriterObject != null)
                {
                    dataWriterObject.DetachStream();
                    dataWriterObject = null;
                }
            }
        }

        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        private async Task WriteAsync(string msg)
        {
            Task<UInt32> storeAsyncTask;

            if (msg == "")
                msg = "none";// sendText.Text;
            if (msg.Length != 0)
            //if (msg.sendText.Text.Length != 0)
            {
                // Load the text from the sendText input text box to the dataWriter object
                dataWriterObject.WriteString(msg);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriterObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    string status_Text = msg + ", ";
                    status_Text += bytesWritten.ToString();
                    status_Text += " bytes written successfully!";
                    System.Diagnostics.Debug.WriteLine(status_Text);
                }
            }
            else
            {
                string status_Text2 = "Enter the text you want to write and then click on 'WRITE'";
                System.Diagnostics.Debug.WriteLine(status_Text2);
            }
        }


        private void StartStopReceive_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    // 10 seconds
                    timer.Start();
                }
                else
                {
                    timer.Stop();

                }
            }
        }
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            timer.Start();
        }

        void OnUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {

                this.timer.Stop();
            }
            catch (Exception ex)
            { }

        }

        void OnTimerTick(object sender, object e)
        {
            try
            {
                PerformClickActionCaptureImage();
            }
            catch (Exception exc)
            {

            }
        }


        /***************************************************************************************************/

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
            PerformClickActionCaptureImage();
        }



        public async void PerformClickActionCaptureImage()
        {

            progressBar.Visibility = Visibility.Visible;

            // Capture current frame from webcam, store it in temporary storage and set the source of a BitmapImage to said photo
            currentIdPhotoFile = await webcam.CapturePhoto();
            var photoStream = await currentIdPhotoFile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapImage idPhotoImage = new BitmapImage();
            await idPhotoImage.SetSourceAsync(photoStream);


            #region Before Convert in Byte[] and then in stream
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


            string subscriptionKey = "3963f888b7374cbb8b78f492758bbb02";
            EmotionServiceClient emotionServiceClient = new EmotionServiceClient(subscriptionKey);

            try
            {
                Emotion[] emotionResult;
                emotionResult = await emotionServiceClient.RecognizeAsync(stream);

                if (emotionResult != null)
                {

                    string score = "Happiness: " + emotionResult[0].Scores.Happiness.ToString("0.0000") + "\n";
                    score += "Fear: " + emotionResult[0].Scores.Fear.ToString("0.0000") + "\n";
                    score += "Anger: " + emotionResult[0].Scores.Anger.ToString("0.0000") + "\n";
                    score += "Contempt: " + emotionResult[0].Scores.Contempt.ToString("0.0000") + "\n";
                    score += "Disgust: " + emotionResult[0].Scores.Disgust.ToString("0.0000") + "\n";
                    score += "Neutral: " + emotionResult[0].Scores.Neutral.ToString("0.0000") + "\n";
                    score += "Sadness: " + emotionResult[0].Scores.Sadness.ToString("0.0000") + "\n";
                    score += "Surprise: " + emotionResult[0].Scores.Surprise.ToString("0.0000") + "\n";


                    txtText.Text = score;// "Happiness: " +emotionResult[0].Scores.Happiness.ToString()+"\n";

                    if (emotionResult[0].Scores.Happiness >= 0.85)
                        Send("111");
                    else if (emotionResult[0].Scores.Happiness >= 0.50 && emotionResult[0].Scores.Happiness < 0.85)
                    {
                        Send("001");
                    }
                    else if (emotionResult[0].Scores.Happiness > 0.25 && emotionResult[0].Scores.Happiness < 0.50)
                    {
                        Send("100");
                    }
                    else
                        Send("110");
                }
            }
            catch (Exception exception)
            {

            }

            // Dispose photo stream
            photoStream.Dispose();
            progressBar.Visibility = Visibility.Collapsed;
        }

        private void btnSendData_Click(object sender, RoutedEventArgs e)
        {
            Send(this.txtSendData.Text);
        }
    }
}
