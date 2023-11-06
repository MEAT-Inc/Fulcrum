using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels
{
    /// <summary>
    /// View model content for the About view on the injector application
    /// </summary>
    public class FulcrumAboutThisAppViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for version values
        private string _injectorAppVersion;        // Version for the injector application
        private string _shimDllVersion;            // Version for the Shim DLL
        private string _serviceBaseVersion;        // Version for the service base type
        private string _driveServiceVersion;       // Version for the drive service
        private string _emailServiceVersion;       // Version for the email service
        private string _updaterServiceVersion;     // Version for the updater service
        private string _watchdogServiceVersion;    // Version for the watchdog service

        #endregion // Fields

        #region Properties

        // Public properties holding our version values
        public string InjectorAppVersion { get => _injectorAppVersion; set => PropertyUpdated(value); }
        public string ShimDllVersion { get => _shimDllVersion; set => PropertyUpdated(value); }
        public string ServiceBaseVersion { get => _serviceBaseVersion; set => PropertyUpdated(value); }
        public string DriveServiceVersion { get => _driveServiceVersion; set => PropertyUpdated(value); }
        public string EmailServiceVersion { get => _emailServiceVersion; set => PropertyUpdated(value); }
        public string UpdaterServiceVersion { get => _updaterServiceVersion; set => PropertyUpdated(value); }
        public string WatchdogServiceVersion { get => _watchdogServiceVersion; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="AboutAppUserControl">UserControl which holds the content for our About this app view</param>
        public FulcrumAboutThisAppViewModel(UserControl AboutAppUserControl) : base(AboutAppUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Bind our component values for versions here
            this.ViewModelLogger.WriteLog("SETTING UP TITLE VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this._setComponentVersions();

            // Log that our view model has been built
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper method used to set our version information values for bindings on loaded routines
        /// </summary>
        private void _setComponentVersions()
        {
            // Store all our version values here
            this.ViewModelLogger.WriteLog("STORING APPLICATION COMPONENT VERSION VALUES NOW...", LogType.WarnLog);
            this.InjectorAppVersion = FulcrumVersionInfo.InjectorVersionString;
            this.ShimDllVersion = FulcrumVersionInfo.ShimVersionString;
            this.ServiceBaseVersion = FulcrumVersionInfo.ServiceBaseVersionString;
            this.DriveServiceVersion = FulcrumVersionInfo.DriveVersionString;
            this.EmailServiceVersion = FulcrumVersionInfo.EmailVersionString;
            this.UpdaterServiceVersion = FulcrumVersionInfo.UpdaterVersionString;
            this.WatchdogServiceVersion = FulcrumVersionInfo.WatchdogVersionString;

            // Log out the version information values stored here
            this.ViewModelLogger.WriteLog("VERSION INFORMATION STORED CORRECTLY! LOGGING VERSION VALUES BELOW", LogType.InfoLog);
            string[] VersionStrings = FulcrumVersionInfo.ToVersionTable().Split('\n');
            foreach (string VersionString in VersionStrings) this.ViewModelLogger.WriteLog(VersionString); 
        }
    }
}
