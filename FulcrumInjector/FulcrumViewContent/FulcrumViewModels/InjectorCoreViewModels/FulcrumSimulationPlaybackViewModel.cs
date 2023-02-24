using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogging;
using SharpSimulator;
using SharpWrapper.PassThruTypes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// View model for playback during the injector simulation processing
    /// </summary>
    internal class FulcrumSimulationPlaybackViewModel : FulcrumViewModelBase
    {
        #region Custom Events

        /// <summary>
        /// Processes an event for a new Simulation channel being built or closed out
        /// </summary>
        /// <param name="SendingObject">Object firing this event</param>
        /// <param name="ChannelEventArgs">Channel changed event arguments</param>
        private void SimPlayer_SimChannelChanged(object SendingObject, PassThruSimulationPlayer.SimChannelEventArgs ChannelEventArgs)
        {
            // Convert and build new event to object. Store in temp copy to trigger property updated
            this.SimEventsProcessed = this.SimEventsProcessed.Append(ChannelEventArgs).ToArray();
            this.ViewModelLogger.WriteLog("BUILT NEW CONVERSION FOR SIMULATION CHANNEL INTO OBJECT FOR UI BINDING OK!", LogType.TraceLog);
        }
        /// <summary>
        /// Processes changes in events for message contents being pulled in
        /// </summary>
        /// <param name="SendingObject">Object that sent the event</param>
        /// <param name="MessageEventArgs">Event arguments</param>
        private void SimPlayer_SimMessageProcessed(object SendingObject, PassThruSimulationPlayer.SimMessageEventArgs MessageEventArgs)
        {
            // Convert and build new event to object. Store in temp copy to trigger property updated
            this.SimEventsProcessed = this.SimEventsProcessed.Append(MessageEventArgs).ToArray();
            this.ViewModelLogger.WriteLog("BUILT NEW CONVERSION FOR SIMULATION MESSAGE INTO OBJECT FOR UI BINDING OK!", LogType.TraceLog);
        }

        #endregion // Custom Events

        #region Fields

        // Private backing fields for our public properties
        private bool _isSimLoaded;
        private bool _isHardwareSetup;
        private bool _isSimStarting;
        private bool _isSimulationRunning;
        private string _loadedSimFile;
        private string _loadedSimFileContent;
        private EventArgs[] _simEventsProcessed;
        private PassThruSimulationPlayer _simulationPlayer;
        private List<PassThruSimulationChannel> _simulationChannels;

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool IsSimLoaded { get => this._isSimLoaded; set => PropertyUpdated(value); }
        public bool IsHardwareSetup { get => this._isHardwareSetup; set => PropertyUpdated(value); }
        public bool IsSimStarting { get => this._isSimStarting; set => PropertyUpdated(value); }
        public bool IsSimulationRunning { get => this._isSimulationRunning; set => PropertyUpdated(value); }

        // Content for the current loaded simulation file
        public string LoadedSimFile { get => this._loadedSimFile; set => PropertyUpdated(value); }
        public string LoadedSimFileContent { get => this._loadedSimFileContent; set => PropertyUpdated(value); }

        // Lists of Messages that are being tracked by our simulation
        public EventArgs[] SimEventsProcessed { get => this._simEventsProcessed; set => PropertyUpdated(value); }
        public PassThruSimulationChannel[] SimulationChannels { get => this._simulationChannels.ToArray(); set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes


        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new simulation playback view model
        /// </summary>
        /// <param name="SimulationUserControl">UserControl which holds the content for our simulation playback view</param>
        public FulcrumSimulationPlaybackViewModel(UserControl SimulationUserControl) : base(SimulationUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP SIMULATION PLAYBACK VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Setup empty list of our events here
            this.SimEventsProcessed ??= Array.Empty<EventArgs>();
            this._simulationChannels = new List<PassThruSimulationChannel>();

            // Log completed building view model instance and exit out
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads in a new simulation file and stores it onto our view model and view
        /// </summary>
        /// <param name="SimFile">File to load into our simulation playback helper</param>
        /// <returns>True if loaded, false if not</returns>
        public bool LoadSimulation(string SimFile)
        {
            // Try and load the simulation file in first
            if (!File.Exists(SimFile)) {
                this.ViewModelLogger.WriteLog($"FILE {SimFile} DOES NOT EXIST! CAN NOT LOAD NEW FILE!");
                this.IsSimLoaded = false;
                return false;
            }

            // Clear out all of our old values first
            int FailedCounter = 0;
            this.IsSimLoaded = false; this.IsSimulationRunning = false;
            this.LoadedSimFile = string.Empty; this.LoadedSimFileContent = string.Empty;
            this._simulationChannels ??= new List<PassThruSimulationChannel>();

            try
            {
                // Store all the file content on this view model instance and load in the Simulation channels from it now
                this.LoadedSimFileContent = File.ReadAllText(SimFile);
                var PulledChannels = JObject.Parse(this.LoadedSimFileContent);
                foreach (var ChannelInstance in PulledChannels.Children())
                {
                    try
                    {
                        // Try and build our channel here
                        JToken ChannelToken = ChannelInstance.First;
                        if (ChannelToken == null) 
                            throw new InvalidDataException("Error! Input channel was seen to be an invalid layout!");

                        // Now using the JSON Converter, unwrap the channel into a simulation object and store it on our player
                        PassThruSimulationChannel BuiltChannel = ChannelToken.ToObject<PassThruSimulationChannel>();
                        this._simulationChannels.Add(BuiltChannel);
                    }
                    catch (Exception ConvertEx)
                    {
                        // Log failures out here
                        FailedCounter++;
                        this.ViewModelLogger.WriteLog("FAILED TO CONVERT SIMULATION CHANNEL FROM JSON TO OBJECT!", LogType.ErrorLog);
                        this.ViewModelLogger.WriteLog("EXCEPTION AND CHANNEL OBJECT ARE BEING LOGGED BELOW...", LogType.WarnLog);
                        this.ViewModelLogger.WriteLog($"SIM CHANNEL JSON:\n{ChannelInstance.ToString(Formatting.Indented)}", LogType.TraceLog);
                        this.ViewModelLogger.WriteException("EXCEPTION THROWN:", ConvertEx);
                    }
                }

                // Load file contents and store name of file on our view model
                this.IsSimLoaded = true;
                this.IsSimLoaded = true;
                this.LoadedSimFile = SimFile;
                this.ViewModelLogger.WriteLog($"LOADED NEW SIMULATION FILE {SimFile} OK! STORING CONTENTS OF IT ON VIEW MODEL FOR EDITOR NOW...", LogType.WarnLog);
                this.ViewModelLogger.WriteLog($"PULLED IN A TOTAL OF {this._simulationChannels.Count} INPUT SIMULATION CHANNELS INTO OUR LOADER WITHOUT FAILURE!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog($"ENCOUNTERED A TOTAL OF {FailedCounter} FAILURES WHILE LOADING CHANNELS!", LogType.InfoLog);
                return true;
            }
            catch (Exception LoadSimEx)
            {
                // Log failure out and return false
                this.ViewModelLogger.WriteLog($"FAILED TO LOAD IN SIMULATION FILE {SimFile}!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("SIMULATION LOAD EXCEPTION IS BEING LOGGED BELOW!", LoadSimEx);

                // Set Loaded to false and return false
                this.IsSimLoaded = false;
                return false;
            }
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
            // Setup the simulation player
            this.ViewModelLogger.WriteLog("SETTING UP NEW SIMULATION PLAYER FOR THE CURRENTLY BUILT LOADER OBJECT...", LogType.WarnLog);

            // Pull in a base configuration and build a new reader
            // TODO: CONFIGURE THIS TO USE INPUT VALUES FROM SETUP
            var SimConfig = PassThruSimulationConfiguration.LoadSimulationConfig(ProtocolId.ISO15765);
            this._simulationPlayer = FulcrumConstants.SharpSessionAlpha == null
                ? new PassThruSimulationPlayer(this.SimulationChannels, Version, DllName, DeviceName)
                : new PassThruSimulationPlayer(this.SimulationChannels, FulcrumConstants.SharpSessionAlpha);

            // Reset input values if the sharp session isn't null
            this.ViewModelLogger.WriteLog($"NEW SIMULATION PLAYER WITH FOR VERSION {Version} USING DEVICE {DeviceName} ({DllName}) HAS BEEN SETUP!", LogType.InfoLog);

            // Subscribe to the player events and channel events
            // BUG: WHEN WE TRIGGER TOO MANY EVENTS, SOME ARE BEING DROPPED!
            this._simulationPlayer.SimChannelChanged += SimPlayer_SimChannelChanged;
            this._simulationPlayer.SimMessageProcessed += SimPlayer_SimMessageProcessed;
            this.ViewModelLogger.WriteLog("SUBSCRIBED OUR VIEW MODEL TO OUR SIMULATION PLAYER OK!", LogType.InfoLog);

            // Configure our simulation player here
            this._simulationPlayer.SetResponsesEnabled(true);
            this._simulationPlayer.SetDefaultConfigurations(SimConfig.ReaderConfigs);
            this._simulationPlayer.SetDefaultMessageFilters(SimConfig.ReaderFilters);
            this._simulationPlayer.SetDefaultConnectionType(SimConfig.ReaderProtocol, SimConfig.ReaderChannelFlags, SimConfig.ReaderBaudRate);
            this._simulationPlayer.SetDefaultMessageValues(SimConfig.ReaderTimeout, SimConfig.ReaderMsgCount, SimConfig.ResponseTimeout);
            this.ViewModelLogger.WriteLog("CONFIGURED ALL NEEDED SETUP VALUES FOR OUR SIMULATION PLAYER OK! STARTING INIT ROUTINE NOW...", LogType.InfoLog);

            // Run the init routine and start reading output here
            this._simulationPlayer.InitializeSimReader(); 
            this._simulationPlayer.StartSimulationReader(); 
            this.ViewModelLogger.WriteLog("STARTED SIMULATION PLAYER FOR OUR LOADED SIMULATION OK! MESSAGE DATA IS BEING PROCESSED TILL THIS TASK IS KILLED!");

            // Return done.
            this.IsSimulationRunning = true;
            return true;
        }
        /// <summary>
        /// Stops the simulation reader currently running
        /// </summary>
        /// <returns>True if stopped. False if not</returns>
        public bool StopSimulation()
        {
            // Stop the reader object here if it's playing
            if (!this._simulationPlayer.SimulationReading || !this.IsSimulationRunning) {
                this.ViewModelLogger.WriteLog("CAN NOT STOP SIM READER SINCE IT IS NOT CURRENTLY RUNNING!", LogType.ErrorLog);
                return false;
            }

            // Stop it now and log passed
            this.IsSimulationRunning = false;
            this._simulationPlayer.StopSimulationReader();
            this.ViewModelLogger.WriteLog("STOPPED SIMULATION READER WITHOUT ISSUES!", LogType.InfoLog);
            return true;
        }
    }
}
