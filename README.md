# Gear VR Controller 4 Windows

[![latest version](https://img.shields.io/nuget/v/GearVrController4Windows)](https://www.nuget.org/packages/GearVrController4Windows) [![downloads](https://img.shields.io/nuget/dt/GearVrController4Windows)](https://www.nuget.org/packages/GearVrController4Windows)

This library allows you to use a Gear VR controller in a UWP app over Bluetooth!

![Gear VR Controller](https://raw.githubusercontent.com/ilyabru/GearVrController4Windows/master/Docs/GVRCimage.jpg)

## Download & Install

**Nuget Package: [GearVrController4Windows](https://www.nuget.org/packages/GearVrController4Windows/)**

```sh
Install-Package GearVrController4Windows
```

## Operating Instructions

### Setup Connection

1. Create an instance of the GearVrController class:

    ```csharp
    var gearVrController = new GearVrController();
    ```

2.
    Using the `Windows.Devices.Enumeration` APIs, pair the Gear VR controller and retrieve the `DeviceInformation` object.
    One way to do this is by using the `DevicePicker` class. This code snippet from the GearVrController4WindowsSample project demonstrates how to display the DevicePicker on the form and get the DeviceInformation object:

    ```csharp
    devicePicker = new DevicePicker();

    // only show Bluetooth Low Energy devices
    devicePicker.Filter.SupportedDeviceSelectors.Add(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true));
    devicePicker.Filter.SupportedDeviceSelectors.Add(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false));

    // Calculate the position to show the picker (right below the buttons)
    GeneralTransform ge = pickDeviceButton.TransformToVisual(null);
    Point point = ge.TransformPoint(new Point());
    Rect rect = new Rect(point, new Point(point.X + pickDeviceButton.ActualWidth, point.Y + pickDeviceButton.ActualHeight));

    // save the device information of the clicked device
    DeviceInformation di = await devicePicker.PickSingleDeviceAsync(rect);
    ```

    *If you want to create your own device picker, then use the DeviceWatcher class.
    To see how, check out the official Microsoft documentation and sample on this topic [here](https://github.com/microsoft/Windows-universal-samples/tree/master/Samples/DeviceEnumerationAndPairing). You can also check out my other project [here](https://github.com/ilyabru/Donations-Board) to see how I got the controller to work with an MVVM architectural pattern.*

3.
    Pass the `DeviceInformation` variable into the `gearVrController.Create` method (do this in an async method):

    ```csharp
    await gearVrController.ConnectAsync(di);
    ```

### Reading Inputs

To read inputs either bind a button property to a control in XAML or subscribe to the `PropertyChanged` event.

**Binding to a control in XAML:**

```xml
<TextBlock Text="{x:Bind GearVrController.TouchpadButton, Mode=OneWay}" />
<!--GearVrController is a public property in the code-behind. You can also bind to a ViewModel.
In this example, the TextBlock will have a value of "True" when the button is pressed, and "False" when not.-->
```

**Using PropertyChanged in code behind:**

```csharp
public sealed partial class MainPage : Page
{
    public GearVrController GearVrController { get; set; }

    public MainPage()
    {
        GearVrController.PropertyChanged += Gvc_PropertyChanged;
    }

    private void Gvc_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
```

Please see the sample project for a complete implementation.

## Notes

* Motion readings (Accel, Gyro, Mag) are not fully correct. Official implementation uses multiple readings for each coordinate (X, Y, or Z) and consolidates them?
* No fusion drift algorithm applied, having one would make the readings more useful to developers.
* Magnetometer calibration not implemented.
* Only works with UWP apps running on Windows 10 1803 or above due to the Bluetooth APIs used.

## Credits

* Jim Yang for his research on reverse engineering the controller: [https://jsyang.ca/hacks/gear-vr-rev-eng](https://jsyang.ca/hacks/gear-vr-rev-eng/)
