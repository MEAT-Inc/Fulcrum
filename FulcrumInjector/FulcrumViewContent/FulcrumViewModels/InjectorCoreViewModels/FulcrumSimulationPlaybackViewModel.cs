﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumJson;
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
    public class FulcrumSimulationPlaybackViewModel : FulcrumViewModelBase
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
        private bool _isNewConfig;                                                  // Sets if we're building a new config or not
        private bool _isSimLoaded;                                                  // Determines if the simulation is loaded
        private bool _isSimStarting;                                                // Determines if the simulation is booting
        private bool _isEditingConfig;                                              // Tells us if we're in edit mode or not
        private bool _isHardwareSetup;                                              // Determines if hardware is configured
        private bool _canDeleteConfig;                                              // Determines if we can delete a configuration
        private string _loadedSimFile;                                              // Currently loaded simulation file
        private bool _isSimulationRunning;                                          // Determines if the simulation is running
        private string _loadedSimFileContent;                                       // Currently loaded simulation file content
        private EventArgs[] _simEventsProcessed;                                    // Events fired during the simulation
        private PassThruSimulationPlayer _simulationPlayer;                         // The player running the simulation
        private List<PassThruSimulationChannel> _simulationChannels;                // The channels being simulated
        private PassThruSimulationConfiguration _loadedConfiguration;               // Configuration currently loaded for playback
        private PassThruSimulationConfiguration _customConfiguration;               // Configuration for user defined routines
        private List<PassThruSimulationConfiguration> _simulationConfigurations;    // All supported simulation configurations

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool IsEditingConfig
        {
            get => this._isEditingConfig;
            set
            {
                // Update the backing property and store the current configuration as our custom one
                this.PropertyUpdated(value);
                if (value) this.CustomConfiguration = LoadedConfiguration;
            }
        }
        public bool IsNewConfig { get => this._isNewConfig; set => PropertyUpdated(value); }
        public bool IsSimLoaded { get => this._isSimLoaded; set => PropertyUpdated(value); }
        public bool IsSimStarting { get => this._isSimStarting; set => PropertyUpdated(value); }
        public bool CanDeleteConfig { get => this._canDeleteConfig; set => PropertyUpdated(value); }
        public bool IsHardwareSetup { get => this._isHardwareSetup; set => PropertyUpdated(value); }
        public bool IsSimulationRunning { get => this._isSimulationRunning; set => PropertyUpdated(value); }

        // Content for the current loaded simulation file
        public string LoadedSimFile { get => this._loadedSimFile; set => PropertyUpdated(value); }
        public string LoadedSimFileContent { get => this._loadedSimFileContent; set => PropertyUpdated(value); }

        // Lists of Messages that are being tracked by our simulation along with the simulation configuration
        public EventArgs[] SimEventsProcessed { get => this._simEventsProcessed; set => PropertyUpdated(value); }
        public PassThruSimulationChannel[] SimulationChannels { get => this._simulationChannels.ToArray(); set => PropertyUpdated(value.ToList()); }

        // Currently applied simulation configuration and configurations we're able to load in
        public PassThruSimulationConfiguration LoadedConfiguration
        {
            get => this._loadedConfiguration;
            set
            {
                // Update our backing field and configure some other values for editing
                PropertyUpdated(value);
                
                // Set if we can delete this configuration or not
                this.IsNewConfig = string.IsNullOrWhiteSpace(value.ConfigurationName);
                this.CanDeleteConfig = !PassThruSimulationConfiguration.SupportedConfigurations.ToList().Contains(value);
            }
        }
        public PassThruSimulationConfiguration CustomConfiguration
        {
            get => this._customConfiguration;
            set => this.PropertyUpdated(value);
        }
        public List<PassThruSimulationConfiguration> SimulationConfigurations { get => this._simulationConfigurations; set => PropertyUpdated(value); }

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

            // Setup empty list of our events here and build a collection of simulation configurations
            this._importSimulationConfigurations();
            this.SimEventsProcessed ??= Array.Empty<EventArgs>();
            this.SimulationChannels = new PassThruSimulationChannel[] { };
            
            // Log completed building view model instance and exit out
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method used to save a given configuration object to our view model and settings page
        /// </summary>
        /// <param name="UpdatedConfiguration">The configuration we're saving</param>
        /// <returns>True if the configuration is saved, false if it is not</returns>
        public bool SaveSimConfiguration(PassThruSimulationConfiguration UpdatedConfiguration)
        {
            // Add this configuration to our list of imported values and store it on our settings file
            string ConfigurationName = UpdatedConfiguration.ConfigurationName;
            this.ViewModelLogger.WriteLog($"SAVING OR UPDATING VALUES FOR CONFIGURATION {ConfigurationName}...");
            int IndexOfConfig = this.SimulationConfigurations.FindIndex(ConfigObj => ConfigObj.ConfigurationName == ConfigurationName);
            if (IndexOfConfig != -1)
            {
                // Find the existing value and just replace it
                this.SimulationConfigurations[IndexOfConfig] = UpdatedConfiguration;
                this.OnPropertyChanged(nameof(this.SimulationConfigurations));
                this.ViewModelLogger.WriteLog($"REPLACED CONFIGURATION {ConfigurationName} WITH UPDATED VALUES!");
            }
            else
            {
                // Add the configuration as a new object here
                this.SimulationConfigurations.Add(UpdatedConfiguration);
                this.ViewModelLogger.WriteLog($"ADDED CONFIGURATION {ConfigurationName} TO CONFIGURATIONS LIST!");
            }

            // Build a new list of configurations to write out to our JSON file
            var CustomConfigsObject = ValueLoaders.GetConfigValue<object>("FulcrumSimConfigurations.CustomConfigurations");
            if (CustomConfigsObject is not PassThruSimulationConfiguration[] CustomConfigs) CustomConfigs = new[] { UpdatedConfiguration }; 
            else CustomConfigs = CustomConfigs.Append(UpdatedConfiguration).ToArray();

            // Build a new list of protocols to write out to our JSON file 
            List<ProtocolId> SupportedProtocols = PassThruSimulationConfiguration.SupportedProtocols.ToList();
            foreach (var CustomConfig in CustomConfigs)
                if (!SupportedProtocols.Contains(CustomConfig.ReaderProtocol))
                    SupportedProtocols.Add(CustomConfig.ReaderProtocol);

            // Write the JSON contents out for our configurations
            this.ViewModelLogger.WriteLog("STORING USER DEFINED CONFIGURATIONS ON OUR SETTINGS FILE NOW", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"TOTAL OF {CustomConfigs.Length} CUSTOM CONFIGURATIONS AND {SupportedProtocols.Count} PROTOCOLS");
            ValueSetters.SetValue("FulcrumSimConfigurations.SupportedProtocols", SupportedProtocols);
            ValueSetters.SetValue("FulcrumSimConfigurations.CustomConfigurations", CustomConfigs);

            // Log that we've saved the content requested and exit out
            this.ViewModelLogger.WriteLog("STORED UPDATED CONFIGURATION VALUES ON SETTINGS FILE CORRECTLY!", LogType.InfoLog);
            this.LoadedConfiguration = UpdatedConfiguration;
            return true;
        }
        /// <summary>
        /// Helper method used to delete a given configuration object from view model and settings page
        /// </summary>
        /// <param name="ConfigurationName">The configuration name we're deleting</param>
        /// <returns>True if the configuration is deleted, false if it is not</returns>
        public bool DeleteSimConfiguration(string ConfigurationName)
        {
            // TODO: Build logic for removing configurations from view model list
            return false;
        }

        /// <summary>
        /// Loads in a new simulation file and stores it onto our view model and view
        /// </summary>
        /// <param name="SimulationFile">File to load into our simulation playback helper</param>
        /// <returns>True if loaded, false if not</returns>
        public bool LoadSimulation(string SimulationFile)
        {
            // Try and load the simulation file in first
            if (!File.Exists(SimulationFile))
            {
                this.ViewModelLogger.WriteLog($"FILE {SimulationFile} DOES NOT EXIST! CAN NOT LOAD NEW FILE!");
                this.IsSimLoaded = false;
                return false;
            }

            // Clear out all of our old values first
            int FailedCounter = 0;
            this.IsSimLoaded = false;
            this.IsSimulationRunning = false;
            this.LoadedSimFile = string.Empty;
            this.LoadedSimFileContent = string.Empty;
            this._simulationChannels ??= new List<PassThruSimulationChannel>();

            try
            {
                // Store all the file content on this view model instance and load in the Simulation channels from it now
                this.LoadedSimFileContent = File.ReadAllText(SimulationFile);
                var PulledChannels = JArray.Parse(this.LoadedSimFileContent);
                foreach (var ChannelInstance in PulledChannels.Children())
                {
                    try
                    {
                        // Try and build our channel here
                        JToken ChannelToken = ChannelInstance.Last;
                        if (ChannelToken == null)
                            throw new InvalidDataException("Error! Input channel was seen to be an invalid layout!");

                        // Now using the JSON Converter, unwrap the channel into a simulation object and store it on our player
                        PassThruSimulationChannel BuiltChannel = ChannelToken.First.ToObject<PassThruSimulationChannel>();
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
                this.LoadedSimFile = SimulationFile;
                this.ViewModelLogger.WriteLog($"LOADED NEW SIMULATION FILE {SimulationFile} OK! STORING CONTENTS OF IT ON VIEW MODEL FOR EDITOR NOW...", LogType.WarnLog);
                this.ViewModelLogger.WriteLog($"PULLED IN A TOTAL OF {this._simulationChannels.Count} INPUT SIMULATION CHANNELS INTO OUR LOADER WITHOUT FAILURE!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog($"ENCOUNTERED A TOTAL OF {FailedCounter} FAILURES WHILE LOADING CHANNELS!", LogType.InfoLog);
                return true;
            }
            catch (Exception LoadSimEx)
            {
                // Log failure out and return false
                this.ViewModelLogger.WriteLog($"FAILED TO LOAD IN SIMULATION FILE {SimulationFile}!", LogType.ErrorLog);
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

            // Pull in a base configuration and build a new reader. If no configuration is given, return out
            if (this._loadedConfiguration == null) return false;
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
            this._simulationPlayer.SetPlaybackConfiguration(this._loadedConfiguration);
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

            try
            {
                // Stop it now and log passed
                this.IsSimulationRunning = false;
                this._simulationPlayer.StopSimulationReader();
                this.ViewModelLogger.WriteLog("STOPPED SIMULATION READER WITHOUT ISSUES!", LogType.InfoLog);
                return true;
            }
            catch
            {
                // Ignored since no matter what happens the reader is killed
                this.ViewModelLogger.WriteLog("WARNING! SESSION CLOSE ROUTINE EXITED INCORRECTLY!", LogType.WarnLog);
                this.ViewModelLogger.WriteLog("THIS IS NOT THE END OF THE WORLD, BUT SHOULD BE ADDRESSED!", LogType.WarnLog);
                return true;
            }
        }

        /// <summary>
        /// Helper method used to load the simulation configurations from both the simulation package and user defined configurations
        /// </summary>
        private void _importSimulationConfigurations()
        {
            // Store the predefined configurations on our view model instance first
            List<ProtocolId> SupportedProtocols = PassThruSimulationConfiguration.SupportedProtocols.ToList();
            this.SimulationConfigurations = PassThruSimulationConfiguration.SupportedConfigurations.ToList();

            // Store the default configurations and the default protocols on our settings object
            ValueSetters.SetValue("FulcrumSimConfigurations.PredefinedConfigurations", this.SimulationConfigurations);
            this.ViewModelLogger.WriteLog($"PULLED IN {this.SimulationConfigurations.Count} PREDEFINED SIM CONFIGURATIONS FOR {SupportedProtocols.Count} PROTOCOLS!");

            // Look at our settings object for user defined configurations here
            var CustomConfigsObject = ValueLoaders.GetConfigValue<object>("FulcrumSimConfigurations.CustomConfigurations");
            if (CustomConfigsObject is not PassThruSimulationConfiguration[] CustomConfigs) 
                this.ViewModelLogger.WriteLog("NO CUSTOM SIM CONFIGURATIONS WERE FOUND!", LogType.WarnLog);
            else
            {
                // Import all the user defined configurations and update our protocol list
                this.ViewModelLogger.WriteLog($"IMPORTING {CustomConfigs.Length} CUSTOM SIMULATION CONFIGURATIONS...");
                foreach (var PassThruSimConfig in CustomConfigs)
                {
                    // Store the configuration and protocol if they're unique to this configuration
                    if (this.SimulationConfigurations.All(SimConfig => SimConfig.ConfigurationName != PassThruSimConfig.ConfigurationName)) 
                        this.SimulationConfigurations.Add(PassThruSimConfig);
                    if (!SupportedProtocols.Contains(PassThruSimConfig.ReaderProtocol))
                        SupportedProtocols.Add(PassThruSimConfig.ReaderProtocol);

                    // Log out the configuration added in and move on to the next one
                    this.ViewModelLogger.WriteLog($"IMPORTED CONFIGURATION {PassThruSimConfig.ConfigurationName}!");
                }
            }

            // Write the new list of supported protocol values out to the settings file 
            List<string> ProtocolStrings = SupportedProtocols.Select(ProcId => ProcId.ToString()).ToList();
            ValueSetters.SetValue("FulcrumSimConfigurations.SupportedProtocols", ProtocolStrings);
            this.ViewModelLogger.WriteLog("UPDATED SUPPORTED PROTOCOL LIST CORRECTLY!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"CONFIGURATIONS SUPPORT {SupportedProtocols.Count} PROTOCOLS TOTAL!");
        }
    }
}
