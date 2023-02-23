using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewSupport.FulcrumJson.JsonHelpers;
using Newtonsoft.Json;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.ViewModels
{
    /// <summary>
    /// Viewmodel for installed OE Applications
    /// </summary>
    internal class FulcrumInstalledOeAppsViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields for our public properties
        private bool _canBootApp;
        private bool _canKillApp;
        private Process _runningAppProcess;
        private FulcrumOeAppModel _targetAppModel;
        private FulcrumOeAppModel _runningAppModel;
        private ObservableCollection<FulcrumOeAppModel> _installedOeApps;

        #endregion //Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool CanBootApp { get => _canBootApp; private set => PropertyUpdated(value); }
        public bool CanKillApp { get => _canKillApp; private set => PropertyUpdated(value); }
        public FulcrumOeAppModel RunningAppModel { get => _runningAppModel; private set => PropertyUpdated(value); }
        public ObservableCollection<FulcrumOeAppModel> InstalledOeApps { get => _installedOeApps; private set => PropertyUpdated(value); }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="InstalledAppsView">The user control holding the installed apps view used to setup this VM</param>
        public FulcrumInstalledOeAppsViewModel(UserControl InstalledAppsView) : base(InstalledAppsView)
        {           
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Log information and store values 
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("SETTING UP OE APPLICATION LIST NOW...", LogType.WarnLog);

            // Import the list of possible OE App names from our json configuration file now.
            this.InstalledOeApps = this.ImportOeApplications();
            this.ViewModelLogger.WriteLog("IMPORT PROCESS COMPLETE! VIEW SHOULD BE UPDATED WITH APP INSTANCE OBJECTS NOW!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("BOUND NEW APP OBJECT TO INDEX ZERO ON THE VIEW CONTENT! THIS IS GOOD!", LogType.InfoLog);

            // Store default values here.
            this.CanKillApp = false; this.CanBootApp = true;
            this.ViewModelLogger.WriteLog("SETUP DEFAULT VALUES FOR BOOT AND KILL BOOL OBJECTS ON OE APP VIEW MODEL OK!", LogType.InfoLog);

            // Log completed setup.
            this.ViewModelLogger.WriteLog("SETUP NEW OE APP STATUS MONITOR VALUES OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("RETURNING OUT TO CONTINUE BUILDING MAIN CONTENT FOR VIEW OBJECTS NOW...", LogType.WarnLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls in a list of OE application names and paths as a set of objects.
        /// Converts them into a list and returns them.
        /// </summary>
        public ObservableCollection<FulcrumOeAppModel> ImportOeApplications()
        {
            // Log info. Pull app objects in from the settings file, and begin to import them.
            this.ViewModelLogger.WriteLog("PULLING IN LIST OF PREDEFINED OE APPLICATIONS AND STORING THEM ONTO OUR VIEW OBJECT NOW...", LogType.WarnLog);
            var PulledAppsObject = ValueLoaders.GetConfigValue<object[]>("FulcrumOeAppNames");

            // Store output in this list.
            List<FulcrumOeAppModel> OutputApps = new List<FulcrumOeAppModel>();
            foreach (var AppObject in PulledAppsObject)
            {
                // Cast the application object into a new model for our app instances.
                this.ViewModelLogger.WriteLog($"TRYING TO CAST OBJECT {PulledAppsObject.ToList().IndexOf(AppObject)} OF {PulledAppsObject.Length} NOW...", LogType.TraceLog);
                try
                {
                    // Convert this into a string of Json. Then built it into a json cast OE app model
                    string JsonOfObject = JsonConvert.SerializeObject(AppObject);
                    FulcrumOeAppModel NextAppModel = JsonConvert.DeserializeObject<FulcrumOeAppModel>(JsonOfObject);

                    // Add to list of outputs
                    OutputApps.Add(NextAppModel);
                }
                catch { this.ViewModelLogger.WriteLog("FAILED TO CAST CURRENT OBJECT INTO A NEW OE APP MODEL! MOVING ON", LogType.WarnLog); }
            }

            // Put our usable apps first and soft those A-Z. Append the not usable ones and sort them A-Z
            OutputApps = OutputApps.OrderBy(AppObj => AppObj.IsAppUsable).Reverse().ToList();
            OutputApps = new[] {
                OutputApps.Where(AppObj => AppObj.IsAppUsable).OrderBy(AppObj => AppObj.OEAppName).ToList(),
                OutputApps.Where(AppObj => !AppObj.IsAppUsable).OrderBy(AppObj => AppObj.OEAppName).ToList()    
            }.SelectMany(AppSet => AppSet).ToList();

            // Log output information here.
            this.ViewModelLogger.WriteLog($"PULLED IN A TOTAL OF {PulledAppsObject.Length} OBJECTS AND CREATED {OutputApps.Count} CAST APP OBJECTS!", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("RETURNING BUILT APP OBJECT INSTANCES NOW...");
            return new ObservableCollection<FulcrumOeAppModel>(OutputApps);
        }

        /// <summary>
        /// Boots a new OE Application based on the current value given for it.
        /// </summary>
        /// <param name="AppToStore">App to boot </param>
        /// <returns>True if booted. false if failed.</returns>
        public bool SetTargetOeApplication(FulcrumOeAppModel AppToStore)
        {
            // Store the app here and return status.
            this.ViewModelLogger.WriteLog($"STORING NEW OE APPLICATION NAMED {AppToStore.OEAppName} NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("STORING CONTENT CONTROL BOOL VALUES FOR OUR BUTTON SENDER NOW...", LogType.InfoLog);

            // Store bool values for the state of the command button.
            this._targetAppModel = AppToStore;
            this.CanBootApp = this.RunningAppModel == null;
            this.CanKillApp = this.RunningAppModel != null && this._runningAppProcess?.HasExited == false;   

            // Store the input button object here.
            this.ViewModelLogger.WriteLog($"STORED NEW VALUES FOR BOOLEAN CONTENT CONTROLS OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"VALUES SET --> BOOTABLE: {this.CanBootApp} | KILLABLE: {this.CanKillApp}", LogType.TraceLog);
            return true;
        }
        /// <summary>
        /// Boots a new OE Application based on the current value given for it.
        /// </summary>
        /// <param name="BootedAppProcess">The built process object for the booted application</param>
        /// <returns>True if booted. false if failed.</returns>
        public bool LaunchOeApplication(out Process BootedAppProcess)
        {
            // Check if app to kill is not null.
            if (this._targetAppModel == null || !this.CanBootApp) {
                this.ViewModelLogger.WriteLog("ERROR! SELECTED APP OBJECT WAS NULL! ENSURE ONE HAS BEEN CONFIRMED BEFORE RUNNING THIS METHOD!", LogType.ErrorLog);
                BootedAppProcess = null;
                return false;
            }

            // Boot the app here and return status. Build process object out and return it.
            this.ViewModelLogger.WriteLog($"BOOTING OE APPLICATION NAMED {this._targetAppModel.OEAppName} NOW...", LogType.WarnLog);
            this._runningAppProcess = new Process() {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo(this._targetAppModel.OEAppPath) {
                    Verb = "runas", WindowStyle = ProcessWindowStyle.Maximized,
                }
            };

            // Tack on the exited process event. Clear out values and setup new process object.
            this._runningAppProcess.Exited += (SendingApp, _) => {
                this.RunningAppModel = null; this.CanKillApp = false; this.CanBootApp = true;
                this.ViewModelLogger.WriteLog($"WARNING! OE APP PROCESS EXITED WITHOUT USER COMMAND!", LogType.WarnLog);
            };

            // Store output process and return.
            this._runningAppProcess.Start();
            this.ViewModelLogger.WriteLog($"BOOTED NEW OE APP PROCESS OK! PROCESS ID: {_runningAppProcess.Id}", LogType.InfoLog);

            // Set our new running object and remove the temp value object.
            this.CanKillApp = true;
            this.CanBootApp = false;
            BootedAppProcess = _runningAppProcess;
            this.RunningAppModel = this._targetAppModel;

            // Return output state
            this.ViewModelLogger.WriteLog("TOGGLED OE APP STATE VALUES CORRECTLY! MOVING ON TO WAIT FOR EXIT NOW...", LogType.InfoLog);
            return true;    
        }
        /// <summary>
        /// Boots a new OE Application based on the current value given for it.
        /// </summary>
        /// <param name="LastRunOeApp">The last built and run OE application model object</param>
        /// <returns>True if killed. false if failed.</returns>
        public bool KillOeApplication(out FulcrumOeAppModel LastRunOeApp)
        {
            // Check if app to kill is not null.
            if (this.RunningAppModel == null || !this.CanKillApp) {
                this.ViewModelLogger.WriteLog("ERROR! SELECTED APP OBJECT WAS NULL! ENSURE ONE HAS BEEN CONFIRMED BEFORE RUNNING THIS METHOD!", LogType.ErrorLog);
                LastRunOeApp = null;
                return false;
            }

            // Kill the app here and return status.
            this.ViewModelLogger.WriteLog($"KILLING RUNNING OE APPLICATION NAMED {this.RunningAppModel.OEAppName} NOW...", LogType.WarnLog);
            this._runningAppProcess.Kill();

            // Set Store values for controls.
            this.CanKillApp = false;
            this.CanBootApp = true;
            LastRunOeApp = this.RunningAppModel;
            this.RunningAppModel = null;

            // Return passed output here.
            this.ViewModelLogger.WriteLog("KILLED APP OBJECT CORRECTLY AND STORED CONTENT VALUES ON VM OK! READY TO PROCESS A NEW BOOT OR KILL COMMAND", LogType.InfoLog);
            return true;
        }
    }
}
