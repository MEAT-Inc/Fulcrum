using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FulcrumSupport;
using SharpLogging;
using FulcrumService;
using NLog.Targets;

namespace FulcrumWatchdogService.WatchdogServiceModels
{
    /// <summary>
    /// Class structure for a watched file instance inside a watched directory
    /// </summary>
    public class WatchdogFile : IDisposable
    {
        #region Custom Events

        // Events for states of the file object instance
        public event EventHandler<WatchdogFileEventArgs> FileChanged;
        public event EventHandler<FileAccessedEventArgs> FileAccessed;
        public event EventHandler<FileModifiedEventArgs> FileModified;

        /// <summary>
        /// Method to invoke when a new file changed event occurs
        /// </summary>
        /// <param name="EventArgs">Args fire along with this event</param>
        protected virtual void OnFileChanged(WatchdogFileEventArgs EventArgs)
        {
            // Invoke the event handler if it's not null and fire event to update our directory
            this.FileChanged?.Invoke(this, EventArgs);

            // Update our time values here
            if (!File.Exists(this.FullFilePath)) return;
            FileInfo NewFileInfos = new FileInfo(this.FullFilePath);
            this.TimeCreated = NewFileInfos.CreationTime;
            this.TimeAccessed = NewFileInfos.LastAccessTime;
            this.TimeModified = NewFileInfos.LastWriteTime;

            // Log the file event being processed
            _fileLogger.WriteLog($"PROCESSING A FILECHANGED (GENERIC) EVENT FOR FILE {this.FileName}", LogType.TraceLog);
        }
        /// <summary>
        /// Method to invoke when a new file changed event occurs
        /// </summary>
        /// <param name="EventArgs">Args fire along with this event</param>
        protected virtual void OnFileAccessed(FileAccessedEventArgs EventArgs)
        {
            // Invoke the event handler if it's not null
            this.OnFileChanged(EventArgs);
            this.FileAccessed?.Invoke(this, EventArgs);

            // Log file event being processed
            _fileLogger.WriteLog($"PROCESSING A FILEACCESSED EVENT FOR FILE {this.FileName}", LogType.TraceLog);
        }
        /// <summary>
        /// Method to invoke when a new file changed event occurs
        /// </summary>
        /// <param name="EventArgs">Args fire along with this event</param>
        protected virtual void OnFileModified(FileModifiedEventArgs EventArgs)
        {
            // Invoke the event handler if it's not null
            this.OnFileChanged(EventArgs);
            this.FileModified?.Invoke(this, EventArgs);

            // Log file event being processed
            _fileLogger.WriteLog($"PROCESSING A FILEMODIFIED EVENT FOR FILE {this.FileName}", LogType.TraceLog);
        }

        #endregion //Custom Events

        #region Fields

        // Logger object for this file instance
        private static SharpLogger _fileLogger;

        // Sets if we're watching this file or not 
        private int _refreshTime = 250;
        private CancellationToken _watchToken;
        private CancellationTokenSource _watchTokenSource;

        // Basic information about the file location
        public readonly string FileName;
        public readonly string FileFolder;
        public readonly string FullFilePath;
        public readonly string FileExtension;

        #endregion //Fields

        #region Properties

        // Public properties holding information about our currently watched file instance
        public bool IsMonitoring
        {
            get => this._watchTokenSource is { IsCancellationRequested: false };
            set
            {
                // If setting monitoring off, cancel the task to monitor and reset our cancellation source
                if (!value)
                {
                    // Reset the source object and cancel the task
                    this._watchTokenSource?.Cancel();
                    this._watchTokenSource = null;
                    return;
                }

                // If we're starting it up, then build a new task and run the operation to monitor
                // Setup new token objects for this task instance.
                this._watchTokenSource = new CancellationTokenSource();
                this._watchToken = this._watchTokenSource.Token;

                // Invoke our new task routine here for watching this file instance
                Task.Run(() =>
                {
                    // Start a new task which watches this file object and tracks the properties of it.
                    while (!this._watchTokenSource.IsCancellationRequested)
                    {
                        // Store a new file information object and wait for a given time period
                        FileInfo OldFileInfo = new FileInfo(this.FullFilePath);
                        Thread.Sleep(this._refreshTime);

                        // Attempt comparisons inside a try catch to avoid failures
                        try
                        {
                            // If the file was built or destroyed, let the directory object deal with it.
                            if (!this.FileExists || !OldFileInfo.Exists) return;

                            // If we're not dealing with a deleted or created object, then update values here
                            if (this.FileSize != OldFileInfo.Length)
                                this.OnFileChanged(new FileModifiedEventArgs(this));
                            else if (this.TimeAccessed != OldFileInfo.LastAccessTime)
                                this.OnFileAccessed(new FileAccessedEventArgs(this));
                            else if (this.TimeModified != OldFileInfo.LastWriteTime)
                                this.OnFileModified(new FileModifiedEventArgs(this));
                        }
                        catch (Exception CompareFilesEx)
                        {
                            // Catch the exception and log it out
                            if (CompareFilesEx is ObjectDisposedException) return;
                            _fileLogger?.WriteException(CompareFilesEx);
                        }
                    }
                }, this._watchToken);
            }
        }
        public bool FileExists
        {
            get
            {
                // Check if the file exists. If it doesn't dispose this object
                bool FileExists = File.Exists(this.FullFilePath);
                if (!FileExists) this.Dispose();

                // Return if the file is real or not
                return FileExists;
            }
        }

        // Public properties containing file size and access information
        public DateTime TimeCreated { get; private set; }
        public DateTime TimeModified { get; private set; }
        public DateTime TimeAccessed { get; private set; }

        // Public properties holding file Size information as a long value and a string formatted byte value
        public long FileSize => this.FileExists ? new FileInfo(this.FullFilePath).Length : 0;
        public string FileSizeString
        {
            get
            {
                // Check if the file is not real or no bytes are found
                if (!this.FileExists) return "File Not Found!";
                return this.FileSize == 0 ? "0 B" : this.FileSize.ToFileSize();
            }
        }

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Base class for a file event arg object to be fired when files are watched
        /// </summary>
        public class WatchdogFileEventArgs : EventArgs
        {
            // The sending file object and the time this event was fired
            public readonly WatchdogFile SendingFile;
            public readonly DateTime TimeEventSent;

            // String name of the file being updated
            public readonly string FileName;
            public readonly string FullFilePath;

            // --------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Builds a new instance of the file watchdog instance event args
            /// </summary>
            /// <param name="InputFile">File which sent out this event</param>
            public WatchdogFileEventArgs(WatchdogFile InputFile)
            {
                // Store values for our watched file on the instance and set the date time value
                this.SendingFile = InputFile;
                this.TimeEventSent = DateTime.Now;

                // Store file name and path values
                this.FileName = this.SendingFile.FileName;
                this.FullFilePath = this.SendingFile.FullFilePath;
            }
        }
        /// <summary>
        /// Event arguments for a file being modified
        /// </summary>
        public class FileModifiedEventArgs : WatchdogFileEventArgs
        {
            // Information for the file exists or not
            public readonly bool FileExists;

            // File size information
            public readonly long FileSize;
            public readonly string FileSizeString;

            // --------------------------------------------------------------------------------------------------------------------------------------

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
        /// <summary>
        /// Event arguments for a file being accessed
        /// </summary>
        public class FileAccessedEventArgs : WatchdogFileEventArgs
        {
            // Time the file was accessed
            public readonly DateTime TimeAccessed;

            // --------------------------------------------------------------------------------------------------------------------------------------

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

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Disposal method for cleaning up the resources of this object when done being used
        /// </summary>
        public void Dispose()
        {
            // Cancel the refresh task and dispose the token source
            this._watchTokenSource?.Cancel();
            this._watchTokenSource?.Dispose();

            // Log disposing and exit out
            // _fileLogger?.WriteLog($"DISPOSING LOGGER FOR FILE INSTANCE {this.FileName}", LogType.TraceLog);
        }
        /// <summary>
        /// Converts this file object into a formatted string output which contains all the information about this file
        /// </summary>
        /// <returns>A String which has the file name and all properties of the file instance</returns>
        public override string ToString()
        {
            try
            {
                // Convert this file object into a text table object and return the output of it.
                string OutputFileString =
                    $"Watchdog File: {this.FileName}\n" +
                    $"\t\\__ File Path:      {this.FullFilePath}\n" +
                    $"\t\\__ File Exists:    {(this.FileExists ? "Yes" : "No")}\n" +
                    $"\t\\__ File Size:      {this.FileSizeString}\n" +
                    $"\t\\__ File Extension: {this.FileExtension}\n" +
                    $"\t\\__ Time Created:   {this.TimeCreated:G}\n" +
                    $"\t\\__ Time Modified:  {this.TimeModified:G}\n" +
                    $"\t\\__ Time Accessed:  {this.TimeAccessed:G}\n";

                // Return the built string output
                return OutputFileString;
            }
            catch
            {
                // If this fails out, then just return the name of the path of the file
                return this.FullFilePath;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new watched file object and sets up the basic properties of the file instance
        /// </summary>
        /// <param name="FileToWatch">The path for our file to track on this class instance</param>
        /// <param name="ThrowOnMissing">Throws an exception on construction if the file does not exist when true</param>
        public WatchdogFile(string FileToWatch, bool ThrowOnMissing = false)
        {
            // Throw a new exception if the file to watch does not exist
            if (ThrowOnMissing && !File.Exists(FileToWatch))
                throw new FileNotFoundException($"ERROR! FILE: {FileToWatch} COULD NOT BE WATCHED SINCE IT DOES NOT EXIST!");

            // If our logger instance is null, build it now
            if (_fileLogger == null)
            {
                // Try and find an existing logger for the service instance first
                var LocatedLogger = SharpLogBroker.FindLoggers($"{nameof(FulcrumWatchdog)}_FileLogger").FirstOrDefault();
                if (LocatedLogger != null)
                {
                    // Store the found logger and write we've built it out here
                    _fileLogger = LocatedLogger;
                    _fileLogger.WriteLog("STORED STATIC FILE LOGGER INSTANCE FOR OUR WATCHDOGS OK!", LogType.InfoLog);
                }
                else
                {
                    // Spawn our logger and register targets to it for the needed outputs
                    _fileLogger = new SharpLogger(LoggerActions.FileLogger, $"{nameof(FulcrumWatchdog)}_FolderLogger");
                    _fileLogger.WriteLog("REGISTERED AND BUILT NEW LOGGER FOR WATCHDOG FILE OPERATIONS OK!", LogType.InfoLog);
                }
            }

            try
            {
                // Store file path location information and setup refreshing routines for properties
                this.FullFilePath = FileToWatch;
                this.FileName = Path.GetFileName(this.FullFilePath);
                this.FileExtension = Path.GetExtension(this.FullFilePath);
                this.FileFolder = Path.GetDirectoryName(this.FullFilePath);
            }
            catch (Exception SetFileInfoEx)
            {
                // Catch the exception and log it out
                _fileLogger?.WriteException(SetFileInfoEx);
                return;
            }

            try
            {
                // If the file exists, then we set up the time values and size information
                if (!this.FileExists) return;
                FileInfo WatchedFileInfo = new FileInfo(this.FullFilePath);
                this.TimeCreated = WatchedFileInfo.CreationTime;
                this.TimeModified = WatchedFileInfo.LastWriteTime;
                this.TimeAccessed = WatchedFileInfo.LastAccessTime;
            }
            catch (Exception SetFileTimeEx)
            {
                // Catch the exception and log it out
                _fileLogger?.WriteException(SetFileTimeEx);
                return;
            }

            try
            {
                // Now try and start monitoring our file instance here
                this.IsMonitoring = true;
            }
            catch (Exception SetMonitoringStateEx)
            {
                // Catch the exception and log it out
                _fileLogger?.WriteException(SetMonitoringStateEx);
                return;
            }
        }
    }
}