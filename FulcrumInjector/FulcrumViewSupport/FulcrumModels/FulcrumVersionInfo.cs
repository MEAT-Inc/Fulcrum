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
    public class FulcrumVersionInfo : IComparable
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields holding our version information objects
        private readonly Version _shimVersion;
        private readonly Version _injectorVersion;
        private readonly Version _serviceBaseVersion;
        private readonly Version _driveServiceVersion;
        private readonly Version _emailServiceVersion;
        private readonly Version _updaterServiceVersion;
        private readonly Version _watchdogServiceVersion;

        #endregion //Fields

        #region Properties

        // Public facing properties holding the same version information as above but shown as string content
        public string ShimVersionString => this._shimVersion.ToString();
        public string InjectorVersionString => this._injectorVersion.ToString();
        public string ServiceBaseVersionString => this._serviceBaseVersion.ToString();
        public string DriveVersionString => this._driveServiceVersion.ToString();
        public string EmailVersionString => this._emailServiceVersion.ToString();
        public string UpdaterVersionString => this._updaterServiceVersion.ToString();
        public string WatchdogVersionString => this._watchdogServiceVersion.ToString();

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Comparison routines for checking two version objects
        /// </summary>
        /// <param name="CompareAgainst">Input object to compare</param>
        /// <returns>An int value showing which version is newer and by how much.</returns>
        public int CompareTo(object CompareAgainst)
        {
            // Check type for conversion information
            if (CompareAgainst is not FulcrumVersionInfo)
                throw new InvalidOperationException($"INVALID INPUT TYPE OF {CompareAgainst.GetType()}");

            // Run the comparison. Return a positive number if the input version is newer. Negative if it's lower
            FulcrumVersionInfo CastInput = CompareAgainst as FulcrumVersionInfo;
            int CurrentInjectorVersionInt =
                this._injectorVersion.Major +
                this._injectorVersion.Minor +
                this._injectorVersion.Build +
                this._injectorVersion.Revision;
            int InputInjectorVersionInt =
                CastInput._injectorVersion.Major +
                CastInput._injectorVersion.Minor +
                CastInput._injectorVersion.Build +
                CastInput._injectorVersion.Revision;

            // Return the difference in the two of the int values
            return InputInjectorVersionInt - CurrentInjectorVersionInt;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new injector version information object using the current running assembly
        /// </summary>
        public FulcrumVersionInfo()
        {
            // Populate our version information objects here
            this._shimVersion = this._getShimVersion();
            this._injectorVersion = this._getInjectorVersion();
            this._serviceBaseVersion = this._getServiceBaseVersion();
            this._driveServiceVersion = this._getDriveServiceVersion();
            this._emailServiceVersion = this._getEmailServiceVersion();
            this._updaterServiceVersion = this._getUpdaterServiceVersion();
            this._watchdogServiceVersion = this._getWatchdogServiceVersion();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper routine used to pull the version of the Shim DLL
        /// </summary>
        /// <returns>The version of the Shim DLL</returns>
        private Version _getShimVersion()
        {
#if DEBUG
            // Find the ShimDLL version based on our local DLL Version built from source
            string InjectorDllPath = Path.GetFullPath("..\\..\\..\\FulcrumShim\\Debug\\FulcrumShim.dll");
            FileVersionInfo InjectorShimFileInfo = FileVersionInfo.GetVersionInfo(InjectorDllPath);
            return Version.Parse(InjectorShimFileInfo.FileVersion);
#else
            // Build version information from our registry entries
            return RegistryControl.ShimInstallVersion;
#endif
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector application
        /// </summary>
        /// <returns>The version of the injector application</returns>
        private Version _getInjectorVersion()
        {
#if DEBUG
            // Build version information from current directory contents
            Assembly InjectorAssembly = Assembly.GetExecutingAssembly();
            return InjectorAssembly.GetName()?.Version;
#else
            // Build version information from our registry entries
            return RegistryControl.InjectorVersion;
#endif
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector service base type
        /// </summary>
        /// <returns>The version of the injector service base type</returns>
        private Version _getServiceBaseVersion()
        {
#if DEBUG
            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath("FulcrumService.dll");
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
#else
            // Build version information from our registry entries
            return RegistryControl.InjectorServiceVersion;
#endif
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector drive service
        /// </summary>
        /// <returns>The version of the injector drive service</returns>
        private Version _getDriveServiceVersion()
        {
#if DEBUG
            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath("FulcrumDriveService.exe");
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
#else
            // Build version information from our registry entries
            return RegistryControl.DriveServiceVersion;
#endif
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector email service
        /// </summary>
        /// <returns>The version of the injector email service</returns>
        private Version _getEmailServiceVersion()
        {
#if DEBUG
            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath("FulcrumEmailService.exe");
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
#else
            // Build version information from our registry entries
            return RegistryControl.EmailServiceVersion;
#endif
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector updater service
        /// </summary>
        /// <returns>The version of the injector updater service</returns>
        private Version _getUpdaterServiceVersion()
        {
#if DEBUG
            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath("FulcrumUpdaterService.exe");
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
#else
            // Build version information from our registry entries
            return RegistryControl.UpdaterServiceVersion;
#endif
        }
        /// <summary>
        /// Private helper routine used to pull the version of the injector watchdog service
        /// </summary>
        /// <returns>The version of the injector watchdog service</returns>
        private Version _getWatchdogServiceVersion()
        {
#if DEBUG
            // Build version information from current directory contents
            string AssemblyPath = Path.GetFullPath("FulcrumWatchdogService.exe");
            FileVersionInfo AssemblyVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyPath);
            return Version.Parse(AssemblyVersionInfo.FileVersion);
#else
            // Build version information from our registry entries
            return RegistryControl.WatchdogServiceVersion;
#endif
        }
    }
}
