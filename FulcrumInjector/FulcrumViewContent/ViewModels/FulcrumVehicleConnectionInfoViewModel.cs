using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using FulcrumInjector.FulcrumLogic.PassThruWatchdog;
using FulcrumInjector.FulcrumViewContent.Models.EventModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruImport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels
{
    /// <summary>
    /// View model object for our connected vehicle information helper
    /// </summary>
    public class FulcrumVehicleConnectionInfoViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("VehicleConnectionViewModelLogger")) ?? new SubServiceLogger("VehicleConnectionViewModelLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        // Private control values
        private string _selectedDevice;        // Name of the currently connected and consumed Device
        private double _deviceVoltage;         // Last known voltage value. If no device found, this returns 0.00
        private string _vehicleVIN;            // VIN Of the current vehicle connected
        private string _vehicleInfoString;     // YMM String of the current vehicle

        // Public values for our view to bind onto 
        public string SelectedDevice { get => _selectedDevice; set => PropertyUpdated(value); }
        public double DeviceVoltage { get => _deviceVoltage; set => PropertyUpdated(value); }
        public string VehicleVIN
        {
            get => _vehicleVIN ?? "No VIN Number"; 
            set => PropertyUpdated(value);
        }            
        public string VehicleInfoString
        {
            get => _vehicleInfoString ?? "No VIN Number To Decode"; 
            set => PropertyUpdated(value);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumVehicleConnectionInfoViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP HARDWARE INSTANCE VIEW BOUND VALUES NOW...", LogType.WarnLog);
            
            // Attach listeners to our device changed events.
            ViewModelLogger.WriteLog("HOOKED NEW EVENT INSTANCE INTO OUR LISTENER FOR DEVICE CHANGED EVENTS OK!",  LogType.InfoLog);
            this.SelectedDevice = InjectorConstants.FulcrumInstalledHardwareViewModel.SelectedDevice;
            InjectorConstants.FulcrumInstalledHardwareViewModel.DeviceOrDllChanged += (Sender, Args) =>
            {
                // Build new listener object here.
                ViewModelLogger.WriteLog("NEW DEVICE CHANGED EVENT PROCESSED!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"--> DEVICE NAME FOUND: {Args.DeviceName}");
                ViewModelLogger.WriteLog($"--> DLL NAME FOUND:    {Args.DeviceDLL}");

                // Store device and DLL info.
                this.SelectedDevice = Args.DeviceName;
                ViewModelLogger.WriteLog("STORED NEW DEVICE NAME OK!", LogType.InfoLog);
            };
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Updates our device voltage value based on our currently selected device information
        /// </summary>
        /// <returns>Voltage of the device connected and selected</returns>
        public double RefreshDeviceVoltage() 
        {
            // Try and open the device to pull our voltage reading here
            return 0.00;
        }
    }
}
