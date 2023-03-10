using FulcrumInjector.FulcrumViewSupport.FulcrumWatchdog;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.WatchdogModels
{
    /// <summary>
    /// Event arguments for a file being modified
    /// </summary>
    internal class FileModifiedEventArgs : FileEventArgs
    {
        // Information for the file exists or not
        public readonly bool FileExists;

        // File size information
        public readonly long FileSize;
        public readonly string FileSizeString;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of the file watchdog instance event args
        /// </summary>
        /// <param name="InputFile">File which sent out this event</param>
        public FileModifiedEventArgs(WatchdogFile InputFile) : base(InputFile)
        {
            // Setup basic information for this file object event
            this.FileExists = InputFile.FileExists;

            // Now set the file size information
            this.FileSize = InputFile.FileSize;
            this.FileSizeString = InputFile.FileSizeString;
        }
    }
}
