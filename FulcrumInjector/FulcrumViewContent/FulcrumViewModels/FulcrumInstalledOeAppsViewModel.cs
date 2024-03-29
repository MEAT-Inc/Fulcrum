﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using FulcrumJson;
using Newtonsoft.Json;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels
{
    /// <summary>
    /// Viewmodel for installed OE Applications
    /// </summary>
    public class FulcrumInstalledOeAppsViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields for our public properties
        private bool _canBootApp;
        private bool _canKillApp;
        private bool _isEditMode;
        private Process _runningAppProcess;
        private FulcrumOeApplication _runningApp;
        private FulcrumOeApplication _selectedApp;
        private ObservableCollection<FulcrumOeApplication> _installedOeApps;

        #endregion //Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool CanBootApp => this.SelectedApp.IsAppUsable;
        public bool CanKillApp  { get => this._canKillApp; set => PropertyUpdated(value); }
        public bool IsEditMode { get => this._isEditMode; set => PropertyUpdated(value); }
        public FulcrumOeApplication RunningApp { get => this._runningApp; set => PropertyUpdated(value); }
        public FulcrumOeApplication SelectedApp { get => this._selectedApp; set => PropertyUpdated(value); }
        public ObservableCollection<FulcrumOeApplication> InstalledOeApps { get => this._installedOeApps; set => PropertyUpdated(value); }

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Model object of our OE Applications installed on the system.
        /// </summary>
        [JsonConverter(typeof(FulcrumOeApplicationJsonConverter))]
        public class FulcrumOeApplication
        {
            #region Custom Events
            #endregion //Custom Events

            #region Fields
            #endregion //Fields

            #region Properties

            // Properties about an OE Application
            public string OEAppName { get; private set; }
            public string OEAppPath { get; private set; }
            public string OEAppVersion { get; private set; }
            public string OEAppCommand { get; private set; }
            public string[] OEAppPathList { get; private set; }
            public bool IsAppUsable => File.Exists(OEAppPath);

            #endregion //Properties

            #region Structs and Classes
            #endregion //Structs and Classes

            // ------------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Returns hyphenated string object for this app instance
            /// </summary>
            /// <returns></returns>
            public override string ToString() { return $"{OEAppName} - {OEAppPath} - {OEAppVersion} - {OEAppCommand}"; }

            // ------------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Builds a new OE application object from a given set of values.
            /// </summary>
            public FulcrumOeApplication(string Name, string Path, string Version = "N/A", string BatLaunchCommand = null, string[] PathSet = null)
            {
                // Store values. Append into our list of models.
                this.OEAppName = Name;
                this.OEAppPath = Path;
                this.OEAppVersion = Version;
                this.OEAppPathList = PathSet ?? new[] { this.OEAppPath };
                this.OEAppCommand = BatLaunchCommand ?? $"cmd.exe /C \"{OEAppPath}\"";
            }
        }

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="InstalledAppsView">UserControl which holds the content for our Installed apps view</param>
        public FulcrumInstalledOeAppsViewModel(UserControl InstalledAppsView) : base(InstalledAppsView)
        {           
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP OE APPLICATION LIST NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Import the list of possible OE App names from our json configuration file now.
            this.InstalledOeApps = this.ImportOeApplications();
            this.ViewModelLogger.WriteLog("IMPORT PROCESS COMPLETE! VIEW SHOULD BE UPDATED WITH APP INSTANCE OBJECTS NOW!");
            this.ViewModelLogger.WriteLog("BOUND NEW APP OBJECT TO INDEX ZERO ON THE VIEW CONTENT! THIS IS GOOD!");

            // Store default values here.
            this.ViewModelLogger.WriteLog("SETUP NEW OE APP STATUS MONITOR VALUES OK!");
            this.ViewModelLogger.WriteLog("SETUP DEFAULT VALUES FOR BOOT AND KILL BOOL OBJECTS ON OE APP VIEW MODEL OK!");
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls in a list of OE application names and paths as a set of objects.
        /// Converts them into a list and returns them.
        /// </summary>
        public ObservableCollection<FulcrumOeApplication> ImportOeApplications()
        {
            // Log info. Pull app objects in from the settings file, and begin to import them.
            this.ViewModelLogger.WriteLog("PULLING IN LIST OF PREDEFINED OE APPLICATIONS AND STORING THEM ONTO OUR VIEW OBJECT NOW...", LogType.WarnLog);
            var LoadedOeApplications = ValueLoaders.GetConfigValue<FulcrumOeApplication[]>("FulcrumOeApplications");

            // Put our usable apps first and soft those A-Z. Append the not usable ones and sort them A-Z
            LoadedOeApplications = LoadedOeApplications.OrderBy(AppObj => AppObj.IsAppUsable).Reverse().ToArray();
            LoadedOeApplications = new[] {
                LoadedOeApplications.Where(AppObj => AppObj.IsAppUsable).OrderBy(AppObj => AppObj.OEAppName).ToList(),
                LoadedOeApplications.Where(AppObj => !AppObj.IsAppUsable).OrderBy(AppObj => AppObj.OEAppName).ToList()    
            }.SelectMany(AppSet => AppSet).ToArray();

            // Log output information here.
            this.ViewModelLogger.WriteLog($"PULLED IN A TOTAL OF {LoadedOeApplications.Length} OBJECTS AND CREATED {LoadedOeApplications.Length} CAST APP OBJECTS!", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("RETURNING BUILT APP OBJECT INSTANCES NOW...");
            return new ObservableCollection<FulcrumOeApplication>(LoadedOeApplications);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Boots a new OE Application based on the current value given for it.
        /// </summary>
        /// <param name="AppToStore">App to boot </param>
        /// <returns>True if booted. false if failed.</returns>
        public void SetTargetOeApplication(FulcrumOeApplication AppToStore)
        {
            // Check if the provided app object is null or not
            if (AppToStore == null)
            {
                // Clear the current app model and exit out
                this.ViewModelLogger.WriteLog("REMOVING CURRENTLY SELECTED OE APP MODEL NOW...");
                this.SelectedApp = null;
                return;
            }

            // Store the app here and return status.
            this.ViewModelLogger.WriteLog($"STORING NEW OE APPLICATION NAMED {AppToStore.OEAppName} NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("STORING CONTENT CONTROL BOOL VALUES FOR OUR BUTTON SENDER NOW...", LogType.InfoLog);

            // Store bool values for the state of the command button.
            this.SelectedApp = AppToStore;
            this.CanKillApp = 
                this.RunningApp != null &&
                this._runningAppProcess?.HasExited == false && 
                this.RunningApp.OEAppName == AppToStore.OEAppName;

            // Store the input button object here.
            this.ViewModelLogger.WriteLog($"STORED NEW VALUES FOR BOOLEAN CONTENT CONTROLS OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"VALUES SET --> BOOTABLE: {this.CanBootApp} | KILLABLE: {this.CanKillApp}", LogType.TraceLog);
        }
        /// <summary>
        /// Boots a new OE Application based on the current value given for it.
        /// </summary>
        /// <param name="BootedAppProcess">The built process object for the booted application</param>
        /// <returns>True if booted. false if failed.</returns>
        public bool LaunchOeApplication(out Process BootedAppProcess)
        {
            // Check if app to kill is not null.
            if (this.SelectedApp == null || !this.CanBootApp) {
                this.ViewModelLogger.WriteLog("ERROR! SELECTED APP OBJECT WAS NULL! ENSURE ONE HAS BEEN CONFIRMED BEFORE RUNNING THIS METHOD!", LogType.ErrorLog);
                BootedAppProcess = null;
                return false;
            }

            // Boot the app here and return status. Build process object out and return it.
            this.ViewModelLogger.WriteLog($"BOOTING OE APPLICATION NAMED {this.SelectedApp.OEAppName} NOW...", LogType.WarnLog);
            this._runningAppProcess = new Process() {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo(this.SelectedApp.OEAppPath) {
                    Verb = "runas", WindowStyle = ProcessWindowStyle.Maximized,
                }
            };

            // Tack on the exited process event. Clear out values and setup new process object.
            this._runningAppProcess.Exited += (SendingApp, _) => {
                this.RunningApp = null; 
                this.CanKillApp = false;
                this.ViewModelLogger.WriteLog($"WARNING! OE APP PROCESS EXITED WITHOUT USER COMMAND!", LogType.WarnLog);
            };

            // Store output process and return.
            this._runningAppProcess.Start();
            this.ViewModelLogger.WriteLog($"BOOTED NEW OE APP PROCESS OK! PROCESS ID: {_runningAppProcess.Id}", LogType.InfoLog);

            // Set our new running object and remove the temp value object.
            this.CanKillApp = true;
            this.RunningApp = this.SelectedApp;
            BootedAppProcess = _runningAppProcess;

            // Return output state
            this.ViewModelLogger.WriteLog("TOGGLED OE APP STATE VALUES CORRECTLY! MOVING ON TO WAIT FOR EXIT NOW...", LogType.InfoLog);
            return true;    
        }
        /// <summary>
        /// Boots a new OE Application based on the current value given for it.
        /// </summary>
        /// <param name="LastAppName">The last built and run OE application model object</param>
        /// <returns>True if killed. false if failed.</returns>
        public bool KillOeApplication(out string LastAppName)
        {
            // Check if app to kill is not null.
            if (this.RunningApp == null || !this.CanKillApp) {
                this.ViewModelLogger.WriteLog("ERROR! SELECTED APP OBJECT WAS NULL! ENSURE ONE HAS BEEN CONFIRMED BEFORE RUNNING THIS METHOD!", LogType.ErrorLog);
                LastAppName = null;
                return false;
            }

            // Kill the app here and return status.
            this.ViewModelLogger.WriteLog($"KILLING RUNNING OE APPLICATION NAMED {this.RunningApp.OEAppName} NOW...", LogType.WarnLog);
            this._runningAppProcess.Kill();
            LastAppName = this.RunningApp.OEAppName;

            // Set Store values for controls.
            this.CanKillApp = false;
            this.RunningApp = null;

            // Return passed output here.
            this.ViewModelLogger.WriteLog("KILLED APP OBJECT CORRECTLY AND STORED CONTENT VALUES ON VM OK! READY TO PROCESS A NEW BOOT OR KILL COMMAND", LogType.InfoLog);
            return true;
        }
    }
}
