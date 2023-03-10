using System;
using FulcrumInjector.FulcrumViewSupport.FulcrumWatchdog;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.WatchdogModels
{
    /// <summary>
    /// Event arguments for a file being accessed
    /// </summary>
    internal class FileAccessedEventArgs : FileEventArgs 
    {
        // Time the file was accessed
        public readonly DateTime TimeAccessed;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of the file watchdog instance event args
        /// </summary>
        /// <param name="InputFile">File which sent out this event</param>
        public FileAccessedEventArgs(WatchdogFile InputFile) : base(InputFile)
        {
            // Now set the file access information
            this.TimeAccessed = InputFile.TimeAccessed;
        }
    }
}
