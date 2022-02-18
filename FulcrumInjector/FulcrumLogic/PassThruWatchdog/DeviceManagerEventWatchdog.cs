using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.PassThruWatchdog
{
    /// <summary>
    /// Object used to monitor the status or states of device manager to set if new devices are added or removed
    /// This class is able to trace when new events are registered for device connections and process them as a notification event
    /// </summary>
    public static class DeviceManagerEventWatchdog
    {
        // Logger object.
        private static SubServiceLogger DeviceEventLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("DeviceEventLogger")) ?? new SubServiceLogger("DeviceEventLogger");

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Registers a new device connection changed notification event
        /// </summary>
        /// <param name="Recipient"></param>
        /// <param name="NotiFilter"></param>
        /// <param name="NotiFlags"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr Recipient, IntPtr NotiFilter, int NotiFlags);
        /// <summary>
        /// Removes a device connection changed notification event
        /// </summary>
        /// <param name="NotiHandle"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr NotiHandle);

        /// <summary>
        /// Object struct for sharing the type of event for the device connected
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct DevBroadcastDeviceInterface
        {
            // Properties generated from notification event
            internal int Size;
            internal short Name;
            internal int Reserved;
            internal int DeviceType;
            internal Guid ClassGuid;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // State controls for new devices or changes
        public const int DbtDeviceArrival = 0x8000;             // The system detected a new device        
        public const int DbtDevNodesChanged = 0x0007;           // A device has been added to or removed from the system.
        public const int DbtDeviceRemoveComplete = 0x8004;      // A connected device is gone     

        // Values to state the types of interfaces and device change events
        public const int WmDeviceChange = 0x0219;               // A device change event      
        private const int DbtDevTypeDeviceInterface = 5;        // Type of device change event objects. (USB)

        // Notifier constants and helpers. 
        private static IntPtr _notificationWindowHandle;
        private const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;
        private static readonly Guid GuidDevInterfaceUSBDevice = new("A5DCBF10-6530-11D2-901F-00C04FB951ED");

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Registers a window to receive notifications when devices are plugged or unplugged.
        /// </summary>
        /// <param name="WindowHandle">Handle to the window receiving notifications.</param>
        /// <param name="OnlyUSBDevices">True to filter to USB devices only, false to be notified for all devices.</param>
        public static void RegisterDeviceNotification(IntPtr WindowHandle, bool OnlyUSBDevices = false)
        {
            // Build new interface broadcast struct for the event built.
            var BroadcastFilterObject = new DevBroadcastDeviceInterface
            {
                Name = 0,
                Reserved = 0,
                DeviceType = DbtDevTypeDeviceInterface,
                ClassGuid = GuidDevInterfaceUSBDevice,
            };

            // Log this event and print the content of our noti struct to the log
            DeviceEventLogger?.WriteLog("REGISTERED A NEW EVENT FOR A DEVICE STATE CHANGE!", LogType.WarnLog);
            DeviceEventLogger?.WriteLog($"EVENT STRUCTURE CONTENTS:\n{JsonConvert.SerializeObject(BroadcastFilterObject, Formatting.Indented)}", LogType.TraceLog);

            // Setup the size values for our broadcast buffer
            BroadcastFilterObject.Size = Marshal.SizeOf(BroadcastFilterObject);
            IntPtr BroadcastBuffer = Marshal.AllocHGlobal(BroadcastFilterObject.Size);
            Marshal.StructureToPtr(BroadcastFilterObject, BroadcastBuffer, true);
            DeviceEventLogger?.WriteLog("MARSHALLED OUT A STRUCT FOR OUR NOTIFICATION BUFFER OK!");    

            // Build a new handle for our notification objects
            _notificationWindowHandle = RegisterDeviceNotification(
                WindowHandle, 
                BroadcastBuffer, 
                OnlyUSBDevices ? 0 : DEVICE_NOTIFY_ALL_INTERFACE_CLASSES
            );
            DeviceEventLogger?.WriteLog("HANDLE FOR EVENT TRIGGER HAS BEEN CONFIGURED OK!", LogType.InfoLog);
        }

        /// <summary>
        /// Unregisters the window for device notifications
        /// </summary>
        public static void UnregisterDeviceNotification()
        {
            // Unregister the event currently being tracked here
            DeviceEventLogger?.WriteLog("REMOVING NOTI EVENT TRIGGER FOR DEVICE MONITOR...", LogType.WarnLog);
            UnregisterDeviceNotification(_notificationWindowHandle);
            DeviceEventLogger?.WriteLog("REMOVED EVENT TRIGGER CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Method that receives window messages.
        /// Used for device state triggers
        /// </summary>
        public static IntPtr WindowEventHandlerHook(IntPtr WindowPointer, int MessageType, IntPtr WrapperPointer, IntPtr SendingPointer, ref bool WasHandled)
        {
            // Check our event types
            if (MessageType != DbtDevNodesChanged) {
                DeviceEventLogger?.WriteLog("MESSAGE TRIGGER EVENT TYPE WAS NOT OF DEVICE CHANGE! RETURNING", LogType.TraceLog);
                WasHandled = false;
                return IntPtr.Zero;
            }

            // Switch our type event. Check if there's a new device added or removed.
            WasHandled = true;
            switch ((int)WrapperPointer)
            {
                // For Devices added 
                case DbtDeviceArrival:
                    // TODO: INVOKE CALLBACK FOR NEW ARRIVAL DEVICE!
                    DeviceEventLogger?.WriteLog("INJECTOR PROCESSING NEW DEVICE ARRIVAL NOW...", LogType.WarnLog);
                    break;

                // For Devices removed
                case DbtDeviceRemoveComplete:
                    // TODO: INVOKE CALLBACK FOR NEW REMOVED DEVICE!
                    DeviceEventLogger.WriteLog("INJECTOR PROCESSING NEW DEVICE REMOVAL NOW...", LogType.WarnLog);
                    break;

                // Default case for not processed
                default:
                    DeviceEventLogger?.WriteLog("NOT PROCESSING EVENT TRIGGER SINCE IT WAS NOT ONE OF OUR TRACED TYPES!", LogType.WarnLog);
                    WasHandled = false;
                    break;
            }

            // Return an empty pointer here.
            DeviceEventLogger?.WriteLog("RETURNING OUT FOR BASE HANDLER POINTER NOW!", LogType.InfoLog);
            return IntPtr.Zero;
        }
    }
}
