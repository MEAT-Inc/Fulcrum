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
            ViewModelLogger.WriteLog("HOOKED NEW EVENT INSTANCE INTO OUR LISTENER FOR DEVICE CHANGED EVENTS OK!", LogType.InfoLog);
            this.SelectedDevice = InjectorConstants.FulcrumInstalledHardwareViewModel.SelectedDevice;

            // Setup voltage monitoring watchdog event here.
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

                // Check to see if the device is usable or not.
                ViewModelLogger.WriteLog("STARTING VOLTAGE MONITORING ROUTINE NOW...", LogType.InfoLog);
                ViewModelLogger.WriteLog("ONCE A VOLTAGE OVER 11.0 IS FOUND, WE WILL TRY TO READ THE VIN OF THE CONNECTED VEHICLE", LogType.InfoLog);

                // Check if we want to use voltage monitoring or not.
                if (!FulcrumSettingsShare.InjectorGeneralSettings.GetSettingValue("Enable Voltage Monitoring", true))
                    ViewModelLogger.WriteLog("NOT USING VOLTAGE MONITORING ROUTINES SINCE THE USER HAS SET THEM TO OFF!", LogType.WarnLog);

                // Start monitoring. Throw if this fails.
                if (this.StartVehicleMonitoring()) {
                    ViewModelLogger.WriteLog("STARTED MONITORING ROUTINE OK!", LogType.InfoLog);
                    ViewModelLogger.WriteLog("WHEN A VOLTAGE OVER 11.0 IS FOUND, A VIN REQUEST WILL BE MADE!", LogType.InfoLog);
                    return;
                }

                // Log failures for starting routine here
                ViewModelLogger.WriteLog("FAILED TO START OUR MONITORING ROUTINES!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("THIS IS LIKELY DUE TO A DEVICE IN USE OR SOMETHING CONSUMING OUR PT INTERFACE!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("IF THE DEVICE IS NOT IN USE AND THIS IS HAPPENING, IT'S LIKELY A BAD DEVICE", LogType.ErrorLog);
            };
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Consumes our active device and begins a voltage reading routine.
        /// </summary>
        /// <returns>True if consumed, false if not.</returns>
        public bool StartVehicleMonitoring()
        {
            // Check if the refresh source is null or not. If it's not, we stop the current instance object.
            if (this.InstanceSession != null && this.RefreshSource != null)
            {
                ViewModelLogger.WriteLog($"STOPPING REFRESH SESSION TASK FOR DEVICE {SelectedDevice} NOW...", LogType.WarnLog);
                this.RefreshSource.Cancel();
                this.InstanceSession.PTClose();
                this.InstanceSession = null;
                ViewModelLogger.WriteLog("STOPPED REFRESHING AND KILLED OUR INSTANCE OK!", LogType.InfoLog);

                // Reset the voltage value to nothing.
                ViewModelLogger.WriteLog("FORCING VOLTAGE BACK TO 0.00 AND RESETTING INFO STRINGS", LogType.WarnLog);
                this.VehicleVIN = null; this.VehicleInfoString = null; this.DeviceVoltage = 0.00;
            }

            // Check to see if our device is usable or not.
            if (this.SelectedDevice.Contains("in use")) {
                ViewModelLogger.WriteLog("NOT RUNNING ROUTINE FOR VIN PULLING SINCE DEVICE IS IN USE!", LogType.ErrorLog);
                return false;
            }

            // Now build a new instance for refreshing.
            this.InstanceSession = new Sharp2534Session(JVersion.V0404, this._selectedDLL, this._selectedDevice);
            ViewModelLogger.WriteLog($"BUILT NEW SESSION INSTANCE FOR DEVICE NAME {this.SelectedDevice} OK!", LogType.InfoLog);

            // Begin refreshing here.
            this.RefreshSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                // Log starting and refresh voltage at an interval of 500ms while the task is valid.
                int RefreshTimer = 500;
                ViewModelLogger.WriteLog("STARTING VOLTAGE REFRESH ROUTINE NOW...", LogType.InfoLog);
                while (!this.RefreshSource.IsCancellationRequested)
                {
                    // Pull in our next voltage value here.
                    var NextVoltage = this.RefreshDeviceVoltage();

                    // Check for the voltage value pulled. Then check for on Lost or on gained
                    // If current voltage is less than 11, and new value is greater than 11 then run this
                    if (this.DeviceVoltage < 11 && NextVoltage >= 11)
                    {
                        // Log information, pull our vin number, then restart this process using the OnLost value.
                        this.DeviceVoltage = NextVoltage;
                        ViewModelLogger.WriteLog("PULLED NEW VOLTAGE VALUE AND DETECTED INPUT FROM 12V OBD!", LogType.InfoLog);

                        // Make sure we want to use our Auto ID routines
                        if (!FulcrumSettingsShare.InjectorGeneralSettings.GetSettingValue("Enable Auto ID Routines", true))
                            ViewModelLogger.WriteLog("NOT USING VEHICLE AUTO ID ROUTINES SINCE THE USER HAS SET THEM TO OFF!", LogType.WarnLog);

                        // Pull our Vin number of out the vehicle now.
                        if (this.RequestVehicleVin(out var VinFound, out ProtocolId ProtocolUsed))
                        {
                            // Log information, store these values.
                            // Tick the refresh timer so we don't constantly spam the log once we found a VIN
                            RefreshTimer = 1500;
                            this.VehicleVIN = VinFound;
                            this.VehicleInfoString = "Not Yet Coded";
                            ViewModelLogger.WriteLog("PULLED NEW VIN NUMBER VALUE OK!", LogType.InfoLog);
                            ViewModelLogger.WriteLog($"VIN PULLED: {VinFound}", LogType.InfoLog);
                            ViewModelLogger.WriteLog($"PROTOCOL USED TO PULL VIN: {ProtocolUsed}", LogType.InfoLog);

                            // Store class values, cancel task, and restart it for on lost.
                            ViewModelLogger.WriteLog("STARTING NEW TASK TO WAIT FOR VOLTAGE BEING LOST NOW...", LogType.WarnLog);
                            continue;
                        }

                        // Log failures and move on. This only happens when a VIN is not found.
                        ViewModelLogger.WriteLog("FAILED TO FIND A NEW VIN NUMBER FOR OUR VEHICLE!", LogType.ErrorLog);
                        this.VehicleVIN = "VIN REQUEST ERROR!";
                        this.VehicleInfoString = "N/A";
                    }

                    // Check for voltage lost instead of connected.
                    if (NextVoltage < 11 && this.DeviceVoltage >= 11)
                    {
                        // Log information, clear out class values, and move on.
                        ViewModelLogger.WriteLog("LOST OBD 12V INPUT! CLEARING OUT STORED VALUES NOW...", LogType.InfoLog);

                        // Clear class values here.
                        RefreshTimer = 250;
                        this.VehicleVIN = null;
                        this.VehicleInfoString = null;
                        this.DeviceVoltage = NextVoltage;
                        ViewModelLogger.WriteLog("CLEARED OUT LAST KNOWN VALUES FOR LOCATED VEHICLE VIN OK!", LogType.InfoLog);
                    }

                    // Wait 1500ms if VIN found, or 250ms if VIN not found.
                    // This way, if someone kicks the cable loose, it won't fail out right away.
                    Thread.Sleep(RefreshTimer);
                };
            }, this.RefreshSource.Token);

            // Log started, return true.
            ViewModelLogger.WriteLog("LOGGING VOLTAGE TO OUR LOG FILES AND PREPARING TO READ TO VIEW MODEL", LogType.InfoLog);
            return true;
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Updates our device voltage value based on our currently selected device information
        /// </summary>
        /// <returns>Voltage of the device connected and selected</returns>
        private double RefreshDeviceVoltage() 
        {
            // TODO: FIND OUT IF WE CAN ISSUE VOLTAGE COMMANDS WITHOUT OPENING A CHANNEL!
            // If this is possible, our reading routine will clear out quite nicely.

            // Try and open the device to pull our voltage reading here
            uint ChannelIdToUse; int ChannelIndex = 0;
            if (this.InstanceSession.DeviceChannels.All(ChannelObj => ChannelObj == null)) 
                this.InstanceSession.PTConnect(ChannelIndex, ProtocolId.ISO15765, 0x00, 50000, out ChannelIdToUse);
            else 
            {
                // Store channel object values based on current instance.
                var ChannelObject = this.InstanceSession.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelObject == null) throw new NullReferenceException("ERROR! FAILED TO FIND A CHANNEL THAT WAS NOT NULL FOR OUR DEVICE INSTANCE!");

                // Once the channel passes our null check, store values
                ChannelIndex = ChannelObject.ChannelIndex;
                ChannelIdToUse = ChannelObject.ChannelId;
            }

            // Now with our new channel ID, we open an instance and pull the channel voltage.
            this.InstanceSession.PTReadVoltage(out var DoubleVoltage, (int)ChannelIdToUse, true); this.DeviceVoltage = DoubleVoltage;
            if (Debugger.IsAttached) ViewModelLogger.WriteLog($"[{this.InstanceSession.DeviceName}] ::: VOLTAGE: {this.DeviceVoltage}", LogType.TraceLog);

            // TODO: FIND OUT IF THIS DISCONNECT ROUTINE IS REALLY NEEDED
            // Disconnect channel, and return the new voltage value as a double (Example: 11.1)
            this.InstanceSession.PTDisconnect(ChannelIndex);
            return DoubleVoltage;
        }
        /// <summary>
        /// Pulls a VIN From a vehicle connected to our car
        /// </summary>
        /// <param name="VinString"></param>
        /// <param name="ProtocolUsed"></param>
        /// <returns></returns>
        private bool RequestVehicleVin(out string VinString, out ProtocolId ProtocolUsed)
        {
            // Get a list of all supported protocols and then pull in all the types of auto ID routines we can use
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
                    return true;
                }
            }

            // If we got here, fail out.
            VinString = null; ProtocolUsed = default;
            ViewModelLogger.WriteLog($"FAILED TO FIND A VIN NUMBER AFTER SCANNING {UsableTypes.Length} DIFFERENT TYPE PROTOCOLS!", LogType.ErrorLog);
            return false;
        }
    }
}
