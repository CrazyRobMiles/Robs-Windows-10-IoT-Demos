using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Gpio;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LogoBlaster
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        LogoManager manager = null;

        public MainPage()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            timer.Start();

            if (manager == null)
                manager = new LogoManager(DisplayCanvas);

            Unloaded += MainPage_Unloaded;

            InitGPIO();
        }

        const int B1Pin = 12;  // wired to pin 32
        const int B2Pin = 13;  // wired to pin 33
        const int B3Pin = 16;  // wired to pin 36
        const int B4Pin = 26;  // wired to pin 37

        private GpioPin button1Pin;
        private GpioPin button2Pin;
        private GpioPin button3Pin;
        private GpioPin button4Pin;

        private List<GpioPin> allPins = new List<GpioPin>();

        GpioPin openPin(GpioController gpio, int pinNumber)
        {
            GpioPin pin;

            pin = gpio.OpenPin(pinNumber);
            if (pin == null)
            {
                manager.DisplayMessage("There were problems initializing the GPIO pin at " + pinNumber.ToString());
                return null;
            }

            if (!allPins.Contains(pin))
                allPins.Add(pin);

            return pin;
        }

        private void InitGPIO()
        {

            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                manager.DisplayMessage("There is no GPIO controller on this device.");
                return;
            }

            button1Pin = openPin(gpio, B1Pin);
            button2Pin = openPin(gpio, B2Pin);
            button3Pin = openPin(gpio, B3Pin);
            button4Pin = openPin(gpio, B4Pin);

            foreach (GpioPin pin in allPins)
            {
                pin.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 20);
            }

            button1Pin.ValueChanged += Button1Pin_ValueChanged;
            button2Pin.ValueChanged += Button2Pin_ValueChanged;
            button3Pin.ValueChanged += Button3Pin_ValueChanged;
            button4Pin.ValueChanged += Button4Pin_ValueChanged;
        }

        private void Button4Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (button4Pin.Read() == GpioPinValue.High)
            {
                command = DisplayCommands.ClearScreen;
            }
        }

        private void Button3Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (button3Pin.Read() == GpioPinValue.High)
            {
                command = DisplayCommands.RandomLogo;
            }
        }

        private void Button2Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (button2Pin.Read() == GpioPinValue.High)
            {
                command = DisplayCommands.NextLogo;
            }
        }

        private void Button1Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (button1Pin.Read() == GpioPinValue.High)
            {
                command = DisplayCommands.NextColour;
            }
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            // Cleanup
            button1Pin.Dispose();
            button2Pin.Dispose();
            button3Pin.Dispose();
            button4Pin.Dispose();
        }

        enum DisplayCommands
        {
            noCommand,
            ClearScreen,
            NextColour,
            NextLogo,
            RandomLogo
        };

        DisplayCommands command = DisplayCommands.noCommand;

        private void Timer_Tick(object sender, object e)
        {
            while (command != DisplayCommands.noCommand)
            {
                DisplayCommands currentCommand;
                // copy the flag and clear it
                // Lock this action so that in interrupt from the pins
                // won't cause us to miss a command
                lock (this)
                {
                    currentCommand = command;
                    command = DisplayCommands.noCommand;
                }
                switch (currentCommand)
                {
                    case DisplayCommands.ClearScreen:
                        manager.ClearDisplay();
                        break;
                    case DisplayCommands.NextColour:
                        manager.NextColor();
                        break;
                    case DisplayCommands.NextLogo:
                        manager.NextLogo();
                        break;
                    case DisplayCommands.RandomLogo:
                        manager.RandomImages(10);
                        break;
                }
            }
        }

        private DispatcherTimer timer;
    }
}
