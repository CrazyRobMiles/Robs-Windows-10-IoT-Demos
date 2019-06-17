using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TemperatureReader
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        BME280Sensor bme280;
        private DispatcherTimer timer;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            bme280 = new BME280Sensor();
            await bme280.Initialize();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;

            timer.Start();
        }

        private async void Timer_Tick(object sender, object e)
        {
            float temp = await bme280.ReadTemperature();
            float humidity = await bme280.ReadHumidity();
            float pressure = await bme280.ReadPreasure()/1000;

            TempTextBlock.Text = "Temp: " + temp.ToString();
            HumidityTextBlock.Text = "Humidity: " + humidity.ToString();
            PressureTextBlock.Text = "Pressure: " + pressure.ToString();
         }
    }
}
