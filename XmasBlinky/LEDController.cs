using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace XmasBlinky
{
    internal class LEDController
    {
        private const int RED_LED_PIN = 5;
        private const int GREEN_LED_PIN = 6;
        private GpioPin rpin;
        private GpioPin gpin;
        private GpioPinValue rpinValue;
        private GpioPinValue gpinValue;
        private Timer timer;
        private bool lightsRunning = false;

        public bool LightsRunning
        {
            get
            {
                return lightsRunning;
            }

            set
            {
                lightsRunning = value;
            }
        }

        public async void InitializeLEDController()
        {
            await InitGPIO();
            if (rpin != null && gpin != null)
            {
                
            }
            else
            {
                Debug.WriteLine("One of the pins isn't right");
            }
        }

        public void StartLights()
        {
            this.lightsRunning = true;
            timer = new Timer(Timer_Tick, this, 0, 700);
        }

        public void StopLights()
        {
            this.lightsRunning = false;
            timer.Dispose();
            rpinValue = GpioPinValue.High;
            rpin.Write(rpinValue);
            gpinValue = GpioPinValue.High;
            gpin.Write(gpinValue);

        }
        private async Task InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                rpin = null;
                Debug.WriteLine("There is no GPIO controller on this device.");
                return;
            }

            rpin = gpio.OpenPin(RED_LED_PIN);
            rpinValue = GpioPinValue.High;
            rpin.Write(rpinValue);
            rpin.SetDriveMode(GpioPinDriveMode.Output);
            gpin = gpio.OpenPin(GREEN_LED_PIN);
            gpinValue = GpioPinValue.High;
            gpin.Write(gpinValue);
            gpin.SetDriveMode(GpioPinDriveMode.Output);

           Debug.WriteLine("GPIO pin initialized correctly.");

        }
        private async void Timer_Tick(object sender)
        {
            if (rpinValue == GpioPinValue.High)
            {
                rpinValue = GpioPinValue.Low;
                rpin.Write(rpinValue);
                gpinValue = GpioPinValue.High;
                gpin.Write(gpinValue);
            }
            else
            {
                gpinValue = GpioPinValue.Low;
                gpin.Write(gpinValue);
                rpinValue = GpioPinValue.High;
                rpin.Write(rpinValue);
            }
        }
    }
}
