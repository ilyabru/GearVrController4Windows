using GearVrController4Windows;
using GearVrController4WindowsSample.ViewModels;
using System;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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

            ApplicationView.PreferredLaunchViewSize = new Size(100, 110);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            ViewModel = new MainPageViewModel();
        }

        private void PickDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDevicePicker();
        }

        private async void ShowDevicePicker()
        {
            devicePicker = new DevicePicker();

            // only show Bluetooth Low Energy devices
            devicePicker.Filter.SupportedDeviceSelectors.Add(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true));
            devicePicker.Filter.SupportedDeviceSelectors.Add(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false));

            // Calculate the position to show the picker (right below the buttons)
            GeneralTransform ge = pickDeviceButton.TransformToVisual(null);
            Point point = ge.TransformPoint(new Point());
            Rect rect = new Rect(point, new Point(point.X + pickDeviceButton.ActualWidth, point.Y + pickDeviceButton.ActualHeight));

            DeviceInformation di = await devicePicker.PickSingleDeviceAsync(rect);
            if (null != di)
            {
                ViewModel.GearVrController = new GearVrController();
                await ViewModel.GearVrController.Create(di);
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.GearVrController.ClearBluetoothLEDeviceAsync();
        }
    }
}
