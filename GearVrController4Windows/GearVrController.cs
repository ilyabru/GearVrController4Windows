using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace GearVrController4Windows
{
    /// <summary>
    /// Represents the Gear VR Controller
    /// </summary>
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

        private const float ACCEL_FACTOR = 0.00001F;
        private const float GYRO_FACTOR = 0.0001F;

        private bool subscribedForNotifications = false;

        #region Error Codes
        readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion

        private BluetoothLEDevice device = null;

        private IReadOnlyList<GattDeviceService> services;
        private GattDeviceService customService = null;
        private GattCharacteristic notifyCharacteristic = null;
        private GattCharacteristic writeCharacteristic = null;

        private DeviceInformation deviceInformation;

        private byte[] eventData = new byte[60];

        #region Properties
        private bool touchpadButton;
        private bool triggerButton;
        private bool homeButton;
        private bool backButton;
        private bool volumeUpButton;
        private bool volumeDownButton;
        private short axisX;
        private short axisY;
        private bool touchpadTapped;

        private float accelX;
        private float accelY;
        private float accelZ;
        private float gyroX;
        private float gyroY;
        private float gyroZ;
        private float magX;
        private float magY;
        private float magZ;

        /// <summary>
        /// Represents the large touchpad button.
        /// </summary>
        public bool TouchpadButton
        {
            get => touchpadButton;
            set => SetPropertyValue(ref touchpadButton, value);
        }

        /// <summary>
        /// Represent the trigger button.
        /// </summary>
        public bool TriggerButton
        {
            get => triggerButton;
            set => SetPropertyValue(ref triggerButton, value);
        }

        /// <summary>
        /// Represent the home button on the right side.
        /// </summary>
        public bool HomeButton
        {
            get => homeButton;
            set => SetPropertyValue(ref homeButton, value);
        }

        /// <summary>
        /// Represent the back button on the left side.
        /// </summary>
        public bool BackButton
        {
            get => backButton;
            set => SetPropertyValue(ref backButton, value);
        }

        /// <summary>
        /// Represent the volume up button.
        /// </summary>
        public bool VolumeUpButton
        {
            get => volumeUpButton;
            set => SetPropertyValue(ref volumeUpButton, value);
        }

        /// <summary>
        /// Represent the volume down button.
        /// </summary>
        public bool VolumeDownButton
        {
            get => volumeDownButton;
            set => SetPropertyValue(ref volumeDownButton, value);
        }

        /// <summary>
        /// Represent the x-axis touchpad value. Min = 0, Max = 315
        /// </summary>
        public short AxisX
        {
            get => axisX;
            set => SetPropertyValue(ref axisX, value);
        }

        /// <summary>
        /// Represent the y-axis touchpad value. Min = 0, Max = 315
        /// </summary>
        public short AxisY
        {
            get => axisY;
            set => SetPropertyValue(ref axisY, value);
        }

        /// <summary>
        /// This is activated when touchpad is tapped.
        /// </summary>
        public bool TouchpadTapped
        {
            get => touchpadTapped;
            private set => SetPropertyValue(ref touchpadTapped, value);
        }

        /// <summary>
        /// Represents Accelerometer X value
        /// </summary>
        public float AccelX
        {
            get => accelX;
            private set => SetPropertyValue(ref accelX, value);
        }

        /// <summary>
        /// Represents Accelerometer Y value
        /// </summary>
        public float AccelY
        {
            get => accelY;
            private set => SetPropertyValue(ref accelY, value);
        }

        /// <summary>
        /// Represents Accelerometer Z value
        /// </summary>
        public float AccelZ
        {
            get => accelZ;
            private set => SetPropertyValue(ref accelZ, value);
        }

        /// <summary>
        /// Represents Gyroscope X value
        /// </summary>
        public float GyroX
        {
            get => gyroX;
            private set => SetPropertyValue(ref gyroX, value);
        }

        /// <summary>
        /// Represents Gyroscope Y value
        /// </summary>
        public float GyroY
        {
            get => gyroY;
            private set => SetPropertyValue(ref gyroY, value);
        }

        /// <summary>
        /// Represents Gyroscope Z value
        /// </summary>
        public float GyroZ
        {
            get => gyroZ;
            private set => SetPropertyValue(ref gyroZ, value);
        }

        /// <summary>
        /// Represents Magnetometer X value
        /// </summary>
        public float MagX
        {
            get => magX;
            private set => SetPropertyValue(ref magX, value);
        }

        /// <summary>
        /// Represents Magnetometer Y value
        /// </summary>
        public float MagY
        {
            get => magY;
            private set => SetPropertyValue(ref magY, value);
        }

        /// <summary>
        /// Represents Magnetometer Z value
        /// </summary>
        public float MagZ
        {
            get => magZ;
            private set => SetPropertyValue(ref magZ, value);
        }
        #endregion

        /// <summary>
        /// Connect to Gear VR Controller and start listening for notifications
        /// </summary>
        /// <param name="deviceInformation">DeviceInformation obtained from DeviceWatcher or similar</param>
        /// <returns></returns>
        public async Task ConnectAsync(DeviceInformation deviceInformation)
        {
            this.deviceInformation = deviceInformation;

            await GetBLEDevice();

            device.ConnectionStatusChanged += Device_ConnectionStatusChanged;

            await GetCustomService();

            await EnumerateCharacteristics();

            await SubscribeToNotifications();

            await RunCommand(CMD_VR_MODE); // enables high frequency mode
            await RunCommand(CMD_SENSOR); // enables sending of input data

        }

        /// <summary>
        /// Disable notifications and clear BLE device
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ClearBluetoothLEDeviceAsync()
        {
            if (subscribedForNotifications)
            {
                // Need to clear the CCCD from the remote device so we stop receiving notifications
                var result = await notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                if (result != GattCommunicationStatus.Success)
                {
                    return false;
                }
                else
                {
                    RemoveValueChangedHandler();
                }
            }
            if (device != null)
            {
                device.ConnectionStatusChanged -= Device_ConnectionStatusChanged;
                device?.Dispose();
            }

            device = null;
            return true;
        }

        private async Task GetBLEDevice()
        {
            if (!await ClearBluetoothLEDeviceAsync())
            {
                Debug.WriteLine("Error: Unable to reset state, try again.");
                return;
            }

            try
            {
                // BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                device = await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);

                if (device == null)
                {
                    Debug.WriteLine("Failed to connect to device.");
                }
            }
            catch (Exception ex) when (ex.HResult == E_DEVICE_NOT_AVAILABLE)
            {
                Debug.WriteLine("Bluetooth radio is not on.");
                throw;
            }
        }

        private async Task GetCustomService()
        {
            if (device != null)
            {
                GattDeviceServicesResult result = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    services = result.Services;
                    Debug.WriteLine("Found {0} services", services.Count);

                    customService = services.FirstOrDefault(s => s.Uuid == new Guid(UUID_CUSTOM_SERVICE));
                }
                else
                {
                    Debug.WriteLine("Device unreachable");
                }
            }
        }


        private async Task EnumerateCharacteristics()
        {
            RemoveValueChangedHandler();

            IReadOnlyList<GattCharacteristic> characteristics = null;
            try
            {
                // Ensure we have access to the device.
                var accessStatus = await customService.RequestAccessAsync();
                if (accessStatus == DeviceAccessStatus.Allowed)
                {
                    // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                    // and the new Async functions to get the characteristics of unpaired devices as well. 
                    var result = await customService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        characteristics = result.Characteristics;
                    }
                    else
                    {
                        Debug.WriteLine("Error accessing service.");

                        // On error, act as if there are no characteristics.
                        characteristics = new List<GattCharacteristic>();
                    }
                }
                else
                {
                    Debug.WriteLine("Error accessing service.");

                    // On error, act as if there are no characteristics.
                    characteristics = new List<GattCharacteristic>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Restricted service. Can't read characteristics: {ex.Message}");
                // On error, act as if there are no characteristics.
                characteristics = new List<GattCharacteristic>();
            }

            notifyCharacteristic = characteristics
                .FirstOrDefault(c => c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)
                    && c.Uuid == new Guid(UUID_CUSTOM_SERVICE_NOTIFY));

            writeCharacteristic = characteristics
                .FirstOrDefault(c => c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write)
                    && c.Uuid == new Guid(UUID_CUSTOM_SERVICE_WRITE));
        }

        private async Task SubscribeToNotifications()
        {
            if (!subscribedForNotifications)
            {
                try
                {
                    // BT_Code: Must write the CCCD in order for server to send indications.
                    // We receive them in the ValueChanged event handler
                    var status = await notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.Notify);

                    if (status == GattCommunicationStatus.Success)
                    {
                        AddValueChangedHandler();
                        Debug.WriteLine("Successfully subscribed for value changes");
                    }
                    else
                    {
                        Debug.WriteLine($"Error registering for value changes: {status}");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                try
                {
                    var result = await notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.None);
                    if (result == GattCommunicationStatus.Success)
                    {
                        RemoveValueChangedHandler();
                        Debug.WriteLine("Successfully un-registered for notifications");
                    }
                    else
                    {
                        Debug.WriteLine($"Error un-registering for notifications: {result}");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private void AddValueChangedHandler()
        {
            if (!subscribedForNotifications)
            {
                notifyCharacteristic.ValueChanged += Characteristic_ValueChanged;
                subscribedForNotifications = true;
            }
        }

        private void RemoveValueChangedHandler()
        {
            if (subscribedForNotifications)
            {
                notifyCharacteristic.ValueChanged -= Characteristic_ValueChanged;
                notifyCharacteristic = null;
                subscribedForNotifications = false;
            }
        }

        // This handler will be called when the controller re-connects
        private async void Device_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                Debug.WriteLine("Device has reconnected.");

                if (writeCharacteristic != null)
                {
                    // running these commands will re enable sending of data on a reconnect
                    await RunCommand(CMD_VR_MODE); // enables high frequency mode
                    await RunCommand(CMD_SENSOR); // enables sending of input data
                }
            }
            else
            {
                Debug.WriteLine("Device was disconnected.");
            }
        }

        private async Task<bool> RunCommand(short commandValue)
        {
            var writer = new DataWriter();
            writer.WriteInt16(commandValue);

            try
            {
                var writeResult = await writeCharacteristic.WriteValueWithResultAsync(writer.DetachBuffer());
                if (writeResult.Status == GattCommunicationStatus.Success)
                {
                    Debug.WriteLine($"Successfully wrote value {commandValue} to device");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"Write failed: {writeResult.Status}");
                    return false;
                }
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_INVALID_PDU)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED || ex.HResult == E_ACCESSDENIED)
            {
                // This usually happens when a device reports that it support writing, but it actually doesn't.
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (eventData.Length == args.CharacteristicValue.Length)
            {
                DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(eventData);

                // We must update the collection on the UI thread because the collection is databound to a UI element.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    TouchpadButton = (eventData[58] & (1 << 3)) != 0;
                    BackButton = (eventData[58] & (1 << 2)) != 0;
                    HomeButton = (eventData[58] & (1 << 1)) != 0;
                    VolumeUpButton = (eventData[58] & (1 << 4)) != 0;
                    VolumeDownButton = (eventData[58] & (1 << 5)) != 0;
                    TriggerButton = (eventData[58] & (1 << 0)) != 0;
                    AxisX = (short)((((eventData[54] & 0xF) << 6) + ((eventData[55] & 0xFC) >> 2)) & 0x3FF);
                    AxisY = (short)((((eventData[55] & 0x3) << 8) + ((eventData[56] & 0xFF) >> 0)) & 0x3FF);
                    TouchpadTapped = AxisX != 0 && AxisY != 0;

                    AccelX = GetAccelerometerData(eventData, 0, 4);
                    AccelY = GetAccelerometerData(eventData, 0, 6);
                    AccelZ = GetAccelerometerData(eventData, 0, 8);
                    GyroX = GetGyroscopeData(eventData, 0, 10);
                    GyroY = GetGyroscopeData(eventData, 0, 12);
                    GyroZ = GetGyroscopeData(eventData, 0, 14);
                    MagX = GetMagnetometerData(eventData, 0);
                    MagY = GetMagnetometerData(eventData, 2);
                    MagZ = GetMagnetometerData(eventData, 4);
                });
            }
        }

        private float GetAccelerometerData(byte[] eventData, int index, int offset)
        {
            byte[] acceldata = new byte[] { eventData[16 * index + offset], eventData[16 * index + offset + 1] };
            short[] arrayOfShort = new short[acceldata.Length / 2];

            System.Buffer.BlockCopy(acceldata, 0, arrayOfShort, 0, acceldata.Length);

            return arrayOfShort[0] * 10000.0F * 9.80665F / 2048.0F * ACCEL_FACTOR;
        }

        private float GetGyroscopeData(byte[] eventData, int index, int offset)
        {
            byte[] acceldata = new byte[] { eventData[16 * index + offset], eventData[16 * index + offset + 1] };
            short[] arrayOfShort = new short[acceldata.Length / 2];

            System.Buffer.BlockCopy(acceldata, 0, arrayOfShort, 0, acceldata.Length);

            return arrayOfShort[0] * 10000.0F * 0.017453292F / 14.285F * GYRO_FACTOR;
        }

        private float GetMagnetometerData(byte[] eventData, int offset)
        {
            byte[] acceldata = new byte[] { eventData[32 + offset], eventData[32 + offset + 1] };
            short[] arrayOfShort = new short[acceldata.Length / 2];

            System.Buffer.BlockCopy(acceldata, 0, arrayOfShort, 0, acceldata.Length);

            return arrayOfShort[0] * 0.06F;
        }
    }
}
