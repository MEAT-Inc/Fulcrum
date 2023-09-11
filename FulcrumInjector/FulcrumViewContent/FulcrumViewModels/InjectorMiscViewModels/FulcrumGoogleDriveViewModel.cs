using SharpLogging;
using SharpSimulator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using static FulcrumInjector.FulcrumViewSupport.FulcrumUpdater;
using System.Text;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using Google.Apis.Services;

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
        private string _driveApiKey;                            // API key used to explore the google drive
        private string[] _driveScopes;                          // The scopes authorized for the drive service
        private string _applicationName;                        // Name of the authorized drive application
        private DriveService _driveService;                     // The service used to navigate our google drive

        // Private backing fields for filtering collections
        private ObservableCollection<string> _yearFilters;      // Years we can filter by 
        private ObservableCollection<string> _makeFilters;      // Makes we can filter by
        private ObservableCollection<string> _modelFilters;     // Models we can filter by

        #endregion // Fields

        #region Properties

        // Public facing properties holding our different filter lists for the file filtering configuration
        public ObservableCollection<string> YearFilters { get => this._yearFilters; set => PropertyUpdated(value); }
        public ObservableCollection<string> MakeFilters { get => this._makeFilters; set => PropertyUpdated(value); }
        public ObservableCollection<string> ModelFilters { get => this._modelFilters; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Class object used to define the JSON object of a google drive explorer configuration
        /// </summary>
        public class DriveExplorerConfiguration
        {
            public string DriveApiKey { get; set; }
            public string[] DriveScopes { get; set; }
            public string ApplicationName { get; set; }
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

            // Try and build our drive service here
            if (!this.ConfigureDriveService(out this._driveService))
                throw new InvalidComObjectException("Error! Failed to build new Drive Explorer Service!");

            // Log completed building view model instance and exit out
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new google drive explorer service for finding files 
        /// </summary>
        /// <returns>The built google drive explorer service</returns>
        public bool ConfigureDriveService(out DriveService BuiltService)
        {
            // Load the google drive API login information and store it on this class instance
            var ExplorerConfiguration = ValueLoaders.GetConfigValue<DriveExplorerConfiguration>("FulcrumConstants.InjectorDriveExplorer");
            this._driveScopes = ExplorerConfiguration.DriveScopes;
            this._applicationName = ExplorerConfiguration.ApplicationName;
            this._driveApiKey = Encoding.UTF8.GetString(Convert.FromBase64String(string.Join(string.Empty, ExplorerConfiguration.DriveApiKey.Reverse())));

            // Log out the information built for the drive explorer object here
            this.ViewModelLogger.WriteLog("PULLED GOOGLE DRIVE EXPLORER LOGIN INFORMATION CORRECTLY!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"GOOGLE DRIVE APP NAME: {this._applicationName}");
            this.ViewModelLogger.WriteLog($"GOOGLE DRIVE EXPLORER API KEY: {this._driveApiKey}");
            this.ViewModelLogger.WriteLog($"GOOGLE DRIVE SCOPES: {string.Join(",", this._driveScopes)}");

            try
            {
                // Configure the google drive service here
                this.ViewModelLogger.WriteLog("BUILDING NEW GOOGLE DRIVE SERVICE NOW...", LogType.WarnLog);
                BuiltService = new DriveService(new BaseClientService.Initializer()
                {
                    // Store the API key and Application name for the authorization helper
                    ApiKey = this._driveApiKey,
                    ApplicationName = this._applicationName
                });

                // Return the new drive service object 
                this.ViewModelLogger.WriteLog("BUILT NEW GOOGLE DRIVE EXPLORER SERVICE WITHOUT ISSUES!", LogType.InfoLog);
                return true;
            }
            catch (Exception ServiceInitEx)
            {
                // Log out the failure for the service creation and exit out false 
                BuiltService = null;
                this.ViewModelLogger.WriteLog("ERROR! FAILED TO BUILD NEW DRIVE EXPLORER SERVICE!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION THROWN DURING SERVICE CREATION IS BEING LOGGED BELOW", ServiceInitEx);
                return false;
            }
        }
        /// <summary>
        /// Helper function used to list all the files in the google drive location holding all injector files
        /// </summary>
        /// <returns>True if the files are queried correctly and one or more are found. False if none are located.</returns>
        /// <param name="InjectorLogSets">The located injector log file sets</param>
        /// <exception cref="InvalidOperationException">Thrown when the google drive helper service is not yet built and can not be configured</exception>
        public bool LocateInjectorLogSets(out List<string> InjectorLogSets)
        {
            // Validate our Drive Explorer service is built and ready for use
            if (this._driveService == null && !this.ConfigureDriveService(out this._driveService)) 
                throw new InvalidOperationException("Error! Google Drive explorer service has not been configured!");

            // TODO: Configure list query for finding log files
            // Initialize the list of log file sets we're returning out from the drive
            InjectorLogSets = new List<string>();

            // Return out based on the number of log file sets located
            return InjectorLogSets.Count != 0;
        }
    }
}
