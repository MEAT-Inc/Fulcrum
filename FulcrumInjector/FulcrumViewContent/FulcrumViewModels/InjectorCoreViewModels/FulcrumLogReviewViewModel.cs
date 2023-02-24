﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.FilteringFormatters;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.InjectorSyntaxFormatters;
using SharpExpressions;
using SharpLogging;
using SharpSimulator;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// Viewmodel object for viewing output log instances from old log files.
    /// </summary>
    internal class FulcrumLogReviewViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private generator objects 
        private PassThruSimulationGenerator _simulationGenerator;
        private PassThruExpressionsGenerator _expressionsGenerator;

        // Helpers for syntax formatting and filtering
        public LogOutputFilteringHelper LogFilteringHelper;
        public InjectorOutputSyntaxHelper InjectorSyntaxHelper;

        // Private backing fields for our public properties
        private string _loadedLogFile = "";
        private string _simulationFile = "";
        private string _expressionsFile = "";
        private string _logFileContents = "";
        private string _simulationFileContents = "";
        private string _expressionsFileContents = "";
        private bool _isLogLoaded = false;
        private bool _simulationBuilt = false;
        private bool _expressionsBuilt = false;
        private ViewerStateType _currentState;

        // Progress Of Viewer actions
        private int _processingProgress = 0;
        private ObservableCollection<PassThruExpression> _lastBuiltExpressions;
        private ObservableCollection<PassThruSimulationChannel> _lastBuiltSimulation;

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool IsLogLoaded { get => _isLogLoaded; set => PropertyUpdated(value); }
        public string LoadedLogFile { get => _loadedLogFile; set => PropertyUpdated(value); }
        public string SimulationFile { get => _simulationFile; set => PropertyUpdated(value); }
        public string ExpressionsFile { get => _expressionsFile; set => PropertyUpdated(value); }
        public string LogFileContents { get => _logFileContents; set => PropertyUpdated(value); }
        public string SimulationFileContents { get => _simulationFileContents; set => PropertyUpdated(value); }
        public string ExpressionsFileContents { get => _expressionsFileContents; set => PropertyUpdated(value); }
        public bool ExpressionsBuilt { get => _expressionsBuilt; set => PropertyUpdated(value); }
        public bool SimulationBuilt { get => _simulationBuilt; set => PropertyUpdated(value); }
        public int ProcessingProgress { get => _processingProgress; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Enum used to set our viewer current state value
        /// </summary>
        public enum ViewerStateType
        {
            [Description("No Content")] NoContent,
            [Description("Input Log File")] ShowingLogFile,
            [Description("Expressions File")] ShowingExpressions,
            [Description("Simulation File")] ShowingSimulation
        }

        #endregion // Structs and Classes


        // ------------------------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="LogReviewUserControl">UserControl which holds our content for the Log Review view</param>
        public FulcrumLogReviewViewModel(UserControl LogReviewUserControl) : base(LogReviewUserControl) 
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP INJECTOR LOG REVIEW VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Toggle parsed value based on contents.
            this.IsLogLoaded = false;
            this.ExpressionsBuilt = false;
            this.ViewModelLogger.WriteLog("TOGGLED ENABLED STATUS OF TOGGLE BUTTON OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);

            // BUG: This is turned off for now. No need to dupe import all of these objects
            // Import Regex objects. 
            // this.ViewModelLogger.WriteLog("CONFIGURING REGEX ENTRIES NOW...");
            // var BuiltObjects = PassThruExpressionShare.GeneratePassThruRegexModels();
            // this.ViewModelLogger.WriteLog($"GENERATED A TOTAL OF {BuiltObjects.Count} REGEX OBJECTS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Combines a set of input log files into one output file.
        /// </summary>
        /// <param name="LogFilePaths"></param>
        /// <returns>Path to a combined output log file.</returns>
        public string CombineLogFiles(string[] LogFilePaths)
        {
            // Find the name of the first file and use it as our base.
            string OutputPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorLogging.DefaultImportFilePath")
            );

            // Build file name here.
            string BaseFileName = $"{Guid.NewGuid().ToString("D").ToUpper()}";
            string FinalFileName = Path.Combine(OutputPath, $"CombinedLogs_{BaseFileName}.shimLog");

            // Now load the files in one by one and combine their output.
            string[] TotalContent = Array.Empty<string>();
            TotalContent = LogFilePaths.SelectMany(FileObj =>
            {
                try { return File.ReadAllLines(FileObj); }
                catch {
                    this.ViewModelLogger.WriteLog("ERROR! FAILED TO PARSE IN ONE OR MORE LOG FILES!", LogType.ErrorLog);
                    return Array.Empty<string>();
                }
            }).ToArray();

            // Write final output contents now.
            this.ViewModelLogger.WriteLog($"WRITING A TOTAL OF {TotalContent.Length} NEW FILE LINES OUT TO OUR OUTPUT LOCATION NOW...", LogType.InfoLog);
            File.WriteAllLines(FinalFileName, TotalContent);
            return FinalFileName;
        }
        /// <summary>
        /// Loads the contents of an input log file object from a given path and stores them into the view.
        /// </summary>
        /// <param name="NewLogFile"></param>
        public bool LoadLogContents(string NewLogFile)
        {
            // Log information, load contents, store values.
            this.ViewModelLogger.WriteLog("LOADING NEW LOG FILE CONTENTS NOW...", LogType.InfoLog);
            FulcrumLogReviewView CastView = this.BaseViewControl as FulcrumLogReviewView;

            try
            {
                // Setup class values
                this._simulationFile = null; this._simulationFileContents = null;
                this._expressionsFile = null; this._expressionsFileContents = null;
                this.ExpressionsBuilt = false; this.SimulationBuilt = false;

                // Make sure the new log file exists
                if (string.IsNullOrWhiteSpace(NewLogFile)) {
                    this.ViewModelLogger.WriteLog("NO LOG FILE LOADED! LOAD A LOG FILE BEFORE TRYING TO USE THIS METHOD!", LogType.InfoLog);
                    throw new FileNotFoundException("FAILED TO LOCATE THE DESIRED FILE! ENSURE ONE IS LOADED FIRST!");
                }

                // Check for an Expressions input file
                if (NewLogFile.EndsWith(".ptExp"))
                {
                    // Store contents for expressions only
                    this.IsLogLoaded = true;
                    this.ExpressionsBuilt = true;
                    this.ExpressionsFile = NewLogFile;
                    this.LoadedLogFile = NewLogFile;
                    this.LogFileContents = File.ReadAllText(this.LoadedLogFile);
                    this.ViewModelLogger.WriteLog("PULLED IN A NEW EXPRESSIONS FILE AND CONVERTED IT INTO A RAW LOG OK!");

                    // Toggle the viewer to show out output
                    if (!this.ToggleViewerContents(ViewerStateType.ShowingExpressions))
                        throw new InvalidOperationException("FAILED TO PROCESS NEW FILE!");
                }
                // Check for a simulation input file
                else if (NewLogFile.EndsWith(".ptSim"))
                {
                    // Store contents for simulations only
                    this.IsLogLoaded = true;
                    this.SimulationBuilt = true;
                    this.SimulationFile = NewLogFile;
                    this.LoadedLogFile = NewLogFile;

                    // Toggle the viewer to show out output
                    if (!this.ToggleViewerContents(ViewerStateType.ShowingSimulation))
                        throw new InvalidOperationException("FAILED TO PROCESS NEW FILE!");
                }
                // All other file types
                else
                {
                    // Pull in the log file for default processing routines
                    this.IsLogLoaded = true;
                    this.LoadedLogFile = NewLogFile;

                    // Toggle the viewer to show out output
                    if (!this.ToggleViewerContents(ViewerStateType.ShowingLogFile))
                        throw new InvalidOperationException("FAILED TO PROCESS NEW FILE!");

                    // Return passed and copy into our temp location
                    string LogFileName = Path.GetFileName(NewLogFile);
                    string DefaultImportLocation = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorLogging.DefaultImportFilePath");
                    Directory.CreateDirectory(DefaultImportLocation);
                    File.Copy(NewLogFile, Path.Combine(DefaultImportLocation, LogFileName), true);
                    this.ViewModelLogger.WriteLog("COPIED IMPORT LOG INTO OUR TEMP FOLDER!");
                }

                // Set log loaded to true and log the results
                this.ViewModelLogger.WriteLog("PROCESSED NEW LOG CONTENT INTO THE MAIN VIEW OK!", LogType.InfoLog);
                return true;
            }
            catch (Exception Ex)
            {
                // Log failed to load and set our contents to just "Failed to Load!" with the exception stack trace.
                this.ViewModelLogger.WriteLog("FAILED TO LOAD NEW LOG FILE! VIEW IS SHOWING STACK TRACE NOW!", LogType.InfoLog);
                this.ViewModelLogger.WriteException("EXCEPTION THROWN IS BEING LOGGED BELOW.", Ex);

                // Store new values.
                this.IsLogLoaded = false; this.LoadedLogFile = null;
                this.LogFileContents = Ex.Message + "\n" + "STACK TRACE:\n" + Ex.StackTrace;
                CastView.Dispatcher.Invoke(() => {
                    CastView.ReplayLogInputContent.Text = this.LogFileContents;
                    CastView.FilteringLogFileTextBox.Text = $"Failed to Load File: {Path.GetFileName(this.LoadedLogFile)}!";
                });

                // Return split content
                return false;   
            }
        }
        /// <summary>
        /// Splits out the input command lines into a set of PTObjects.
        /// </summary>
        /// <param name="OutputExpressions"></param>
        /// <returns></returns>
        public bool BuildLogExpressions()
        {
            try
            {
                // Log we're building a expression file set and build a new expressions generator here 
                this.ProcessingProgress = 0;
                this.ViewModelLogger.WriteLog("PROCESSING LOG LINES INTO EXPRESSIONS NOW...", LogType.InfoLog);
                this._expressionsGenerator = PassThruExpressionsGenerator.LoadPassThruLogFile(this.LoadedLogFile);
                this._expressionsGenerator.OnGeneratorProgress += (SendingGenerator, GeneratorArgs) =>
                {
                    // Invoke a new progress update to our UI content using the generator built
                    if (this.BaseViewControl is not FulcrumLogReviewView CastView) return;
                    this.ProcessingProgress = (int)GeneratorArgs.CurrentProgress;
                };

                // Start by building PTExpressions from input string object sets.
                this.ViewModelLogger.WriteLog("PROCESSING LOG LINES INTO PT EXPRESSION OBJECTS FOR BINDING NOW...", LogType.InfoLog); 
                var BuiltExpressions = this._expressionsGenerator.GenerateLogExpressions();
                this._lastBuiltExpressions = new ObservableCollection<PassThruExpression>(BuiltExpressions);

                // Convert the expression set into a list of file strings now and return list built.
                this._expressionsFile = this._expressionsGenerator.SaveExpressionsFile(this.LoadedLogFile);
                if (this._expressionsFile == "") throw new InvalidOperationException("FAILED TO FIND OUT NEW EXPRESSIONS CONTENT!");
                this.ViewModelLogger.WriteLog($"GENERATED A TOTAL OF {BuiltExpressions.Length} EXPRESSION OBJECTS!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog($"SAVED EXPRESSIONS TO NEW FILE OBJECT NAMED: {this._expressionsFile}!", LogType.InfoLog);
                this.ProcessingProgress = 100; this.ExpressionsBuilt = true;

                // Toggle the viewer to show out output
                if (!this.ToggleViewerContents(ViewerStateType.ShowingExpressions))
                    throw new InvalidOperationException("FAILED TO PROCESS NEW FILE!");

                // Return true at this point since it seems like we built everything correctly
                return true;
            }
            catch (Exception Ex)
            {
                // Log failures, return nothing
                this.ProcessingProgress = 100;
                this.ViewModelLogger.WriteLog("FAILED TO GENERATE NEW EXPRESSION SETUP FROM INPUT CONTENT!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW", Ex);
                this.ExpressionsBuilt = false;
                return false;
            }
        }
        /// <summary>
        /// Builds out a set of expression objects for the simulator generation helper
        /// </summary>
        /// <param name="GeneratorBuilt">Built generation helper</param>
        /// <returns>True if built ok. False if not</returns>
        public bool BuildLogSimulation()
        {
            try
            { 
                // Log we're building a simulation file set and build a new expressions generator here 
                this.ProcessingProgress = 0;
                this.ViewModelLogger.WriteLog("BUILDING SIMULATION FROM LOADED LOG FILE NOW...", LogType.InfoLog);
                this._simulationGenerator = new PassThruSimulationGenerator(this.LoadedLogFile, this._lastBuiltExpressions.ToArray());
                this._expressionsGenerator.OnGeneratorProgress += (SendingGenerator, GeneratorArgs) =>
                {
                    // Invoke a new progress update to our UI content using the generator built
                    if (this.BaseViewControl is not FulcrumLogReviewView CastView) return; 
                    this.ProcessingProgress = (int)GeneratorArgs.CurrentProgress;
                };

                // Now Build our simulation content objects for this generator
                var GeneratedChannels = this._simulationGenerator.GenerateLogSimulation(); 
                this.ViewModelLogger.WriteLog($"BUILT OUT CHANNEL OBJECT SIMULATIONS OK! {GeneratedChannels.Length} ID PAIRS", LogType.InfoLog);

                // Save the built simulation channels as a JSON file here.
                this.SimulationFile = this._simulationGenerator.SaveSimulationFile(this.LoadedLogFile);
                if (this._simulationFile == "") throw new InvalidOperationException("FAILED TO FIND OUT NEW SIMULATION CONTENT!");
                this.ViewModelLogger.WriteLog($"SAVED SIMULATION FILE AT PATH {this.SimulationFile} FROM INPUT EXPRESSIONS!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog($"BUILT A TOTAL OF {this._simulationGenerator.SimulationChannels.Length} SIM CHANNELS!", LogType.InfoLog);
                this.ProcessingProgress = 100; this.SimulationBuilt = true;

                // Toggle the viewer to show out output
                if (!this.ToggleViewerContents(ViewerStateType.ShowingSimulation))
                    throw new InvalidOperationException("FAILED TO PROCESS NEW FILE!");

                // Return true at this point since it seems like we built everything correctly
                return true;
            } 
            catch (Exception BuildSimEx) 
            {
                // Log failures out and return nothing
                this.ProcessingProgress = 100;
                this.ViewModelLogger.WriteLog("FAILED TO BUILD NEW SIMULATION FILE USING INPUT EXPRESSIONS!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION THROWN IS BEING LOGGED BELOW NOW...", BuildSimEx);
                this.SimulationBuilt = false;
                return false;
            }
        }
        /// <summary>
        /// Searches the AvalonEdit object for text matching what we want.
        /// </summary>
        /// <param name="TextToFind"></param>
        public void SearchForText(string TextToFind)
        {
            // Make sure transformer is built
            if (LogFilteringHelper == null) return;
            this.LogFilteringHelper.SearchForText(TextToFind);
        }
        /// <summary>
        /// Toggles the current view contents around the processing output viewer
        /// </summary>
        /// <param name="StateToSet">State to apply</param>
        /// <returns></returns>
        public bool ToggleViewerContents(ViewerStateType StateToSet)
        {
            try
            {
                // Start by getting our string values needed for the desired file.
                this.ViewModelLogger.WriteLog("PULLING IN NEW CONTENT FOR A DESIRED FILE OBJECT OUTPUT NOW!", LogType.WarnLog);

                // Once pulled in, load our content values out.
                string NewViewerContents = string.Empty;
                string NewViewerFileName = string.Empty;
                switch (StateToSet)
                {
                    // For showing expressions
                    case ViewerStateType.ShowingExpressions:
                        if (!this.ExpressionsBuilt) return false;
                        if (_currentState == StateToSet) { return true; }

                        // Store new values
                        NewViewerFileName = this.ExpressionsFile;
                        NewViewerContents = File.ReadAllText(this.ExpressionsFile);
                        this.ExpressionsFileContents = NewViewerContents;
                        break;

                    // For showing simulations
                    case ViewerStateType.ShowingSimulation:
                        if (!this.SimulationBuilt) return false;
                        if (_currentState == StateToSet) { return true; }

                        // Store new values
                        NewViewerFileName = this.SimulationFile;
                        NewViewerContents = File.ReadAllText(this.SimulationFile);
                        this.SimulationFileContents = NewViewerContents;
                        break;

                    // For showing raw log contents
                    case ViewerStateType.ShowingLogFile:
                        if (!this.IsLogLoaded) return false;
                        if (_currentState == StateToSet) { return true; }

                        // Store new values
                        NewViewerFileName = this.LoadedLogFile;
                        NewViewerContents = File.ReadAllText(this.LoadedLogFile);
                        this.LogFileContents = NewViewerContents;
                        break;

                    // For showing nothing in the viewer
                    case ViewerStateType.NoContent:
                        NewViewerFileName = "No Log File!";
                        NewViewerContents = string.Empty;
                        break;
                }

                // Store our contents out here
                this._currentState = StateToSet;
                FulcrumLogReviewView CastView = this.BaseViewControl as FulcrumLogReviewView;
                this.ViewModelLogger.WriteLog("FILE CONTENT PARSED OK! STORING TO VIEW NOW...", LogType.InfoLog);
                CastView.Dispatcher.Invoke(() => {
                    CastView.ReplayLogInputContent.Text = NewViewerContents;
                    CastView.FilteringLogFileTextBox.Text = NewViewerFileName;
                    CastView.ViewerContentComboBox.SelectedIndex = (int)StateToSet - 1;
                });

                // Toggle the showing parsed value.
                this.ViewModelLogger.WriteLog("IMPORTED CONTENT WITHOUT ISSUES! RETURNING NOW.", LogType.InfoLog);
                return true;
            }
            catch (Exception LoadEx)
            {
                // Log failures. Return false.
                this.ViewModelLogger.WriteLog("FAILED TO LOAD IN NEW CONTENTS FOR OUR FILE ENTRIES!");
                this.ViewModelLogger.WriteException("EXCEPTIONS ARE BEING LOGGED BELOW", LoadEx);
                return false;
            }
        }
    }
}