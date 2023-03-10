using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumWatchdog
{
    /// <summary>
    /// Simple structure used to help configure new Watchdog services
    /// </summary>
    internal class WatchdogConfiguration
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Public fields holding information about this service setup
        [JsonIgnore] public Action WatchdogAction;                                  // A defined watchdog action for this folder                     
        [JsonProperty("FolderPath")] public readonly string WatchdogPath;           // Path to watch on this service
        [JsonProperty("FileExtensions")] public readonly string[] FileExtensions;   // Extensions being watched for this folder

        #endregion //Fields

        #region Properties

        // Public properties holding our watchdog action setup and state of this configuration
        [JsonIgnore] public bool IsWatchable =>
            (!string.IsNullOrWhiteSpace(this.WatchdogPath) && Directory.Exists(this.WatchdogPath)) &&
            (this.FileExtensions?.Length != 0 &&
             (this.FileExtensions?.All(Ext => !string.IsNullOrWhiteSpace(Ext)) ?? true));

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Override for converting one of these configuration objects into a string
        /// </summary>
        /// <returns>A string holding the values used to configure this watchdog configuration</returns>
        public override string ToString()
        {
            // Build the output string and return it out
            string WatchdogString =
                $"Watchdog Configuration - {(this.IsWatchable ? "Watchable Path" : "Not Watchable!")}\n" +
                $"\t\\__ Directory:    {(string.IsNullOrWhiteSpace(this.WatchdogPath) ? "No Path Set!" : this.WatchdogPath)}" +
                $"{(string.IsNullOrWhiteSpace(this.WatchdogPath) ? "\n" : $"\n\\__ Path Exists:  {(Directory.Exists(this.WatchdogPath) ? "Yes" : "No")}\n")}" +
                $"\t\\__ File Types:   {(this.FileExtensions.Length == 0 ? "No Supported File Types!" : string.Join(", ", this.FileExtensions))}\n" + 
                $"\t\\__ Action Setup: {(this.WatchdogAction != null && this.WatchdogAction != new Action(() => { }) ? "Action Configured" : "No Action Set!")}";

            // Return the built string value here
            return WatchdogString;
        }

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// JSON CTOR routine for a WatchdogConfiguration structure
        /// </summary>
        [JsonConstructor]
        public WatchdogConfiguration()
        {
            // Store default values for the configuration now
            this.WatchdogPath = string.Empty;
            this.WatchdogAction = new(() => { });
            this.FileExtensions = new string[] { };
        }
        /// <summary>
        /// Spawns a new configuration for a watchdog service
        /// </summary>
        /// <param name="WatchdogPath">The path to monitor for our service</param>
        /// <param name="WatchdogAction">The action our service will invoke</param>
        public WatchdogConfiguration(string WatchdogPath, Action WatchdogAction = null)
        {
            // Store configuration values and exit out
            this.WatchdogPath = WatchdogPath;
            this.WatchdogAction = WatchdogAction;
            this.FileExtensions = new[] { "*.*" };
        }
        /// <summary>
        /// Spawns a new configuration for a watchdog service
        /// </summary>
        /// <param name="WatchdogPath">The path to monitor for our service</param>
        /// <param name="FileExtension">The file extension we need to use for monitoring</param>
        /// <param name="WatchdogAction">The action our service will invoke</param>
        public WatchdogConfiguration(string WatchdogPath, string FileExtension, Action WatchdogAction = null)
        {
            // Store configuration values and exit out
            this.WatchdogPath = WatchdogPath;
            this.WatchdogAction = WatchdogAction;
            this.FileExtensions = new[] { FileExtension };
        }
        /// <summary>
        /// Spawns a new configuration for a watchdog service
        /// </summary>
        /// <param name="WatchdogPath">The path to monitor for our service</param>
        /// <param name="FileExtensions">The file extensions we need to use for monitoring</param>
        /// <param name="WatchdogAction">The action our service will invoke</param>
        public WatchdogConfiguration(string WatchdogPath, IEnumerable<string> FileExtensions, Action WatchdogAction = null)
        {
            // Store configuration values and exit out
            this.WatchdogPath = WatchdogPath;
            this.WatchdogAction = WatchdogAction;
            this.FileExtensions = FileExtensions.ToArray();
        }
    }
}
