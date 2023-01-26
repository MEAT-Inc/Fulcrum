using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.Models.EventModels;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruImport;
using SharpWrapper.SupportingLogic;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// Class used to bind our installed/active hardware objects to the view content for showing current PassThru devices.
    /// </summary>
    public class FulcrumInstalledHardwareViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("InstalledHardwareViewModelLogger", LoggerActions.SubServiceLogger);

        // Private Control Values
        private bool _isRefreshing;
        private bool _isIgnoredDLL;
        private J2534Dll _selectedDLL;
        private string _selectedDevice;
        private ObservableCollection<J2534Dll> _installedDLLs;
        private ObservableCollection<string> _installedDevices;

        // Selected DLL object
        public bool IsIgnoredDLL { get => _isIgnoredDLL; set => PropertyUpdated(value); }
        public bool IsRefreshing { get => _isRefreshing; set => PropertyUpdated(value); }
        public J2534Dll SelectedDLL
        {
            get => _selectedDLL;
            set
            {
                // Update the connection view model values here
                PropertyUpdated(value); 
                if (value != null) InstalledDevices = this.PopulateDevicesForDLL(value);

                // Check our values here. 
                if (FulcrumConstants.FulcrumVehicleConnectionInfoViewModel == null) return;
                FulcrumConstants.FulcrumVehicleConnectionInfoViewModel.SelectedDLL = 
                    value == null ? "No DLL Selected" : value.Name;
                FulcrumConstants.FulcrumVehicleConnectionInfoViewModel.VersionType =
                    value?.DllVersion ?? JVersion.ALL_VERSIONS;
            }
        }

        // Selected Device Object
        public string SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                // Update private properties and set new values on other components
                PropertyUpdated(value);
                
                // Update the connection view model values here. Don't set if the device is the same.
                if (FulcrumConstants.FulcrumVehicleConnectionInfoViewModel != null)
                {
                    // Store values if the VM is not null
                    FulcrumConstants.FulcrumVehicleConnectionInfoViewModel.SelectedDevice = value;
                    if (value == null) FulcrumConstants.FulcrumVehicleConnectionInfoViewModel.DeviceVoltage = 0.00;
                }

                // Update the simulation view model values here.
                if (FulcrumConstants.FulcrumSimulationPlaybackViewModel != null)
                {
                    // Store values if the VM is not null
                    bool IsDeviceReady = this.SelectedDLL != null && !string.IsNullOrEmpty(value);
                    FulcrumConstants.FulcrumSimulationPlaybackViewModel.IsHardwareSetup = IsDeviceReady;
                }
            }
        }

        // Current Installed DLL List object and installed Devices for Said DLL
        public ObservableCollection<J2534Dll> InstalledDLLs { get => _installedDLLs; set => PropertyUpdated(value); }
        public ObservableCollection<string> InstalledDevices { get => _installedDevices; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumInstalledHardwareViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP HARDWARE INSTANCE VIEW BOUND VALUES NOW...", LogType.WarnLog);

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
        /// Populates an observable collection of J2534 Devices for a given input J2534 DLL
        /// </summary>
        /// <param name="DllEntry">DLL to find devices for</param>
        /// <returns>Collection of devices found built</returns>
        private ObservableCollection<string> PopulateDevicesForDLL(J2534Dll DllEntry)
        {
            // Log information and pull in our new Device entries for the DLL given if any exist.
            if (DllEntry == null) return new ObservableCollection<string>();
            ViewModelLogger.WriteLog($"FINDING DEVICE ENTRIES FOR DLL NAMED {DllEntry.Name} NOW", LogType.WarnLog);

            // Check for not supported DLL Values.
            ObservableCollection<string> DevicesFound = new ObservableCollection<string>();
            var IgnoredDLLs = ValueLoaders.GetConfigValue<string[]>("FulcrumInjectorConstants.InjectorHardwareRefresh.IgnoredDLLNames");
            IsIgnoredDLL = IgnoredDLLs.Contains(DllEntry.Name);
            if (IgnoredDLLs.Contains(DllEntry.Name)) {
                ViewModelLogger.WriteLog("NOT UPDATING DEVICES FOR A DLL WHICH IS KNOWN TO NOT BE USABLE WITH THE FULCRUM!", LogType.WarnLog);
                return new ObservableCollection<string>();
            }

            try
            {
                // Pull devices, list count, and return values.
                IsRefreshing = true;
                var PulledDeviceList = DllEntry.FindConnectedDeviceNames();
                if (PulledDeviceList.Count == 0) throw new InvalidOperationException("FAILED TO FIND ANY DEVICES TO USE FOR OUR J2534 INSTANCE!");

                // Log information about pulling and return values.
                // this.SelectedDevice = PulledDeviceList[0];
                ViewModelLogger.WriteLog("PULLED NEW DEVICES IN WITHOUT ISSUES!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"DEVICES FOUND: {string.Join(",", PulledDeviceList)}", LogType.InfoLog);

                // Return our build list of objects here
                DevicesFound = new ObservableCollection<string>(PulledDeviceList.Distinct());
            }
            catch (Exception FindEx)
            {
                // Log the exception found and return nothing.
                ViewModelLogger.WriteLog("ERROR! FAILED TO PULL J2534 DEVICES FROM OUR GIVEN DLL ENTRY!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN IS BEING SHOWN BELOW NOW", FindEx);

                // List out the full count of devices built and return it.
                ViewModelLogger.WriteLog("WARNING: NO DEVICES WERE FOUND FOR THE GIVEN DLL ENTRY TYPE!", LogType.ErrorLog);
            }

            // Stop Refreshing, return out list output
            this.IsRefreshing = false;
            return DevicesFound;
        }
    }
}
