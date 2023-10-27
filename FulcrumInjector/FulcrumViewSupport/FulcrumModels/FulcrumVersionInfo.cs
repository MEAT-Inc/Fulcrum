using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumSupport;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels
{
    /// <summary>
    /// Class object containing information about the current version of this application
    /// </summary>
    public static class FulcrumVersionInfo
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields holding our version information objects
        private static readonly Version _shimVersion;
        private static readonly Version _injectorVersion;
        private static readonly Version _serviceBaseVersion;
        private static readonly Version _driveServiceVersion;
        private static readonly Version _emailServiceVersion;
        private static readonly Version _updaterServiceVersion;
        private static readonly Version _watchdogServiceVersion;

#if DEBUG
        // Store a local flag for if we're using a debug build or not
        private static readonly bool _isDebugBuild = true;
#else
        // Store a local flag for if we're using a debug build or not
        private static readonly bool _isDebugBuild = false;
#endif

        #endregion //Fields

        #region Properties

        // Public facing properties holding the same version information as above but shown as string content
        public static string ShimVersionString => _shimVersion.ToString();
        public static string InjectorVersionString => _injectorVersion.ToString();
        public static string ServiceBaseVersionString => _serviceBaseVersion.ToString();
        public static string DriveVersionString => _driveServiceVersion.ToString();
        public static string EmailVersionString => _emailServiceVersion.ToString();
        public static string UpdaterVersionString => _updaterServiceVersion.ToString();
        public static string WatchdogVersionString => _watchdogServiceVersion.ToString();

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new injector version information object using the current running assembly
        /// </summary>
        static FulcrumVersionInfo()
        {
            // Populate our version information objects here
            _shimVersion = _getShimVersion();
            _injectorVersion = _getInjectorVersion();
            _serviceBaseVersion = _getServiceBaseVersion();
            _driveServiceVersion = _getDriveServiceVersion();
            _emailServiceVersion = _getEmailServiceVersion();
            _updaterServiceVersion = _getUpdaterServiceVersion();
            _watchdogServiceVersion = _getWatchdogServiceVersion();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper routine used to pull the version of the Shim DLL
        /// </summary>
        /// <returns>The version of the Shim DLL</returns>
        private static Version _getShimVersion()
        {
            // If no debugger is found, pull our value from the registry
            if (!Debugger.IsAttached) return RegistryControl.ShimDllVersion;

            // If a debugger is found, find our DLL based on the working directory
            string InjectorDllConfig = _isDebugBuild ? "Debug" : "Release";
            string InjectorDllPath = Path.GetFullPath($"..\\..\\..\\FulcrumShim\\{InjectorDllConfig}\\FulcrumShim.dll");
            FileVersionInfo InjectorShimFileInfo = FileVersionInfo.GetVersionInfo(InjectorDllPath);
            return Version.Parse(InjectorShimFileInfo.FileVersion);
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector application
        /// </summary>
        /// <returns>The version of the injector application</returns>
        private static Version _getInjectorVersion()
        {
            // If no debugger is found, pull our value from the registry
            if (!Debugger.IsAttached) return RegistryControl.InjectorVersion;

            // Build version information from current directory contents
            Assembly InjectorAssembly = Assembly.GetExecutingAssembly();
            return InjectorAssembly.GetName()?.Version;
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector service base type
        /// </summary>
        /// <returns>The version of the injector service base type</returns>
        private static Version _getServiceBaseVersion()
        {
            // If no debugger is found, pull our value from the registry
            if (!Debugger.IsAttached) return RegistryControl.InjectorServiceVersion;

            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath("FulcrumService.dll");
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector drive service
        /// </summary>
        /// <returns>The version of the injector drive service</returns>
        private static Version _getDriveServiceVersion()
        {
            // If no debugger is found, pull our value from the registry
            if (!Debugger.IsAttached) return RegistryControl.DriveServiceVersion;

            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath("FulcrumDriveService.exe");
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector email service
        /// </summary>
        /// <returns>The version of the injector email service</returns>
        private static Version _getEmailServiceVersion()
        {
            // If no debugger is found, pull our value from the registry
            if (!Debugger.IsAttached) return RegistryControl.EmailServiceVersion;

            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath("FulcrumEmailService.exe");
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector updater service
        /// </summary>
        /// <returns>The version of the injector updater service</returns>
        private static Version _getUpdaterServiceVersion()
        {
            // If no debugger is found, pull our value from the registry
            if (!Debugger.IsAttached) return RegistryControl.UpdaterServiceVersion;

            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath("FulcrumUpdaterService.exe");
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector watchdog service
        /// </summary>
        /// <returns>The version of the injector watchdog service</returns>
        private static Version _getWatchdogServiceVersion()
        {
            // If no debugger is found, pull our value from the registry
            if (!Debugger.IsAttached) return RegistryControl.WatchdogServiceVersion;

            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath("FulcrumWatchdogService.exe");
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
        }
    }
}
