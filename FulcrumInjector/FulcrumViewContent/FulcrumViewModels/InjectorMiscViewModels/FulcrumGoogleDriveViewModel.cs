using SharpLogging;
using SharpSimulator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using static FulcrumInjector.FulcrumViewSupport.FulcrumUpdater;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels.DriveModels;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Google.Apis.Services;
using Newtonsoft.Json;
using Octokit.Internal;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels
{
    /// <summary>
    /// View model for the Google Drive viewing content used throughout the Injector application
    /// </summary>
    internal class FulcrumGoogleDriveViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for google drive explorer
        private DriveService _driveService;                                 // The service used to navigate our google drive

        // Private backing field for refresh timer and progress
        private double _refreshProgress;                                    // Progress for refresh routines
        private Stopwatch _refreshTimer;                                    // Timer used to track refresh duration

        // Private backing field for the collection of loaded logs 
        private List<DriveLogFileSet> _locatedLogFolders;   // Collection of all loaded log files found
        private List<DriveLogFileModel> _locatedLogFiles;   // Collection of all loaded log files found

        // Private backing fields for filtering collections
        private List<string> _yearFilters;                  // Years we can filter by 
        private List<string> _makeFilters;                  // Makes we can filter by
        private List<string> _modelFilters;                 // Models we can filter by

        #endregion // Fields

        #region Properties

        // Public property for refresh timer
        public Stopwatch RefreshTimer { get => this._refreshTimer; set => PropertyUpdated(value); }
        public double RefreshProgress { get => this._refreshProgress; set => PropertyUpdated(value); }

        // Public facing properties holding our collection of log files loaded
        public List<DriveLogFileModel> LocatedLogFiles { get => this._locatedLogFiles; set => PropertyUpdated(value); }
        public List<DriveLogFileSet> LocatedLogFolders { get => this._locatedLogFolders; set => PropertyUpdated(value); }

        // Public facing properties holding our different filter lists for the file filtering configuration
        public List<string> YearFilters { get => this._yearFilters; set => PropertyUpdated(value); }
        public List<string> MakeFilters { get => this._makeFilters; set => PropertyUpdated(value); }
        public List<string> ModelFilters { get => this._modelFilters; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new google drive view model
        /// </summary>
        /// <param name="GoogleDriveUserControl">UserControl which holds the content for our google drive view</param>
        public FulcrumGoogleDriveViewModel(UserControl GoogleDriveUserControl) : base(GoogleDriveUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP GOOGLE DRIVE VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            
            // Try and build our drive service here
            if (!FulcrumDriveBroker.ConfigureDriveService(out this._driveService))
                throw new InvalidComObjectException("Error! Failed to build new Drive Explorer Service!");

            // Log completed building view model instance and exit out
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper function used to list all the folders in the google drive location holding all injector files
        /// </summary>
        /// <returns>True if the folders are queried correctly and one or more are found. False if none are located.</returns>
        /// <param name="InjectorLogSets">The located injector log file sets</param>
        /// <exception cref="InvalidOperationException">Thrown when the google drive helper service is not yet built and can not be configured</exception>
        public bool LocateInjectorLogFiles(out List<DriveLogFileSet> InjectorLogSets)
        {
            // Initialize our list of output files and a timer for diagnostic purposes
            this.ViewModelLogger.WriteLog("REFRESHING INJECTOR LOG FILE SETS NOW...");
            this.RefreshTimer = new Stopwatch();
            this.RefreshTimer.Start();
            this.RefreshProgress = 0;

            // Setup filtering lists and our log file collection list
            this.LocatedLogFolders ??= new List<DriveLogFileSet>();
            this.LocatedLogFiles ??= new List<DriveLogFileModel>();
            this.YearFilters = new List<string>() { "-- Year --"};
            this.MakeFilters = new List<string>() { "-- Make --"};
            this.ModelFilters = new List<string>() { "-- Model -- "};
            this.ViewModelLogger.WriteLog("CONFIGURED EMPTY RESULT AND FILTERING LISTS CORRECTLY!", LogType.InfoLog);

            // Validate our Drive Explorer service is built and ready for use
            this.ViewModelLogger.WriteLog("VALIDATING INJECTOR DRIVE SERVICE...");
            if (this._driveService == null && !FulcrumDriveBroker.ConfigureDriveService(out this._driveService))
                throw new InvalidOperationException("Error! Google Drive explorer service has not been configured!");

            // Clear out the old list of log folders here
            this.LocatedLogFiles.Clear(); 
            this.LocatedLogFolders.Clear();
            this.ViewModelLogger.WriteLog("CLEARED OUT EXISTING LOG FILE AND FOLDER SETS CORRECTLY!");

            // Build a new request to list all the files in the drive
            this.ViewModelLogger.WriteLog("BUILDING REQUEST TO QUERY DRIVE CONTENTS NOW...");
            if (!FulcrumDriveBroker.ListDriveContents(out var LocatedDriveFolders, FulcrumDriveBroker.ResultTypes.FOLDERS_ONLY))
                throw new InvalidOperationException($"Error! Failed to refresh Drive Contents for Scan Sessions! (ID: {FulcrumDriveBroker.GoogleDriveId})!");

            // Configure a new filtering regex for building log file sets here
            Regex FilterParseRegex = new Regex(@"(\d{4})_([^_]+)_([^_]+)_([^\s]+)");
            this.ViewModelLogger.WriteLog("CONFIGURED NEW REGEX FOR PARSING FOLDER NAMES OK!");

            // Iterate the contents and build a new list of files to filter 
            int FoldersIterated = 0;
            int TotalFolderCount = LocatedDriveFolders.Count;
            foreach (var FolderLocated in LocatedDriveFolders)
            {
                // Update our progress counter value here
                this.RefreshProgress = (FoldersIterated++ / (double)TotalFolderCount) * 100.00;

                // Parse the name and build our folders for YMM
                Match FilterResults = FilterParseRegex.Match(FolderLocated.Name);
                if (!FilterResults.Success)
                {
                    // If the regex fails to match then don't bother moving past this point
                    this.ViewModelLogger.WriteLog($"ERROR! FAILED TO PARSE FOLDER NAME: {FolderLocated.Name}!", LogType.ErrorLog);
                    continue;
                }

                // Build a new folder set and pull in all files for it. 
                DriveLogFileSet NextFileSet = new DriveLogFileSet(FolderLocated);
                if (NextFileSet.RefreshFolderFiles())
                {
                    // Add this folder to our set of stored sets and append all files found inside it
                    this.LocatedLogFolders.Add(NextFileSet);
                    foreach (var LogFileModel in NextFileSet.LogSetFiles)
                        this.LocatedLogFiles.Add((DriveLogFileModel)LogFileModel);
                }
                else
                {
                    // If the refresh fails, ignore the log folder set and move on
                    this.ViewModelLogger.WriteLog($"WARNING! NO LOG FILES WERE FOUND FOR FOLDER: {FolderLocated.Name}!", LogType.WarnLog);
                    continue;
                }

                // Pull out the year make and model values here and add them to our filter lists
                string FilteredYear = FilterResults.Groups[1].Value;
                string FilteredMake = FilterResults.Groups[2].Value;
                string FilteredModel = FilterResults.Groups[3].Value;

                // Store the filtering values here and log that this folder is stored
                if (!this.YearFilters.Contains(FilteredYear)) this.YearFilters.Add(FilteredYear);
                if (!this.MakeFilters.Contains(FilteredMake)) this.MakeFilters.Add(FilteredMake);
                if (!this.ModelFilters.Contains(FilteredModel)) this.ModelFilters.Add(FilteredModel);
            }

            // Stop our timer and log out the results of this routine
            InjectorLogSets = this.LocatedLogFolders.ToList();
            this.ViewModelLogger.WriteLog($"FOUND A TOTAL OF {InjectorLogSets.Count} FOLDERS IN {this.RefreshTimer.Elapsed:mm\\:ss\\:fff}");
            this.ViewModelLogger.WriteLog($"CONFIGURED {this.YearFilters.Count} YEAR FILTERS", LogType.TraceLog);
            this.ViewModelLogger.WriteLog($"CONFIGURED {this.MakeFilters.Count} MAKE FILTERS", LogType.TraceLog);
            this.ViewModelLogger.WriteLog($"CONFIGURED {this.ModelFilters.Count} MODEL FILTERS", LogType.TraceLog);
            this.ViewModelLogger.WriteLog("DONE REFRESHING INJECTOR LOG FILE SETS!", LogType.InfoLog);
            this.RefreshTimer.Stop();

            // Return out based on the number of files loaded in 
            return InjectorLogSets.Count != 0;
        }
    }
}
