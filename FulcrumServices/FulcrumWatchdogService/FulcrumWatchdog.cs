﻿using System;
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

        // Public property holding all of our watched folders
        public List<WatchdogFolder> WatchedDirectories
        {
            // Pull the value from our service host or the local instance based on client configuration
            get => !this.IsServiceClient
                ? this._watchedDirectories
                : this.GetPipeMemberValue(nameof(WatchedDirectories)) as List<WatchdogFolder>;

            private set
            {
                // Check if we're using a service client or not and set the value accordingly
                if (!this.IsServiceClient)
                {
                    // Set our value and exit out
                    this._watchedDirectories = value;
                    return;
                }

                // If we're using a client instance, invoke a pipe routine
                if (!this.SetPipeMemberValue(nameof(WatchedDirectories), value))
                    throw new InvalidOperationException($"Error! Failed to update pipe member {nameof(WatchedDirectories)}!");
            }
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
            // Check if we're consuming this service instance or not
            if (this.IsServiceClient)
            {
                // If we're a client, just log out that we're piping commands across to our service and exit out
                this._serviceLogger.WriteLog("WARNING! WATCHDOG SERVICE IS BEING BOOTED IN CLIENT CONFIGURATION!", LogType.WarnLog);
                this._serviceLogger.WriteLog("ALL COMMANDS/ROUTINES EXECUTED ON THE DRIVE SERVICE WILL BE INVOKED USING THE HOST SERVICE!", LogType.WarnLog);
                return;
            }

            // Log we're building this new service and log out the name we located for it
            this._serviceLogger.WriteLog("SPAWNING NEW WATCHDOG SERVICE!", LogType.InfoLog);
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
            if (!ServiceConfig.ServiceEnabled) {
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
                    _serviceInitLogger.WriteLog("SPAWNED NEW INJECTOR WATCHDOG SERVICE OK!", LogType.InfoLog);

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
                // Log out what type of service is being configured currently
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
                this.AddWatchedFolders(this._serviceConfig.WatchedFolders);

                // Log booted service without issues here and exit out of this routine
                this._serviceLogger.WriteLog($"BOOTED A NEW FILE WATCHDOG SERVICE FOR {this.WatchedDirectories.Count} DIRECTORY OBJECTS OK!", LogType.InfoLog);
            }
            catch (Exception StartWatchdogEx)
            {
                // Log out the failure and exit this method
                this._serviceLogger.WriteLog("ERROR! FAILED TO BOOT NEW WATCHDOG SERVICE INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE START ROUTINE IS LOGGED BELOW", StartWatchdogEx);
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
        public bool AddWatchedFolders(List<WatchdogFolder> FoldersToWatch)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(AddWatchedFolders), FoldersToWatch);
                return bool.Parse(PipeAction.PipeCommandResult.ToString());
            }

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
            return true; 
        }
        /// <summary>
        /// Attempts to remove a set of new folder values for the given service
        /// </summary>
        /// <param name="FoldersToRemove">The folder objects we wish to stop watching</param>
        public bool RemoveWatchedFolders(List<WatchdogFolder> FoldersToRemove)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(RemoveWatchedFolders), FoldersToRemove);
                return bool.Parse(PipeAction.PipeCommandResult.ToString());
            }

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
            return true;
        }
    }
}