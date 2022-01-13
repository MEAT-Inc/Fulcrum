using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.EmailReporting;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;

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
        private ObservableCollection<J2534Device> _installedDevices;

        // Public values for our view to bind onto 
        public ObservableCollection<J2534Dll> InstalledDLLs { get => _installedDLLs; set => PropertyUpdated(value); }
        public ObservableCollection<J2534Device> InstalledDevices { get => InstalledDevices; set => PropertyUpdated(value); };

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumInstalledHardwareViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP HARDWARE INSTANCE VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Pull in our DLL Entries now.


            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW VIEW MODEL FOR HARDWARE INSTANCE VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("CONTENT ON THE VIEW SHOULD REFLECT THE SHARPWRAP HARDWARE LISTING!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

    }
}
