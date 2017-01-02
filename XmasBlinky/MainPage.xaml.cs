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
       



        public MainPage()
        {
            InitializeComponent();
            
            AdcMediaController adcCtrl = new AdcMediaController();            
            adcCtrl.InitializeMediaController();
            Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);
        }

       
    }
}
