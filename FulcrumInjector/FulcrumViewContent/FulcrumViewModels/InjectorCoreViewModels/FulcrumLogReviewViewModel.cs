using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
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

        // Fields holding our generator and log formatting objects 
        public LogOutputFilteringHelper LogFilteringHelper;         // Format helper for the loaded log files
        public InjectorOutputSyntaxHelper InjectorSyntaxHelper;     // Format helper for the PassThru log files
        
        // Private backing fields holding information about our viewer
        private bool _isLogLoaded = false;                          // Tells us if a log file is loaded or not
        private int _processingProgress = 0;                        // Current progress value for parsing routines
        private ViewerStateType _currentState;                      // Current view type for our output viewer

        // Private backing fields holding information about the currently loaded log files
        private LogFileSet _currentLogSet;                   // The currently loaded log file set
        private FulcrumLogFileModel _currentLogFile;                // The log file being viewed at this time
        
        #endregion // Fields

        #region Properties

        // Public properties holding information about the loaded log file and log file sets
        public LogFileSet CurrentLogSet
        {
            get => this._currentLogSet;
            set
            {
                // Update the value of IsLoaded and store our value
                PropertyUpdated(value);
            } 
        }
        public FulcrumLogFileModel CurrentLogFile
        {
            get => this._currentLogFile;
            set
            {
                // Store a new value for our model instance type
                PropertyUpdated(value);
                this.IsLogLoaded = value != null;
                
                // Based on the value we're storing, update our viewer contents
                if (this._currentLogFile.LogFileType == FulcrumLogFileModel.LogFileTypes.PASSTHRU_FILE)
                    this._toggleViewerContents(ViewerStateType.ShowingLogFile);
                else if (this._currentLogFile.LogFileType == FulcrumLogFileModel.LogFileTypes.EXPRESSIONS_FILE)
                    this._toggleViewerContents(ViewerStateType.ShowingExpressions);
                else if (this._currentLogFile.LogFileType == FulcrumLogFileModel.LogFileTypes.SIMULATIONS_FILE)
                    this._toggleViewerContents(ViewerStateType.ShowingSimulation);
                else if (this._currentLogFile.LogFileType == FulcrumLogFileModel.LogFileTypes.UNKNOWN_FILE)
                    this._toggleViewerContents(ViewerStateType.NoContent);
            }
        }

        // Public facing properties holding configuration values for our view content
        public bool IsLogLoaded { get => _isLogLoaded; set => PropertyUpdated(value); }
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
            this.ViewModelLogger.WriteLog("TOGGLED ENABLED STATUS OF TOGGLE BUTTON OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads the contents of an input log file object from a given path and stores them into the view.
        /// </summary>
        /// <param name="LogFilePaths"></param>
        public bool LoadLogContents(params string[] LogFilePaths)
        {
            // Log information, load contents, store values.
            this.ViewModelLogger.WriteLog("LOADING NEW LOG FILE CONTENTS NOW...", LogType.WarnLog);

            // Check if we've got more than one log file loaded in 
            string LogFileToLoad = string.Empty;
            if (LogFilePaths.Length == 0) throw new ArgumentException("Error! One or more file paths must be provided!");
            else if (LogFilePaths.Length == 1) LogFileToLoad = LogFilePaths.FirstOrDefault();
            else
            {
                // Build our output combined content file name using the first file name provided in our set
                LogFileToLoad = Path.Combine(
                    ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorResources.FulcrumImportFilePath"),
                    $"FulcrumImport_{Path.GetFileNameWithoutExtension(LogFilePaths.FirstOrDefault())}.shimLog");

                // Now load the files in one by one and combine their output.
                File.WriteAllText(LogFileToLoad, string.Join("\n\n", LogFilePaths.Select(File.ReadAllText)));
                this.ViewModelLogger.WriteLog($"WROTE OUT CONTENT FOR A NEW COMBINED LOG FILE NAMED {LogFileToLoad}!", LogType.InfoLog);
            }

            // Make sure the new log file exists
            if (string.IsNullOrWhiteSpace(LogFileToLoad))
            {
                // Log that we're unable to load a file that's not real and exit out of this method
                this.ViewModelLogger.WriteLog("NO LOG FILE LOADED! LOAD A LOG FILE BEFORE TRYING TO USE THIS METHOD!", LogType.ErrorLog);
                throw new FileNotFoundException("FAILED TO LOCATE THE DESIRED FILE! ENSURE ONE IS LOADED FIRST!");
            }

            // Try and find a model set where this file exists currently. If none are found, then build a new instance
            LogFileSet LogFileModelSet = new LogFileSet();
            FulcrumLogFileModel LoadedFileModel = new FulcrumLogFileModel(LogFileToLoad);
            this.ViewModelLogger.WriteLog($"BUILT NEW FILE MODEL AND LOG FILE MODEL SET FOR FILE {LogFileToLoad}");

            // Now store the new PassThru log file value on our model set and add it to our collection on the view model
            LogFileModelSet.StorePassThruLogFile(LoadedFileModel);
            this.ViewModelLogger.WriteLog("LOG FILE MODEL HAS BEEN STORED ON THE LOG FILE SET CORRECTLY");

            // If the current log file value is not set, then set it now
            this.CurrentLogSet = LogFileModelSet;
            this.CurrentLogFile = LoadedFileModel;
            this.ViewModelLogger.WriteLog("STORED NEW LOG FILE MODELS ON OUR VIEW MODEL INSTANCE FOR REVIEW CORRECTLY");

            // Build out the new log file name and ensure the output directory exists
            string DefaultImportLocation = ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorResources.FulcrumImportFilePath");
            if (!File.Exists(Path.Combine(DefaultImportLocation, Path.GetFileName(LogFileToLoad))))
            {
                // Only do this if the log file is not built
                if (!Directory.Exists(DefaultImportLocation)) Directory.CreateDirectory(DefaultImportLocation);
                File.Copy(LogFileToLoad, Path.Combine(DefaultImportLocation, Path.GetFileName(LogFileToLoad)), true);
                this.ViewModelLogger.WriteLog("IMPORTED LOCAL LOG FILE INTO OUR INJECTOR IMPORT FOLDER CORRECTLY");
            }

            // Log out that we've finally imported this file instance correctly and return passed
            this.ViewModelLogger.WriteLog("PROCESSED NEW LOG CONTENT INTO THE MAIN VIEW OK!", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Splits out the input command lines into a set of PTObjects.
        /// </summary>
        /// <returns>True if expressions generation passes, false if not</returns>
        public bool GenerateLogExpressions()
        {
            try
            {
                // Log we're building a expression file set and build a new expressions generator here 
                this.ProcessingProgress = 0;
                this.ViewModelLogger.WriteLog("PROCESSING LOG LINES INTO EXPRESSIONS NOW...", LogType.InfoLog);
                var ExpGenerator = PassThruExpressionsGenerator.LoadPassThruLogFile(this.CurrentLogSet.PassThruLogFile.LogFilePath);
                ExpGenerator.OnGeneratorProgress += (_, GeneratorArgs) =>
                {
                    // Invoke a new progress update to our UI content using the generator built
                    if (this.BaseViewControl is not FulcrumLogReviewView) return;

                    // If the progress value reported back is the same as it is currently, don't set it again
                    int NextProgress = (int)GeneratorArgs.CurrentProgress;
                    if (this.ProcessingProgress != NextProgress) this.ProcessingProgress = NextProgress;
                };

                // Start by building PTExpressions from input string object sets.
                this.ViewModelLogger.WriteLog("PROCESSING LOG LINES INTO PT EXPRESSION OBJECTS FOR BINDING NOW...", LogType.InfoLog); 
                var BuiltExpressions = ExpGenerator.GenerateLogExpressions();
                var BuiltExpressionsFile = ExpGenerator.SaveExpressionsFile(this.CurrentLogSet.PassThruLogFile.LogFilePath);
                if (BuiltExpressionsFile == "") throw new InvalidOperationException("FAILED TO FIND OUT NEW EXPRESSIONS CONTENT!");
                
                // Once we've built the new expressions file contents and files, we can store them on our log file set
                var ExpressionsFileModel = new FulcrumLogFileModel(BuiltExpressionsFile);
                this.CurrentLogSet.StoreExpressionsFile(ExpressionsFileModel, BuiltExpressions);

                // Log out some information about the expressions built and toggle our view contents
                this.ViewModelLogger.WriteLog($"GENERATED A TOTAL OF {BuiltExpressions.Length} EXPRESSION OBJECTS!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog($"SAVED EXPRESSIONS TO NEW FILE OBJECT NAMED: {BuiltExpressionsFile}!", LogType.InfoLog);
                this.ProcessingProgress = 100;

                // Set our new log file model so the view content updates and exit out
                FulcrumLogReviewView CastView = this.BaseViewControl as FulcrumLogReviewView;
                CastView.Dispatcher.Invoke(() => { CastView.ViewerContentComboBox.SelectedIndex = 1; });
                return true;
            }
            catch (Exception Ex)
            {
                // Log failures, return nothing
                this.ProcessingProgress = 100;
                this.ViewModelLogger.WriteLog("FAILED TO GENERATE NEW EXPRESSION SETUP FROM INPUT CONTENT!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW", Ex);
                return false;
            }
        }
        /// <summary>
        /// Builds out a set of simulation objects from a given input expressions file set or PassThru log file
        /// </summary>
        /// <returns>True if simulation channel generation passes, false if not</returns>
        public bool GenerateLogSimulation()
        {
            try
            { 
                // Log we're building a simulation file set and build a new expressions generator here 
                this.ProcessingProgress = 0;
                this.ViewModelLogger.WriteLog("BUILDING SIMULATION FROM LOADED LOG FILE NOW...", LogType.InfoLog);
                var SimGenerator = new PassThruSimulationGenerator(this.CurrentLogSet.PassThruLogFile.LogFilePath, this.CurrentLogSet.GeneratedExpressions);
                SimGenerator.OnGeneratorProgress += (_, GeneratorArgs) =>
                {
                    // Invoke a new progress update to our UI content using the generator built
                    if (this.BaseViewControl is not FulcrumLogReviewView) return;

                    // If the progress value reported back is the same as it is currently, don't set it again
                    int NextProgress = (int)GeneratorArgs.CurrentProgress;
                    if (this.ProcessingProgress != NextProgress) this.ProcessingProgress = NextProgress;
                };

                // Now Build our simulation content objects for this generator
                this.ViewModelLogger.WriteLog("PROCESSING LOG LINES INTO SIM CHANNEL OBJECTS NOW...", LogType.InfoLog);
                var BuiltSimChannels = SimGenerator.GenerateLogSimulation();
                var BuiltSimFileName = SimGenerator.SaveSimulationFile(this.CurrentLogSet.PassThruLogFile.LogFilePath);
                if (BuiltSimFileName == "") throw new InvalidOperationException("FAILED TO FIND OUT NEW SIMULATION CONTENT!");

                // Once we've built the new simulations file contents and files, we can store them on our log file set
                var SimulationsFileModel = new FulcrumLogFileModel(BuiltSimFileName);
                this.CurrentLogSet.StoreSimulationsFile(SimulationsFileModel, BuiltSimChannels);

                // Log out some information about the simulations built and toggle our view contents
                this.ViewModelLogger.WriteLog($"SAVED SIMULATION FILE AT PATH {BuiltSimFileName} FROM INPUT EXPRESSIONS!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog($"BUILT A TOTAL OF {BuiltSimChannels} SIM CHANNELS!", LogType.InfoLog);
                this.ProcessingProgress = 100;

                // Set our new log file model so the view content updates and exit out
                FulcrumLogReviewView CastView = this.BaseViewControl as FulcrumLogReviewView;
                CastView.Dispatcher.Invoke(() => { CastView.ViewerContentComboBox.SelectedIndex = 2; });
                return true;
            } 
            catch (Exception BuildSimEx) 
            {
                // Log failures out and return nothing
                this.ProcessingProgress = 100;
                this.ViewModelLogger.WriteLog("FAILED TO BUILD NEW SIMULATION FILE USING INPUT EXPRESSIONS!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION THROWN IS BEING LOGGED BELOW NOW...", BuildSimEx);
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
        private bool _toggleViewerContents(ViewerStateType StateToSet)
        {
            try
            {
                // Start by getting our string values needed for the desired file.
                this.ViewModelLogger.WriteLog("PULLING IN NEW CONTENT FOR A DESIRED FILE OBJECT OUTPUT NOW!", LogType.WarnLog);
                if (!this.IsLogLoaded)
                {
                    // Log that we can't show files which don't exist and exit out
                    this.ViewModelLogger.WriteLog("CAN NOT TOGGLE TO A FILE INSTANCE WHEN NO VALUE IS CONFIGURED FOR A LOG SET!", LogType.ErrorLog);
                    return false;
                }

                // Based on the state value we're storing, find the correct log file contents
                switch (this._currentState = StateToSet)
                {
                    // For showing expressions
                    case ViewerStateType.ShowingExpressions:
                        if (!this.CurrentLogSet.HasExpressions)
                            throw new FileNotFoundException("Error! Current log set does not have expressions built!");

                        // Set our new current log file to the expressions file and break out
                        // this.CurrentLogFile = this.CurrentLogSet.ExpressionsFile;
                        this.ViewModelLogger.WriteLog("UPDATED LOG REVIEW UI TO HOLD EXPRESSIONS FILE CONTENT!", LogType.InfoLog);
                        return true;

                    // For showing simulations
                    case ViewerStateType.ShowingSimulation:
                        if (!this.CurrentLogSet.HasSimulations)
                            throw new FileNotFoundException("Error! Current log set does not have simulations built!");

                        // Set our new current log file to the simulations file and break out
                        // this.CurrentLogFile = this.CurrentLogSet.SimulationsFile;
                        this.ViewModelLogger.WriteLog("UPDATED LOG REVIEW UI TO HOLD SIMULATION FILE CONTENT!", LogType.InfoLog);
                        return true;

                    // For showing raw log contents
                    case ViewerStateType.ShowingLogFile:
                        if (!this.CurrentLogSet.HasPassThruLog) 
                            throw new FileNotFoundException("Error! Current log set does not a base log file built!");

                        // Set our new current log file to the base pass thru file and break out
                        // this.CurrentLogFile = this.CurrentLogSet.PassThruLogFile;
                        this.ViewModelLogger.WriteLog("UPDATED LOG REVIEW UI TO HOLD BASE LOG FILE FILE CONTENT!", LogType.InfoLog);
                        return true;

                    // For showing nothing in the viewer
                    case ViewerStateType.NoContent:
                        // this.CurrentLogFile = this.CurrentLogSet.PassThruLogFile;
                        this.ViewModelLogger.WriteLog("WARNING! RESETTING THE CURRENT LOG FILE MODEL TO NULL FOR NO CONTENT!", LogType.TraceLog);
                        break;

                    // On default, throw a failure out and move on. This should never happen really
                    default: throw new InvalidEnumArgumentException($"Error! Unable to process view type {this._currentState}!");
                }

                // Store our contents for the log file view object back on our editor controls now
                FulcrumLogReviewView CastView = this.BaseViewControl as FulcrumLogReviewView;
                CastView.Dispatcher.Invoke(() => 
                {                   
                    // Set our new index value for the current file type, and setup our output content in text viewers
                    CastView.FilteringLogFileTextBox.Text = this.CurrentLogFile?.LogFilePath ?? $"No Log File Loaded!"; ;
                    CastView.ReplayLogInputContent.Text = File.Exists(this.CurrentLogFile?.LogFilePath) 
                        ? File.ReadAllText(this.CurrentLogFile?.LogFilePath ?? string.Empty) 
                        : $"No Log File Contents Loaded!";
                });

                // Toggle the showing parsed value.
                return false;
            }
            catch (Exception LoadEx)
            {
                // Log failures. Return false.
                this.ViewModelLogger.WriteLog("FAILED TO LOAD IN NEW CONTENTS FOR OUR FILE ENTRIES!");
                this.ViewModelLogger.WriteException("EXCEPTIONS ARE BEING LOGGED BELOW", LoadEx);
                
                // Update our content values on the view for this failure if possible
                if (this.BaseViewControl is not FulcrumLogReviewView CastView) return false;

                // Store some default values here for the controls on our view content
                string DefaultValue = CastView.FilteringLogFileTextBox.Text;
                CastView.FilteringLogFileTextBox.Foreground = Brushes.Red;
                CastView.FilteringLogFileTextBox.FontWeight = FontWeights.Bold;
                CastView.FilteringLogFileTextBox.Text = $"Failed To Load {this._currentState.ToDescriptionString()}! Did you build it?";

                // Now Reset values for our view content in a background thread
                Task.Run(() =>
                {
                    // Wait 3.5 seconds and reset the content for our view now 
                    Thread.Sleep(3500);
                    CastView.Dispatcher.Invoke(() => CastView.FilteringLogFileTextBox.Text = DefaultValue);
                });
                
                // Return false at this point since something went wrong
                return false;
            }
        }
    }
}
