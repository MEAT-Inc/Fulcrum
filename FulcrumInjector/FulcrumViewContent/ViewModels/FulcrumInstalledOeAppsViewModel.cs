using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.Models;
using Newtonsoft.Json;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels
{
    /// <summary>
    /// Viewmodel for installed OE Applications
    /// </summary>
    public class FulcrumInstalledOeAppsViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InstalledOeAppsViewModelLogger")) ?? new SubServiceLogger("InstalledOeAppsViewModelLogger");

        // Private Control Values
        private ObservableCollection<OeApplicationModel> _installedOeApps;

        // Public values for our view to bind onto 
        public ObservableCollection<OeApplicationModel> InstalledOeApps { get => _installedOeApps; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumInstalledOeAppsViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP OE APPLICATION LIST NOW...", LogType.WarnLog);

            // Import the list of possible OE App names from our json configuration file now.
            this.InstalledOeApps = this.ImportOeApplications();
            ViewModelLogger.WriteLog("IMPORT PROCESS COMPLETE! VIEW SHOULD BE UPDATED WITH APP INSTANCE OBJECTS NOW!", LogType.InfoLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW OE APP STATUS MONITOR VALUES OK!", LogType.InfoLog);
        }


        /// <summary>
        /// Pulls in a list of OE application names and paths as a set of objects.
        /// Converts them into a list and returns them.
        /// </summary>
        internal ObservableCollection<OeApplicationModel> ImportOeApplications()
        {
            // Log info. Pull app objects in from the settings file, and begin to import them.
            ViewModelLogger.WriteLog("PULLING IN LIST OF PREDEFINED OE APPLICATIONS AND STORING THEM ONTO OUR VIEW OBJECT NOW...", LogType.WarnLog);
            var PulledAppsObject = ValueLoaders.GetConfigValue<object[]>("OeApplicationNames");

            // Store output in this list.
            List<OeApplicationModel> OutputApps = new List<OeApplicationModel>();
            foreach (var AppObject in PulledAppsObject)
            {
                // Cast the application object into a new model for our app instances.
                ViewModelLogger.WriteLog($"TRYING TO CAST OBJECT {PulledAppsObject.ToList().IndexOf(AppObject)} OF {PulledAppsObject.Length} NOW...", LogType.TraceLog);
                try
                {
                    // Convert this into a string of Json. Then built it into a json cast OE app model
                    string JsonOfObject = JsonConvert.SerializeObject(AppObject);
                    OeApplicationModel NextAppModel = JsonConvert.DeserializeObject<OeApplicationModel>(JsonOfObject);

                    // Add to list of outputs
                    OutputApps.Add(NextAppModel);
                }
                catch { ViewModelLogger.WriteLog("FAILED TO CAST CURRENT OBJECT INTO A NEW OE APP MODEL! MOVING ON", LogType.WarnLog); }
            }

            // Put our usable apps first and soft those A-Z. Append the not usable ones and sort them A-Z
            OutputApps = OutputApps.OrderBy(AppObj => AppObj.IsAppUsable).Reverse().ToList();
            OutputApps = new[] {
                OutputApps.Where(AppObj => AppObj.IsAppUsable).OrderBy(AppObj => AppObj.OEAppName).ToList(),
                OutputApps.Where(AppObj => !AppObj.IsAppUsable).OrderBy(AppObj => AppObj.OEAppName).ToList()    
            }.SelectMany(AppSet => AppSet).ToList();

            // Log output information here.
            ViewModelLogger.WriteLog($"PULLED IN A TOTAL OF {PulledAppsObject.Length} OBJECTS AND CREATED {OutputApps.Count} CAST APP OBJECTS!", LogType.WarnLog);
            ViewModelLogger.WriteLog("RETURNING BUILT APP OBJECT INSTANCES NOW...");
            return new ObservableCollection<OeApplicationModel>(OutputApps);
        }


        /// <summary>
        /// Append a new app into our list of OE apps and store it inside our JSON
        /// </summary>
        /// <returns></returns>
        internal OeApplicationModel AddNewOeApp(string Name, string Path, string Version = "N/A", string BatLaunchCommand = "NO_COMMAND") {
            throw new NotImplementedException("Adding OE Apps is not yet supported");
        }
        /// <summary>
        /// Modifies the command object value of an oe application
        /// </summary>
        /// <param name="AppName"></param>
        /// <param name="NewCommandValue"></param>
        /// <returns></returns>
        internal OeApplicationModel ModifyOeAppCommand(string AppName, string NewCommandValue) {
            throw new NotImplementedException("Modifying OE Apps is not yet supported");
        }
        /// <summary>
        /// Modifies the command object value of an oe application
        /// </summary>
        /// <param name="AppName"></param>
        /// <param name="NewCommandValue"></param>
        /// <returns></returns>
        internal OeApplicationModel ModifyOeAppCommand(OeApplicationModel AppName, string NewCommandValue) {
            throw new NotImplementedException("Modifying OE Apps is not yet supported");
        }
        /// <summary>
        /// Removes an OE App from our list of apps and the JSON
        /// </summary>
        /// <param name="AppToRemove"></param>
        /// <returns>True if removed. False if not.</returns>
        internal bool RemoveOeApp(string AppToRemove) { throw new NotImplementedException("Removing OE Apps is not yet supported"); }
        /// <summary>
        /// Removes an OE App from our list of apps and the JSON
        /// </summary>
        /// <param name="AppToRemove"></param>
        /// <returns>True if removed. False if not.</returns>
        internal bool RemoveOeApp(OeApplicationModel AppToRemove) { throw new NotImplementedException("Removing OE Apps is not yet supported"); }
    }
}
