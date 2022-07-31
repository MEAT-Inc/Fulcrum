using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.FulcrumUpdater;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels
{
    /// <summary>
    /// ViewModel logic for our title view component
    /// </summary>
    public class FulcrumTitleViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("TitleViewModelLogger")) ?? new SubServiceLogger("TitleViewModelLogger");

        // Private control values
        private string _titleTextString;          // Private value for title view title text
        private string _injectorVersionString;    // Private value for title view version text
        private string _shimDLLVersionString;     // Private value for title version for Shim DLL
        private bool _injectorUpdateReady;        // Private value for injector update ready or not.

        // Public values for our view to bind onto 
        public string TitleTextString { get => _titleTextString; set => PropertyUpdated(value); }
        public string InjectorVersionString { get => _injectorVersionString; set => PropertyUpdated(value); }
        public string ShimDLLVersionString { get => _shimDLLVersionString; set => PropertyUpdated(value); }
        public bool InjectorUpdateReady { get => _injectorUpdateReady; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumTitleViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP TITLE VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Store title and version string values now.
            this.ShimDLLVersionString = $"Shim Version: {FulcrumConstants.InjectorVersions.ShimVersionString}";
            this.InjectorVersionString = $"Version: {FulcrumConstants.InjectorVersions.InjectorVersionString}";
            this.TitleTextString = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.AppInstanceName");

            // Log output information
            ViewModelLogger.WriteLog("PULLED NEW TITLE AND VERSION VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"INJECTOR:  {InjectorVersionString}");
            ViewModelLogger.WriteLog($"SHIM:      {ShimDLLVersionString}");

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW TITLE AND VERSION STRING VALUES OK!", LogType.InfoLog);
        }
    }
}
