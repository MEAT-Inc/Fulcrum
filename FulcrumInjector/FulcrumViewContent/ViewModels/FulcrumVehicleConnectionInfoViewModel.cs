using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruWatchdog;
using FulcrumInjector.FulcrumViewContent.Models.EventModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruImport;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

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

        // Task control for stopping refresh operations for our background voltage reading.
        private Sharp2534Session InstanceSession;
        private CancellationTokenSource RefreshSource;

        // Private control values
        private string _selectedDLL;           // Name of the currently selected DLL
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
                this._selectedDLL = Args.DeviceDLL;
                this.SelectedDevice = Args.DeviceName;
                ViewModelLogger.WriteLog("STORED NEW DEVICE NAME AND DLL NAME OK!", LogType.InfoLog);
            };
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Consumes our active device and begins a voltage reading routine.
        /// </summary>
        /// <returns>True if consumed, false if not.</returns>
        public bool StartVoltageMonitoring()
        {
            // Check if the refresh source is null or not. If it's not, we stop the current instance object.
            if (this.InstanceSession != null && this.RefreshSource != null) {
                ViewModelLogger.WriteLog($"STOPPING REFRESH SESSION TASK FOR DEVICE {SelectedDevice} NOW...", LogType.WarnLog);
                this.RefreshSource.Cancel();
                this.InstanceSession.PTClose();
                this.InstanceSession = null;
                ViewModelLogger.WriteLog("STOPPED REFRESHING AND KILLED OUR INSTANCE OK!", LogType.InfoLog);

                // Reset the voltage value to nothing.
                ViewModelLogger.WriteLog("FORCING VOLTAGE BACK TO 0.00 AND RESETTING INFO STRINGS", LogType.WarnLog);
                this.VehicleVIN = null; this.VehicleInfoString = null; this.DeviceVoltage = 0.00;
            }

            // Now build a new instance for refreshing.
            this.InstanceSession = new Sharp2534Session(JVersion.V0404, this._selectedDLL, this._selectedDevice);
            ViewModelLogger.WriteLog($"BUILT NEW SESSION INSTANCE FOR DEVICE NAME {this.SelectedDevice} OK!", LogType.InfoLog);

            // Begin refreshing here.
            this.RefreshSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                // Log starting and refresh voltage at an interval of 500ms while the task is valid.
                ViewModelLogger.WriteLog("STARTING VOLTAGE REFRESH ROUTINE NOW...", LogType.InfoLog);
                while (!this.RefreshSource.IsCancellationRequested)
                {
                    // Issue a PTClose, PTOpen, PTConnect, and read our voltage. Wait 500ms, run again
                    this.DeviceVoltage = this.RefreshDeviceVoltage();
                    Thread.Sleep(500);
                };
            }, this.RefreshSource.Token);

            // TODO: PULL IN VIN NUMBER FROM OUR CAR HERE!
            //       This block will call our AutoID routine to pull in the VIN of the currently connected car.
            //       Once pulled, we ping an API For decoding our VIN Numbers and then return the YMM info string of it.
            //       From here, we can then configure this instance to try and only show OE apps for the given device and vehicle.

            // Log started, return true.
            ViewModelLogger.WriteLog("LOGGING VOLTAGE TO OUR LOG FILES AND PREPARING TO READ TO VIEW MODEL", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Updates our device voltage value based on our currently selected device information
        /// </summary>
        /// <returns>Voltage of the device connected and selected</returns>
        private double RefreshDeviceVoltage() 
        {
            // Try and open the device to pull our voltage reading here
            uint ChannelIdToUse;
            if (this.InstanceSession.DeviceChannels.All(ChannelObj => ChannelObj == null)) 
                this.InstanceSession.PTConnect(0, ProtocolId.ISO15765, 0x00, 50000, out ChannelIdToUse);
            else ChannelIdToUse = this.InstanceSession.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj != null)!.ChannelId;

            // Now with our new channel ID, we open an instance and pull the channel voltage.
            this.InstanceSession.PTReadVoltage(out var DoubleVoltage, (int)ChannelIdToUse, true); this.DeviceVoltage = DoubleVoltage;
            ViewModelLogger.WriteLog($"[{this.InstanceSession.DeviceName}] ::: VOLTAGE: {this.DeviceVoltage}", LogType.TraceLog);
            return DoubleVoltage;
        }
    }
}
