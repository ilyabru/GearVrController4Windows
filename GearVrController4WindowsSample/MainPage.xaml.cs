using GearVrController4Windows;
using GearVrController4WindowsSample.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GearVrController4WindowsSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DevicePicker devicePicker = null;

        //public GearVrController GearVrController { get; set; }

        public MainPageViewModel ViewModel { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(500, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            ViewModel = new MainPageViewModel();
        }

        private void PickDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            disconnectButton.IsEnabled = false;
            ShowDevicePicker();
            disconnectButton.IsEnabled = true;
        }

        private async void ShowDevicePicker()
        {
            pickDeviceButton.IsEnabled = false;

            devicePicker = new DevicePicker();

            // only show Bluetooth Low Energy devices
            devicePicker.Filter.SupportedDeviceSelectors.Add(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true));
            //devicePicker.Filter.SupportedDeviceSelectors.Add(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false));

            // Calculate the position to show the picker (right below the buttons)
            GeneralTransform ge = pickDeviceButton.TransformToVisual(null);
            Point point = ge.TransformPoint(new Point());
            Rect rect = new Rect(point, new Point(point.X + pickDeviceButton.ActualWidth, point.Y + pickDeviceButton.ActualHeight));

            DeviceInformation di = await devicePicker.PickSingleDeviceAsync(rect);
            if (null != di)
            {
                ViewModel.GearVrController = new GearVrController();
                await ViewModel.GearVrController.ConnectAsync(di);
            }

            ViewModel.GearVrController.PropertyChanged += Gvc_Changed;

            pickDeviceButton.IsEnabled = true;
        }

        private void Gvc_Changed(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(GearVrController.TouchpadButton):
                    if (ViewModel.GearVrController.TouchpadTapped == true)
                    {
                        Debug.WriteLine("Touchpad pressed and is true!");
                    }
                    break;
                case nameof(GearVrController.HomeButton):
                    Debug.WriteLine("Pressed home button.");
                    break;
                default:
                    break;
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.GearVrController.ClearBluetoothLEDeviceAsync();
        }
    }
}
