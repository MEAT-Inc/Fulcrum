﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport;
using SharpAutoId;
using SharpLogging;
using SharpWrapper;
using SharpWrapper.PassThruTypes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels
{
    /// <summary>
    /// View model object for our connected vehicle information helper
    /// </summary>
    public class FulcrumVehicleConnectionInfoViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Task control for stopping refresh operations for our background voltage reading.
        private CancellationTokenSource RefreshSource;

        // Private backing fields for our public properties
        private JVersion _versionType;         // J2534 Version in use
        private string _selectedDLL;           // Name of the currently selected DLL
        private string _selectedDevice;        // Name of the currently connected and consumed Device
        private double _deviceVoltage;         // Last known voltage value. If no device found, this returns 0.00
        private string _vehicleVIN;            // VIN Of the current vehicle connected
        private string _vehicleInfo;           // YMM String of the current vehicle
        private bool _autoIdRunning;           // Sets if AUTO Id routines are running at this time or not.
        private bool _canManualId;             // Sets if we can start a new manual ID value
        private bool _isMonitoring;            // Sets if we're monitoring input voltage on the vehicle or not.

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
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

        // Device and DLL Information
        public JVersion VersionType { get => _versionType; set => PropertyUpdated(value); }
        public string SelectedDLL { get => _selectedDLL; set => PropertyUpdated(value); }
        public string SelectedDevice
        {
            get => _selectedDevice ?? "No Device Selected";
            set
            {
                // Check for the same value passed in again or nothing passed at all.
                if (value == this.SelectedDevice)
                {
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
                    this.ViewModelLogger.WriteLog("STOPPED SESSION INSTANCE OK AND CLEARED OUT DEVICE NAME!", LogType.InfoLog);
                    return;
                }

                // Check if we want to use voltage monitoring or not.
                bool UseMonitoring = FulcrumConstants.FulcrumSettings.InjectorHardwareSettings.GetSettingValue("Enable Vehicle Monitoring", true);
                if (!UseMonitoring)
                {
                    this.ViewModelLogger.WriteLog("NOT USING VOLTAGE MONITORING ROUTINES SINCE THE USER HAS SET THEM TO OFF!", LogType.WarnLog);
                    this.ViewModelLogger.WriteLog("TRYING TO PULL A VOLTAGE READING ONCE!", LogType.InfoLog);
                    return;
                }

                try
                {
                    // Close device out and dispose of the session object instance
                    if (FulcrumConstants.SharpSessionAlpha != null)
                    {
                        // Close Session
                        Sharp2534Session.CloseSession(FulcrumConstants.SharpSessionAlpha);
                        this.ViewModelLogger.WriteLog("CLOSED EXISTING SHARP SESSION OK!", LogType.WarnLog);

                        // Null out the session
                        FulcrumConstants.SharpSessionAlpha = null;
                        this.ViewModelLogger.WriteLog("SET CURRENT INSTANCE SESSION TO NULL VALUE!", LogType.WarnLog);
                    }

                    // Build a new session object here now.
                    FulcrumConstants.SharpSessionAlpha = Sharp2534Session.OpenSession(this._versionType, this._selectedDLL, this._selectedDevice);
                    this.ViewModelLogger.WriteLog("CONFIGURED VIEW MODEL CONTENT OBJECTS FOR BACKGROUND REFRESHING OK!", LogType.InfoLog);

                    // Start monitoring. Throw if this fails.
                    if (!this.StartVehicleMonitoring()) throw new InvalidOperationException("FAILED TO START OUR MONITORING ROUTINE!");
                    this.ViewModelLogger.WriteLog("STARTED MONITORING ROUTINE OK!", LogType.InfoLog);
                    this.ViewModelLogger.WriteLog("WHEN A VOLTAGE OVER 11.0 IS FOUND, A VIN REQUEST WILL BE MADE!", LogType.InfoLog);
                }
                catch (Exception SetupSessionEx)
                {
                    // Log failures for starting routine here
                    this.ViewModelLogger.WriteLog("FAILED TO START OUR MONITORING ROUTINES!", LogType.ErrorLog);
                    this.ViewModelLogger.WriteLog("THIS IS LIKELY DUE TO A DEVICE IN USE OR SOMETHING CONSUMING OUR PT INTERFACE!", LogType.ErrorLog);
                    this.ViewModelLogger.WriteLog("IF THE DEVICE IS NOT IN USE AND THIS IS HAPPENING, IT'S LIKELY A BAD DEVICE", LogType.ErrorLog);
                    this.ViewModelLogger.WriteException("EXCEPTION THROWN DURING SETUP ROUTINE!", SetupSessionEx);
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

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="VehicleConnectionUserControl">UserControl that holds the content for our vehicle connection view</param>
        public FulcrumVehicleConnectionInfoViewModel(UserControl VehicleConnectionUserControl) : base(VehicleConnectionUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP HARDWARE INSTANCE VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);

            // BUG: THIS BLOCK OF CODE IS TRIGGERING UPDATES TOO FAST! Removing this since I've figured out what was going wrong.
            // Build an instance session if our DLL and Device are not null yet. 
            // var DLLSelected = InjectorConstants.FulcrumInstalledHardwareViewModel?.SelectedDLL;
            // if (DLLSelected == null) {
            //     this.ViewModelLogger.WriteLog("NO DLL ENTRY WAS FOUND TO BE USED YET! NOT CONFIGURING A NEW SESSION...", LogType.InfoLog);
            //     return;
            // }

            // Store DLL Values and device instance
            // this._selectedDLL = DLLSelected.Name;
            // this._versionType = DLLSelected.DllVersion;
            // this.ViewModelLogger.WriteLog($"ATTEMPTING TO BUILDING NEW SESSION FOR DEVICE NAMED {SelectedDevice} FROM CONNECTION VM...", LogType.WarnLog);
            // this.ViewModelLogger.WriteLog($"WITH DLL {DLLSelected.Name} (VERSION: {DLLSelected.DllVersion}", LogType.WarnLog);

            // Build our session instance here.
            // this.SelectedDevice = InjectorConstants.FulcrumInstalledHardwareViewModel?.SelectedDevice;
            // this.ViewModelLogger.WriteLog($"STORED NEW DEVICE VALUE OF {this.SelectedDevice}", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls out the VIN number of the current vehicle and stores the voltage of it.
        /// </summary>
        /// <returns>True if pulled ok. False if not.</returns>
        public bool ReadVoltageAndVin()
        {
            // Now build our session instance and pull voltage first.
            bool NeedsMonitoringReset = this.IsMonitoring;
            if (NeedsMonitoringReset) this.StopVehicleMonitoring();
            this.ViewModelLogger.WriteLog($"BUILT NEW SESSION INSTANCE FOR DEVICE NAME {this.SelectedDevice} OK!", LogType.InfoLog);

            // Store voltage value, log information. If voltage is less than 11.0, then exit.
            this.DeviceVoltage = this._readDeviceVoltage();
            if (this.DeviceVoltage < 11.0) {
                this.ViewModelLogger.WriteLog("ERROR! VOLTAGE VALUE IS LESS THAN THE ACCEPTABLE 11.0V CUTOFF! NOT AUTO IDENTIFYING THIS CAR!", LogType.ErrorLog);
                return false;
            }

            // Return passed and store our new values
            bool VinResult = this._readVehicleVin(out string NewVin, out var ProcPulled);
            if (!VinResult) { this.ViewModelLogger.WriteLog("FAILED TO PULL A VIN VALUE!", LogType.ErrorLog); }
            else
            {
                // Log information, store new values.
                this.VehicleVin = NewVin; this.VehicleVin = NewVin;
                this.ViewModelLogger.WriteLog($"VOLTAGE VALUE PULLED OK! READ IN NEW VALUE {this.DeviceVoltage:F2}!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog($"PULLED VIN NUMBER: {this.VehicleVin} WITH PROTOCOL ID: {ProcPulled}!", LogType.InfoLog);
            }

            // Kill our session here
            if (NeedsMonitoringReset) this.StartVehicleMonitoring();
            this.ViewModelLogger.WriteLog("CLOSING REQUEST SESSION MANUALLY NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("SESSION CLOSED AND NULLIFIED OK!", LogType.InfoLog);
            return VinResult;
        }
        /// <summary>
        /// Consumes our active device and begins a voltage reading routine.
        /// </summary>
        /// <returns>True if consumed, false if not.</returns>
        public bool StartVehicleMonitoring()
        {
            // Try and kill old sessions then begin refresh routine
            this.RefreshSource = new CancellationTokenSource();
            int RefreshTimer = 500; IsMonitoring = true; bool VinReadRun = false;
            this.ViewModelLogger.WriteLog("STARTING VOLTAGE REFRESH ROUTINE NOW...", LogType.InfoLog);

            // Close our device here if it's open currently.
            if (FulcrumConstants.SharpSessionAlpha.JDeviceInstance.IsOpen)
                FulcrumConstants.SharpSessionAlpha.PTClose();

            // Find out VIN Number values here.
            FulcrumConstants.SharpSessionAlpha.PTOpen();
            bool CheckVinNumber = FulcrumConstants.FulcrumSettings.InjectorHardwareSettings.GetSettingValue("Enable Auto ID Routines", true);
            if (!CheckVinNumber) this.ViewModelLogger.WriteLog("NOT USING VEHICLE AUTO ID ROUTINES SINCE THE USER HAS SET THEM TO OFF!", LogType.WarnLog);

            // Run as a task to avoid locking up UI
            Task.Run(() =>
            {
                // Do this as long as we need to keep reading based on the token
                while (!this.RefreshSource.IsCancellationRequested)
                {
                    // Pull in our next voltage value here. Check for voltage gained or removed
                    Thread.Sleep(RefreshTimer);
                    this.DeviceVoltage = this._readDeviceVoltage();
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
                            if (this._readVehicleVin(out var VinFound, out ProtocolId ProtocolUsed))
                            {
                                // Log information, store these values.
                                this.VehicleVin = VinFound; this.VehicleVin = VinFound; VinReadRun = true;
                                this.ViewModelLogger.WriteLog("PULLED NEW VIN NUMBER VALUE OK!", LogType.InfoLog);
                                this.ViewModelLogger.WriteLog($"VIN PULLED: {VinFound}", LogType.InfoLog);
                                this.ViewModelLogger.WriteLog($"PROTOCOL USED TO PULL VIN: {ProtocolUsed}", LogType.InfoLog);

                                // Store class values, cancel task, and restart it for on lost.
                                this.ViewModelLogger.WriteLog("STARTING NEW TASK TO WAIT FOR VOLTAGE BEING LOST NOW...", LogType.WarnLog);
                                continue;
                            }

                            // Log failed to pull but not thrown exception
                            this.VehicleVin = "Failed to Read VIN!"; VinReadRun = true;
                            this.ViewModelLogger.WriteLog("ROUTINE RAN CORRECTLY BUT NO VIN NUMBER WAS FOUND! THIS MEANS THERE'S SOMETHING ELSE WRONG...", LogType.ErrorLog);
                            this.ViewModelLogger.WriteLog("VIN REQUEST WILL ONLY RUN IF MANUALLY CALLED NOW!", LogType.WarnLog);
                            continue;
                        }
                        catch (Exception VinEx)
                        {
                            // Log failures generated by VIN Request routine. Fall out of here and try to run again
                            this.VehicleVin = "Failed to Read VIN!"; VinReadRun = true;
                            this.ViewModelLogger.WriteLog("FAILED TO PULL IN A NEW VIN NUMBER DUE TO AN UNHANDLED EXCEPTION!", LogType.ErrorLog);
                            this.ViewModelLogger.WriteException("LOGGING EXCEPTION THROWN DOWN BELOW", VinEx);
                            continue;
                        }
                    }

                    // Check for voltage lost instead of connected.
                    if (this.VehicleVin.Contains("Voltage")) continue;
                    if (this.SelectedDevice != "No Device Selected") this.VehicleVin = "No Vehicle Voltage!";
                    this.ViewModelLogger.WriteLog("LOST OBD 12V INPUT! CLEARING OUT STORED VALUES NOW...", LogType.InfoLog);
                    this.ViewModelLogger.WriteLog("CLEARED OUT LAST KNOWN VALUES FOR LOCATED VEHICLE VIN OK!", LogType.InfoLog);
                };
            }, this.RefreshSource.Token);

            // Log started, return true.
            this.ViewModelLogger.WriteLog("LOGGING VOLTAGE TO OUR LOG FILES AND PREPARING TO READ TO VIEW MODEL", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Stops a refresh session. 
        /// </summary>
        /// <returns>True if stopped ok. False if not.</returns>
        public void StopVehicleMonitoring()
        {
            // Reset all values here.
            // if (this?.SelectedDevice != "No Device Selected") 
            //     this.ViewModelLogger.WriteLog($"STOPPING REFRESH SESSION TASK FOR DEVICE {this.SelectedDevice} NOW...", LogType.WarnLog);

            // Dispose our instance object here
            this.VehicleVin = null;
            this.IsMonitoring = false;
            this.DeviceVoltage = 0.00;
            this.RefreshSource?.Cancel();
            FulcrumConstants.SharpSessionAlpha?.PTClose();

            // Log information output
            this.ViewModelLogger.WriteLog("FORCING VOLTAGE BACK TO 0.00 AND RESETTING INFO STRINGS", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("STOPPED REFRESHING AND KILLED OUR INSTANCE OK!", LogType.InfoLog);
        }

        /// <summary>
        /// Updates our device voltage value based on our currently selected device information
        /// </summary>
        /// <returns>Voltage of the device connected and selected</returns>
        private double _readDeviceVoltage()
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
                // this.ViewModelLogger.WriteLog("FAILED TO READ NEW VOLTAGE VALUE!", LogType.ErrorLog);
                return 0.00;
            }
        }
        /// <summary>
        /// Pulls a VIN From a vehicle connected to our car
        /// </summary>
        /// <param name="VinString"></param>
        /// <param name="ProtocolUsed"></param>
        /// <returns></returns>
        private bool _readVehicleVin(out string VinString, out ProtocolId ProtocolUsed)
        {
            // Get a list of all supported protocols and then pull in all the types of auto ID routines we can use
            this.AutoIdRunning = true;
            foreach (var ProcObject in AutoIdConfiguration.SupportedProtocols)
            {
                try
                {
                    // Build a new AutoID Session here
                    var AutoIdInstance = AutoIdHelper.BuildAutoIdHelper(FulcrumConstants.SharpSessionAlpha, ProcObject);
                    this.ViewModelLogger.WriteLog($"BUILT NEW INSTANCE OF SESSION FOR TYPE {ProcObject} OK!", LogType.InfoLog);
                    this.ViewModelLogger.WriteLog("PULLING VIN AND OPENING CHANNEL FOR TYPE INSTANCE NOW...", LogType.InfoLog);

                    // Open channel, read VIN, and close out
                    AutoIdInstance.ConnectAutoIdChannel(out _);
                    if (!AutoIdInstance.RetrieveVehicleVIN(out VinString))
                    {
                        this.ViewModelLogger.WriteLog($"NO VIN NUMBER PULLED FOR PROTOCOL VALUE {ProcObject}!", LogType.WarnLog);
                        continue;
                    }

                    // Check our Vin Value
                    this.ViewModelLogger.WriteLog("PULLED A VIN NUMBER VALUE OK!");
                    this.ViewModelLogger.WriteLog($"PULLED VIN NUMBER: {VinString}", LogType.WarnLog);

                    // Store values and exit out.
                    ProtocolUsed = ProcObject;
                    AutoIdInstance.CloseAutoIdSession();
                    this.AutoIdRunning = false;
                    return true;
                }
                catch (Exception VinEx)
                {
                    // Log Failure thrown during routine and move on
                    this.ViewModelLogger.WriteLog("EXCEPTION THROWN DURING VIN ROUTINE! MOVING ONTO THE NEXT PROTOCOL!", LogType.ErrorLog);
                    this.ViewModelLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW", VinEx);
                }
            }

            // If we got here, no vin was found on the network
            VinString = null; ProtocolUsed = default; this.AutoIdRunning = false;
            this.ViewModelLogger.WriteLog($"FAILED TO FIND A VIN NUMBER AFTER SCANNING ALL POSSIBLE PROTOCOLS!", LogType.ErrorLog);
            return false;
        }
    }
}
