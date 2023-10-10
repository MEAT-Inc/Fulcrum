using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.WatchdogModels
{
    /// <summary>
    /// Model object for our watchdog service settings configuration
    /// </summary>
    internal class WatchdogSettings
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public facing properties for our watchdog configuration
        public int ExecutionGap { get; set; }                       // Sets the minimum time between executions of the watchdog helper
        public bool WatchdogEnabled { get; set; }                   // Sets if the watchdog is enabled for this session
        public string ServiceName { get; set; }                     // Stores the name of the injector watchdog service
        public List<WatchdogFolder> WatchedFolders { get; set; }    // List of folders being watched for the service

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes
    }
}
