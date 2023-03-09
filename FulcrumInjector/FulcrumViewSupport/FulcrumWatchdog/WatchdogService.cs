using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumWatchdog
{
    /// <summary>
    /// The actual service base component used for the injector watchdog helper
    /// </summary>
    internal class WatchdogService : ServiceBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Service instance logger object for this watchdog session
        private readonly SharpLogger _watchdogLogger;

        // Private backing fields for our watchdog service configuration
        private readonly IContainer _components = null;                 // Component objects used by this service instance
        private List<WatchdogFolder> _watchedDirectories;               // The watchdog folder objects built for this service
        private List<WatchdogConfiguration> _watchedFolderConfigs;      // The directory paths being watched by this service

        #endregion //Fields

        #region Properties

        // Public facing properties holding information about our folders being watched and all their files
        public WatchdogFolder[] WatchedDirectories
        {
            get => this._watchedDirectories.Where(WatchedDir => WatchedDir != null).ToArray();
            private set => this._watchedDirectories = value.Where(WatchedDir => WatchedDir != null).ToList();
        }
        public WatchdogFile[] WatchedFiles => this.WatchedDirectories
            .SelectMany(WatchedDir => WatchedDir.WatchedFiles)
            .OrderBy(WatchedFile => WatchedFile.FullFilePath)
            .ToArray();

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Simple structure used to help configure new Watchdog services
        /// </summary>
        public struct WatchdogConfiguration
        {
            // Public fields holding information about this service setup
            public readonly string WatchdogPath;          // Path to watch on this service
            public readonly string[] FileExtensions;      // Extensions being watched for this folder
            public readonly Action WatchdogAction;        // Action to execute when invoked 

            // --------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Spawns a new configuration for a watchdog service
            /// </summary>
            /// <param name="WatchdogPath">The path to monitor for our service</param>
            /// <param name="WatchdogAction">The action our service will invoke</param>
            public WatchdogConfiguration(string WatchdogPath, Action WatchdogAction = null)
            {
                // Store configuration values and exit out
                this.WatchdogPath = WatchdogPath;
                this.FileExtensions = new[] { "*.*" };
                this.WatchdogAction = WatchdogAction ?? (() => { });
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
                this.FileExtensions = new[] { FileExtension };
                this.WatchdogAction = WatchdogAction ?? (() => { });
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
                this.FileExtensions = FileExtensions.ToArray();
                this.WatchdogAction = WatchdogAction ?? (() => { });
            }
        }

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="IsDisposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool IsDisposing)
        {
            // Dispose our component collection and the base service
            if (IsDisposing && (_components != null)) _components.Dispose();
            base.Dispose(IsDisposing);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR routine for this watchdog service. Sets up our component object and our logger instance
        /// </summary>
        public WatchdogService()
        {
            // Init our component object here
            this._components = new Container();
            this.ServiceName = ValueLoaders.GetConfigValue<string>("FulcrumWatchdog.ServiceName");

            // Built main broker logger and write setup completed without issues
            this._watchdogLogger = new SharpLogger(LoggerActions.CustomLogger);
            this._watchdogLogger.WriteLog("FULCRUM INJECTOR WATCHDOG SERVICE IS BOOTING NOW...", LogType.InfoLog);
            this._watchdogLogger.WriteLog($"PULLED IN A NEW SERVICE NAME OF {this.ServiceName}", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Instance/debug startup method for the Watchdog service
        /// <param name="FoldersToWatch">Optional folder configurations to use for our service instance</param>
        /// </summary>
        public void StartWatchdogService(params WatchdogConfiguration[] FoldersToWatch)
        {
            // Store our folder instances and file extensions here
            this._watchedFolderConfigs = FoldersToWatch.Length == 0
                ? FoldersToWatch.ToList()
                : new List<WatchdogConfiguration>()
                    { new(SharpLogBroker.LogFileFolder, "*.log") };

            // Now fire off our service instance
            this.OnStart(null);
        }
        /// <summary>
        /// Instance/debug custom command method for the Watchdog service.
        /// This allows us to run custom actions on the watchdog service in real time if we've defined them here
        /// </summary>
        /// <param name="ServiceCommand">The int value of the command we're trying to run (130 is the help command)</param>
        public void InvokeCustomCommand(int ServiceCommand)
        {
            // Invoke the service command and exit out
            this.OnCustomCommand(ServiceCommand);
        }
        /// <summary>
        /// Instance/debug shutdown/stop method for the Watchdog service
        /// </summary>
        public void StopWatchdogService()
        {
            // Stop the service instance here if possible. This should only be done when it's running
            this.OnStop();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts the service up and builds a watchdog helper process
        /// </summary>
        /// <param name="WatchedFolders"></param>
        protected override void OnStart(string[] WatchedFolders)
        {
            try 
            {
                // Check our folder arguments and store them on our instance now. If no content is found, use our default values
                this._watchdogLogger.WriteLog($"CONFIGURING NEW FOLDER WATCHDOG OBJECTS FOR INJECTOR SERVICE...", LogType.InfoLog);
                
                // Loop all of our folder instances and store them on our service instance
                for (int WatchdogIndex = 0; WatchdogIndex < this._watchedDirectories.Count; WatchdogIndex++)
                {
                    // Make sure this instance is not null before trying to use it
                    if (this._watchedDirectories[WatchdogIndex] == null) continue;

                    // Dispose the file instance and null it out
                    this._watchedDirectories[WatchdogIndex].Dispose();
                    this._watchedDirectories[WatchdogIndex] = null;
                }
                foreach (var WatchdogConfiguration in this._watchedFolderConfigs)
                {
                    // Build our new watchdog and store it on our class
                    this._watchedDirectories.Add(new WatchdogFolder(WatchdogConfiguration.WatchdogPath, WatchdogConfiguration.FileExtensions));
                    this._watchdogLogger.WriteLog($"--> BUILT NEW WATCHDOG FOLDER FOR PATH: {WatchdogConfiguration}");
                }

                // Log booted service without issues here and exit out of this routine
                this._watchdogLogger.WriteLog($"BOOTED A NEW FILE WATCHDOG SERVICE FOR {this.WatchedDirectories.Length} DIRECTORY OBJECTS OK!", LogType.InfoLog);
            }
            catch (Exception StartWatchdogEx)
            {
                // Log out the failure and exit this method
                this._watchdogLogger.WriteLog("ERROR! FAILED TO BOOT NEW WATCHDOG SERVICE INSTANCE!", LogType.ErrorLog);
                this._watchdogLogger.WriteException($"EXCEPTION THROWN FROM THE START ROUTINE IS LOGGED BELOW", StartWatchdogEx);
            }
        }
        /// <summary>
        /// Invokes a custom command routine for our service based on the int code provided to it.
        /// </summary>
        /// <param name="ServiceCommand">The command to execute on our service instance (128-255)</param>
        protected override void OnCustomCommand(int ServiceCommand)
        {
            try 
            {
                // Check what type of command is being executed and perform actions accordingly.
                switch (ServiceCommand)
                {
                    // For any other command value or something that is not recognized
                    case 128:

                        // Log out the command help information for the user to read in the log file.
                        this._watchdogLogger.WriteLog("----------------------------------------------------------------------------------------------------------------", LogType.InfoLog);
                        this._watchdogLogger.WriteLog($"                                     FulcrumInjector Watchdog Command Help", LogType.InfoLog);
                        this._watchdogLogger.WriteLog($"- The provided command value of {ServiceCommand} is reserved to show this help message.", LogType.InfoLog);
                        this._watchdogLogger.WriteLog($"- Enter any command number above 128 to execute an action on our service instance.", LogType.InfoLog);
                        this._watchdogLogger.WriteLog($"- Execute this command again with the service command ID 128 to get a list of all possible commands", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("Help Commands", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("   Command 128:  Displays this help message", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("Variable Actions", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("   Command 130:  Turns verbose logging on or off in our log files for the watched directories", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("   Command 131:  Reloads the configuration file on our service and imports all folders again", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("   Command 132:  (Not Built Yet) Adding a new directory instance to our service host", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("   Command 133:  (Not Built Yet) Removing an existing directory instance from our service host", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("Information Commands", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("   Command 140:  Prints out the names of each path being watched", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("   Command 141:  Prints out the information about all folders being watched", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("   Command 142:  Prints out the names of every file being watched in every folder as a list", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("Execution Commands", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("   Command 150:  Force executes the watchdog refresh action on all currently built folders", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("----------------------------------------------------------------------------------------------------------------", LogType.InfoLog);
                        return;

                    // For importing the configuration file again
                    case 131:
                        // Log reimporting contents now
                        this._watchdogLogger.WriteLog("RELOADING CONFIGURATION FILE AND IMPORTING DIRECTORY ENTRIES NOW...", LogType.InfoLog);

                        // Clear out the existing folder instances first then build a new configuration file instance.
                        for (int WatchdogIndex = 0; WatchdogIndex < this._watchedDirectories.Count; WatchdogIndex++)
                        {
                            // Make sure this instance is not null before trying to use it
                            if (this._watchedDirectories[WatchdogIndex] == null) continue;

                            // Dispose the file instance and null it out
                            this._watchedDirectories[WatchdogIndex].Dispose();
                            this._watchedDirectories[WatchdogIndex] = null;
                        }

                        // Now reimport our configuration file and start this process over again
                        this._watchdogLogger.WriteLog("DISPOSED AND CLEARED OUT OLD LISTED DIRECTORIES ON THIS SERVICE!", LogType.InfoLog);
                        this._watchdogLogger.WriteLog("RESTARTING SERVICE INSTANCE NOW...", LogType.InfoLog);
                        this.OnStart(null);
                        return;

                    // For printing out the names of our folders
                    case 140:
                        // Log printing out names now
                        this._watchdogLogger.WriteLog("GETTING AND DISPLAYING FULCRUM INJECTOR WATCHDOG DIRECTORY NAMES NOW...", LogType.InfoLog);

                        // Loop the paths and log them all out now.
                        for (int DirIndex = 0; DirIndex < this._watchedDirectories.Count; DirIndex++)
                        {
                            // Build a string value to print out from the content of each folder
                            string WatchedPath = this._watchedDirectories[DirIndex].WatchedDirectoryPath;
                            this._watchdogLogger.WriteLog($"\t--> [FORCED] ::: DIRECTORY (INDEX {DirIndex}) -- {WatchedPath}", LogType.InfoLog);
                        }

                        // Log done refreshing all folders and exit out
                        this._watchdogLogger.WriteLog($"COMPLETED LISTING OF ALL WATCHED DIRECTORY NAMES. TOTAL OF {this._watchedDirectories.Count} DIRECTORIES WERE FOUND", LogType.InfoLog);
                        return;

                    // For the print folders command
                    case 141:
                        // Log printing out tables now.
                        this._watchdogLogger.WriteLog("GETTING AND DISPLAYING FULCRUM INJECTOR WATCHDOG DIRECTORIES AS TABLES NOW...", LogType.InfoLog);

                        // Loop all the folders and one by one refresh them.
                        foreach (WatchdogFolder WatchedDirectory in this._watchedDirectories)
                        {
                            // Format the output string to be tabbed in one level
                            string[] SplitFolderStrings = WatchedDirectory
                                .ToString().Split('\n')
                                .Select(StringPart => $"\t{StringPart.Trim()}").ToArray();

                            // Now write the formatted string content out
                            this._watchdogLogger.WriteLog(string.Join("\n", SplitFolderStrings) + "\n");
                        }

                        // Once all entries are printed, log done and exit out
                        this._watchdogLogger.WriteLog($"PRINTED OUT ALL INFORMATION FOR REQUESTED DIRECTORIES OK!", LogType.InfoLog);
                        return;

                    // For printing out every file name directly without the table formatting
                    case 142:
                        // Log printing out tables now.
                        this._watchdogLogger.WriteLog("GETTING AND DISPLAYING FULCRUM INJECTOR WATCHDOG FILES AS A LIST NOW...", LogType.InfoLog);

                        // Get all the file instances we need now.
                        Tuple<string, string>[] FilesBeingWatched = this._watchedDirectories.Select(DirObj =>
                        {
                            // Store the name of the folder and the name of the file in question
                            string FolderName = DirObj.WatchedDirectoryName;
                            string[] FileNames = DirObj.WatchedFiles
                                .Select(FileObj => FileObj.FullFilePath)
                                .ToArray();

                            // Build a list of tuples to return out
                            Tuple<string, string>[] FilesForDir = FileNames
                                .Select(FilePath => new Tuple<string, string>(FolderName, FilePath))
                                .ToArray();

                            // Return the tuple sets here
                            return FilesForDir;
                        }).SelectMany(TupleSet => TupleSet).ToArray();

                        // Now loop each file and print it out to the console.
                        foreach (Tuple<string, string> FileAndDirPair in FilesBeingWatched)
                            this._watchdogLogger.WriteLog($"\t--> DIRECTORY: {FileAndDirPair.Item1} | FILE: {FileAndDirPair.Item2}", LogType.InfoLog);

                        // Once all entries are printed, log done and exit out
                        this._watchdogLogger.WriteLog($"PRINTED OUT ALL INFORMATION FOR ALL REQUESTED FILE ENTRIES OK!", LogType.InfoLog);
                        return;


                    // To force execute a new refresh routine on the service
                    case 150:
                        // Log starting to perform refresh operations
                        this._watchdogLogger.WriteLog("FORCE INVOKING A REFRESH ROUTINE ON ALL DIRECTORIES NOW...", LogType.InfoLog);

                        // Loop all the folders and one by one refresh them.
                        foreach (WatchdogFolder WatchedDirectory in this._watchedDirectories)
                        {
                            // Log done once the refresh action has completed
                            WatchedDirectory.ExecuteWatchdogAction();
                            this._watchdogLogger.WriteLog($"\t--> [FORCED] ::: DIRECTORY {WatchedDirectory.WatchedDirectoryName} ROUTINE EXECUTED!", LogType.InfoLog);
                        }

                        // Log done refreshing all folders and exit out
                        this._watchdogLogger.WriteLog($"COMPLETED ALL REFRESH ROUTINES NEEDED FOR ALL REQUESTED DIRECTORIES!", LogType.InfoLog);
                        return;
                }
            }
            catch (Exception SendCustomCommandEx)
            {
                // Log out the failure and exit this method
                this._watchdogLogger.WriteLog("ERROR! FAILED TO INVOKE A CUSTOM COMMAND ON AN EXISTING WATCHDOG SERVICE INSTANCE!", LogType.ErrorLog);
                this._watchdogLogger.WriteException($"EXCEPTION THROWN FROM THE CUSTOM COMMAND ROUTINE IS LOGGED BELOW", SendCustomCommandEx);
            }
        }
        /// <summary>
        /// Stops the service and runs cleanup routines
        /// </summary>
        protected override void OnStop()
        {
            try 
            {
                // Log done building and prepare to get our input directories
                this._watchdogLogger.WriteLog("FULCRUM INJECTOR WATCHDOG SERVICE IS SHUTTING DOWN NOW...", LogType.InfoLog);

                // Dispose all our directories here
                for (int WatchdogIndex = 0; WatchdogIndex < this._watchedDirectories.Count; WatchdogIndex++)
                {
                    // Make sure this instance is not null before trying to use it
                    if (this._watchedDirectories[WatchdogIndex] == null) continue;

                    // Dispose the file instance and null it out
                    this._watchedDirectories[WatchdogIndex].Dispose();
                    this._watchedDirectories[WatchdogIndex] = null;
                }

                // Clear out the list here and log closing out now
                this._watchedDirectories.Clear();
                this._watchedFolderConfigs.Clear();
                this._watchdogLogger.WriteLog("FULCRUM INJECTOR WATCHDOG SERVICE HAS BEEN SHUT DOWN WITHOUT ISSUES!", LogType.InfoLog);
            }
            catch (Exception StopWatchdogEx)
            {
                // Log out the failure and exit this method
                this._watchdogLogger.WriteLog("ERROR! FAILED TO SHUTDOWN EXISTING WATCHDOG SERVICE INSTANCE!", LogType.ErrorLog);
                this._watchdogLogger.WriteException($"EXCEPTION THROWN FROM THE STOP ROUTINE IS LOGGED BELOW", StopWatchdogEx);
            }
        }
    }
}
