using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using SharpLogging;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruImport;
using SharpWrapper.PassThruTypes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// Class used to bind our installed/active hardware objects to the view content for showing current PassThru devices.
    /// </summary>
    internal class FulcrumInstalledHardwareViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for our public properties
        private bool _isRefreshing;
        private bool _isIgnoredDLL;
        private J2534Dll _selectedDLL;
        private string _selectedDevice;
        private ObservableCollection<J2534Dll> _installedDLLs;
        private ObservableCollection<string> _installedDevices;

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool IsIgnoredDLL { get => _isIgnoredDLL; set => PropertyUpdated(value); }
        public bool IsRefreshing { get => _isRefreshing; set => PropertyUpdated(value); }

        // Selected Device Object and Selected DLL Object
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
        public J2534Dll SelectedDLL
        {
            get => _selectedDLL;
            set
            {
                // Update the connection view model values here
                PropertyUpdated(value);
                if (value != null) InstalledDevices = this._populateDevicesForDLL(value);

                // Check our values here. 
                if (FulcrumConstants.FulcrumVehicleConnectionInfoViewModel == null) return;
                FulcrumConstants.FulcrumVehicleConnectionInfoViewModel.SelectedDLL =
                    value == null ? "No DLL Selected" : value.Name;
                FulcrumConstants.FulcrumVehicleConnectionInfoViewModel.VersionType =
                    value?.DllVersion ?? JVersion.ALL_VERSIONS;
            }
        }

        // Current Installed DLL List object and installed Devices for Said DLL
        public ObservableCollection<J2534Dll> InstalledDLLs { get => _installedDLLs; set => PropertyUpdated(value); }
        public ObservableCollection<string> InstalledDevices { get => _installedDevices; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="HardwareUserControl">UserControl which holds the content for the installed hardware view</param>
        public FulcrumInstalledHardwareViewModel(UserControl HardwareUserControl) : base(HardwareUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP HARDWARE INSTANCE VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Pull in our DLL Entries and our device entries now.
            this.ViewModelLogger.WriteLog("UPDATING AND IMPORTING CURRENT DLL LIST FOR THIS SYSTEM NOW...", LogType.WarnLog);
            this.InstalledDLLs = new ObservableCollection<J2534Dll>(new PassThruImportDLLs().LocatedJ2534DLLs);

            // See if using default PT Device is on or off.
            bool AutoConsume = FulcrumConstants.FulcrumSettings.InjectorGeneralFulcrumSettings.GetSettingValue("Auto Consume CarDAQ-Plus 3", false);
            if (!AutoConsume) this.ViewModelLogger.WriteLog("NOT TRYING TO AUTO CONFIGURE A NEW CARDAQ PLUS 3 INSTANCE SINCE THE USER HAS SET IT TO OFF!", LogType.WarnLog); 
            else 
            {
                // Pull in our PT Device now.
                if (this.InstalledDLLs.Any(DLLObj => DLLObj.Name.Contains("CarDAQ-Plus 3"))) {
                    this.SelectedDLL = this.InstalledDLLs.FirstOrDefault(DLLObj => DLLObj.Name.Contains("CarDAQ-Plus 3"));
                    this.ViewModelLogger.WriteLog("STORED OUR DEFAULT CDP3 DLL AND DEVICE INSTANCE OK!");
                }
                else this.ViewModelLogger.WriteLog("ERROR! UNABLE TO FIND A USABLE CARDAQ PLUS 3 INSTANCE!", LogType.ErrorLog);
            }

            // Log completed setup and exit out of this constructor
            this.ViewModelLogger.WriteLog("DLL ENTRIES UPDATED OK! STORED THEM TO OUR VIEWMODEL FOR DLL IMPORTING CORRECTLY");
            this.ViewModelLogger.WriteLog("SETUP NEW VIEW MODEL FOR HARDWARE INSTANCE VALUES OK!");
            this.ViewModelLogger.WriteLog("CONTENT ON THE VIEW SHOULD REFLECT THE SHARPWRAP HARDWARE LISTING!");
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Populates an observable collection of J2534 Devices for a given input J2534 DLL
        /// </summary>
        /// <param name="DllEntry">DLL to find devices for</param>
        /// <returns>Collection of devices found built</returns>
        private ObservableCollection<string> _populateDevicesForDLL(J2534Dll DllEntry)
        {
            // Log information and pull in our new Device entries for the DLL given if any exist.
            if (DllEntry == null) return new ObservableCollection<string>();
            this.ViewModelLogger.WriteLog($"FINDING DEVICE ENTRIES FOR DLL NAMED {DllEntry.Name} NOW", LogType.WarnLog);

            // Check for not supported DLL Values.
            ObservableCollection<string> DevicesFound = new ObservableCollection<string>();
            var IgnoredDLLs = ValueLoaders.GetConfigValue<string[]>("FulcrumInjectorConstants.InjectorHardwareRefresh.IgnoredDLLNames");
            IsIgnoredDLL = IgnoredDLLs.Contains(DllEntry.Name);
            if (IgnoredDLLs.Contains(DllEntry.Name)) {
                this.ViewModelLogger.WriteLog("NOT UPDATING DEVICES FOR A DLL WHICH IS KNOWN TO NOT BE USABLE WITH THE FULCRUM!", LogType.WarnLog);
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
                this.ViewModelLogger.WriteLog("PULLED NEW DEVICES IN WITHOUT ISSUES!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog($"DEVICES FOUND: {string.Join(",", PulledDeviceList)}", LogType.InfoLog);

                // Return our build list of objects here
                DevicesFound = new ObservableCollection<string>(PulledDeviceList.Distinct());
            }
            catch (Exception FindEx)
            {
                // Log the exception found and return nothing.
                this.ViewModelLogger.WriteLog("ERROR! FAILED TO PULL J2534 DEVICES FROM OUR GIVEN DLL ENTRY!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION THROWN IS BEING SHOWN BELOW NOW", FindEx);

                // List out the full count of devices built and return it.
                this.ViewModelLogger.WriteLog("WARNING: NO DEVICES WERE FOUND FOR THE GIVEN DLL ENTRY TYPE!", LogType.ErrorLog);
            }

            // Stop Refreshing, return out list output
            this.IsRefreshing = false;
            return DevicesFound;
        }
    }
}
