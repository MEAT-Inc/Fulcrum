using System.Collections.Generic;
using FulcrumService;

namespace FulcrumWatchdogService.WatchdogServiceModels
{
    /// <summary>
    /// Model object for our watchdog service settings configuration
    /// </summary>
    public class WatchdogSettings : FulcrumServiceSettings
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public facing properties for our watchdog configuration
        public int ExecutionGap { get; set; }                       // Sets the minimum time between executions of the watchdog helper
        public bool WatchdogEnabled { get; set; }                   // Sets if the watchdog is enabled for this session
        public List<WatchdogFolder> WatchedFolders { get; set; }    // List of folders being watched for the service

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes
    }
}
