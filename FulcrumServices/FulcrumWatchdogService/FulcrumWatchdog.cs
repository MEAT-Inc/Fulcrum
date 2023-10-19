using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FulcrumJson;
using FulcrumService;
using FulcrumWatchdogService.WatchdogServiceModels;
using SharpLogging;

namespace FulcrumWatchdogService
{
    /// <summary>
    /// The actual service base component used for the injector watchdog helper
    /// </summary>
    public partial class FulcrumWatchdog : FulcrumServiceBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields for our updater service instance
        private static FulcrumWatchdog _serviceInstance;        // Instance of our service object
        private static readonly object _serviceLock = new();    // Lock object for building service instances

        // Private backing fields for our watchdog service configuration
        private WatchdogSettings _serviceConfig;             // Settings configuration for the service
        private List<WatchdogFolder> _watchedDirectories;    // Watched folders for the service

        #endregion //Fields

        #region Properties

        // Public facing properties holding information about our folders being watched and all their files
        public WatchdogFile[] WatchedFiles => this.WatchedDirectories
            .SelectMany(WatchedDir => WatchedDir.WatchedFiles)
            .OrderBy(WatchedFile => WatchedFile.FullFilePath)
            .ToArray();
        public WatchdogFolder[] WatchedDirectories
        {
            get => this._watchedDirectories.Where(WatchedDir => WatchedDir != null).ToArray();
            private set => this._watchedDirectories = value.Where(WatchedDir => WatchedDir != null).ToList();
        }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR routine for this watchdog service. Sets up our component object and our logger instance
        /// </summary>
        /// <param name="ServiceSettings">Optional settings object for our service configuration</param>
        internal FulcrumWatchdog(WatchdogSettings ServiceSettings = null) : base(ServiceTypes.WATCHDOG_SERVICE)
        {
            // Build and register a new watchdog logging target here for a file and the console
            this.ServiceLoggingTarget = LocateServiceFileTarget<FulcrumWatchdog>();
            this._serviceLogger.RegisterTarget(this.ServiceLoggingTarget);

            // Log we're building this new service and log out the name we located for it
            this._serviceLogger.WriteLog("SPAWNING NEW DRIVE SERVICE!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"PULLED IN A NEW SERVICE NAME OF {this.ServiceName}", LogType.InfoLog);

            // Init our component object here and setup logging
            this._serviceConfig = ServiceSettings ?? ValueLoaders.GetConfigValue<WatchdogSettings>("FulcrumServices.FulcrumWatchdogService");
            this._serviceLogger.WriteLog("PULLED BASE SERVICE CONFIGURATION VALUES CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"SERVICE NAME: {this._serviceConfig.ServiceName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SERVICE ENABLED: {this._serviceConfig.ServiceEnabled}", LogType.TraceLog);

            // Log out information about our configuration values here 
            this._serviceLogger.WriteLog("PULLED WATCHDOG CONFIGURATION VALUES CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"FOLDER COUNT: {this._serviceConfig.WatchedFolders.Count}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"EXECUTION GAP: {this._serviceConfig.ExecutionGap}", LogType.TraceLog);

            // Log that we've stored the basic configuration for our watchdog service here and exit out
            this._serviceLogger.WriteLog("FULCRUM INJECTOR WATCHDOG SERVICE HAS BEEN BUILT AND IS READY TO RUN!", LogType.InfoLog);
        }
        /// <summary>
        /// Static CTOR for the watchdog service which builds and configures a new watchdog service
        /// </summary>
        /// <param name="ForceInit">When true, we force rebuild the requested service instance</param>
        /// <returns>The built and configured watchdog helper service</returns>
        public static Task<FulcrumWatchdog> InitializeWatchdogService(bool ForceInit = false)
        {
            // Make sure we actually want to use this watchdog service 
            WatchdogSettings ServiceConfig = ValueLoaders.GetConfigValue<WatchdogSettings>("FulcrumServices.FulcrumWatchdogService");
            if (!ServiceConfig.WatchdogEnabled) {
                _serviceInitLogger.WriteLog("WARNING! WATCHDOG SERVICE IS TURNED OFF IN OUR CONFIGURATION FILE! NOT BOOTING IT", LogType.WarnLog);
                return null;
            }

            // Spin up a new injector drive service here if needed           
            _serviceInitLogger.WriteLog($"SPAWNING A NEW WATCHDOG SERVICE INSTANCE NOW...", LogType.WarnLog);
            return Task.Run(() =>
            {
                // Lock our service object for thread safe operations
                lock (_serviceLock)
                {
                    // Check if we need to force rebuilt this service or not
                    if (_serviceInstance != null && !ForceInit) {
                        _serviceInitLogger.WriteLog("FOUND EXISTING WATCHDOG SERVICE INSTANCE! RETURNING IT NOW...");
                        return _serviceInstance;
                    }

                    // Build and boot a new service instance for our watchdog
                    _serviceInstance = new FulcrumWatchdog(ServiceConfig);
                    _serviceInstance.OnStart(null);
                    _serviceInitLogger.WriteLog("BOOTED NEW INJECTOR WATCHDOG SERVICE OK!", LogType.InfoLog);

                    // Return the service instance here
                    return _serviceInstance;
                }
            });
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts the service up and builds a watchdog helper process
        /// </summary>
        /// <param name="StartupArgs">NOT USED!</param>
        protected override void OnStart(string[] StartupArgs)
        {
            try
            {
                // Check our folder arguments and store them on our instance now. If no content is found, use our default values
                this._serviceLogger.WriteLog($"BOOTING NEW {this.GetType().Name} SERVICE NOW...", LogType.WarnLog);
                this._serviceLogger.WriteLog($"CONFIGURING NEW FOLDER WATCHDOG OBJECTS FOR INJECTOR SERVICE...", LogType.InfoLog);

                // Loop all of our folder instances and store them on our service instance
                this._watchedDirectories ??= new List<WatchdogFolder>();
                for (int WatchdogIndex = 0; WatchdogIndex < this._watchedDirectories.Count; WatchdogIndex++)
                {
                    // Make sure this instance is not null before trying to use it
                    if (this._watchedDirectories[WatchdogIndex] == null) continue;

                    // Dispose the file instance and null it out
                    this._watchedDirectories[WatchdogIndex].Dispose();
                    this._watchedDirectories[WatchdogIndex] = null;
                }

                // Now using the configuration file, load in our predefined folders to monitor
                this._serviceLogger.WriteLog($"LOADED IN A TOTAL OF {this._serviceConfig.WatchedFolders.Count} WATCHED PATH VALUES! IMPORTING PATH VALUES NOW...");
                this._serviceLogger.WriteLog("IMPORTED PATH CONFIGURATIONS WILL BE LOGGED BELOW", LogType.TraceLog);

                // Clear out any previous configurations/folders and add our new ones now
                this._watchedDirectories = new List<WatchdogFolder>();
                this.AddWatchedFolders(this._serviceConfig.WatchedFolders.ToArray());

                // Log booted service without issues here and exit out of this routine
                this._serviceLogger.WriteLog($"BOOTED A NEW FILE WATCHDOG SERVICE FOR {this.WatchedDirectories.Length} DIRECTORY OBJECTS OK!", LogType.InfoLog);
            }
            catch (Exception StartWatchdogEx)
            {
                // Log out the failure and exit this method
                this._serviceLogger.WriteLog("ERROR! FAILED TO BOOT NEW WATCHDOG SERVICE INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE START ROUTINE IS LOGGED BELOW", StartWatchdogEx);
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
                        this._serviceLogger.WriteLog("----------------------------------------------------------------------------------------------------------------", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"                                     FulcrumInjector Watchdog Command Help", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"- The provided command value of {ServiceCommand} is reserved to show this help message.", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"- Enter any command number above 128 to execute an action on our service instance.", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"- Execute this command again with the service command ID 128 to get a list of all possible commands", LogType.InfoLog);
                        this._serviceLogger.WriteLog("", LogType.InfoLog);
                        this._serviceLogger.WriteLog("Help Commands", LogType.InfoLog);
                        this._serviceLogger.WriteLog("   Command 128:  Displays this help message", LogType.InfoLog);
                        this._serviceLogger.WriteLog("", LogType.InfoLog);
                        this._serviceLogger.WriteLog("Variable Actions", LogType.InfoLog);
                        this._serviceLogger.WriteLog("   Command 131:  Reloads the configuration file on our service and imports all folders again", LogType.InfoLog);
                        this._serviceLogger.WriteLog("   Command 132:  (Not Built Yet) Adding a new directory instance to our service host", LogType.InfoLog);
                        this._serviceLogger.WriteLog("   Command 133:  (Not Built Yet) Removing an existing directory instance from our service host", LogType.InfoLog);
                        this._serviceLogger.WriteLog("", LogType.InfoLog);
                        this._serviceLogger.WriteLog("Information Commands", LogType.InfoLog);
                        this._serviceLogger.WriteLog("   Command 140:  Prints out the names of each path being watched", LogType.InfoLog);
                        this._serviceLogger.WriteLog("   Command 141:  Prints out the information about all folders being watched", LogType.InfoLog);
                        this._serviceLogger.WriteLog("   Command 142:  Prints out the names of every file being watched in every folder as a list", LogType.InfoLog);
                        this._serviceLogger.WriteLog("", LogType.InfoLog);
                        this._serviceLogger.WriteLog("Execution Commands", LogType.InfoLog);
                        this._serviceLogger.WriteLog("   Command 150:  Force executes the watchdog refresh action on all currently built folders", LogType.InfoLog);
                        this._serviceLogger.WriteLog("----------------------------------------------------------------------------------------------------------------", LogType.InfoLog);
                        return;

                    // For importing the configuration file again
                    case 131:
                        // Log reimporting contents now
                        this._serviceLogger.WriteLog("RELOADING CONFIGURATION FILE AND IMPORTING DIRECTORY ENTRIES NOW...", LogType.InfoLog);

                        // Loop all of our folder instances and store them on our service instance
                        for (int WatchdogIndex = 0; WatchdogIndex < this._watchedDirectories.Count; WatchdogIndex++)
                        {
                            // Make sure this instance is not null before trying to use it
                            if (this._watchedDirectories[WatchdogIndex] == null) continue;

                            // Dispose the file instance and null it out
                            this._watchedDirectories[WatchdogIndex].Dispose();
                            this._watchedDirectories[WatchdogIndex] = null;
                        }

                        // Now reimport our configuration file and start this process over again
                        this._serviceLogger.WriteLog("DISPOSED AND CLEARED OUT OLD LISTED DIRECTORIES ON THIS SERVICE!", LogType.InfoLog);
                        this._serviceLogger.WriteLog("RESTARTING SERVICE INSTANCE NOW...", LogType.InfoLog);
                        this.OnStart(null);
                        return;

                    // For printing out the names of our folders
                    case 140:
                        // Log printing out names now
                        this._serviceLogger.WriteLog("GETTING AND DISPLAYING FULCRUM INJECTOR WATCHDOG DIRECTORY NAMES NOW...", LogType.InfoLog);

                        // Loop the paths and log them all out now.
                        for (int DirIndex = 0; DirIndex < this._watchedDirectories.Count; DirIndex++)
                        {
                            // Build a string value to print out from the content of each folder
                            string WatchedPath = this._watchedDirectories[DirIndex].WatchedDirectoryPath;
                            this._serviceLogger.WriteLog($"\t--> [FORCED] ::: DIRECTORY (INDEX {DirIndex}) -- {WatchedPath}", LogType.InfoLog);
                        }

                        // Log done refreshing all folders and exit out
                        this._serviceLogger.WriteLog($"COMPLETED LISTING OF ALL WATCHED DIRECTORY NAMES. TOTAL OF {this._watchedDirectories.Count} DIRECTORIES WERE FOUND", LogType.InfoLog);
                        return;

                    // For the print folders command
                    case 141:
                        // Log printing out tables now.
                        this._serviceLogger.WriteLog("GETTING AND DISPLAYING FULCRUM INJECTOR WATCHDOG DIRECTORIES AS TABLES NOW...", LogType.InfoLog);

                        // Loop all the folders and one by one refresh them.
                        foreach (WatchdogFolder WatchedDirectory in this._watchedDirectories)
                        {
                            // Format the output string to be tabbed in one level
                            string[] SplitFolderStrings = WatchedDirectory
                                .ToString().Split('\n')
                                .Select(StringPart => $"\t{StringPart.Trim()}").ToArray();

                            // Now write the formatted string content out
                            this._serviceLogger.WriteLog(string.Join("\n", SplitFolderStrings) + "\n");
                        }

                        // Once all entries are printed, log done and exit out
                        this._serviceLogger.WriteLog($"PRINTED OUT ALL INFORMATION FOR REQUESTED DIRECTORIES OK!", LogType.InfoLog);
                        return;

                    // For printing out every file name directly without the table formatting
                    case 142:
                        // Log printing out tables now.
                        this._serviceLogger.WriteLog("GETTING AND DISPLAYING FULCRUM INJECTOR WATCHDOG FILES AS A LIST NOW...", LogType.InfoLog);

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
                            this._serviceLogger.WriteLog($"\t--> DIRECTORY: {FileAndDirPair.Item1} | FILE: {FileAndDirPair.Item2}", LogType.InfoLog);

                        // Once all entries are printed, log done and exit out
                        this._serviceLogger.WriteLog($"PRINTED OUT ALL INFORMATION FOR ALL REQUESTED FILE ENTRIES OK!", LogType.InfoLog);
                        return;


                    // To force execute a new refresh routine on the service
                    case 150:
                        // Log starting to perform refresh operations
                        this._serviceLogger.WriteLog("FORCE INVOKING A REFRESH ROUTINE ON ALL DIRECTORIES NOW...", LogType.InfoLog);

                        // Loop all the folders and one by one refresh them.
                        foreach (WatchdogFolder WatchedDirectory in this._watchedDirectories)
                        {
                            // Log done once the refresh action has completed
                            WatchedDirectory.ExecuteWatchdogAction();
                            this._serviceLogger.WriteLog($"\t--> [FORCED] ::: DIRECTORY {WatchedDirectory.WatchedDirectoryName} ROUTINE EXECUTED!", LogType.InfoLog);
                        }

                        // Log done refreshing all folders and exit out
                        this._serviceLogger.WriteLog($"COMPLETED ALL REFRESH ROUTINES NEEDED FOR ALL REQUESTED DIRECTORIES!", LogType.InfoLog);
                        return;
                }
            }
            catch (Exception SendCustomCommandEx)
            {
                // Log out the failure and exit this method
                this._serviceLogger.WriteLog("ERROR! FAILED TO INVOKE A CUSTOM COMMAND ON AN EXISTING WATCHDOG SERVICE INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE CUSTOM COMMAND ROUTINE IS LOGGED BELOW", SendCustomCommandEx);
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
                this._serviceLogger.WriteLog("FULCRUM INJECTOR WATCHDOG SERVICE IS SHUTTING DOWN NOW...", LogType.InfoLog);

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
                this._serviceLogger.WriteLog("FULCRUM INJECTOR WATCHDOG SERVICE HAS BEEN SHUT DOWN WITHOUT ISSUES!", LogType.InfoLog);
            }
            catch (Exception StopWatchdogEx)
            {
                // Log out the failure and exit this method
                this._serviceLogger.WriteLog("ERROR! FAILED TO SHUTDOWN EXISTING WATCHDOG SERVICE INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE STOP ROUTINE IS LOGGED BELOW", StopWatchdogEx);
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Attempts to add in a set of new folder values for the given service
        /// </summary>
        /// <param name="FoldersToWatch">The folder objects we wish to watch</param>
        public void AddWatchedFolders(params WatchdogFolder[] FoldersToWatch)
        {
            // Loop all the passed folder objects and add/update them one by one
            this._serviceLogger.WriteLog("ATTEMPTING TO REGISTER NEW FOLDERS ON A WATCHDOG SERVICE!", LogType.WarnLog);
            foreach (var FolderToAdd in FoldersToWatch)
            {
                // Find if anything in our list exists like this currently
                var LocatedFolder = this.WatchedDirectories.FirstOrDefault(WatchedDir =>
                    WatchedDir.WatchedDirectoryPath == FolderToAdd.WatchedDirectoryPath);

                // If the index value exists, then just update this configuration
                if (LocatedFolder == null)
                {
                    // If no configuration was found, then just add it in to our collection
                    this._watchedDirectories.Add(FolderToAdd);
                    this._serviceLogger.WriteLog($"--> STORED NEW CONFIGURATION FOR FOLDER {FolderToAdd.WatchedDirectoryPath}!\n{FolderToAdd.ToString()}", LogType.TraceLog);
                }
                else
                {
                    // Find the index of our folder we're replacing on our service
                    int IndexOfFolder = this._watchedDirectories.IndexOf(LocatedFolder);

                    // Now spawn a new folder and log that we've built a new one now
                    this._watchedDirectories[IndexOfFolder].Dispose();
                    this._watchedDirectories[IndexOfFolder] = FolderToAdd;
                    this._serviceLogger.WriteLog($"--> UPDATED CONFIGURATION AND RESET WATCHDOG OBJECTS FOR FOLDER {FolderToAdd.WatchedDirectoryPath}!\n{FolderToAdd}", LogType.TraceLog);
                }
            }

            // Log that we've looped all our values correctly and exit out
            this._serviceLogger.WriteLog("UPDATED AND STORED ALL NEEDED FOLDER CONFIGURATIONS WITHOUT ISSUES!", LogType.InfoLog);
        }
        /// <summary>
        /// Attempts to remove a set of new folder values for the given service
        /// </summary>
        /// <param name="FoldersToRemove">The folder objects we wish to stop watching</param>
        public void RemoveWatchedFolders(params WatchdogFolder[] FoldersToRemove)
        {
            // Loop all the passed folder objects and remove them one by one
            this._serviceLogger.WriteLog("ATTEMPTING TO REMOVE EXISTING FOLDERS FROM A WATCHDOG SERVICE!", LogType.WarnLog);
            foreach (var FolderToRemove in FoldersToRemove)
            {
                // Find if anything in our list exists like this currently
                var LocatedFolder = this.WatchedDirectories.FirstOrDefault(WatchedDir =>
                    WatchedDir.WatchedDirectoryPath == FolderToRemove.WatchedDirectoryPath);

                // If the index value exists, then remove it from our collection
                if (LocatedFolder == null)
                {
                    // If no configuration was found, try and dispose any matching folders and move on
                    this._watchedDirectories.FirstOrDefault(DirObj => DirObj.WatchedDirectoryPath == FolderToRemove.WatchedDirectoryPath)?.Dispose();

                    // Log that no configuration was found and move onto our next folder path
                    this._serviceLogger.WriteLog($"--> CONFIGURATION PATH {FolderToRemove.WatchedDirectoryPath} WAS NOT FOUND!", LogType.TraceLog);
                    this._serviceLogger.WriteLog("--> THIS IS NORMAL WHEN A REMOVAL REQUEST IS RUN ON A PATH THAT ISN'T BEING WATCHED!", LogType.TraceLog);
                }
                else
                {
                    // Find the index of our folder we're replacing on our service
                    int IndexOfFolder = this._watchedDirectories.IndexOf(LocatedFolder);
                    this._watchedDirectories[IndexOfFolder].Dispose();
                    this._watchedDirectories.RemoveAt(IndexOfFolder);

                    // Log we've removed this configuration and move on to our next folder
                    this._serviceLogger.WriteLog($"--> REMOVED CONFIGURATION AND RESET WATCHDOG OBJECTS FOR FOLDER {FolderToRemove.WatchedDirectoryPath}!", LogType.InfoLog);
                }
            }

            // Log that we've looped all our values correctly and exit out
            this._serviceLogger.WriteLog("UPDATED AND REMOVED ALL NEEDED FOLDER CONFIGURATIONS WITHOUT ISSUES!", LogType.InfoLog);
        }
    }
}
