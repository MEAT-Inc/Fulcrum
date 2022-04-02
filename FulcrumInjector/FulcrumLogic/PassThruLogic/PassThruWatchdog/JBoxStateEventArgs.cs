using System;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruWatchdog
{
    /// <summary>
    /// Builds a new instance of our event watchdog handler for events on our device manager
    /// This class contains two methods. One for device registered and one for removed.
    /// They are only executed when a J2534 interface is connected to our machine.
    /// We only will consume a MAX of 2 cardaq interfaces. If two are connected, then open one and force it to stay allocated by this app
    /// If connected, make a SharpSession and pull Voltage values out and read a VIN. Then once done, setup a background refresh for voltage
    /// For disconnected, set that the device is gone. Use events to fire off a JBoxLost Event that will modify our UI and reset the search routines
    /// </summary>
    public class JBoxStateEventArgs : EventArgs
    {
        // Device and DLL information about a built event
        public string DllName { get; set; }
        public string DeviceName { get; set; }

        // Time event was fired off and the state of the device.
        public bool IsConnected { get; set; }
        public DateTime TimeStateTriggered { get; set; }
    }
}
