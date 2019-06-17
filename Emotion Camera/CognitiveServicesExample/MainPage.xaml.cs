using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace CognitiveServicesExample
{
    public sealed partial class MainPage : Page
    {
        private const string _subscriptionKey = "1fa1b6f0381a4ae880e9d998c9ed6bb3";
        private const string _serviceEndpoint = "https://northeurope.api.cognitive.microsoft.com/";

        private FaceClient faceServiceClient;
        private MediaCapture cameraMediaCapture = null;


        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            // Get available devices for capturing pictures
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            // Get the desired camera by panel
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);

            // If there is no device mounted on the desired panel, return the first device found
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        private async Task<bool> startCamera()
        {
            bool initOK = false;


            if (cameraMediaCapture==null)
            {
                var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);

                if (cameraDevice == null)
                {
                    Debug.WriteLine("No camera device found!");
                    return false;
                }

                // Create MediaCapture and its settings
                cameraMediaCapture = new MediaCapture();

                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                try
                {
                    await cameraMediaCapture.InitializeAsync(settings);
                    PreviewControl.Source = cameraMediaCapture;
                    await cameraMediaCapture.StartPreviewAsync();
                    initOK = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("The app was denied access to the camera");
                }
            }

            return initOK;
        }

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!await startCamera())
            {
                MessageDialog m = new MessageDialog("Camera not found");
                await m.ShowAsync();
            }
        }

        private async Task<DetectedFace[]> UploadAndDetectFacesStream(Stream imageStream)
        {
            Debug.WriteLine("FaceClient is created");

            //
            // Create Face API Service client
            //
            faceServiceClient = new FaceClient(
                new ApiKeyServiceClientCredentials(_subscriptionKey),
                new System.Net.Http.DelegatingHandler[] { })
            {
                Endpoint = _serviceEndpoint
            };  // need to provide and endpoint and a delegate.


            // See https://docs.microsoft.com/en-us/azure/cognitive-services/face/glossary#a
            // for the current list of supported options.
            var requiredFaceAttributes = new FaceAttributeType[]
            {
                FaceAttributeType.Age,
                FaceAttributeType.Gender,
                FaceAttributeType.HeadPose,
                FaceAttributeType.Smile,
                FaceAttributeType.FacialHair,
                FaceAttributeType.Glasses,
                FaceAttributeType.Emotion,
                FaceAttributeType.Hair,
                FaceAttributeType.Makeup,
                FaceAttributeType.Occlusion,
                FaceAttributeType.Accessories,
                FaceAttributeType.Blur,
                FaceAttributeType.Exposure,
                FaceAttributeType.Noise
            };

            Debug.WriteLine("Calling Face.DetectWithUrlAsync()...");

            try
            {
                //
                // Detect the faces in the URL
                //

                var detectedFaces = await faceServiceClient.Face.DetectWithStreamAsync(imageStream, true, false, requiredFaceAttributes);

                return detectedFaces.ToArray();
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Detection failed. Please make sure that you have the right subscription key and proper URL to detect.");
                Debug.WriteLine(exception.ToString());
                return null;
            }
        }

        private async void Camera_Button_Clicked(object sender, RoutedEventArgs e)
        {
            var stream = new InMemoryRandomAccessStream();

            Debug.WriteLine("Taking photo...");

            await cameraMediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateBmp(), stream);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap,
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);


            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(softwareBitmapBGR8);

            ImageBrush imageBrush = new ImageBrush
            {
                ImageSource = bitmapSource
            };

            ImageCanvas.Background = imageBrush;

            detectionStatus.Text = "";

            using (var ms = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms);
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();

                DetectedFace[] detectedFaces = await UploadAndDetectFacesStream(ms.AsStream());

                if (detectedFaces != null)
                {
                    ResultBox.Items.Clear();
                    DisplayParsedResults(detectedFaces);
                    DisplayAllResults(detectedFaces);
                    DrawFaceRectangleStream(detectedFaces, stream);

                    detectionStatus.Text = "Detection Done";
                }
                else
                {
                    detectionStatus.Text = "Detection Failed";
                }
            }
        }

        private void DisplayAllResults(DetectedFace[] faceList)
        {
            int index = 0;
            foreach (DetectedFace face in faceList)
            {
                var emotion = face.FaceAttributes.Emotion;

                ResultBox.Items.Add("\nFace #" + index
                    + "\nAnger: " + emotion.Anger
                    + "\nContempt: " + emotion.Contempt
                    + "\nDisgust: " + emotion.Disgust
                    + "\nFear: " + emotion.Fear
                    + "\nHappiness: " + emotion.Happiness
                    + "\nNeutral: " + emotion.Neutral
                    + "\nSadness: " + emotion.Sadness
                    + "\nSurprise: " + emotion.Surprise);

                index++;
            }
        }

        private void DisplayParsedResults(DetectedFace[] resultList)
        {
            int index = 0;
            string textToDisplay = "";

            foreach (DetectedFace face in resultList)
            {
                string emotionString = ParseResults(face);
                textToDisplay += "Person number " + index.ToString() + " " + emotionString + "\n";
                index++;
            }
            ResultBox.Items.Add(textToDisplay);
        }

        private string ParseResults(DetectedFace face)
        {
            double topScore = 0.0d;
            string topEmotion = "";
            string retString = "";
            var emotion = face.FaceAttributes.Emotion;

            // anger
            topScore = face.FaceAttributes.Emotion.Anger;
            topEmotion = "Anger";

            // contempt
            if (topScore < emotion.Contempt)
            {
                topScore = emotion.Contempt;
                topEmotion = "Contempt";
            }

            // disgust
            if (topScore < emotion.Disgust)
            {
                topScore = emotion.Disgust;
                topEmotion = "Disgust";
            }

            // fear
            if (topScore < emotion.Fear)
            {
                topScore = emotion.Fear;
                topEmotion = "Fear";
            }

            // happiness
            if (topScore < emotion.Happiness)
            {
                topScore = emotion.Happiness;
                topEmotion = "Happiness";
            }

            // neural
            if (topScore < emotion.Neutral)
            {
                topScore = emotion.Neutral;
                topEmotion = "Neutral";
            }

            // happiness
            if (topScore < emotion.Sadness)
            {
                topScore = emotion.Sadness;
                topEmotion = "Sadness";
            }

            // surprise
            if (topScore < emotion.Surprise)
            {
                topScore = emotion.Surprise;
                topEmotion = "Surprise";
            }

            retString = $"is expressing {topEmotion} with a certainty of {topScore}.";
            return retString;
        }

        public async void DrawFaceRectangleStream(DetectedFace[] faceResult, InMemoryRandomAccessStream imageStream)
        {
            ImageCanvas.Children.Clear();

            if (faceResult != null && faceResult.Length > 0)
            {

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(imageStream);


                double resizeFactorH = ImageCanvas.Height / decoder.PixelHeight;
                double resizeFactorW = ImageCanvas.Width / decoder.PixelWidth;


                foreach (var face in faceResult)
                {

                    FaceRectangle faceRect = face.FaceRectangle;

                    var rectangle1 = new Rectangle();
                    Windows.UI.Color faceColor = Windows.UI.Color.FromArgb(50, 255, 255, 255);
                    Windows.UI.Color borderColor = Windows.UI.Colors.Blue;
                    rectangle1.Fill = new SolidColorBrush(faceColor);
                    rectangle1.Width = faceRect.Width;
                    rectangle1.Height = faceRect.Height;
                    rectangle1.Stroke = new SolidColorBrush(borderColor);
                    rectangle1.StrokeThickness = 1;
                    rectangle1.RadiusX = 10;
                    rectangle1.RadiusY = 10;
                    ImageCanvas.Children.Add(rectangle1);

                    Canvas.SetLeft(rectangle1, faceRect.Left);
                    Canvas.SetTop(rectangle1, faceRect.Top);
                }
            }
        }
    }
}
