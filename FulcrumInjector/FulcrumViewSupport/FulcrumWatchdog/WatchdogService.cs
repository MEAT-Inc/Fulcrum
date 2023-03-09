using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
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
        private IContainer _components = null;                  // Component objects used by this service instance
        private List<WatchdogFolder> _watchedDirectories;       // The watchdog folder objects built for this service
        private readonly List<string> _watchedExtensions;       // The extensions we wish to watch in this service
        private readonly List<string> _watchedFolderPaths;      // The directory paths being watched by this service

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
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            // Dispose our component collection and the base service
            if (disposing && (_components != null)) _components.Dispose();
            base.Dispose(disposing);
        }
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // Spawn our new components container here
            _components = new Container();
            this.ServiceName = "FulcrumWatchdog";
        }

        /// <summary>
        /// CTOR routine for this watchdog service
        /// </summary>
        public WatchdogService(IEnumerable<string> WatchedFolders, params string[] WatchedExtensions)
        {
            // Init our component object here
            InitializeComponent();

            // Built main broker logger and write setup completed without issues
            this._watchdogLogger = new SharpLogger(LoggerActions.CustomLogger);
            this._watchdogLogger.WriteLog("FULCRUM INJECTOR WATCHDOG SERVICE IS BOOTING NOW...", LogType.InfoLog);

            // Store our folder instances and file extensions here
            this._watchedFolderPaths = WatchedFolders.Any()
                ? WatchedFolders.ToList()
                : new List<string>() { SharpLogBroker.LogFileFolder };
            this._watchedExtensions = WatchedExtensions.Length == 0
                ? WatchedExtensions.ToList()
                : new List<string>() { ".*" };

            // Log out each folder instance being watched now and then exit out
            this._watchdogLogger.WriteLog($"WATCHING A TOTAL OF {this._watchedFolderPaths.Count} DIRECTORIES! LOGGING THEM BELOW");
            foreach (var WatchedPath in this._watchedFolderPaths) this._watchdogLogger.WriteLog($"--> WATCHED PATH: {WatchedPath}");

            // Log out each folder instance being watched now and then exit out
            this._watchdogLogger.WriteLog($"WATCHING A TOTAL OF {this._watchedExtensions.Count} EXTENSIONS!");
            this._watchdogLogger.WriteLog($"WATCHED EXTENSIONS: {string.Join(", ", this._watchedExtensions)}");
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts the service up and builds a watchdog helper process
        /// </summary>
        /// <param name="WatchedFolders"></param>
        protected override void OnStart(string[] WatchedFolders)
        {
            // Check our folder arguments and store them on our instance now. If no content is found, use our default values
            this._watchdogLogger.WriteLog($"CONFIGURING NEW FOLDER WATCHDOG OBJECTS FOR INJECTOR SERVICE...", LogType.InfoLog);
            this._watchedFolderPaths.AddRange(WatchedFolders.Where(FolderPath => !this._watchedFolderPaths.Contains(FolderPath)));

            // Loop all of our folder instances and store them on our service instance
            for (int WatchdogIndex = 0; WatchdogIndex < this._watchedDirectories.Count; WatchdogIndex++)
            {
                // Make sure this instance is not null before trying to use it
                if (this._watchedDirectories[WatchdogIndex] == null) continue;

                // Dispose the file instance and null it out
                this._watchedDirectories[WatchdogIndex].Dispose();
                this._watchedDirectories[WatchdogIndex] = null;
            }
            foreach (var WatchedPath in this._watchedFolderPaths)
            {
                // Build our new watchdog and store it on our class
                this._watchedDirectories.Add(new WatchdogFolder(WatchedPath));
                this._watchdogLogger.WriteLog($"--> BUILT NEW WATCHDOG FOLDER FOR PATH: {WatchedPath}");
            }

            // Log booted service without issues here and exit out of this routine
            this._watchdogLogger.WriteLog($"BOOTED A NEW FILE WATCHDOG SERVICE FOR {this.WatchedDirectories.Length} DIRECTORY OBJECTS OK!", LogType.InfoLog);
        }
        /// <summary>
        /// Invokes a custom command routine for our service based on the int code provided to it.
        /// </summary>
        /// <param name="ServiceCommand">The command to execute on our service instance (128-255)</param>
        protected override void OnCustomCommand(int ServiceCommand)
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
                        WatchedDirectory.WatchdogAction.Invoke();
                        this._watchdogLogger.WriteLog($"\t--> [FORCED] ::: DIRECTORY {WatchedDirectory.WatchedDirectoryName} ROUTINE EXECUTED!", LogType.InfoLog);
                    }

                    // Log done refreshing all folders and exit out
                    this._watchdogLogger.WriteLog($"COMPLETED ALL REFRESH ROUTINES NEEDED FOR ALL REQUESTED DIRECTORIES!", LogType.InfoLog);
                    return;
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
                this._watchedFolderPaths.Clear();
                this._watchdogLogger.WriteLog("FULCRUM INJECTOR WATCHDOG SERVICE HAS BEEN SHUT DOWN WITHOUT ISSUES!", LogType.InfoLog);
            }
            catch (Exception DisposeFolderEx)
            {
                // Log the exception and continue on
                this._watchdogLogger.WriteLog("ERROR! AN EXCEPTION OCCURRED DURING CLEANUP OF THE WATCHDOG SERVICE!", LogType.ErrorLog);
                this._watchdogLogger.WriteException("EXCEPTION THROWN IS BEING SHOWN BELOW", DisposeFolderEx, LogType.ErrorLog);
            }
        }
    }
}
