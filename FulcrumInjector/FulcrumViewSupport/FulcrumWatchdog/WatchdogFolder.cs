using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using NLog;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumWatchdog
{
    /// <summary>
    /// Host type for a file watchdog helper
    /// Looks at a defined directory and performs actions when files are located inside of it
    /// </summary>
    internal class WatchdogFolder : IDisposable
    {
        #region Custom Events

        /// <summary>
        /// Event used to process the addition or removal of file objects from our list of files being checked on the directory instance
        /// </summary>
        /// <param name="SendingWatcher">Sending watcher object</param>
        /// <param name="EventArgs">Events fired with this call</param>
        /// <exception cref="IndexOutOfRangeException">Thrown when the file to remove can not be found</exception>
        private void OnWatchdogFolderChanged(object SendingWatcher, FileSystemEventArgs EventArgs)
        {
            // Get the name of our new file object and clear out files that no longer exist
            var FilesToRemove = this._watchedFiles.Where(FileObj => FileObj.FullFilePath == EventArgs.FullPath);
            foreach (var FileToRemove in FilesToRemove) this._watchedFiles.Remove(FileToRemove);

            // Switch the type of event being processed on our instance and fire actions accordingly
            if (EventArgs.ChangeType is WatcherChangeTypes.Changed or WatcherChangeTypes.Renamed)
                this._watchedFiles.Add(new WatchdogFile(EventArgs.FullPath));

            // Invoke the watchdog action here to refresh our contents
            if (this.WatchdogAction == null) return;
            Task.Run(() => this._watchdogAction.Invoke());
        }

        #endregion //Custom Events

        #region Fields

        // Logger object used for this watchdog instance
        private readonly SharpLogger _watchdogLogger;

        // Backing fields holding information about our watched directory and the watcher itself
        private Action _watchdogAction;                                      // The action to run when events are fired
        private readonly string _watchedDirectory;                           // The path we're watching on the system
        private readonly string[] _watchedFileFilters;                       // The filters for our paths on the system
        private readonly List<FileSystemWatcher> _directoryWatchers;         // The file system watcher to track file changes
        private readonly ObservableCollection<WatchdogFile> _watchedFiles;   // Collection of our watched file objects.

        #endregion //Fields

        #region Properties

        // Public facing properties about our watched directory
        public string WatchedDirectoryPath => this._watchedDirectory;
        public string WatchedDirectoryName => Path.GetDirectoryName(this._watchedDirectory);

        // Public facing filter set and directory information for the watched path
        public bool IsMonitoring
        {
            get => this._directoryWatchers.All(WatcherObj => WatcherObj.EnableRaisingEvents);
            private set
            {
                // Set all the watcher states here
                foreach (FileSystemWatcher WatcherObject in this._directoryWatchers)
                    WatcherObject.EnableRaisingEvents = value;
            }
        }
        public WatchdogFile[] WatchedFiles => this._watchedFiles.ToArray();
        public DirectoryInfo WatchedDirectoryInfo => new(this._watchedDirectory);
        public IEnumerable<string> WatchedFileFilters => this._watchedFileFilters;

        // Public facing property which holds the action to invoke when the folder contents are changed around
        public Action WatchdogAction
        {
            get => this._watchdogAction ?? (() => this._watchdogLogger?.WriteLog($"NO ACTION CONFIGURED FOR FOLDER {this.WatchedDirectoryPath}!", LogType.WarnLog));
            set
            {
                // Store the new action value and log out we've set a new action routine
                this._watchdogAction = value;
                if (this._watchdogAction == null) this._watchdogLogger.WriteLog($"WARNING! NO ACTION IS SET FOR DIRECTORY {this.WatchedDirectoryName}!", LogType.WarnLog);
                else 
                {
                    // Log out that we've built a new action to set on this folder instance
                    this._watchdogLogger.WriteLog($"STORED NEW ACTION FOR DIRECTORY {this.WatchedDirectoryName}!", LogType.InfoLog);
                    this._watchdogLogger.WriteLog("THIS EVENT WILL FIRE WHENEVER EVENTS ARE CALLED FROM THIS WATCHDOG", LogType.InfoLog);
                }
            }
        }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Disposal method for cleaning up the resources of this object when done being used
        /// </summary>
        public void Dispose()
        {
            // Dispose the process and watcher objects first
            foreach (FileSystemWatcher WatcherObject in this._directoryWatchers) WatcherObject.Dispose();
            this._watchdogLogger.WriteLog($"DISPOSED ALL FILESYSTEM WATCHERS FOR WATCHDOG INSTANCE {this.WatchedDirectoryName}", LogType.TraceLog);

            // Now dispose all the objects on our collection list
            for (int FileIndex = 0; FileIndex < this._watchedFiles.Count; FileIndex++)
            {
                // Dispose the file and null it out
                this._watchedFiles[FileIndex].Dispose();
                this._watchedFiles[FileIndex] = null;
            }

            // Clear our list of files
            this._watchedFiles.Clear();
            this._watchdogLogger.WriteLog($"DISPOSED ALL FILE INSTANCES FOR WATCHDOG INSTANCE {this.WatchedDirectoryName}", LogType.TraceLog);
        }
        /// <summary>
        /// Converts this file directory into a formatted text table which contains all the information about the files inside of it
        /// </summary>
        /// <returns>A text table holding all the information about each file inside of it</returns>
        public override string ToString()
        {
            try
            {
                // Setup a list of values to show in our table and then build our output table object
                string[] TableHeaders = new[] { "File", "Exists", "File Size", "Extension", "Time Created", "Time Accessed", "Time Modified", };
                Tuple<string, string, string, string, string, string, string>[] FileValues = this.WatchedFiles.Select(FileObj =>
                {
                    // Get the values of out file object here
                    string FileName = FileObj.FileName;
                    string ExistsString = FileObj.FileExists ? "YES" : "NO";
                    string FileSize = FileObj.FileSizeString;
                    string FileExtension = FileObj.FileExtension;
                    string TimeCreated = FileObj.TimeCreated.ToString("G");
                    string TimeAccessed = FileObj.TimeAccessed.ToString("G");
                    string TimeModified = FileObj.TimeModified.ToString("G");

                    // Build our new output tuple here
                    return new Tuple<string, string, string, string, string, string, string>(
                        FileName, ExistsString, FileSize, FileExtension, TimeCreated, TimeAccessed, TimeModified
                    );
                }).ToArray();

                // Get our output table object here and append the name of the folder at the top along with a file count
                string TableOutput = FileValues.Length == 0
                    ? $"No Files In Directory {this.WatchedDirectoryPath}!"
                    : FileValues.ToStringTable(
                        TableHeaders,
                        FileObj => FileObj.Item1,
                        FileObj => FileObj.Item2,
                        FileObj => FileObj.Item3,
                        FileObj => FileObj.Item4,
                        FileObj => FileObj.Item5,
                        FileObj => FileObj.Item6,
                        FileObj => FileObj.Item7
                    );

                // Now build our final output string values and return the content generated
                if (TableOutput.Split('\n').Length == 1) return TableOutput;
                List<string> TableOutputSplit = TableOutput.Split('\n').ToList();
                string[] InformationLines = new[]
                {
                    $"| Directory Name:          {this.WatchedDirectoryName}",
                    $"| Directory Path:          {this.WatchedDirectoryPath}",
                    $"| Directory Types:         {string.Join(", ", this.WatchedFileFilters)}",
                    $"| Directory Size:          {this._watchedFiles.Sum(FileObj => FileObj.FileSize).ToFileSize()}",
                    $"| Directory File Count:    {this._watchedFiles.Count} Files",
                    $"| Directory Monitoring:    {(this.IsMonitoring ? "On" : "Off")}"
                };

                // Format the strings to be the right length value to pad evenly
                InformationLines = InformationLines.Select(StringValue =>
                {
                    // Find the size difference first and build the padding string
                    int DifferenceInSize = TableOutputSplit[0].Length - StringValue.Length - 1;
                    string PaddingString = string.Join(string.Empty, Enumerable.Repeat(" ", DifferenceInSize));

                    // Add the padding string with a trailing | and return out
                    return StringValue + PaddingString + "|";
                }).ToArray();

                // Insert a heading line at the start of the information lines and store it on our table output list content
                TableOutputSplit.InsertRange(0, InformationLines.Prepend(TableOutputSplit[0]).ToArray());
                string TableOutputWithInfo = string.Join("\n", TableOutputSplit);

                // Return the built output string for our table instance
                return TableOutputWithInfo;
            }
            catch
            {
                // If this fails out, then just return the name of the path of the folder
                return this.WatchedDirectoryPath;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for a new file watchdog host helper
        /// </summary>
        /// <param name="WatchedDirectory">The path being monitored by this watchdog helper</param>
        /// <param name="WatchdogFilters">Optional filters used to filter what files we're watching</param>
        public WatchdogFolder(string WatchedDirectory, params string[] WatchdogFilters)
        {
            // Store our configuration values first
            this._watchedFileFilters = WatchdogFilters.ToArray();
            this._watchedDirectory = Path.GetFullPath(WatchedDirectory);

            // Spawn a new logger based on the watched path name
            string LoggerName = Path.GetDirectoryName(WatchedDirectory);
            this._watchdogLogger = new SharpLogger(LoggerActions.UniversalLogger, LoggerName);
            this._watchdogLogger.WriteLog($"SPAWNED NEW WATCHDOG LOGGER FOR DIRECTORY {this._watchedDirectory}!", LogType.InfoLog);
            this._watchdogLogger.WriteLog($"FILE FILTERS STORED: {string.Join(", ", this._watchedFileFilters)}");
            this._watchdogLogger.WriteLog("BUILDING NEW FILESYSTEM WATCHERS FOR THE FILERS REQUESTED NOW...");

            // Spawn our new base collection objects here
            this._directoryWatchers = new List<FileSystemWatcher>();
            this._watchedFiles = new ObservableCollection<WatchdogFile>();
            this._watchdogLogger.WriteLog("BUILT NEW COLLECTIONS FOR WATCHDOG FILES AND FILESYSTEM WATCHERS OK!", LogType.InfoLog);

            // Loop all of the supported extension types and build watcher objects for each of them
            foreach (string FileExtension in this._watchedFileFilters.Select(ExtValue => $"*{ExtValue}"))
            {
                FileSystemWatcher NextWatcher = new FileSystemWatcher(this.WatchedDirectoryPath);
                NextWatcher.EnableRaisingEvents = true;
                NextWatcher.IncludeSubdirectories = false;
                NextWatcher.Filter = FileExtension;
                NextWatcher.NotifyFilter =
                    NotifyFilters.Attributes | NotifyFilters.CreationTime |
                    NotifyFilters.DirectoryName | NotifyFilters.FileName |
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite |
                    NotifyFilters.Security | NotifyFilters.Size;

                // Event ties for changes and modified files we can't process directly here
                // These three event types can't be compared inside our file objects because they would require pointless
                // duplicate assignments. So the solution is to tie these changes into the execute watchdog action
                NextWatcher.Created += OnWatchdogFolderChanged;
                NextWatcher.Deleted += OnWatchdogFolderChanged;
                NextWatcher.Renamed += OnWatchdogFolderChanged;

                // Log what extension was just watched
                this._directoryWatchers.Add(NextWatcher);
            }

            // Now store our new watchdog file instances for this path location
            this._watchdogLogger.WriteLog("ATTEMPTING TO LOAD AND CREATE NEW WATCHDOG FILE INSTANCES NOW...");
            foreach (string FilePath in Directory.GetFiles(this.WatchedDirectoryPath))
            {
                // Find the extension of this file and make sure we support it first
                if (!this.WatchedFileFilters.Contains(Path.GetExtension(FilePath))) continue;

                // Build a new file, store the execute routine as an event handler, and add it onto our list
                this._watchedFiles.Add(new WatchdogFile(FilePath));
                this._watchdogLogger.WriteLog($"--> ADDED NEW WATCHDOG FILE! FILE NAME {FilePath}", LogType.TraceLog);
            }

            // Log setup of this folder is now complete
            this._watchdogLogger.WriteLog($"CONFIGURED NEW WATCHDOG DIRECTORY {this.WatchedDirectoryName} OK!", LogType.InfoLog);
            this._watchdogLogger.WriteLog($"CONFIGURATION OF THIS INSTANCE IS BEING SHOWN BELOW:\n{this}", LogType.TraceLog);
        }
    }
}
