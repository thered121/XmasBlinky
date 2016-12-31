using System;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.Devices.Adc;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IoT.AdcMcp3008;
using System.Diagnostics;
using Microsoft.Maker.Media.UniversalMediaEngine;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace XmasBlinky
{
    public sealed partial class MainPage : Page
    {
       
       
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
        public Timer timer2;


        public MainPage()
        {
            InitializeComponent();

                  
           
            timer2 = new Timer(timerCallback, this, 0, 1000);
           
            InitChip();
            mEngine.InitializeAsync();
            Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

            

            mEngine.MediaStateChanged += MEngine_MediaStateChanged;
            mEngine.Volume = 0.01;
            mEngine.Play("ms-appx:///Assets/song.mp3");
            

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
