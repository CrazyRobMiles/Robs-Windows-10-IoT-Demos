using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace LogoBlaster
{
    class LogoManager
    {
        static Random randomPos = new Random(1);

        MediaElement soundOut;

        Canvas displayCanvas;

        TextBlock statusTextBlock;

        string[] imageNames = new string[] {
                "AngledSquares", "BlueWindows10", "LightWindow",
                "OneProduct", "Squares", "Windows10","RobMiles","Cheese","LogoBlasterLogo","catwhite" };

        Dictionary<string, Image> Logos = new Dictionary<string, Image>();

        void SetupLogos(string[] logoNames)
        {
            foreach (string name in logoNames)
            {
                Image newImage = new Image();
                BitmapImage newBitmap = new BitmapImage();
                Uri imageURI = new Uri("ms-appx:///Images/" + name + ".png");
                newBitmap.UriSource = imageURI;
                newImage.Source = newBitmap;
                Logos.Add(name, newImage);
            }
        }

        int logoCount = 0;

        void CentreOnDisplay(Image image)
        {

            displayCanvas.Children.Add(image);

            double vWidthFactor = image.ActualWidth / displayCanvas.ActualWidth;
            double hWidthFactor = image.ActualHeight / displayCanvas.ActualHeight;
            double left, top;

            if (vWidthFactor > 1 || hWidthFactor > 1)
            {
                double scale;
                if (hWidthFactor > vWidthFactor)
                {
                    scale = 1.0 / hWidthFactor;
                }
                else
                {
                    scale = 1.0 / vWidthFactor;
                }
                image.Width = image.ActualWidth * scale;
                image.Height = image.ActualHeight * scale;
                left = (displayCanvas.ActualWidth - image.Width) / 2;
                top = (displayCanvas.ActualHeight - image.Height) / 2;
            }
            else
            {
                left = (displayCanvas.ActualWidth - image.ActualWidth) / 2;
                top = (displayCanvas.ActualHeight - image.ActualHeight) / 2;
            }
            Canvas.SetTop(image, top);
            Canvas.SetLeft(image, left);
        }

        public void RandomImages(int noOfImages)
        {
            for (int i = 0; i < noOfImages; i++)
            {
                string name = imageNames[randomPos.Next(0, Logos.Count)];
                Image image = new Image();
                BitmapImage newBitmap = new BitmapImage();
                Uri imageURI = new Uri("ms-appx:///Images/" + name + ".png");
                newBitmap.UriSource = imageURI;
                image.Source = newBitmap;
                double x = randomPos.Next((int)(-image.ActualWidth), (int)(displayCanvas.ActualWidth + image.ActualWidth));
                double y = randomPos.Next((int)(-image.ActualHeight), (int)(displayCanvas.ActualHeight + image.ActualHeight));
                Canvas.SetLeft(image, x);
                Canvas.SetTop(image, y);
                displayCanvas.Children.Add(image);
            }
        }

        public void NextLogo()
        {
            if (logoCount >= Logos.Count)
                logoCount = 0;

            ClearDisplay();

            CentreOnDisplay(Logos.Values.ElementAt(logoCount));

            logoCount++;
        }

        string[] soundNames = new string[]
        {
            "beep","ding","gameOver","lose"
        };

        Dictionary<string, Uri> Sounds = new Dictionary<string, Uri>();

        void SetupSounds(string[] soundNames)
        {
            soundOut = new MediaElement();
            displayCanvas.Children.Add(soundOut);

            foreach (string name in soundNames)
            {
                Uri soundUri = new Uri("ms-appx:///Sounds/" + name + ".wav");
                Sounds.Add(name, soundUri);
            }
        }

        public void PlaySound(string name)
        {
            soundOut.Source = Sounds[name];
            soundOut.Play();
        }

        public void DisplayImage(string name)
        {
            Image image = Logos[name];

            CentreOnDisplay(image);
        }

        public void DisplayMessage(string text)
        {
            statusTextBlock.Text = text;
        }

        public void ClearDisplay()
        {
            displayCanvas.Children.Clear();
            displayCanvas.Children.Add(statusTextBlock);
        }

        public LogoManager(Canvas inDisplayCanvas)
        {
            displayCanvas = inDisplayCanvas;
            statusTextBlock = new TextBlock();
            displayCanvas.Children.Add(statusTextBlock);
            Canvas.SetLeft(statusTextBlock, 200);
            Canvas.SetTop(statusTextBlock, 200);

            setupBackgroundBrushes();

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            displayCanvas.PointerPressed += DisplayCanvas_PointerPressed;

            SetupLogos(imageNames);
            //SetupSounds(soundNames);

            DisplayImage("LogoBlasterLogo");

            SetColor(Colors.White);

        }

        private void DisplayCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            RandomImages(10);
        }

        Dictionary<Color, Brush> BackgroundBrushes = new Dictionary<Color, Brush>();

        Color[] backgroundColors = new[] {
            Colors.White, Colors.Black, Colors.Red, Colors.Green, Colors.Blue,
            Colors.Yellow, Colors.Teal, Colors.Purple, Colors.Pink, Colors.Plum, Colors.Orange };

        void setupBackgroundBrushes()
        {
            foreach (Color c in backgroundColors)
            {
                SolidColorBrush b = new SolidColorBrush(c);
                BackgroundBrushes.Add(c, b);
            }
        }

        int colorPos = 0;

        public void SetColor(Color c)
        {
            Brush newBrush;
            if (BackgroundBrushes.ContainsKey(c))
            {
                newBrush = BackgroundBrushes[c];
            }
            else
            {
                SolidColorBrush b = new SolidColorBrush();
                BackgroundBrushes.Add(c, b);
                newBrush = b;
            }
            displayCanvas.Background = newBrush;
        }

        public void NextColor()
        {
            if (colorPos >= BackgroundBrushes.Count)
            {
                colorPos = 0;
            }
            displayCanvas.Background = BackgroundBrushes.Values.ElementAt(colorPos);
            colorPos++;
        }


        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case Windows.System.VirtualKey.A:
                    NextColor();
                    break;
                case Windows.System.VirtualKey.B:
                    NextLogo();
                    break;
                case Windows.System.VirtualKey.C:
                    RandomImages(10);
                    break;
                case Windows.System.VirtualKey.D:
                    ClearDisplay();
                    break;
            }
        }
    }
}
