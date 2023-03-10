using System;
using FulcrumInjector.FulcrumViewSupport.FulcrumWatchdog;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.WatchdogModels
{
    /// <summary>
    /// Base class for a file event arg object to be fired when files are watched
    /// </summary>
    internal class FileEventArgs : EventArgs
    {
        // The sending file object and the time this event was fired
        public readonly WatchdogFile SendingFile;
        public readonly DateTime TimeEventSent;

        // String name of the file being updated
        public readonly string FileName;
        public readonly string FullFilePath;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of the file watchdog instance event args
        /// </summary>
        /// <param name="InputFile">File which sent out this event</param>
        public FileEventArgs(WatchdogFile InputFile)
        {
            // Store values for our watched file on the instance and set the date time value
            this.SendingFile = InputFile;
            this.TimeEventSent = DateTime.Now;

            // Store file name and path values
            this.FileName = this.SendingFile.FileName;
            this.FullFilePath = this.SendingFile.FullFilePath;
        }
}
}
