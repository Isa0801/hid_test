// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;


[DllImport("hid.dll", SetLastError = true)]
static extern void HidD_GetHidGuid(out GUID HidGuid);

[DllImport("setupapi.dll", SetLastError = true)]
static extern IntPtr SetupDiGetClassDevs(ref GUID classGuid, IntPtr enumerator, IntPtr hwndParent, uint flags);

[DllImport("setupapi.dll", SetLastError = true)]
static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref GUID interfaceClassGuid, uint memberIndex, ref DeviceInterfaceData deviceInterfaceData);

[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
static extern bool SetupDiGetDeviceInterfaceDetail(
    IntPtr deviceInfoSet,
    ref DeviceInterfaceData deviceInterfaceData,
    IntPtr deviceInterfaceDetailData,   // Using IntPtr to handle both cases
    uint deviceInterfaceDetailDataSize,
    out uint requiredSize,
    IntPtr deviceInfoData);


const uint DIGCF_PRESENT = 0x00000002;
const uint DIGCF_DEVICEINTERFACE = 0x00000010;
GUID hidGuid;


HidD_GetHidGuid(out hidGuid);  // Get the HID GUID
Console.WriteLine($"HID GUID: {hidGuid}");

IntPtr deviceInfoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
if (deviceInfoSet == IntPtr.Zero)
{
    Console.WriteLine("Error getting device information set.");
    return;
}

DeviceInterfaceData deviceInterfaceData = new DeviceInterfaceData();
deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

// Enumerate through the device interfaces
for (uint index = 0; SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, index, ref deviceInterfaceData); index++)
{
    uint requiredSize = 0;

    // First call to get the required buffer size
    SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);

    // Allocate memory for the detail data
    IntPtr detailDataBuffer = Marshal.AllocHGlobal((int)requiredSize);
    DeviceInterfaceDetailData deviceInterfaceDetailData = new DeviceInterfaceDetailData();
    deviceInterfaceDetailData.cbSize = IntPtr.Size == 8 ? 8 : 5; // Adjust size depending on architecture

    // Cast the pointer to the DeviceInterfaceDetailData structure
    Marshal.StructureToPtr(deviceInterfaceDetailData, detailDataBuffer, true);

    // Second call to retrieve the actual device path
    bool success = SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailDataBuffer, requiredSize, out requiredSize, IntPtr.Zero);
    if (success)
    {
        // Retrieve the device path from the structure
        deviceInterfaceDetailData = (DeviceInterfaceDetailData)Marshal.PtrToStructure(detailDataBuffer, typeof(DeviceInterfaceDetailData));
        Console.WriteLine($"Device Path: {deviceInterfaceDetailData.DevicePath}");
    }
    else
    {
        Console.WriteLine("Failed to get device interface detail.");
    }

    Marshal.FreeHGlobal(detailDataBuffer); // Free allocated memory
}


Console.WriteLine("Hello, World!");



// Structures and constants used
[StructLayout(LayoutKind.Sequential)]
struct DeviceInterfaceData
{
    public int cbSize;
    public Guid InterfaceClassGuid;
    public int Flags;
    public IntPtr Reserved;
}

// GUID structure definition for HID
[StructLayout(LayoutKind.Sequential)]
struct GUID
{
    public int Data1;
    public short Data2;
    public short Data3;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[] Data4;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
struct DeviceInterfaceDetailData
{
    public int cbSize;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string DevicePath;
}