using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels
{
    /// <summary>
    /// Class object containing information about the current version of this application
    /// </summary>
    public class FulcrumVersionInfo : IComparable
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Public facing fields for the versions of the currently installed shim and injector objects
        public readonly Version ShimVersion = new(0, 0);
        public readonly Version InjectorVersion = new(0, 0);

        // Public facing fields for the compile times of the currently installed shim and injector objects
        public readonly DateTime ShimBuildDate = DateTime.MinValue;
        public readonly DateTime InjectorBuildDate = DateTime.MinValue;

        #endregion //Fields

        #region Properties

        // Public facing properties holding the same version information as above but shown as string content
        public string ShimVersionString => this.ShimVersion.ToString();
        public string InjectorVersionString => this.InjectorVersion.ToString();

        // Public facing properties holding the same build date/time information as above but shown as string content
        public string ShimBuildDateString => this.ShimBuildDate.ToString("MM/dd/yyyy - HH:mm:ss");
        public string InjectorBuildDateString => this.InjectorBuildDate.ToString("MM/dd/yyyy - HH:mm:ss");

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Comparision routines for checking two version objects
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
                this.InjectorVersion.Major +
                this.InjectorVersion.Minor +
                this.InjectorVersion.Build +
                this.InjectorVersion.Revision;
            int InputInjectorVersionInt =
                CastInput.InjectorVersion.Major +
                CastInput.InjectorVersion.Minor +
                CastInput.InjectorVersion.Build +
                CastInput.InjectorVersion.Revision;

            // Return the difference in the two of the int values
            return InputInjectorVersionInt - CurrentInjectorVersionInt;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new injector version information object using the current running assembly
        /// </summary>
        public FulcrumVersionInfo()
        {
            // Build version information from current object executing
            AssemblyName InjectorAssyName = Assembly.GetEntryAssembly()?.GetName();
            this.InjectorVersion = InjectorAssyName?.Version;
            if (this.InjectorVersion == null)
                throw new InvalidOperationException("FAILED TO FIND OUR CURRENT INJECTOR VERSION!");

            // Now find the ShimDLL version
            string InjectorDllPath =
#if DEBUG
                Path.GetFullPath("..\\..\\..\\FulcrumShim\\Debug\\FulcrumShim.dll");
#else
                ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorDllInformation.FulcrumDLL");  
#endif

            // Make sure the injector DLL Exists
            if (!File.Exists(InjectorDllPath))
                throw new InvalidOperationException($"FAILED TO FIND OUR INJECTOR DLL AT {InjectorDllPath}!");

            // Store version information about the injector shim DLL
            FileVersionInfo InjectorShimFileInfo = FileVersionInfo.GetVersionInfo(InjectorDllPath);
            this.ShimVersion = Version.Parse(InjectorShimFileInfo.FileVersion);
            if (this.ShimVersion == null)
                throw new InvalidOperationException("FAILED TO FIND OUR CURRENT SHIM VERSION!");

            // Store the build date/time for the injector and shim DLLs here
            this.ShimBuildDate = this._getBuildTime(Assembly.LoadFile(InjectorDllPath));
            this.InjectorBuildDate = this._getBuildTime(Assembly.GetExecutingAssembly());
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper method used to calculate the build date of the currently running assembly
        /// </summary>
        /// <param name="TargetTime">The time zone configuration for the returned time</param>
        /// <returns>The DateTime value of when the assembly was built</returns>
        private DateTime _getBuildTime(Assembly ExecutingAssembly, TimeZoneInfo TargetTime = null)
        {
            // Find the input assembly location and store some constants for calculating time information
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            string InputAssyLocation = ExecutingAssembly.Location;

            // Define a buffer to store the header content of our assembly resources
            var HeaderBuffer = new byte[2048];
            using (var AssyStream = new FileStream(InputAssyLocation, FileMode.Open, FileAccess.Read))
                AssyStream.Read(HeaderBuffer, 0, 2048);

            // Calculate the offset of our header file
            int HeaderOffset = BitConverter.ToInt32(HeaderBuffer, c_PeHeaderOffset);
            int SecondsSinceTimMin = BitConverter.ToInt32(HeaderBuffer, HeaderOffset + c_LinkerTimestampOffset);
            DateTime CurrentEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Calculate the linker time here using the epoch calculated
            TargetTime ??= TimeZoneInfo.Local;
            var UtlLinkerTime = CurrentEpoch.AddSeconds(SecondsSinceTimMin);
            var LocalLinkTime = TimeZoneInfo.ConvertTimeFromUtc(UtlLinkerTime, TargetTime);

            // Return the calculated build time
            return LocalLinkTime;
        }
    }
}
