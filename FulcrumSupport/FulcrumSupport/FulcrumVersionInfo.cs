using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FulcrumSupport
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

        /// <summary>
        /// Private enumeration used to pull versions for different services in this class
        /// </summary>
        private enum ServiceTypes
        {
            [Description("FulcrumService.dll")]         BASE_SERVICE,       // Default value. Base service type
            [Description("FulcrumDriveService.exe")]    DRIVE_SERVICE,      // Watchdog Service Type
            [Description("FulcrumEmailService.exe")]    EMAIL_SERVICE,      // Drive Service type
            [Description("FulcrumUpdaterService.exe")]  UPDATER_SERVICE,    // Email Service Type
            [Description("FulcrumWatchdogService.exe")] WATCHDOG_SERVICE    // Updater Service Type
        }

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
            _serviceBaseVersion = _getServiceVersion(ServiceTypes.BASE_SERVICE);
            _driveServiceVersion = _getServiceVersion(ServiceTypes.DRIVE_SERVICE);
            _emailServiceVersion = _getServiceVersion(ServiceTypes.EMAIL_SERVICE);
            _updaterServiceVersion = _getServiceVersion(ServiceTypes.UPDATER_SERVICE);
            _watchdogServiceVersion = _getServiceVersion(ServiceTypes.WATCHDOG_SERVICE);
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
        /// Private helper method used to pull the version of a service installed on this machine
        /// </summary>
        /// <param name="ServiceType">The type of service to pull the version for</param>
        /// <returns>The version of the requested service</returns>
        private static Version _getServiceVersion(ServiceTypes ServiceType)
        {
            // If a debugger is found, just find the version information from our file
            if (!Debugger.IsAttached) return ServiceType switch
            {
                ServiceTypes.BASE_SERVICE => RegistryControl.InjectorServiceVersion,
                ServiceTypes.DRIVE_SERVICE => RegistryControl.DriveServiceVersion,
                ServiceTypes.EMAIL_SERVICE => RegistryControl.EmailServiceVersion,
                ServiceTypes.WATCHDOG_SERVICE => RegistryControl.WatchdogServiceVersion,
                ServiceTypes.UPDATER_SERVICE => RegistryControl.UpdaterServiceVersion,
                _ => throw new ArgumentException($"Error! Service type {ServiceType.ToDescriptionString()} could not be found!")
            };

            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath(ServiceType.ToDescriptionString());
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Static method which is used to convert our version information into a set of string values
        /// </summary>
        /// <returns>A text table holding our version information values here</returns>
        public static string ToVersionTable()
        {
            // Build a new set of values for our text table here
            Tuple<string, string>[] VersionStrings = new Tuple<string, string>[]
            {
                new(nameof(InjectorVersionString).Replace("String", string.Empty), InjectorVersionString),
                new(nameof(ShimVersionString).Replace("String", string.Empty), ShimVersionString),
                new(nameof(ServiceBaseVersionString).Replace("String", string.Empty), ServiceBaseVersionString),
                new(nameof(DriveVersionString).Replace("String", string.Empty), DriveVersionString),
                new(nameof(EmailVersionString).Replace("String", string.Empty), EmailVersionString),
                new(nameof(UpdaterVersionString).Replace("String", string.Empty), UpdaterVersionString),
                new(nameof(WatchdogVersionString).Replace("String", string.Empty), WatchdogVersionString),
            };

            // Convert these values into a text table and return it out
            string VersionInfoString =
                "Injector App Versions\n" +
                VersionStrings.ToStringTable(
                    new[] { "Component Name", "Component Version" },
                    VersionObj => VersionObj.Item1,
                    VersionObj => VersionObj.Item2);

            // Return the built version string values 
            return VersionInfoString;
        }
    }
}
