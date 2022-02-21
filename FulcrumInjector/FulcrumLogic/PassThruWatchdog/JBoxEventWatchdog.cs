using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruImport;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace FulcrumInjector.FulcrumLogic.PassThruWatchdog
{
    /// <summary>
    /// This class starts up a background refreshing routine that checks our Device Manager and returns out a J2534 interface
    /// if one is currently on our system. This will contain a list of currently connected devices and will be modified on the connect/disconnect
    /// events setup
    /// </summary>
    public class JBoxEventWatchdog
    {
        // Logger object.
        private static SubServiceLogger JBoxSniffLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("JBoxDeviceSniffLogger")) ?? new SubServiceLogger("JBoxDeviceSniffLogger");

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Task objects and events
        private static CancellationToken _refreshToken;
        private static CancellationTokenSource _refreshTokenSource;
        public static event EventHandler<JBoxStateEventArgs> JBoxStateChanged;

        // DLL and Device import helper objects
        private static PassThruImportDLLs _installedDllHelper;
        public static PassThruImportDLLs InstalledDllHelper => _installedDllHelper ??= new PassThruImportDLLs();

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls in a list of currently installed DLL instances on our machine
        /// This will locate both V0404 and V0500 instances
        /// </summary>
        /// <returns>List of all our installed DLLs on this machine</returns>
        private static J2534Dll[] RefreshInstalledDllInstances()
        {
            // Build DLL Location helper using SharpSession
            JBoxSniffLogger.WriteLog("PULLING IN NEW LIST OF CURRENT DLL INSTANCES FOR ALL J2534 VERSIONS NOW...", LogType.InfoLog);

            // Combine all key values here.
            var AllDllKeys = new List<string>();
            AllDllKeys.AddRange(InstalledDllHelper.DllKeyValues_0404);
            AllDllKeys.AddRange(InstalledDllHelper.DllKeyValues_0500);
            JBoxSniffLogger.WriteLog($"PULLED IN A TOTAL OF {AllDllKeys.Count} DLL KEY ENTRIES FROM OUR REGISTRY!", LogType.InfoLog);

            // Print them all out, and return their names.
            foreach (var DllKey in AllDllKeys) JBoxSniffLogger.WriteLog($"--> {DllKey}");
            return InstalledDllHelper.LocatedJ2534DLLs.ToArray();

        }
        /// <summary>
        /// Pulls in a list of currently installed DLL instances on our machine
        /// <param name="VersionToLocate">Version of the DLLS to pull in</param>
        /// </summary>
        /// <returns>List of all our installed DLLs on this machine for the DLL Version given</returns>
        private static J2534Dll[] RefreshInstalledDllInstances(JVersion VersionToLocate)
        {
            // Build DLL Location helper using SharpSession
            if (VersionToLocate == JVersion.ALL_VERSIONS) return RefreshInstalledDllInstances();
            JBoxSniffLogger.WriteLog($"PULLING IN NEW LIST OF CURRENT DLL INSTANCES FOR J2534 VERSION {VersionToLocate} NOW...", LogType.InfoLog);

            // Log out our V0404 Keys and V0500 Keys
            JBoxSniffLogger.WriteLog($"THE FOLLOWING VERSION {(VersionToLocate == JVersion.V0404 ? "V0404" : "V0500")} DLLs HAVE BEEN LOCATED:", LogType.InfoLog);
            foreach (var DllKey in (VersionToLocate == JVersion.V0404 ? InstalledDllHelper.DllKeyValues_0404 : InstalledDllHelper.DllKeyValues_0500))
                JBoxSniffLogger.WriteLog($"--> (Version {(VersionToLocate == JVersion.V0404 ? "V0404)" : "V0500)")} -- {DllKey}");

            // Now build a list of all the names of the keys
            var DLLsFound = VersionToLocate == JVersion.V0404 ?
                InstalledDllHelper.LocatedJ2534DLLs.Where(DllObj => DllObj.DllVersion == JVersion.V0404) :
                InstalledDllHelper.LocatedJ2534DLLs.Where(DllObj => DllObj.DllVersion == JVersion.V0500);
            JBoxSniffLogger.WriteLog($"FOUND A TOTAL OF {DLLsFound.Count()} DLL INSTANCES FOR VERSION {VersionToLocate} DLLS!", LogType.InfoLog);

            // Return our built list of DLL instances.
            return InstalledDllHelper.LocatedJ2534DLLs.Where(DLLObj => DLLObj.DllVersion == VersionToLocate).ToArray();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Begins a background refreshing operation for the given params
        /// </summary>
        /// <param name="Version">Filter by this version only</param>
        /// <param name="DeviceRefreshInterval">Refresh delay after each loop</param>
        /// <param name="DLLRefreshInterval">Refresh time for DLL Entries on the system</param>
        public static void StartBackgroundRefresh(JVersion Version = JVersion.ALL_VERSIONS, int DeviceRefreshInterval = 500, int DLLRefreshInterval = 0)
        {
            // Check to see if this is running or not.
            if (_refreshTokenSource != null) {
                JBoxSniffLogger.WriteLog("CAN NOT INVOKE A NEW REFRESH METHOD WHILE OLD ONES ARE RUNNING! STOP THIS TASK FIRST!", LogType.ErrorLog);
                return;
            }

            // Build a list of our parameters for search refresh objects 
            DateTime TimeStarted = DateTime.Now;
            _refreshTokenSource = new CancellationTokenSource(); _refreshToken = _refreshTokenSource.Token;
            JBoxSniffLogger.WriteLog("BUILDING UP NEW BACKGROUND REFRESH INSTANCE FOR OUR JBOX FINDER NOW...", LogType.WarnLog);

            // Now Fill in our DLL List and begin checking for devices.
            JBoxSniffLogger.WriteLog("--> REFRESHING DEVICE AND DLL LIST CONTENTS NOW...", LogType.InfoLog);
            var DllInstanceList = RefreshInstalledDllInstances(Version);
            var DeviceInstanceList = DllInstanceList.Select(DllObj =>
                    new Tuple<J2534Dll, PassThruStructs.SDevice[]>(DllObj, DllObj.FindConnectedSDevices().ToArray()))
                .ToArray();

            // Fire events for ALL DEVICES ON first loop event
            foreach (var DeviceSet in DeviceInstanceList) 
                foreach (var DevObj in DeviceSet.Item2) 
                    _fireDeviceStateEvent(DeviceSet, DevObj.DeviceName, true);
            JBoxSniffLogger.WriteLog("--> INVOKED BASE EVENT OBJECTS FOR ALL CURRENT DEVICES OK!", LogType.InfoLog);

            // Now kickoff our background task instance
            Task.Run(() =>
            {
                // Now begin looping value refreshing
                var TimeSinceLastDLLRefresh = 0;
                while (!_refreshToken.IsCancellationRequested)
                {
                    // Pull device instances, check if any exist. If they do, fire off new events
                    var ElapsedTime = DateTime.Now - TimeStarted;
                    TimeSinceLastDLLRefresh += ElapsedTime.Milliseconds;

                    // Check DLL Refresh requirement
                    if (DLLRefreshInterval != 0 && TimeSinceLastDLLRefresh >= DLLRefreshInterval)
                    {
                        // Log info about DLL Refresh and run it
                        JBoxSniffLogger.WriteLog($"--> REFRESHING DLLS AT INTERVAL OF {DLLRefreshInterval}! TRIGGERING UPDATE NOW...", LogType.TraceLog);
                        DllInstanceList = RefreshInstalledDllInstances(Version);

                        // Reset timer for the DLL Updating routine
                        TimeSinceLastDLLRefresh = 0;
                    }

                    // Update the devices list now
                    var UpdatedDeviceList = DllInstanceList.Select(DllObj =>
                            new Tuple<J2534Dll, PassThruStructs.SDevice[]>(DllObj, DllObj.FindConnectedSDevices().ToArray()))
                        .ToArray();

                    // Pull out JUST our device objects.
                    if (UpdatedDeviceList.Length == DeviceInstanceList.Length) { Thread.Sleep(DeviceRefreshInterval); continue; }
                    var NewDevicesOnly = UpdatedDeviceList.SelectMany(DevSet => DevSet.Item2).ToArray();
                    var OldDevicesOnly = DeviceInstanceList.SelectMany(DevSet => DevSet.Item2).ToArray();

                    // Find Changes in device instances
                    var AddedDevices = NewDevicesOnly.Where(DevObj => !OldDevicesOnly.Contains(DevObj)).ToArray();
                    var RemovedDevices = OldDevicesOnly.Where(DevObj => !NewDevicesOnly.Contains(DevObj)).ToArray();
                    if (AddedDevices.Length == 0 && RemovedDevices.Length == 0) { Thread.Sleep(DeviceRefreshInterval); continue; }
                    if (AddedDevices.Length != 0) JBoxSniffLogger.WriteLog($"--> TOTAL OF {AddedDevices.Length} NEW DEVICES ADDED", LogType.TraceLog);
                    if (RemovedDevices.Length != 0) JBoxSniffLogger.WriteLog($"--> TOTAL OF {RemovedDevices.Length} NEW DEVICES REMOVED", LogType.TraceLog);

                    // Fire event objects for changed state values for added instances
                    foreach (var DeviceObj in AddedDevices)
                    {
                        // Build object to be used as our sender and then build event args objects
                        var SendingObject = UpdatedDeviceList.FirstOrDefault(DevSet => DevSet.Item2.Contains(DeviceObj));
                        _fireDeviceStateEvent(SendingObject, DeviceObj.DeviceName, true);
                    }

                    // Fire event objects for changed state values for removed instances
                    foreach (var DeviceObj in AddedDevices)
                    {
                        // Build object to be used as our sender and then build event args objects
                        var SendingObject = DeviceInstanceList.FirstOrDefault(DevSet => DevSet.Item2.Contains(DeviceObj));
                        _fireDeviceStateEvent(SendingObject, DeviceObj.DeviceName, false);
                    }

                    // Log fired events, wait, and continue on.
                    JBoxSniffLogger.WriteLog($"--> FIRED OFF A TOTAL OF {RemovedDevices.Length + AddedDevices.Length} EVENT OBJECTS FOR DEVICES OK!", LogType.TraceLog);
                    JBoxSniffLogger.WriteLog($"--> WAITING FOR {DeviceRefreshInterval}ms BEFORE MOVING TO NEXT REFRESH ITERATION...", LogType.TraceLog);
                    Thread.Sleep(DeviceRefreshInterval);
                }
            }, _refreshToken);
        }
        /// <summary>
        /// Stops our refresh operation for the new 
        /// </summary>
        public static void StopBackgroundRefresh()
        {           
            // Check to see if this is running or not.
            if (_refreshTokenSource == null) {
                JBoxSniffLogger.WriteLog("CAN NOT STOP A REFRESH METHOD THAT HAS NEVER BEEN STARTED!", LogType.ErrorLog);
                return;
            }

            // Stop the method here.
            JBoxSniffLogger.WriteLog("STOPPING REFRESH OPERATIONS NOW...", LogType.InfoLog);
            _refreshTokenSource.Cancel(); _refreshTokenSource = null; _refreshToken = default;
            JBoxSniffLogger.WriteLog("STOPPED OPERATIONS WITHOUT ISSUES! READY TO STARTUP ANOTHER ONE!", LogType.InfoLog);
        }


        /// <summary>
        /// Fires off a new device instance event for a provided set of information
        /// </summary>
        /// <param name="SenderObject">DLL And All SDevices for this DLL instance</param>
        /// <param name="DeviceName">Name of device firing our event</param>
        private static void _fireDeviceStateEvent(Tuple<J2534Dll, PassThruStructs.SDevice[]> SenderObject, string DeviceName, bool IsConnected)
        {
            // Setup new Event and fire it off.
            EventHandler<JBoxStateEventArgs> StateHandler = JBoxStateChanged;
            StateHandler?.Invoke(SenderObject, new JBoxStateEventArgs()
            {
                DeviceName = DeviceName,
                DllName = SenderObject.Item1.Name,
                IsConnected = IsConnected,
                TimeStateTriggered = DateTime.Now
            });
        }
    }
}
