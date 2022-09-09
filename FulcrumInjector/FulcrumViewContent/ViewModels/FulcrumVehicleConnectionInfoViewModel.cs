using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent.Models.EventModels;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using Newtonsoft.Json;
using SharpAutoId.SharpAutoIdHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrapper;
using SharpWrapper.PassThruTypes;
using SharpWrapper.SupportingLogic;

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
        private CancellationTokenSource RefreshSource;

        // Private control values
        private JVersion _versionType;         // J2534 Version in use
        private string _selectedDLL;           // Name of the currently selected DLL
        private string _selectedDevice;        // Name of the currently connected and consumed Device
        private double _deviceVoltage;         // Last known voltage value. If no device found, this returns 0.00
        private string _vehicleVIN;            // VIN Of the current vehicle connected
        private string _vehicleInfo;           // YMM String of the current vehicle
        private bool _autoIdRunning;           // Sets if AUTO Id routines are running at this time or not.
        private bool _canManualId;             // Sets if we can start a new manual ID value
        private bool _isMonitoring;            // Sets if we're monitoring input voltage on the vehicle or not.

        // Public values for our view to bind onto 
        public string VehicleVin
        {
            get => string.IsNullOrWhiteSpace(_vehicleVIN) ? "No VIN Number" : _vehicleVIN;
            set => PropertyUpdated(value);
        }

        public string VehicleInfo
        {
            get => string.IsNullOrWhiteSpace(_vehicleInfo) ? "Not Yet Supported" : _vehicleInfo;
            set => PropertyUpdated(value);
        }

        // Device Information
        public JVersion VersionType { get => _versionType; set => PropertyUpdated(value); }
        public string SelectedDLL { get => _selectedDLL; set => PropertyUpdated(value); }
        public string SelectedDevice 
        {
            get => _selectedDevice ?? "No Device Selected";
            set
            {
                // Check for the same value passed in again or nothing passed at all.
                if (value == this.SelectedDevice) {
                    PropertyUpdated(value);
                    return;
                }

                // Check for a null device name or no device name provided.
                PropertyUpdated(value);
                this.VehicleVin = "No VIN Number";
                if (string.IsNullOrWhiteSpace(value) || value == "No Device Selected")
                {
                    // Set view content values
                    this.DeviceVoltage = 0.00;
                    this.VehicleVin = "No VIN Number";

                    // Update private values, dispose the instance.
                    if (this.IsMonitoring) this.StopVehicleMonitoring(); 
                    if (FulcrumConstants.SharpSessionAlpha != null) Sharp2534Session.CloseSession(FulcrumConstants.SharpSessionAlpha);
                    ViewModelLogger.WriteLog("STOPPED SESSION INSTANCE OK AND CLEARED OUT DEVICE NAME!", LogType.InfoLog);
                    return;
                }

                // Check if we want to use voltage monitoring or not.
                bool UseMonitoring = FulcrumSettingsShare.InjectorGeneralSettings.GetSettingValue("Enable Vehicle Monitoring", true);
                if (!UseMonitoring) {
                    ViewModelLogger.WriteLog("NOT USING VOLTAGE MONITORING ROUTINES SINCE THE USER HAS SET THEM TO OFF!", LogType.WarnLog);
                    ViewModelLogger.WriteLog("TRYING TO PULL A VOLTAGE READING ONCE!", LogType.InfoLog);
                    return;
                }

                try
                {
                    // Close device out and dispose of the session object instance
                    if (FulcrumConstants.SharpSessionAlpha != null) 
                    {
                        // Close Session
                        Sharp2534Session.CloseSession(FulcrumConstants.SharpSessionAlpha);
                        ViewModelLogger.WriteLog("CLOSED EXISTING SHARP SESSION OK!", LogType.WarnLog);

                        // Null out the session
                        FulcrumConstants.SharpSessionAlpha = null;
                        ViewModelLogger.WriteLog("SET CURRENT INSTANCE SESSION TO NULL VALUE!", LogType.WarnLog);
                    }

                    // Build a new session object here now.
                    FulcrumConstants.SharpSessionAlpha = Sharp2534Session.OpenSession(this._versionType, this._selectedDLL, this._selectedDevice);
                    ViewModelLogger.WriteLog("CONFIGURED VIEW MODEL CONTENT OBJECTS FOR BACKGROUND REFRESHING OK!", LogType.InfoLog);

                    // Start monitoring. Throw if this fails.
                    if (!this.StartVehicleMonitoring()) throw new InvalidOperationException("FAILED TO START OUR MONITORING ROUTINE!");
                    ViewModelLogger.WriteLog("STARTED MONITORING ROUTINE OK!", LogType.InfoLog);
                    ViewModelLogger.WriteLog("WHEN A VOLTAGE OVER 11.0 IS FOUND, A VIN REQUEST WILL BE MADE!", LogType.InfoLog);
                }
                catch (Exception SetupSessionEx)
                {
                    // Log failures for starting routine here
                    ViewModelLogger.WriteLog("FAILED TO START OUR MONITORING ROUTINES!", LogType.ErrorLog);
                    ViewModelLogger.WriteLog("THIS IS LIKELY DUE TO A DEVICE IN USE OR SOMETHING CONSUMING OUR PT INTERFACE!", LogType.ErrorLog);
                    ViewModelLogger.WriteLog("IF THE DEVICE IS NOT IN USE AND THIS IS HAPPENING, IT'S LIKELY A BAD DEVICE", LogType.ErrorLog);
                    ViewModelLogger.WriteLog("EXCEPTION THROWN DURING SETUP ROUTINE!", SetupSessionEx);
                }
            }
        }

        // Auto ID control values
        public bool AutoIdRunning
        {
            get => _autoIdRunning;
            set
            {
                // Set the new value and set Can ID to false if value is now true
                PropertyUpdated(value);
                this.CanManualId = value == false &&
                                   this.DeviceVoltage >= 11 &&
                                   this.SelectedDevice != "No Device Selected!";
            }
        }
        public double DeviceVoltage
        {
            get => _deviceVoltage;
            set
            {
                PropertyUpdated(value);
                this.CanManualId = value >= 11 &&
                                   !this.AutoIdRunning &&
                                   this.SelectedDevice != "No Device Selected";
            }
        }
        public bool CanManualId { get => _canManualId; set => PropertyUpdated(value); }
        public bool IsMonitoring { get => _isMonitoring; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumVehicleConnectionInfoViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP HARDWARE INSTANCE VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // BUG: THIS BLOCK OF CODE IS TRIGGERING UPDATES TOO FAST! Removing this since I've figured out what was going wrong.
            // Build an instance session if our DLL and Device are not null yet. 
            // var DLLSelected = InjectorConstants.FulcrumInstalledHardwareViewModel?.SelectedDLL;
            // if (DLLSelected == null) {
            //     ViewModelLogger.WriteLog("NO DLL ENTRY WAS FOUND TO BE USED YET! NOT CONFIGURING A NEW SESSION...", LogType.InfoLog);
            //     return;
            // }

            // Store DLL Values and device instance
            // this._selectedDLL = DLLSelected.Name;
            // this._versionType = DLLSelected.DllVersion;
            // ViewModelLogger.WriteLog($"ATTEMPTING TO BUILDING NEW SESSION FOR DEVICE NAMED {SelectedDevice} FROM CONNECTION VM...", LogType.WarnLog);
            // ViewModelLogger.WriteLog($"WITH DLL {DLLSelected.Name} (VERSION: {DLLSelected.DllVersion}", LogType.WarnLog);

            // Build our session instance here.
            // this.SelectedDevice = InjectorConstants.FulcrumInstalledHardwareViewModel?.SelectedDevice;
            // ViewModelLogger.WriteLog($"STORED NEW DEVICE VALUE OF {this.SelectedDevice}", LogType.InfoLog);
        }

        // -------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Updates our device voltage value based on our currently selected device information
        /// </summary>
        /// <returns>Voltage of the device connected and selected</returns>
        private double ReadDeviceVoltage()
        {
            try
            {
                // If there is no device, don't read
                if (this.SelectedDevice == "No Device Selected") return 0.00;

                // Now with our new channel ID, we open an instance and pull the channel voltage.
                FulcrumConstants.SharpSessionAlpha.PTOpen();
                if (!FulcrumConstants.SharpSessionAlpha.JDeviceInstance.IsOpen) return 0.00;

                // Only read our voltage if our device was opened ok
                FulcrumConstants.SharpSessionAlpha.PTReadVoltage(out var DoubleVoltage, true);
                return DoubleVoltage;
            }
            catch
            {
                // Log failed to read value, return 0.00v
                // ViewModelLogger.WriteLog("FAILED TO READ NEW VOLTAGE VALUE!", LogType.ErrorLog);
                return 0.00;
            }
        }
        /// <summary>
        /// Pulls a VIN From a vehicle connected to our car
        /// </summary>
        /// <param name="VinString"></param>
        /// <param name="ProtocolUsed"></param>
        /// <returns></returns>
        private bool ReadVehicleVin(out string VinString, out ProtocolId ProtocolUsed)
        {
            // Get a list of all supported protocols and then pull in all the types of auto ID routines we can use
            this.AutoIdRunning = true;
            foreach (var ProcObject in SharpAutoIdConfig.SupportedProtocols)
            {
                try
                {
                    // Build a new AutoID Session here
                    var AutoIdInstance = FulcrumConstants.SharpSessionAlpha.SpawnAutoIdHelper(ProcObject);
                    ViewModelLogger.WriteLog($"BUILT NEW INSTANCE OF SESSION FOR TYPE {ProcObject} OK!", LogType.InfoLog);
                    ViewModelLogger.WriteLog("PULLING VIN AND OPENING CHANNEL FOR TYPE INSTANCE NOW...", LogType.InfoLog);

                    // Open channel, read VIN, and close out
                    AutoIdInstance.ConnectChannel(out _);
                    if (!AutoIdInstance.RetrieveVinNumber(out VinString))
                    {
                        ViewModelLogger.WriteLog($"NO VIN NUMBER PULLED FOR PROTOCOL VALUE {ProcObject}!", LogType.WarnLog);
                        continue;
                    }

                    // Check our Vin Value
                    ViewModelLogger.WriteLog("PULLED A VIN NUMBER VALUE OK!");
                    ViewModelLogger.WriteLog($"PULLED VIN NUMBER: {VinString}", LogType.WarnLog);

                    // Store values and exit out.
                    ProtocolUsed = ProcObject;
                    AutoIdInstance.CloseAutoIdSession();
                    this.AutoIdRunning = false;
                    return true;
                }
                catch (Exception VinEx)
                {
                    // Log Failure thrown during routine and move on
                    ViewModelLogger.WriteLog("EXCEPTION THROWN DURING VIN ROUTINE! MOVING ONTO THE NEXT PROTOCOL!", LogType.ErrorLog);
                    ViewModelLogger.WriteLog("EXCEPTION IS BEING LOGGED BELOW", VinEx);
                }
            }

            // If we got here, no vin was found on the network
            VinString = null; ProtocolUsed = default; this.AutoIdRunning = false;
            ViewModelLogger.WriteLog($"FAILED TO FIND A VIN NUMBER AFTER SCANNING ALL POSSIBLE PROTOCOLS!", LogType.ErrorLog);
            return false;
        }

        // -------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls out the VIN number of the current vehicle and stores the voltage of it.
        /// </summary>
        /// <returns>True if pulled ok. False if not.</returns>
        internal bool ReadVoltageAndVin()
        {
            // Now build our session instance and pull voltage first.
            bool NeedsMonitoringReset = this.IsMonitoring;
            if (NeedsMonitoringReset) this.StopVehicleMonitoring();
            ViewModelLogger.WriteLog($"BUILT NEW SESSION INSTANCE FOR DEVICE NAME {this.SelectedDevice} OK!", LogType.InfoLog);

            // Store voltage value, log information. If voltage is less than 11.0, then exit.
            this.DeviceVoltage = this.ReadDeviceVoltage();
            if (this.DeviceVoltage < 11.0) {
                ViewModelLogger.WriteLog("ERROR! VOLTAGE VALUE IS LESS THAN THE ACCEPTABLE 11.0V CUTOFF! NOT AUTO IDENTIFYING THIS CAR!", LogType.ErrorLog);
                return false;
            }

            // Return passed and store our new values
            bool VinResult = this.ReadVehicleVin(out string NewVin, out var ProcPulled);
            if (!VinResult) { ViewModelLogger.WriteLog("FAILED TO PULL A VIN VALUE!", LogType.ErrorLog); }
            else
            {
                // Log information, store new values.
                this.VehicleVin = NewVin; this.VehicleVin = NewVin;
                ViewModelLogger.WriteLog($"VOLTAGE VALUE PULLED OK! READ IN NEW VALUE {this.DeviceVoltage:F2}!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"PULLED VIN NUMBER: {this.VehicleVin} WITH PROTOCOL ID: {ProcPulled}!", LogType.InfoLog);
            }

            // Kill our session here
            if (NeedsMonitoringReset) this.StartVehicleMonitoring();
            ViewModelLogger.WriteLog("CLOSING REQUEST SESSION MANUALLY NOW...", LogType.WarnLog);
            ViewModelLogger.WriteLog("SESSION CLOSED AND NULLIFIED OK!", LogType.InfoLog);
            return VinResult;
        }

        /// <summary>
        /// Consumes our active device and begins a voltage reading routine.
        /// </summary>
        /// <returns>True if consumed, false if not.</returns>
        internal bool StartVehicleMonitoring()
        {
            // Try and kill old sessions then begin refresh routine
            this.RefreshSource = new CancellationTokenSource();
            int RefreshTimer = 500; IsMonitoring = true; bool VinReadRun = false;
            ViewModelLogger.WriteLog("STARTING VOLTAGE REFRESH ROUTINE NOW...", LogType.InfoLog);

            // Close our device here if it's open currently.
            if (FulcrumConstants.SharpSessionAlpha.JDeviceInstance.IsOpen)
                FulcrumConstants.SharpSessionAlpha.PTClose();

            // Find out VIN Number values here.
            FulcrumConstants.SharpSessionAlpha.PTOpen();
            bool CheckVinNumber = FulcrumSettingsShare.InjectorGeneralSettings.GetSettingValue("Enable Auto ID Routines", true);
            if (!CheckVinNumber) ViewModelLogger.WriteLog("NOT USING VEHICLE AUTO ID ROUTINES SINCE THE USER HAS SET THEM TO OFF!", LogType.WarnLog);

            // Run as a task to avoid locking up UI
            Task.Run(() =>
            {
                // Do this as long as we need to keep reading based on the token
                while (!this.RefreshSource.IsCancellationRequested)
                {
                    // Pull in our next voltage value here. Check for voltage gained or removed
                    Thread.Sleep(RefreshTimer);
                    this.DeviceVoltage = this.ReadDeviceVoltage();
                    RefreshTimer = this.DeviceVoltage >= 11 ? 5000 : 2500;

                    // Check our voltage value. Perform actions based on value pulled
                    if (this.DeviceVoltage >= 11)
                    {
                        // Check our Vin Read status and if we need to Auto ID at all
                        if (this.VehicleVin == "No Vehicle Voltage!") this.VehicleVin = string.Empty;
                        if (VinReadRun || !CheckVinNumber) continue;

                        try
                        {
                            // Pull our Vin number of out the vehicle now.
                            if (this.ReadVehicleVin(out var VinFound, out ProtocolId ProtocolUsed))
                            {
                                // Log information, store these values.
                                this.VehicleVin = VinFound; this.VehicleVin = VinFound; VinReadRun = true;
                                ViewModelLogger.WriteLog("PULLED NEW VIN NUMBER VALUE OK!", LogType.InfoLog);
                                ViewModelLogger.WriteLog($"VIN PULLED: {VinFound}", LogType.InfoLog);
                                ViewModelLogger.WriteLog($"PROTOCOL USED TO PULL VIN: {ProtocolUsed}", LogType.InfoLog);

                                // Store class values, cancel task, and restart it for on lost.
                                ViewModelLogger.WriteLog("STARTING NEW TASK TO WAIT FOR VOLTAGE BEING LOST NOW...", LogType.WarnLog);
                                continue;
                            }

                            // Log failed to pull but not thrown exception
                            this.VehicleVin = "Failed to Read VIN!"; VinReadRun = true;
                            ViewModelLogger.WriteLog("ROUTINE RAN CORRECTLY BUT NO VIN NUMBER WAS FOUND! THIS MEANS THERE'S SOMETHING ELSE WRONG...", LogType.ErrorLog);
                            ViewModelLogger.WriteLog("VIN REQUEST WILL ONLY RUN IF MANUALLY CALLED NOW!", LogType.WarnLog);
                            continue;
                        }
                        catch (Exception VinEx)
                        {
                            // Log failures generated by VIN Request routine. Fall out of here and try to run again
                            this.VehicleVin = "Failed to Read VIN!"; VinReadRun = true;
                            ViewModelLogger.WriteLog("FAILED TO PULL IN A NEW VIN NUMBER DUE TO AN UNHANDLED EXCEPTION!", LogType.ErrorLog);
                            ViewModelLogger.WriteLog("LOGGING EXCEPTION THROWN DOWN BELOW", VinEx);
                            continue;
                        }
                    }

                    // Check for voltage lost instead of connected.
                    if (this.VehicleVin.Contains("Voltage")) continue;
                    if (this.SelectedDevice != "No Device Selected") this.VehicleVin = "No Vehicle Voltage!";
                    ViewModelLogger.WriteLog("LOST OBD 12V INPUT! CLEARING OUT STORED VALUES NOW...", LogType.InfoLog);
                    ViewModelLogger.WriteLog("CLEARED OUT LAST KNOWN VALUES FOR LOCATED VEHICLE VIN OK!", LogType.InfoLog);
                };
            }, this.RefreshSource.Token);

            // Log started, return true.
            ViewModelLogger.WriteLog("LOGGING VOLTAGE TO OUR LOG FILES AND PREPARING TO READ TO VIEW MODEL", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Stops a refresh session. 
        /// </summary>
        /// <returns>True if stopped ok. False if not.</returns>
        internal void StopVehicleMonitoring()
        {
            // Reset all values here.
            // if (this?.SelectedDevice != "No Device Selected") 
            //     ViewModelLogger.WriteLog($"STOPPING REFRESH SESSION TASK FOR DEVICE {this.SelectedDevice} NOW...", LogType.WarnLog);

            // Dispose our instance object here
            this.VehicleVin = null;
            this.IsMonitoring = false;
            this.DeviceVoltage = 0.00;
            this.RefreshSource?.Cancel();
            FulcrumConstants.SharpSessionAlpha?.PTClose();

            // Log information output
            ViewModelLogger.WriteLog("FORCING VOLTAGE BACK TO 0.00 AND RESETTING INFO STRINGS", LogType.WarnLog);
            ViewModelLogger.WriteLog("STOPPED REFRESHING AND KILLED OUR INSTANCE OK!", LogType.InfoLog);
        }
    }
}
