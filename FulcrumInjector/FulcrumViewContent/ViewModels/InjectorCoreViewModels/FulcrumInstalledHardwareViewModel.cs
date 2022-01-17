using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruImport;
using SharpWrap2534.PassThruTypes;

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
        private ObservableCollection<J2534Dll> _installedDLLs;
        private ObservableCollection<string> _installedDevices;

        // Public values for our view to bind onto 
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
            ViewModelLogger.WriteLog("DLL ENTRIES UPDATED OK! STORED THEM TO OUR VIEWMODEL FOR DLL IMPORTING CORRECTLY", LogType.InfoLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW VIEW MODEL FOR HARDWARE INSTANCE VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("CONTENT ON THE VIEW SHOULD REFLECT THE SHARPWRAP HARDWARE LISTING!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Populates an observable collection of J2534 Devices for a given input J2534 DLL
        /// </summary>
        /// <param name="DllEntry">DLL to find devices for</param>
        /// <returns>Collection of devices found built</returns>
        public ObservableCollection<string> PopulateDevicesForDLL(J2534Dll DllEntry)
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
                ViewModelLogger.WriteLog("PULLED NEW DEVICES IN WITHOUT ISSUES!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"DEVICES FOUND: {string.Join(",", PulledDeviceList)}", LogType.InfoLog);
                return new ObservableCollection<string>(PulledDeviceList);
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
