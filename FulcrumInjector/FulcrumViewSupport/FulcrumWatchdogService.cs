using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.WatchdogModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using NLog.Targets;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// The actual service base component used for the injector watchdog helper
    /// </summary>
    internal class FulcrumWatchdogService : ServiceBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Service instance logger object for this watchdog session
        private readonly SharpLogger _watchdogLogger;

        // Private backing fields for our watchdog service configuration
        private readonly IContainer _components = null;                 // Component objects used by this service instance
        private List<WatchdogFolder> _watchedDirectories;               // The watchdog folder objects built for this service

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
        public FulcrumWatchdogService()
        {
            // Init our component object here and setup logging
            this._components = new Container();
            this._watchedDirectories = new List<WatchdogFolder>();
            this.ServiceName = ValueLoaders.GetConfigValue<string>("FulcrumWatchdog.ServiceName");
            this._watchdogLogger = new SharpLogger(LoggerActions.CustomLogger, $"{this.ServiceName}_Logger");

            // Build and register a new watchdog logging target here for a file and the console
            var WatchdogFileTarget = LocateWatchdogFileTarget();
            this._watchdogLogger.RegisterTarget(WatchdogFileTarget);
            
            // Log we're building this new service and log out the name we located for it
            this._watchdogLogger.WriteLog("SPAWNING NEW WATCHDOG SERVICE!", LogType.InfoLog);
            this._watchdogLogger.WriteLog($"PULLED IN A NEW SERVICE NAME OF {this.ServiceName}", LogType.InfoLog);
            this._watchdogLogger.WriteLog("FULCRUM INJECTOR WATCHDOG SERVICE HAS BEEN BUILT AND IS READY TO RUN!", LogType.InfoLog);
        }
        /// <summary>
        /// Spawns or locates a watchdog service instance
        /// </summary>
        /// <returns>The watchdog service built or located from the system</returns>
        public static ServiceController LocateWatchdogService()
        {
            // Try and find our service instance first based on the name of it
            string WatchdogServiceName = ValueLoaders.GetConfigValue<string>("FulcrumWatchdog.ServiceName");
            SharpLogger ServiceLogger = new SharpLogger(LoggerActions.UniversalLogger, "WatchdogAllocationLogger");
            ServiceLogger.WriteLog("ATTEMPTING TO FIND A CURRENTLY RUNNING INSTANCE OF OUR INJECTOR WATCHDOG NOW...", LogType.InfoLog);

            // Find a service instance here. If one exists, return it out now. Otherwise spawn a new one
            var LocatedService = ServiceController.GetServices().FirstOrDefault(Service => Service.ServiceName == WatchdogServiceName);
            if (LocatedService != null)
            {
                // Log we found our service instance, set our status value and exit out
                ServiceLogger.WriteLog($"FOUND WATCHDOG SERVICE OK!", LogType.InfoLog);
                ServiceLogger.WriteLog($"--> SERVICE DISPLAY NAME: {LocatedService.ServiceName}");
                ServiceLogger.WriteLog($"--> SERVICE STATUS VALUE: {LocatedService.Status}");

                // Store the service status and return the controller for it
                return LocatedService;
            }

            // Since no service was found, spawn in a new service controller and return that out
            ServiceLogger.WriteLog("WARNING! NO WATCHDOG SERVICE WAS FOUND! SPAWNING A NEW ONE NOW...", LogType.InfoLog);

            // Spawn a new service instance and get it from our system
            Run(new FulcrumWatchdogService());
            LocatedService = ServiceController.GetServices().FirstOrDefault(Service => Service.ServiceName == WatchdogServiceName);
            if (LocatedService == null) throw new ServiceActivationException("Error! Failed to spawn a new Watchdog Service!");

            // Log we found our service instance, set our status value and exit out
            ServiceLogger.WriteLog($"FOUND NEWLY BOOTED WATCHDOG SERVICE OK!", LogType.InfoLog);
            ServiceLogger.WriteLog($"--> SERVICE DISPLAY NAME: {LocatedService.ServiceName}");
            ServiceLogger.WriteLog($"--> SERVICE STATUS VALUE: {LocatedService.Status}");
            return LocatedService;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Attempts to add in a set of new folder values for the given service
        /// </summary>
        /// <param name="FoldersToWatch">The folder objects we wish to watch</param>
        public void AddWatchedFolders(params WatchdogFolder[] FoldersToWatch)
        {
            // Loop all the passed folder objects and add/update them one by one
            this._watchdogLogger.WriteLog("ATTEMPTING TO REGISTER NEW FOLDERS ON A WATCHDOG SERVICE!", LogType.WarnLog);
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
                    this._watchdogLogger.WriteLog($"--> STORED NEW CONFIGURATION FOR FOLDER {FolderToAdd.WatchedDirectoryPath}!\n{FolderToAdd.ToString()}", LogType.TraceLog);
                }
                else
                {
                    // Find the index of our folder we're replacing on our service
                    int IndexOfFolder = this._watchedDirectories.IndexOf(LocatedFolder);

                    // Now spawn a new folder and log that we've built a new one now
                    this._watchedDirectories[IndexOfFolder].Dispose();
                    this._watchedDirectories[IndexOfFolder] = FolderToAdd;
                    this._watchdogLogger.WriteLog($"--> UPDATED CONFIGURATION AND RESET WATCHDOG OBJECTS FOR FOLDER {FolderToAdd.WatchedDirectoryPath}!\n{FolderToAdd}", LogType.TraceLog);
                }
            }
            
            // Log that we've looped all our values correctly and exit out
            this._watchdogLogger.WriteLog("UPDATED AND STORED ALL NEEDED FOLDER CONFIGURATIONS WITHOUT ISSUES!", LogType.InfoLog);
        }
        /// <summary>
        /// Attempts to remove a set of new folder values for the given service
        /// </summary>
        /// <param name="FoldersToRemove">The folder objects we wish to stop watching</param>
        public void RemoveWatchedFolders(params WatchdogFolder[] FoldersToRemove)
        {
            // Loop all the passed folder objects and remove them one by one
            this._watchdogLogger.WriteLog("ATTEMPTING TO REMOVE EXISTING FOLDERS FROM A WATCHDOG SERVICE!", LogType.WarnLog);
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
                    this._watchdogLogger.WriteLog($"--> CONFIGURATION PATH {FolderToRemove.WatchedDirectoryPath} WAS NOT FOUND!", LogType.TraceLog);
                    this._watchdogLogger.WriteLog("--> THIS IS NORMAL WHEN A REMOVAL REQUEST IS RUN ON A PATH THAT ISN'T BEING WATCHED!", LogType.TraceLog);
                }
                else
                {
                    // Find the index of our folder we're replacing on our service
                    int IndexOfFolder = this._watchedDirectories.IndexOf(LocatedFolder);
                    this._watchedDirectories[IndexOfFolder].Dispose();
                    this._watchedDirectories.RemoveAt(IndexOfFolder);

                    // Log we've removed this configuration and move on to our next folder
                    this._watchdogLogger.WriteLog($"--> REMOVED CONFIGURATION AND RESET WATCHDOG OBJECTS FOR FOLDER {FolderToRemove.WatchedDirectoryPath}!", LogType.InfoLog);
                }
            }

            // Log that we've looped all our values correctly and exit out
            this._watchdogLogger.WriteLog("UPDATED AND REMOVED ALL NEEDED FOLDER CONFIGURATIONS WITHOUT ISSUES!", LogType.InfoLog);
        }

        /// <summary>
        /// Instance/debug startup method for the Watchdog service
        /// </summary>
        public void StartWatchdogService()
        {
            // Check if we want to block execution of the service once we've kicked it off or not
            this._watchdogLogger.WriteLog("INVOKING AN OnStart METHOD FOR OUR WATCHDOG SERVICE...", LogType.WarnLog);
            
            // Boot the service instance now
            this.OnStart(null);
            if (!ValueLoaders.GetConfigValue<bool>("FulcrumWatchdog.WaitForBoot")) return;

            // If we want to block after booting, log that information and do so now using an endless while loop
            this._watchdogLogger.WriteLog("WARNING! BLOCKING EXECUTION SINCE OUR WAIT FOR BOOT FLAG IS SET TO TRUE!", LogType.WarnLog);
            while (true) continue;
        }
        /// <summary>
        /// Instance/debug custom command method for the Watchdog service.
        /// This allows us to run custom actions on the watchdog service in real time if we've defined them here
        /// </summary>
        /// <param name="ServiceCommand">The int value of the command we're trying to run (130 is the help command)</param>
        public void InvokeCustomCommand(int ServiceCommand)
        {
            // Invoke the service command and exit out
            this._watchdogLogger.WriteLog("INVOKING AN OnCustomCommand METHOD FOR OUR WATCHDOG SERVICE...", LogType.WarnLog);
            this.OnCustomCommand(ServiceCommand);
        }
        /// <summary>
        /// Instance/debug shutdown/stop method for the Watchdog service
        /// </summary>
        public void StopWatchdogService()
        {
            // Stop the service instance here if possible. This should only be done when it's running
            this._watchdogLogger.WriteLog("INVOKING AN OnStop METHOD FOR OUR WATCHDOG SERVICE...", LogType.WarnLog);
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

                // Now using the configuration file, load in our predefined folders to monitor
                var WatchedConfigs = ValueLoaders.GetConfigValue<WatchdogFolder[]>("FulcrumWatchdog.WatchedFolders");
                this._watchdogLogger.WriteLog($"LOADED IN A TOTAL OF {WatchedConfigs.Length} WATCHED PATH VALUES! IMPORTING PATH VALUES NOW...");
                this._watchdogLogger.WriteLog("IMPORTED PATH CONFIGURATIONS WILL BE LOGGED BELOW", LogType.TraceLog);

                // Clear out any previous configurations/folders and add our new ones now
                this._watchedDirectories = new List<WatchdogFolder>();
                this.AddWatchedFolders(WatchedConfigs);
                
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
                this._watchdogLogger.WriteLog("FULCRUM INJECTOR WATCHDOG SERVICE HAS BEEN SHUT DOWN WITHOUT ISSUES!", LogType.InfoLog);
            }
            catch (Exception StopWatchdogEx)
            {
                // Log out the failure and exit this method
                this._watchdogLogger.WriteLog("ERROR! FAILED TO SHUTDOWN EXISTING WATCHDOG SERVICE INSTANCE!", LogType.ErrorLog);
                this._watchdogLogger.WriteException($"EXCEPTION THROWN FROM THE STOP ROUTINE IS LOGGED BELOW", StopWatchdogEx);
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures the logger for this generator to output to a custom file path
        /// </summary>
        /// <returns>The configured sharp logger instance</returns>
        internal static FileTarget LocateWatchdogFileTarget()
        {
            // Make sure our output location exists first
            string OutputFolder = Path.Combine(SharpLogBroker.LogFileFolder, "WatchdogLogs");
            if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);
        
            // Configure our new logger name and the output log file path for this logger instance 
            string ServiceLoggerTime = SharpLogBroker.LogFileName.Split('_').Last().Split('.')[0];
            string ServiceLoggerName = $"FulcrumWatchdog_ServiceLogging_{ServiceLoggerTime}";
            string OutputFileName = Path.Combine(OutputFolder, $"{ServiceLoggerName}.log");
            if (File.Exists(OutputFileName)) File.Delete(OutputFileName);
        
            // Spawn the new generation logger and attach in a new file target for it
            var ExistingTarget = SharpLogBroker.LoggingTargets.FirstOrDefault(LoggerTarget => LoggerTarget.Name == ServiceLoggerName);
            if (ExistingTarget is FileTarget LocatedFileTarget) return LocatedFileTarget; 
        
            // Spawn the new generation logger and attach in a new file target for it
            string LayoutString = SharpLogBroker.DefaultFileFormat.LoggerFormatString;
            FileTarget ServiceFileTarget = new FileTarget(ServiceLoggerName)
            {
                KeepFileOpen = false,           // Allows multiple programs to access this file
                Layout = LayoutString,          // The output log line layout for the logger
                ConcurrentWrites = true,        // Allows multiple writes at one time or not
                FileName = OutputFileName,      // The name/full log file being written out
                Name = ServiceLoggerName,       // The name of the logger target being registered
            };
        
            // Return the output logger object built
            return ServiceFileTarget;
        } }
}
