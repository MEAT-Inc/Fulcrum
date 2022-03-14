using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumLogic.PassThruAutoID;
using FulcrumInjector.FulcrumLogic.PassThruWatchdog;
using FulcrumInjector.FulcrumViewContent.Models.EventModels;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using Newtonsoft.Json;
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
            get => "Not Yet Supported";
            // get => string.IsNullOrWhiteSpace(_vehicleInfo) ? "No VIN Number" : _vehicleInfo; 
            // set => PropertyUpdated(value);
        }
        public string SelectedDevice
        {
            get => _selectedDevice ?? "No Device Selected";
            set
            {
                // Update private value
                PropertyUpdated(value);
                this.CanManualId = value != null && value != "No Device Selected" && AutoIdRunning == false;
                if (this.InstanceSession != null && value == this._selectedDevice) return;

                // Setup a new Session if the new value is not the same as our current value
                this.InstanceSession?.PTClose();
                this.InstanceSession = new Sharp2534Session(this._versionType, this._selectedDLL, value);
                ViewModelLogger.WriteLog("CONFIGURED VIEW MODEL CONTENT OBJECTS FOR BACKGROUND REFRESHING OK!", LogType.InfoLog);
            }
        }
        public double DeviceVoltage { get => _deviceVoltage; set => PropertyUpdated(value); }

        // Auto ID control values
        public bool AutoIdRunning
        {
            get => _autoIdRunning;
            set
            {
                // Set the new value and set Can ID to false if value is now true
                PropertyUpdated(value);
                this.CanManualId = !value;
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

            // Attach listeners to our device changed events.
            InjectorConstants.FulcrumInstalledHardwareViewModel.DeviceSelectionChanged += ProcessDeviceChangedEvent;
            ViewModelLogger.WriteLog("HOOKED NEW EVENT INSTANCE INTO OUR LISTENER FOR DEVICE CHANGED EVENTS OK!", LogType.InfoLog);
        }


        /// <summary>
        /// Event action for processing a new device state changed event
        /// </summary>
        /// <param name="Sender">Sending object</param>
        /// <param name="Args">Args for changed device</param>
        private void ProcessDeviceChangedEvent(object Sender, DeviceChangedEventArgs Args)
        {
            // Build new listener object here.
            if (Args.DeviceName == null || Args.DeviceName.Contains("No Device")) {
                ViewModelLogger.WriteLog("NO DEVICE NAME PROVIDED! RETURNING OUT FROM THIS ROUTINE", LogType.WarnLog);
                this.InstanceSession?.PTClose();
                this.InstanceSession = null;
                return;
            }

            // Check for matching device name
            if (Args.DeviceName == this.SelectedDevice) {
                ViewModelLogger.WriteLog("NOT CREATING DUPLICATE SESSION FOR SAME DEVICE NAME!", LogType.WarnLog);
                return;
            }

            // Log information about newly built device
            ViewModelLogger.WriteLog("NEW DEVICE CHANGED EVENT PROCESSED!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"--> API VERSION:       {Args.VersionType.ToDescriptionString()}");
            ViewModelLogger.WriteLog($"--> DLL NAME FOUND:    {Args.DeviceDLL}");
            ViewModelLogger.WriteLog($"--> DEVICE NAME FOUND: {Args.DeviceName}");

            // Store device and DLL info then prepare for refresh
            this._selectedDLL = Args.DeviceDLL;
            this._versionType = Args.VersionType;
            this.SelectedDevice = Args.DeviceName;
            ViewModelLogger.WriteLog("STORED NEW DEVICE NAME AND DLL NAME OK!", LogType.InfoLog);

            try
            {
                // Check if we want to use voltage monitoring or not.
                if (!FulcrumSettingsShare.InjectorGeneralSettings.GetSettingValue("Enable Vehicle Monitoring", true)) {
                    ViewModelLogger.WriteLog("NOT USING VOLTAGE MONITORING ROUTINES SINCE THE USER HAS SET THEM TO OFF!", LogType.WarnLog);
                    ViewModelLogger.WriteLog("TRYING TO PULL A VOLTAGE READING ONCE!", LogType.InfoLog);
                    return;
                }

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
        /// <summary>
        /// Consumes our active device and begins a voltage reading routine.
        /// </summary>
        /// <returns>True if consumed, false if not.</returns>
        private bool StartVehicleMonitoring()
        {
            // Try and kill old sessions then begin refresh routine
            this.RefreshSource = new CancellationTokenSource();
            
            // Make sure our JBox is open here
            int RefreshTimer = 500; IsMonitoring = true;
            ViewModelLogger.WriteLog("STARTING VOLTAGE REFRESH ROUTINE NOW...", LogType.InfoLog);

            // Run as a task to avoid locking up UI
            Task.Run(() =>
            {
                // Do this as long as we need to keep reading based on the token
                while (!this.RefreshSource.IsCancellationRequested)
                {
                    // Pull in our next voltage value here. Check for voltage gained or removed
                    var NextVoltage = this.ReadDeviceVoltage();
                    if (this.DeviceVoltage < 11 && NextVoltage >= 11)
                    {
                        // Log information, pull our vin number, then restart this process using the OnLost value.
                        this.DeviceVoltage = NextVoltage; RefreshTimer = 1500;
                        if (!FulcrumSettingsShare.InjectorGeneralSettings.GetSettingValue("Enable Auto ID Routines", true)) {
                            ViewModelLogger.WriteLog("NOT USING VEHICLE AUTO ID ROUTINES SINCE THE USER HAS SET THEM TO OFF!", LogType.WarnLog);
                            Thread.Sleep(RefreshTimer);
                            continue;
                        }

                        // Pull our Vin number of out the vehicle now.
                        if (this.ReadVehicleVin(out var VinFound, out ProtocolId ProtocolUsed))
                        {
                            // Log information, store these values.
                            // Tick the refresh timer so we don't constantly spam the log once we found a VIN
                            this.VehicleVin = VinFound;
                            ViewModelLogger.WriteLog("PULLED NEW VIN NUMBER VALUE OK!", LogType.InfoLog);
                            ViewModelLogger.WriteLog($"VIN PULLED: {VinFound}", LogType.InfoLog);
                            ViewModelLogger.WriteLog($"PROTOCOL USED TO PULL VIN: {ProtocolUsed}", LogType.InfoLog);

                            // Store class values, cancel task, and restart it for on lost.
                            ViewModelLogger.WriteLog("STARTING NEW TASK TO WAIT FOR VOLTAGE BEING LOST NOW...", LogType.WarnLog);
                            Thread.Sleep(RefreshTimer);
                            continue;
                        }

                        // Log failures and move on. This only happens when a VIN is not found.
                        ViewModelLogger.WriteLog("FAILED TO FIND A NEW VIN NUMBER FOR OUR VEHICLE!", LogType.ErrorLog);
                        this.VehicleVin = "VIN REQUEST ERROR!";
                    }

                    // Check for voltage lost instead of connected.
                    if (NextVoltage < 11 && this.DeviceVoltage >= 11)
                    {
                        // Log information, clear out class values, and move on.
                        ViewModelLogger.WriteLog("LOST OBD 12V INPUT! CLEARING OUT STORED VALUES NOW...", LogType.InfoLog);

                        // Clear class values here.
                        RefreshTimer = 250;
                        this.VehicleVin = null;
                        this.DeviceVoltage = NextVoltage;
                        ViewModelLogger.WriteLog("CLEARED OUT LAST KNOWN VALUES FOR LOCATED VEHICLE VIN OK!", LogType.InfoLog);

                        // Wait and continue
                        Thread.Sleep(RefreshTimer);
                        continue;
                    }

                    // Wait 1500ms if VIN found, or 250ms if VIN not found.
                    // This way, if someone kicks the cable loose, it won't fail out right away.
                    this.DeviceVoltage = NextVoltage;
                    Thread.Sleep(RefreshTimer);
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
        private void StopVehicleMonitoring()
        {
            // Reset all values here.
            ViewModelLogger.WriteLog($"STOPPING REFRESH SESSION TASK FOR DEVICE {SelectedDevice} NOW...", LogType.WarnLog);
            this.InstanceSession?.PTClose();
            this.RefreshSource?.Cancel();

            // Setup task objects again.
            IsMonitoring = false; this.VehicleVin = null; this.DeviceVoltage = 0.00;
            ViewModelLogger.WriteLog("FORCING VOLTAGE BACK TO 0.00 AND RESETTING INFO STRINGS", LogType.WarnLog);
            ViewModelLogger.WriteLog("STOPPED REFRESHING AND KILLED OUR INSTANCE OK!", LogType.InfoLog);
        }

        // -------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls out the VIN number of the current vehicle and stores the voltage of it.
        /// </summary>
        /// <returns>True if pulled ok. False if not.</returns>
        public bool ReadVoltageAndVin()
        {
            // Now build our session instance and pull voltage first.
            if (this.IsMonitoring) this.StopVehicleMonitoring(); 
            ViewModelLogger.WriteLog($"BUILT NEW SESSION INSTANCE FOR DEVICE NAME {this.SelectedDevice} OK!", LogType.InfoLog);

            // Store voltage value, log information. If voltage is less than 11.0, then exit.
            if (!this.InstanceSession.JDeviceInstance.IsOpen) this.InstanceSession.PTOpen();
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
                this.VehicleVin = NewVin;
                ViewModelLogger.WriteLog($"VOLTAGE VALUE PULLED OK! READ IN NEW VALUE {this.DeviceVoltage:F2}!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"PULLED VIN NUMBER: {this.VehicleVin} WITH PROTOCOL ID: {ProcPulled}!", LogType.InfoLog);
            }

            // Kill our session here
            ViewModelLogger.WriteLog("CLOSING REQUEST SESSION MANUALLY NOW...", LogType.WarnLog); 
            this.InstanceSession.PTClose();

            // Return the result of our VIN Request
            ViewModelLogger.WriteLog("SESSION CLOSED AND NULLIFIED OK!", LogType.InfoLog);
            return VinResult;
        }

        /// <summary>
        /// Updates our device voltage value based on our currently selected device information
        /// </summary>
        /// <returns>Voltage of the device connected and selected</returns>
        private double ReadDeviceVoltage() 
        {
            // Return nothing if the session instance is null
            if (this.InstanceSession == null) {
                ViewModelLogger.WriteLog("NO SESSION INSTANCE WAS FOUND! THIS IS FATAL!", LogType.ErrorLog);
                return 0.00;
            }

            // Try and open the device to pull our voltage reading here
            uint ChannelIdToUse; int ChannelIndex = 0;
            if (!this.InstanceSession.JDeviceInstance.IsOpen) this.InstanceSession.PTOpen();
            if (this.InstanceSession.DeviceChannels.All(ChannelObj => ChannelObj == null)) 
                this.InstanceSession.PTConnect(ChannelIndex, ProtocolId.ISO15765, 0x00, 50000, out ChannelIdToUse);

            // Now with our new channel ID, we open an instance and pull the channel voltage.
            this.InstanceSession.PTReadVoltage(16, out var DoubleVoltage, true); this.DeviceVoltage = DoubleVoltage;
            this.InstanceSession.PTDisconnect(ChannelIndex);
            return DoubleVoltage;
        }
        /// <summary>
        /// Pulls a VIN From a vehicle connected to our car
        /// </summary>
        /// <param name="VinString"></param>
        /// <param name="ProtocolUsed"></param>
        /// <returns></returns>
        private bool ReadVehicleVin(out string VinString, out ProtocolId ProtocolUsed)
        {
            // Make sure our device exists.
            if (this.InstanceSession == null) {
                VinString = "-----------------"; ProtocolUsed = default;
                ViewModelLogger.WriteLog("NO SESSION INSTANCE WAS FOUND! THIS IS FATAL!", LogType.ErrorLog);
                return false;
            }

            // Get a list of all supported protocols and then pull in all the types of auto ID routines we can use
            this.AutoIdRunning = true;
            var SupportedRoutines = ValueLoaders.GetConfigValue<string[]>("FulcrumAutoIdRoutines");
            var UsableTypes = SupportedRoutines.Select(ProtocolTypeString =>
            {
                // Get the type for the Auto ID routine here.
                var AutoIdBaseType = Assembly.GetExecutingAssembly().GetTypes()?.FirstOrDefault(TypeObj => TypeObj.Name.Contains("AutoIdRoutine"));
                if (AutoIdBaseType == null) throw new TypeAccessException("FAILED TO FIND TYPE BASE FOR OUR AUTO ID ROUTINE!");

                // Now build the type for our auto ID instance based on the protocol.
                string AutoIdTypeName = $"{AutoIdBaseType.Namespace}.AutoIdRoutine_{ProtocolTypeString}";
                ViewModelLogger.WriteLog($"TRYING TO BUILD TYPE FOR AUTO ID NAMED {AutoIdTypeName}", LogType.InfoLog);
                try
                {
                    // Get the type, build arguments, and generate object
                    Type AutoIdType = Type.GetType(AutoIdTypeName);
                    ViewModelLogger.WriteLog($"--> TYPE PARSED OK! TYPE FOUND WAS: {AutoIdType.FullName}", LogType.InfoLog);
                    return AutoIdType;
                }
                catch (Exception TypeLookupEx)
                {
                    // Log the failures and return nothing.
                    ViewModelLogger.WriteLog($"FAILED TO FIND TYPE: {ProtocolTypeString}!", LogType.ErrorLog);
                    ViewModelLogger.WriteLog("EXCEPTION THROWN DURING TYPE DETECTION ROUTINE!", TypeLookupEx);
                    return null;
                }
            }).Where(TypeObj => TypeObj != null).ToArray();

            // Now one by one build instances and attempt connections
            foreach (var TypeValue in UsableTypes)
            {
                // Cast the protocol object and built arguments for our instance constructor.
                string ProtocolTypeString = TypeValue.Name.Split('_')[1];
                ProtocolId CastProtocol = (ProtocolId)Enum.Parse(typeof(ProtocolId), ProtocolTypeString);
                object[] InitArgs = { JVersion.V0404, this._selectedDLL, this.SelectedDevice, CastProtocol };
                ViewModelLogger.WriteLog("--> BUILT NEW ARGUMENTS FOR TYPE GENERATION OK!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"--> TYPE ARGUMENTS: {JsonConvert.SerializeObject(InitArgs, Formatting.None)}", LogType.TraceLog);

                // Generate our instance here and try to store our VIN
                AutoIdIRoutine AutoIdInstance = (AutoIdIRoutine)Activator.CreateInstance(TypeValue, InitArgs);
                ViewModelLogger.WriteLog($"BUILT NEW INSTANCE OF SESSION FOR TYPE {TypeValue} OK!", LogType.InfoLog);
                ViewModelLogger.WriteLog("PULLING VIN AND OPENING CHANNEL FOR TYPE INSTANCE NOW...", LogType.InfoLog);

                // Connect our channel, read the vin, and then close it.
                AutoIdInstance.ConnectChannel(out var ChannelOpened);
                AutoIdInstance.RetrieveVinNumber(out VinString);
                ViewModelLogger.WriteLog("VIN REQUEST ROUTINE AND CONNECTION PASSED!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"USED CHANNEL ID: {ChannelOpened}", LogType.TraceLog);
                ViewModelLogger.WriteLog("CLOSING OUR SESSION INSTANCE DOWN NOW...", LogType.TraceLog);
                AutoIdInstance.CloseSession();

                // Check our VIN Value
                ProtocolUsed = CastProtocol;
                if (VinString is not { Length: 17 }) ViewModelLogger.WriteLog("NO VIN NUMBER WAS FOUND! MOVING ONTO NEXT PROTOCOL...", LogType.WarnLog);
                else
                {
                    // Log our new vin number pulled, return out of this method
                    ViewModelLogger.WriteLog($"VIN VALUE LOCATED: {VinString}", LogType.InfoLog);
                    ViewModelLogger.WriteLog("VIN NUMBER WAS PULLED CORRECTLY! STORING IT ONTO OUR CLASS INSTANCE NOW...", LogType.InfoLog);
                    this.AutoIdRunning = false;
                    return true;
                }
            }

            // If we got here, fail out.
            this.AutoIdRunning = false;
            VinString = null; ProtocolUsed = default;
            ViewModelLogger.WriteLog($"FAILED TO FIND A VIN NUMBER AFTER SCANNING {UsableTypes.Length} DIFFERENT TYPE PROTOCOLS!", LogType.ErrorLog);
            return false;
        }
    }
}
