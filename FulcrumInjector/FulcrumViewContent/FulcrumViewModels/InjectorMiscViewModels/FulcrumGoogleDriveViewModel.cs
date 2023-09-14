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
        private ObservableCollection<DriveLogFileSet> _locatedLogFolders;   // Collection of all loaded log folders found
        private ObservableCollection<DriveLogFileSet> _filteredLogFolders;  // Collection of all filtered log folders found

        // Private backing fields for filtering collections
        private ObservableCollection<string> _yearFilters;                  // Years we can filter by 
        private ObservableCollection<string> _makeFilters;                  // Makes we can filter by
        private ObservableCollection<string> _modelFilters;                 // Models we can filter by
        private Dictionary<FilterTypes, string> _appliedFilters;            // Currently applied filter values

        #endregion // Fields

        #region Properties

        // Public property for refresh timer
        public Stopwatch RefreshTimer { get => this._refreshTimer; set => PropertyUpdated(value); }
        public double RefreshProgress { get => this._refreshProgress; set => PropertyUpdated(value); }

        // Public facing properties holding our collection of log files loaded
        public ObservableCollection<DriveLogFileSet> LocatedLogFolders { get => this._locatedLogFolders; set => PropertyUpdated(value); }
        public ObservableCollection<DriveLogFileSet> FilteredLogFolders { get => this._filteredLogFolders; set => PropertyUpdated(value);  }

        // Public facing properties holding our different filter lists for the file filtering configuration
        public ObservableCollection<string> YearFilters { get => this._yearFilters; set => PropertyUpdated(value); }
        public ObservableCollection<string> MakeFilters { get => this._makeFilters; set => PropertyUpdated(value); }
        public ObservableCollection<string> ModelFilters { get => this._modelFilters; set => PropertyUpdated(value); }
        public Dictionary<FilterTypes, string> AppliedFilters { get => this._appliedFilters; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration holding our different types of filters supported on the UI
        /// </summary>
        public enum FilterTypes
        {
            YEAR_FILTER,    // Sets a filter type of Year
            MAKE_FILTER,    // Sets a filter type of Make
            MODEL_FILTER,   // Sets a filter type of Model
            VIN_FILTER,     // Sets a filter type of VIN
        }

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

            // Build the default applied filters dictionary
            this.AppliedFilters = new Dictionary<FilterTypes, string>()
            {
                { FilterTypes.YEAR_FILTER, "" },
                { FilterTypes.MAKE_FILTER, "" },
                { FilterTypes.MODEL_FILTER, "" },
                { FilterTypes.VIN_FILTER, "" }
            };
            
            // Try and build our drive service here
            if (!FulcrumDriveBroker.ConfigureDriveService(out this._driveService))
                throw new InvalidComObjectException("Error! Failed to build new Drive Explorer Service!");

            // Log completed building view model instance and exit out
            this.ViewModelLogger.WriteLog("CONFIGURED GOOGLE DRIVE SERVICE AND DEFAULT FILTERING DICTIONARY CORRECTLY!", LogType.InfoLog);
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
            this.YearFilters ??= new ObservableCollection<string>();
            this.MakeFilters ??= new ObservableCollection<string>();
            this.ModelFilters ??= new ObservableCollection<string>();
            this.LocatedLogFolders ??= new ObservableCollection<DriveLogFileSet>();
            this.FilteredLogFolders ??= new ObservableCollection<DriveLogFileSet>();
            this.ViewModelLogger.WriteLog("CONFIGURED EMPTY RESULT AND FILTERING LISTS CORRECTLY!", LogType.InfoLog);

            // Validate our Drive Explorer service is built and ready for use
            this.ViewModelLogger.WriteLog("VALIDATING INJECTOR DRIVE SERVICE...");
            if (this._driveService == null && !FulcrumDriveBroker.ConfigureDriveService(out this._driveService))
                throw new InvalidOperationException("Error! Google Drive explorer service has not been configured!");

            // Clear out the old list of log folders here
            this.LocatedLogFolders.Clear();
            this.FilteredLogFolders.Clear();
            this.ViewModelLogger.WriteLog("CLEARED OUT EXISTING LOG FOLDER SETS CORRECTLY!");

            // Build a new request to list all the files in the drive
            this.ViewModelLogger.WriteLog("BUILDING REQUEST TO QUERY DRIVE CONTENTS NOW...");
            if (!FulcrumDriveBroker.ListDriveContents(out var LocatedDriveFolders, FulcrumDriveBroker.ResultTypes.FOLDERS_ONLY))
                throw new InvalidOperationException($"Error! Failed to refresh Drive Contents for Scan Sessions! (ID: {FulcrumDriveBroker.GoogleDriveId})!");
            
            // Iterate the contents and build a new list of files to filter 
            int FoldersIterated = 0;
            int TotalFolderCount = LocatedDriveFolders.Count;
            foreach (var FolderLocated in LocatedDriveFolders)
            {
                // Update our progress counter value here
                this.RefreshProgress = (FoldersIterated++ / (double)TotalFolderCount) * 100.00;

                // Build a new folder set and pull in all files for it.
                DriveLogFileSet NextFileSet = new DriveLogFileSet(FolderLocated);
                if (string.IsNullOrWhiteSpace(NextFileSet.LogSetName)) continue;
                if (!NextFileSet.RefreshFolderFiles()) continue;

                // Add this folder to our set of stored sets and append all files found inside it
                this.LocatedLogFolders.Add(NextFileSet);
                this.BaseViewControl.Dispatcher.Invoke(() => this.FilteredLogFolders.Add(NextFileSet));
            }

            // Refresh the filtering sets here and stop the refresh timer once done
            this._refreshLogFilters();
            this.RefreshTimer.Stop();

            // Log out how many folder sets we've configured and store them on our view model
            this.ViewModelLogger.WriteLog($"FOUND A TOTAL OF {this.LocatedLogFolders.Count} FOLDERS IN {this.RefreshTimer.Elapsed:mm\\:ss\\:fff}");
            InjectorLogSets = this.LocatedLogFolders.ToList();

            // Return out based on the number of files loaded in 
            return InjectorLogSets.Count != 0;
        }
        /// <summary>
        /// Helper function used to apply a filter to the collection of currently stored log sets
        /// </summary>
        /// <param name="TypeOfFilter">The type of filter being applied</param>
        /// <param name="FilterValue">The value of the filter being applied</param>
        public void ApplyLogSetFilter(FilterTypes TypeOfFilter, string FilterValue)
        {
            // Make sure we're able to apply filters first
            if (this.FilteredLogFolders == null) return;
            this.FilteredLogFolders.Clear();

            // Store the new filter type value and filter our log sets
            this.AppliedFilters[TypeOfFilter] = FilterValue;

            // Find any log sets matching all filters now and store those values
            foreach (var LocatedLogFolder in this.LocatedLogFolders)
            {
                // Check our filters for each filter type and check to see if it's supported
                bool MatchesYear = string.IsNullOrWhiteSpace(this.AppliedFilters[FilterTypes.YEAR_FILTER]) ||
                                   LocatedLogFolder.LogSetYear == this.AppliedFilters[FilterTypes.YEAR_FILTER];
                bool MatchesMake = string.IsNullOrWhiteSpace(this.AppliedFilters[FilterTypes.MAKE_FILTER]) ||
                                   LocatedLogFolder.LogSetMake == this.AppliedFilters[FilterTypes.MAKE_FILTER];
                bool MatchesModel = string.IsNullOrWhiteSpace(this.AppliedFilters[FilterTypes.MODEL_FILTER]) ||
                                   LocatedLogFolder.LogSetModel == this.AppliedFilters[FilterTypes.MODEL_FILTER];
                bool MatchesVIN = string.IsNullOrWhiteSpace(this.AppliedFilters[FilterTypes.VIN_FILTER]) ||
                                  LocatedLogFolder.LogSetVIN.Contains(this.AppliedFilters[FilterTypes.VIN_FILTER]);

                // If we match all three filter values, then keep this log file set on our collection
                if (MatchesYear && MatchesMake && MatchesModel && MatchesVIN) 
                    this.FilteredLogFolders.Add(LocatedLogFolder);
            }

            // Once we've filtered all of our results, we refresh the filter sets
            this._refreshLogFilters();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper method used to refresh the filters stored on the UI for the log filtering
        /// </summary>
        private void _refreshLogFilters()
        {
            // Configure new year filters using the built folder objects here
            List<string> FilteringYears = new List<string>() { "-- Year -- " };
            FilteringYears.AddRange(this.FilteredLogFolders
                .GroupBy(FolderSet => FolderSet.LogSetYear)
                .Select(FilterGroup => FilterGroup.Key)
                .OrderBy(FilterSet => FilterSet));

            // Configure new make filters using the built folder objects here
            List<string> FilteringMakes = new List<string>() { "-- Make -- " };
            FilteringMakes.AddRange(this.FilteredLogFolders
                .GroupBy(FolderSet => FolderSet.LogSetMake)
                .Select(FilterGroup => FilterGroup.Key)
                .OrderBy(FilterSet => FilterSet));

            // Configure new model filters using the built folder objects here
            List<string> FilteringModels = new List<string>() { "-- Model -- " };
            FilteringModels.AddRange(this.FilteredLogFolders
                .GroupBy(FolderSet => FolderSet.LogSetModel)
                .Select(FilterGroup => FilterGroup.Key)
                .OrderBy(FilterSet => FilterSet));

            // Build our new observable collections for the filter sets
            this.YearFilters = new ObservableCollection<string>(FilteringYears); 
            OnPropertyChanged(nameof(this.YearFilters));
            this.MakeFilters = new ObservableCollection<string>(FilteringMakes);
            OnPropertyChanged(nameof(this.MakeFilters));
            this.ModelFilters = new ObservableCollection<string>(FilteringModels);
            OnPropertyChanged(nameof(this.ModelFilters));

            // Log out the properties of our filtering configuration
            this.ViewModelLogger.WriteLog($"FILTERED DOWN TO {this.FilteredLogFolders.Count} LOG SETS FROM {this.LocatedLogFolders.Count} LOCATED", LogType.TraceLog);
            this.ViewModelLogger.WriteLog($"CONFIGURED {this.YearFilters.Count} YEAR FILTERS", LogType.TraceLog);
            this.ViewModelLogger.WriteLog($"CONFIGURED {this.MakeFilters.Count} MAKE FILTERS", LogType.TraceLog);
            this.ViewModelLogger.WriteLog($"CONFIGURED {this.ModelFilters.Count} MODEL FILTERS", LogType.TraceLog);
            this.ViewModelLogger.WriteLog("DONE REFRESHING INJECTOR LOG FILE SETS!", LogType.InfoLog);
        }
    }
}
