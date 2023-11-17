using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using SharpLogging;

namespace FulcrumSupport
{
    /// <summary>
    /// Static helper class used to provide access to the injector/service registry locations
    /// </summary>
    public static class RegistryControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private logger object for our registry control helper 
        private static readonly SharpLogger _registryLogger;

        // Private static fallback values for key locations
        private static readonly string _defaultMeatIncFolder = "C:\\Program Files (x86)\\MEAT Inc";
        private static readonly string _defaultShimDllFolder = $"{_defaultMeatIncFolder}\\FulcrumShim";
        private static readonly string _defaultInjectorFolder = $"{_defaultMeatIncFolder}\\FulcrumInjector";
        private static readonly string _defaultServicesFolder = $"{_defaultMeatIncFolder}\\FulcrumServices";

        // Private static fields holding the MEAT Inc base registry keys
        private static readonly string _meatIncRegistryKey = "SOFTWARE\\MEAT Inc";
        private static readonly string _installPathKey = $"InstallPath";
        private static readonly string _installVersionKey = $"InstallVersion";
        private static readonly string _installExecutableKey = "InstallExecutable";

        // Private static fields holding our Injector App configuration registry keys
        private static readonly string _injectorRegistryKey = $"{_meatIncRegistryKey}\\FulcrumInjector";

        // Private static fields holding our Shim DLL configuration registry keys
        private static readonly string _shimRegistryKey = $"{_meatIncRegistryKey}\\FulcrumShim";
        
        // Private static fields holding our Injector Service configuration registry keys
        private static readonly string _serviceRegistryKey = $"{_meatIncRegistryKey}\\FulcrumServices";
        private static readonly string _driveServiceKey = $"{_serviceRegistryKey}\\FulcrumDrive";
        private static readonly string _emailServiceKey = $"{_serviceRegistryKey}\\FulcrumEmail";
        private static readonly string _updaterServiceKey = $"{_serviceRegistryKey}\\FulcrumUpdater";
        private static readonly string _watchdogServiceKey = $"{_serviceRegistryKey}\\FulcrumWatchdog";

        #endregion // Fields

        #region Properties

        // Public facing properties holding computed values for the Injector App registry key
        public static string InjectorInstallPath => _getRegistryValue(_injectorRegistryKey, _installPathKey, _defaultInjectorFolder);
        public static string InjectorExecutable => _getRegistryValue(_injectorRegistryKey, _installExecutableKey, $"{_defaultInjectorFolder}\\FulcrumInjector.exe");
        public static Version InjectorVersion => _getRegistryVersion(_injectorRegistryKey, _installVersionKey);

        // Public facing properties holding computed values for the Shim DLL registry key
        public static string ShimInstallPath => _getRegistryValue(_shimRegistryKey, _installPathKey, _defaultShimDllFolder);
        public static string ShimDllExecutable => _getRegistryValue(_shimRegistryKey, _installExecutableKey, $"{_defaultShimDllFolder}\\FulcrumShim.dll");
        public static Version ShimDllVersion => _getRegistryVersion(_shimRegistryKey, _installVersionKey);

        // Public facing properties holding computed values for the base Injector Service Key
        public static string InjectorServiceInstallPath => _getRegistryValue(_serviceRegistryKey, _installPathKey, _defaultServicesFolder);
        public static Version InjectorServiceVersion => _getRegistryVersion(_serviceRegistryKey, _installVersionKey);

        // Public facing properties holding computed values for the Drive Service Key
        public static string DriveServiceInstallPath => _getRegistryValue(_driveServiceKey, _installPathKey, $"{_defaultServicesFolder}\\FulcrumDriveService");
        public static string DriveServiceExecutable => _getRegistryValue(_driveServiceKey, _installExecutableKey, $"{_defaultServicesFolder}\\FulcrumDriveService\\FulcrumDriveService.exe");
        public static Version DriveServiceVersion => _getRegistryVersion(_driveServiceKey, _installVersionKey);

        // Public facing properties holding computed values for the Email Service Key
        public static string EmailServiceInstallPath => _getRegistryValue(_emailServiceKey, _installPathKey, $"{_defaultServicesFolder}\\FulcrumEmailService");
        public static string EmailServiceExecutable => _getRegistryValue(_emailServiceKey, _installExecutableKey, $"{_defaultServicesFolder}\\FulcrumEmailService\\FulcrumEmailService.exe");
        public static Version EmailServiceVersion => _getRegistryVersion(_emailServiceKey, _installVersionKey);

        // Public facing properties holding computed values for the Updater Service Key
        public static string UpdaterServiceInstallPath => _getRegistryValue(_updaterServiceKey, _installPathKey, $"{_defaultServicesFolder}\\FulcrumUpdaterService");
        public static string UpdaterServiceExecutable => _getRegistryValue(_updaterServiceKey, _installExecutableKey, $"{_defaultServicesFolder}\\FulcrumUpdaterService\\FulcrumUpdaterService.exe");
        public static Version UpdaterServiceVersion => _getRegistryVersion(_updaterServiceKey, _installVersionKey);

        // Public facing properties holding computed values for the Watchdog Service Key
        public static string WatchdogServiceInstallPath => _getRegistryValue(_watchdogServiceKey, _installPathKey, $"{_defaultServicesFolder}\\FulcrumWatchdogService");
        public static string WatchdogServiceExecutable => _getRegistryValue(_watchdogServiceKey, _installExecutableKey, $"{_defaultServicesFolder}\\FulcrumWatchdogService\\FulcrumWatchdogService.exe");
        public static Version WatchdogServiceVersion => _getRegistryVersion(_watchdogServiceKey, _installVersionKey);

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Static CTOR for the registry control class. Used to configure logger objects for it
        /// </summary>
        static RegistryControl()
        {
            // Configure our logger object and exit out
            _registryLogger = 
                SharpLogBroker.FindLoggers($"{nameof(RegistryControl)}Logger").FirstOrDefault()
                ?? new SharpLogger(LoggerActions.UniversalLogger, $"{nameof(RegistryControl)}Logger");
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper method used to pull a value in the registry at the requested path
        /// </summary>
        /// <param name="KeyPath">The path of the registry to pull our values for</param>
        /// <param name="KeyName">The name of the key we're trying to open</param>
        /// <param name="DefaultValue">Optional default value for our registry entry</param>
        /// <returns>The value of the key if found. Default/null if not found</returns>
        private static string _getRegistryValue(string KeyPath, string KeyName, string DefaultValue = null)
        {
            try
            {
                // Log some debug information about the registry value requested
                _registryLogger.WriteLog($"PULLING REGISTRY VALUE: {KeyPath}\\{KeyName}", LogType.TraceLog);
                
                // Pull the registry key value here and open it if possible
                var OpenedKey = Registry.LocalMachine.OpenSubKey(KeyPath);
                string PulledValue = OpenedKey.GetValue(KeyName, DefaultValue).ToString();
                _registryLogger.WriteLog($"PULLED A VALUE OF {PulledValue} FOR KEY {KeyName}!", LogType.TraceLog);

                // Return the located value here
                return PulledValue;
            }
            catch (Exception OpenKeyEx)
            {
                // Log our failure and return null/default
                _registryLogger.WriteLog($"ERROR! FAILED TO PULL REGISTRY KEY {KeyPath}\\{KeyName}!", LogType.ErrorLog);
                _registryLogger.WriteException("EXCEPTION THROWN IS BEING LOGGED BELOW", OpenKeyEx, LogType.ErrorLog);
                return DefaultValue ?? string.Empty;
            }
        }
        /// <summary>
        /// Private helper method used to pull a version value from the registry
        /// </summary>
        /// <param name="KeyPath">The path of the registry to pull our values for</param>
        /// <param name="KeyName">The name of the key we're trying to open</param>
        /// <param name="DefaultVersion">Optional default version value to return if no version found</param>
        /// <returns>The value of the key as a version if found. A default/empty version if not</returns>
        private static Version _getRegistryVersion(string KeyPath, string KeyName, Version DefaultVersion = null)
        {
            try
            {
                // Log some debug information about the registry value requested
                _registryLogger.WriteLog($"PULLING REGISTRY VERSION VALUE: {KeyPath}\\{KeyName}", LogType.TraceLog);

                // Pull the registry key value here and open it if possible
                var OpenedKey = Registry.LocalMachine.OpenSubKey(KeyPath);
                string VersionString = OpenedKey.GetValue(KeyName).ToString();
                _registryLogger.WriteLog($"PULLED A VALUE OF {VersionString} FOR KEY {KeyName}!", LogType.TraceLog);

                // Convert our string version value into an actual version and return it out
                return Version.Parse(VersionString);
            }
            catch (Exception OpenKeyEx)
            {
                // Log our failure and return null/default
                _registryLogger.WriteLog($"ERROR! FAILED TO PULL REGISTRY KEY {KeyPath}\\{KeyName}!", LogType.ErrorLog);
                _registryLogger.WriteException("EXCEPTION THROWN IS BEING LOGGED BELOW", OpenKeyEx, LogType.ErrorLog);
                return DefaultVersion ?? new Version(0,0,0,0);
            }
        }
    }
}
