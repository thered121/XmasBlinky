using System;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.Devices.Adc;
using System.Threading;
using System.Threading.Tasks;
using XmasBlinky.IoT.AdcMcp3008;
using System.Diagnostics;
using Microsoft.Maker.Media.UniversalMediaEngine;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace XmasBlinky
{
    public sealed class StartupTask : IBackgroundTask
    {
        private const int RED_LED_PIN = 5;
        private const int GREEN_LED_PIN = 6;
        private GpioPin rpin;
        private GpioPin gpin;
        private GpioPinValue rpinValue;
        private GpioPinValue gpinValue;
        private DispatcherTimer timer;       
        private MediaEngine mEngine = new MediaEngine();

        // Use for configuration of the MCP3008 class voltage formula
        const float ReferenceVoltage = 5.0F;

        // Values for which channels we will be using from the ADC chip
        const byte LowPotentiometerADCChannel = 0;
        const byte HighPotentiometerADCChannel = 1;
        const byte CDSADCChannel = 2;

        // Some strings to let us know the current state.
        const string JustRightLightString = "Ah, just right";
        const string LowLightString = "I need a light";
        const string HighLightString = "I need to wear shades";

        // Some internal state information
        enum eState { unknown, JustRight, TooBright, TooDark };
        eState CurrentState = eState.unknown;

        private AdcController adcController;
        private AdcChannel LowPotAdcChannel;
        private AdcChannel HighPotAdcChannel;
        private AdcChannel CdsAdcChannel;

        // A timer to control how often we check the ADC values.
       private Timer timer2;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(700);
            timer.Tick += Timer_Tick;
            timer2 = new Timer(timerCallback, this, 0, 1000);
            InitGPIO();
            InitChip();
            mEngine.InitializeAsync();          

            if (rpin != null && gpin != null)
            {
                timer.Start();
            }
            else
            {
                Debug.WriteLine("One of the pins isn't right");
            }

            mEngine.MediaStateChanged += MEngine_MediaStateChanged;
            mEngine.Volume = 0.01;
            mEngine.Play("ms-appx:///song.mp3");
        }
        private void MEngine_MediaStateChanged(MediaState state)
        {
            switch (state)
            {
                case MediaState.Loading:
                    Debug.WriteLine("PlaybackState.Loading");
                    break;

                case MediaState.Stopped:
                    Debug.WriteLine("PlaybackState.Paused");
                    break;

                case MediaState.Playing:
                    Debug.WriteLine("PlaybackState.Playing");
                    break;

                case MediaState.Error:
                    Debug.WriteLine("PlaybackState.Error_MediaInvalid");
                    break;

                case MediaState.Ended:
                    Debug.WriteLine("PlaybackState.Ended");
                    mEngine.Play("ms-appx:///Assets/song.mp3");
                    break;

            }
        }

        private async Task InitChip()
        {
            if (adcController == null)
            {
                // Initialize the ADC chip for use
                adcController = (await AdcController.GetControllersAsync(AdcMcp3008Provider.GetAdcProvider()))[0];
                LowPotAdcChannel = adcController.OpenChannel(LowPotentiometerADCChannel);
                HighPotAdcChannel = adcController.OpenChannel(HighPotentiometerADCChannel);
                CdsAdcChannel = adcController.OpenChannel(CDSADCChannel);
            }
        }

        private void InitGPIO()
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
        private async void Timer_Tick(object sender, object e)
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

        public float ADCToVoltage(int adc)
        {
            return (float)adc * ReferenceVoltage / (float)adcController.MaxValue;
        }


        private async void timerCallback(object state)
        {
            Debug.WriteLine("\nMainPage::timerCallback");
            if (adcController == null)
            {
                Debug.WriteLine("MainPage::timerCallback not ready");
                return;
            }

            // The new light state, assume it's just right to start.
            eState newState = eState.JustRight;

            // Read from the ADC chip the current values of the two pots and the photo cell.
            int lowPotReadVal = LowPotAdcChannel.ReadValue();
            int highPotReadVal = HighPotAdcChannel.ReadValue();
            int cdsReadVal = CdsAdcChannel.ReadValue();

            // convert the ADC readings to voltages to make them more friendly.
            float lowPotVoltage = ADCToVoltage(lowPotReadVal);
            float highPotVoltage = ADCToVoltage(highPotReadVal);
            float cdsVoltage = ADCToVoltage(cdsReadVal);

            // Let us know what was read in.
            Debug.WriteLine(String.Format("Read values {0}, {1}, {2} ", lowPotReadVal, highPotReadVal, cdsReadVal));
            Debug.WriteLine(String.Format("Voltages {0}, {1}, {2} ", lowPotVoltage, highPotVoltage, cdsVoltage));

            // Compute the new state by first checking if the light level is too low
            if (cdsVoltage < lowPotVoltage)
            {
                newState = eState.TooDark;
                mEngine.Volume = 1.00;

            }
            // And now check if it too high.
            else if (cdsVoltage > highPotVoltage)
            {
                newState = eState.TooBright;
            }
            else
            {
                // mEngine.Volume = 1.00;
            }

            // Use another method to determine what to do with the state.
            await CheckForStateChange(newState);
        }

        private async Task CheckForStateChange(eState newState)
        {
            // Checks for state changes and does something when one is detected.
            if (newState != CurrentState)
            {
                String whatToSay;

                switch (newState)
                {
                    case eState.JustRight:
                        {
                            whatToSay = JustRightLightString;
                        }
                        break;

                    case eState.TooBright:
                        {
                            whatToSay = HighLightString;
                        }
                        break;

                    case eState.TooDark:
                        {
                            whatToSay = LowLightString;
                        }
                        break;

                    default:
                        {
                            whatToSay = "unexpected value";
                        }
                        break;
                }
                Debug.WriteLine(String.Format("MainPage::TextToSpeech {0}", whatToSay));
                // Update the current state for next time.
                CurrentState = newState;

            }
        }
    }
}
