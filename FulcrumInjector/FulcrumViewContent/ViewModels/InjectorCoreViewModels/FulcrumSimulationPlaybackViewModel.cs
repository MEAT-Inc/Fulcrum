using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.FIlteringFormatters;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters;
using Newtonsoft.Json.Linq;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpSimulator;
using SharpSimulator.SimulationObjects;
using SharpWrap2534.SupportingLogic;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// View model for playback during the injector simulation processing
    /// </summary>
    public class FulcrumSimulationPlaybackViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorSimPlaybackViewModelLogger")) ?? new SubServiceLogger("InjectorSimPlaybackViewModelLogger");

        // Simulation Helper objects
        private SimulationLoader _simLoader;

        // Private control values
        private bool _isSimLoaded;
        private string _loadedSimFile;
        private string _loadedSimFileContent;

        // Public values to bind our UI onto
        public bool IsSimLoaded { get => this._isSimLoaded; set => PropertyUpdated(value); }
        public string LoadedSimFile { get => this._loadedSimFile; set => PropertyUpdated(value); }
        public string LoadedSimFileContent { get => this._loadedSimFileContent; set => PropertyUpdated(value); }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new simulation playback view model
        /// </summary>
        public FulcrumSimulationPlaybackViewModel()
        {
            // Log built VM OK and build a new sim loader/generator
            this.IsSimLoaded = false;
            ViewModelLogger.WriteLog("BUILT NEW SIMULATION PLAYBACK VIEW MODEL LOGGER AND INSTANCE OK!", LogType.InfoLog);
        }


        /// <summary>
        /// Loads in a new simulation file and stores it onto our view model and view
        /// </summary>
        /// <param name="SimFile">File to load into our simulation playback helper</param>
        /// <returns>True if loaded, false if not</returns>
        public bool LoadSimulation(string SimFile)
        {
            // Try and load the simulation file in first
            if (!File.Exists(SimFile)) {
                ViewModelLogger.WriteLog($"FILE {SimFile} DOES NOT EXIST! CAN NOT LOAD NEW FILE!");
                this.IsSimLoaded = false;
                return false;
            }

            // Build a new Simulation Loader and parse contents of the sim file into it.
            this._simLoader = new SimulationLoader();
            this.LoadedSimFileContent = File.ReadAllText(SimFile);
            SimulationChannel[] InputSimChannels = JArray.Parse(File.ReadAllText(this.LoadedSimFileContent)).ToObject<SimulationChannel[]>();
            ViewModelLogger.WriteLog("PULLED IN NEW SIMULATION JSON CONTENTS WITHOUT ISSUES! STORING ONTO SIM LOADER NOW...", LogType.InfoLog);
            foreach (var SimChannel in InputSimChannels) { this._simLoader.AddSimChannel(SimChannel); }
            ViewModelLogger.WriteLog($"PULLED ALL {InputSimChannels.Length} INPUT SIMULATION CHANNELS INTO OUR LOADER WITHOUT FAILURE!", LogType.InfoLog);

            // Load file contents and store name of file on our view model
            this.IsSimLoaded = true;
            this.LoadedSimFile = SimFile;
            ViewModelLogger.WriteLog($"LOADED NEW SIMULATION FILE {SimFile} OK! STORING CONTENTS OF IT ON VIEW MODEL FOR EDITOR NOW...", LogType.WarnLog);
            return true;
        }

        /// <summary>
        /// Kicks off our new Simulation player passing in the specified device and DLL name for our instances
        /// </summary>
        /// <param name="Version">Version of J2534 API to use</param>
        /// <param name="DllName">Name of DLL to use</param>
        /// <param name="DeviceName">Name of device to use</param>
        /// <returns></returns>
        public bool StartSimulation(JVersion Version, string DllName, string DeviceName)
        {
            // TODO: BUILD IN LOGIC FOR RUNNING SIMULATIONS!
            return false;
        }
    }
}
