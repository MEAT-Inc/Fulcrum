using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumLogic.PassThruWatchdog;
using FulcrumInjector.FulcrumViewContent.Models.EventModels;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruImport;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// Class used to bind our installed/active hardware objects to the view content for showing current PassThru devices.
    /// </summary>
    public class FulcrumInstalledHardwareViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InstalledHardwareViewModelLogger")) ?? new SubServiceLogger("InstalledHardwareViewModelLogger");

        // Private Control Values
        private J2534Dll _selectedDLL;
        private string _selectedDevice;
        private ObservableCollection<J2534Dll> _installedDLLs;
        private ObservableCollection<string> _installedDevices;

        // Selected DLL object
        public J2534Dll SelectedDLL
        {
            get => _selectedDLL;
            set
            {
                // Update property value. Setup new List of DLLs.
                PropertyUpdated(value);
                InstalledDevices = this.PopulateDevicesForDLL(value);
            }
        }

        // Selected Device Object
        public string SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                // Store class value. Stop background refreshing once we find a device
                PropertyUpdated(value); 
                JBoxEventWatchdog.StopBackgroundRefresh();

                // Fire an event for device changed, and set the property value
                this.OnSelectedDeviceChanged(new DeviceChangedEventArgs(this.SelectedDLL, value));
                ViewModelLogger?.WriteLog("SET NEW SELECTED DLL AND FIRED EVENT FOR LISTENING TARGETS OK!", LogType.InfoLog);
            }
        }

        // Current Installed DLL List object and installed Devices for Said DLL
        public ObservableCollection<J2534Dll> InstalledDLLs { get => _installedDLLs; set => PropertyUpdated(value); }
        public ObservableCollection<string> InstalledDevices { get => _installedDevices; set => PropertyUpdated(value); }

        // -------------------------------------------------------------------------------------------------------------------------

        // Event triggers for device selection changed data input
        public event EventHandler<DeviceChangedEventArgs> DeviceOrDllChanged;
        protected void OnSelectedDeviceChanged(DeviceChangedEventArgs EventArgs) { DeviceOrDllChanged?.Invoke(this, EventArgs); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumInstalledHardwareViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP HARDWARE INSTANCE VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Store event handler object
            JBoxEventWatchdog.JBoxStateChanged += StateChangeEventHandler;
            ViewModelLogger.WriteLog("SETUP NEW EVENT HANDLER FOR DEVICE SELECTION INPUT CHANGED VALUES!", LogType.InfoLog);

            // Pull in our DLL Entries and our device entries now.
            ViewModelLogger.WriteLog("UPDATING AND IMPORTING CURRENT DLL LIST FOR THIS SYSTEM NOW...", LogType.WarnLog);
            this.InstalledDLLs = new ObservableCollection<J2534Dll>(new PassThruImportDLLs().LocatedJ2534DLLs);

            // See if using default PT Device is on or off.
            if (FulcrumSettingsShare.InjectorGeneralSettings.GetSettingValue("Auto Consume CarDAQ-Plus 3", false))
            {
                // Pull in our PT Device now.
                if (this.InstalledDLLs.Any(DLLObj => DLLObj.Name.Contains("CarDAQ-Plus 3"))) {
                    this.SelectedDLL = this.InstalledDLLs.FirstOrDefault(DLLObj => DLLObj.Name.Contains("CarDAQ-Plus 3"));
                    ViewModelLogger.WriteLog("STORED OUR DEFAULT CDP3 DLL AND DEVICE INSTANCE OK!", LogType.InfoLog);
                }
                else ViewModelLogger.WriteLog("ERROR! UNABLE TO FIND A USABLE CARDAQ PLUS 3 INSTANCE!", LogType.ErrorLog);
            }
            else ViewModelLogger.WriteLog("NOT TRYING TO AUTO CONFIGURE A NEW CARDAQ PLUS 3 INSTANCE SINCE THE USER HAS SET IT TO OFF!", LogType.WarnLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("DLL ENTRIES UPDATED OK! STORED THEM TO OUR VIEWMODEL FOR DLL IMPORTING CORRECTLY", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETUP NEW VIEW MODEL FOR HARDWARE INSTANCE VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("CONTENT ON THE VIEW SHOULD REFLECT THE SHARPWRAP HARDWARE LISTING!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Event handler for new JBox State changed conditions
        /// </summary>
        /// <param name="StateChangedHandler">Handler sender for event</param>
        /// <param name="StateChangedArgs">Changed state event object info</param>
        private void StateChangeEventHandler(object StateChangedHandler, JBoxStateEventArgs StateChangedArgs)
        {
            // Check if the device is connected or not first and see if the DLL is in our list of installed DLLs.
            Tuple<J2534Dll, string[]> SenderCast = (Tuple<J2534Dll, string[]>)StateChangedHandler;
            
            // If the current DLL is not matching the DLL of the sending device, return.
            if (!_installedDLLs.Contains(SenderCast.Item1)) _installedDLLs.Add(SenderCast.Item1);
            if (SelectedDLL != SenderCast.Item1) { return; }
            
            // Check if the device is connected or not. If not, try to remove it.
            if (StateChangedArgs.IsConnected == false)
            {
                // Try to remove instance value
                try { InstalledDevices.Remove(StateChangedArgs.DeviceName); }
                catch { ViewModelLogger.WriteLog("WARNING: COULD NOT REMOVE DEVICE INSTANCE FROM LIST OF INSTALLED DEVICES!", LogType.WarnLog); }

                // Store new value here.
                InstalledDevices = InstalledDevices;
                return;
            }

            // If the DLL matches here and our device is connected, then we need to append in a new device object.
            InstalledDevices.Add(StateChangedArgs.DeviceName);
            InstalledDevices = InstalledDevices;
            ViewModelLogger.WriteLog("UPDATED NEW LIST OF DEVICES WITH EVENT TRIGGERED DEVICE VALUE!", LogType.WarnLog);
        }
        /// <summary>
        /// Populates an observable collection of J2534 Devices for a given input J2534 DLL
        /// </summary>
        /// <param name="DllEntry">DLL to find devices for</param>
        /// <returns>Collection of devices found built</returns>
        private ObservableCollection<string> PopulateDevicesForDLL(J2534Dll DllEntry)
        {
            // Log information and pull in our new Device entries for the DLL given if any exist.
            if (DllEntry == null) ViewModelLogger.WriteLog($"FINDING DEVICE ENTRIES FOR DLL NAMED {DllEntry.Name} NOW", LogType.WarnLog);
            
            // Try and get devices here.
            try
            {
                // Pull devices, list count, and return values.
                var PulledDeviceList = DllEntry.FindConnectedDeviceNames();
                if (PulledDeviceList.Count == 0)
                    throw new InvalidOperationException("FAILED TO FIND ANY DEVICES TO USE FOR OUR J2534 INSTANCE!");

                // Log information about pulling and return values.
                // this.SelectedDevice = PulledDeviceList[0];
                ViewModelLogger.WriteLog("PULLED NEW DEVICES IN WITHOUT ISSUES!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"DEVICES FOUND: {string.Join(",", PulledDeviceList)}", LogType.InfoLog);

                // Pull refresh times out of the settings file
                int DLLRefreshTime = ValueLoaders.GetConfigValue<int>("FulcrumInjectorConstants.InjectorHardwareRefresh.RefreshDLLsInterval");
                int DeviceRefreshTime = ValueLoaders.GetConfigValue<int>("FulcrumInjectorConstants.InjectorHardwareRefresh.RefreshDevicesInterval");

                // Build new Watchdog for PTDevice instance helpers
                JBoxEventWatchdog.StartBackgroundRefresh(this.SelectedDLL.Name, JVersion.ALL_VERSIONS, DeviceRefreshTime, DLLRefreshTime);
                ViewModelLogger.WriteLog("STARTING BACKGROUND REFRESH INSTANCE FOR HARDWARE MONITORING NOW...", LogType.InfoLog);

                // Return our build list of objects here
                return new ObservableCollection<string>(PulledDeviceList.Distinct());
            }
            catch (Exception FindEx)
            {
                // Log the exception found and return nothing.
                ViewModelLogger.WriteLog("ERROR! FAILED TO PULL J2534 DEVICES FROM OUR GIVEN DLL ENTRY!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN IS BEING SHOWN BELOW NOW", FindEx);

                // List out the full count of devices built and return it.
                ViewModelLogger.WriteLog("WARNING: NO DEVICES WERE FOUND FOR THE GIVEN DLL ENTRY TYPE!", LogType.ErrorLog);
                return new ObservableCollection<string>();
            }
        }
    }
}
