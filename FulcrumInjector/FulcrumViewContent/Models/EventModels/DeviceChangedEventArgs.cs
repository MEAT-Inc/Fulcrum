﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumViewContent.Models.EventModels
{
    /// <summary>
    /// Class model for changed device names.
    /// Includes the time of change, the name, and the DLL
    /// </summary>
    public class DeviceChangedEventArgs : EventArgs
    {
        // Time Device Picket
        public readonly DateTime TimeOfSelection;

        // Name of our Device and DLL
        public string DeviceName { get; set; }
        public string DeviceDLL { get;  set; }

        /// <summary>
        /// Builds a new event for when an object is triggered to register device changed states
        /// </summary>
        /// <param name="Device">Device name</param>
        /// <param name="DLL">DLL name</param>
        public DeviceChangedEventArgs(string Device, string DLL)
        {
            // Store class properties here
            this.TimeOfSelection = DateTime.Now;
            this.DeviceName = Device;
            this.DeviceDLL = DLL;
        }
    }
}