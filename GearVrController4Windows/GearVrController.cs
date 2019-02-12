using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace GearVrController4Windows
{
    public class GearVrController : ObservableObject
    {
        private const string UUID_CUSTOM_SERVICE = "4f63756c-7573-2054-6872-65656d6f7465";
        private const string UUID_CUSTOM_SERVICE_WRITE = "c8c51726-81bc-483b-a052-f7a14ea3d282";
        private const string UUID_CUSTOM_SERVICE_NOTIFY = "c8c51726-81bc-483b-a052-f7a14ea3d281";

        private const short CMD_OFF = 0x0000;
        private const short CMD_SENSOR = 0x0100;
        private const short CMD_UNKNOWN_FIRMWARE_UPDATE_FUNC = 0x0200;
        private const short CMD_CALIBRATE = 0x0300;
        private const short CMD_KEEP_ALIVE = 0x0400;
        private const short CMD_UNKNOWN_SETTING = 0x0500;
        private const short CMD_LPM_ENABLE = 0x0600;
        private const short CMD_LPM_DISABLE = 0x0700;
        private const short CMD_VR_MODE = 0x0800;

        private GattDeviceService customService = null;
        private GattCharacteristic notifyCharacteristic = null;
        private GattCharacteristic writeCharacteristic = null;

        private DeviceInformation deviceInformation;

        private bool touchpadButton;

        public GearVrController() { }

        public GearVrController(DeviceInformation deviceInformation)
        {
            this.deviceInformation = deviceInformation;
            Initialize();
        }

        public bool TouchpadButton
        {
            get => touchpadButton;
            set => SetPropertyValue(ref touchpadButton, value);
        }

        private async void Initialize()
        {
            var device = await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);

            var servicesResult = await device.GetGattServicesAsync();

            if (servicesResult.Status == GattCommunicationStatus.Success)
            {
                foreach (var service in servicesResult.Services)
                {
                    if (service.Uuid == new Guid(UUID_CUSTOM_SERVICE))
                    {
                        customService = service;
                    }
                }
            }

            if (customService != null)
            {
                var characteristicsResult = await customService.GetCharacteristicsAsync();

                if (characteristicsResult.Status == GattCommunicationStatus.Success)
                {
                    foreach (var characteristic in characteristicsResult.Characteristics)
                    {
                        if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) &&
                            characteristic.Uuid == new Guid(UUID_CUSTOM_SERVICE_NOTIFY))
                        {
                            notifyCharacteristic = characteristic;
                        }
                        else if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) &&
                            characteristic.Uuid == new Guid(UUID_CUSTOM_SERVICE_WRITE))
                        {
                            writeCharacteristic = characteristic;
                        }
                    }
                }
            }

            if (notifyCharacteristic != null)
            {
                var status = await notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);

                if (status == GattCommunicationStatus.Success)
                {
                    notifyCharacteristic.ValueChanged += Characteristic_ValueChanged;
                    RunCommand(CMD_SENSOR);
                }
                else
                {
                    // Log error
                }
            }
        }

        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var eventData = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(eventData);

            // We must update the collection on the UI thread because the collection is databound to a UI element.
            if (TouchpadButton != ((eventData[58] & (1 << 3)) != 0))
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    TouchpadButton = (eventData[58] & (1 << 3)) != 0;
                });
            }
        }

        private async void RunCommand(short commandValue)
        {
            var writer = new DataWriter();
            writer.WriteInt16(commandValue);

            var writeResult = await writeCharacteristic.WriteValueAsync(writer.DetachBuffer());

            if (writeResult == GattCommunicationStatus.Success)
            {
                // do something
            }
            else
            {
                //log error
            }
        }
    }
}
