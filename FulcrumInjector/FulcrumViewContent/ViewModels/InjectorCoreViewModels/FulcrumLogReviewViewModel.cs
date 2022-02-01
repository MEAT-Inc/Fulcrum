using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.FIlteringFormatters;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters;
using Newtonsoft.Json;
using NLog.Targets;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// Viewmodel object for viewing output log instances from old log files.
    /// </summary>
    public class FulcrumLogReviewViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorLogReviewViewModelLogger")) ?? new SubServiceLogger("InjectorLogReviewViewModelLogger");

        // Private control values
        private int _parsingProgress = 0;
        private string _loadedLogFile = "";
        private string _logFileContents = "";

        // Private string for last built expressions file.
        private bool _inputParsed = false;
        private bool _showingParsed = false;
        private string _lastBuiltExpressionsFile;

        // Public values for our view to bind onto 
        public bool InputParsed { get => _inputParsed; set => PropertyUpdated(value); }
        public bool ShowingParsed { get => _showingParsed; set => PropertyUpdated(value); }
        public string LoadedLogFile { get => _loadedLogFile; set => PropertyUpdated(value); }
        public int ParsingProgress { get => _parsingProgress; set => PropertyUpdated(value); }
        public string LogFileContents { get => _logFileContents; set => PropertyUpdated(value); }

        // Helper for syntax formatting and filtering
        public LogOutputFilteringHelper LogFilteringHelper;
        public InjectorOutputSyntaxHelper InjectorSyntaxHelper;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumLogReviewViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP INJECTOR LOG REVIEW VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Toggle parsed value based on contents.
            this.InputParsed = false;
            ViewModelLogger.WriteLog("TOGGLED ENABLED STATUS OF TOGGLE BUTTON OK!", LogType.InfoLog);

            // This is turned off for now. No need to dupe import all of these objects
            //      Import Regex objects. 
            //      ViewModelLogger.WriteLog("CONFIGURING REGEX ENTRIES NOW...");
            //      var BuiltObjects = PassThruExpressionShare.GeneratePassThruRegexModels();
            //      ViewModelLogger.WriteLog($"GENERATED A TOTAL OF {BuiltObjects.Count} REGEX OBJECTS OK!", LogType.InfoLog);

            // Build log content helper and return
            ViewModelLogger.WriteLog("SETUP NEW DLL LOG REVIEW OUTPUT VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"STORED NEW VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Combines a set of input log files into one output file.
        /// </summary>
        /// <param name="LogFilePaths"></param>
        /// <returns>Path to a combined output log file.</returns>
        internal string CombineLogFiles(string[] LogFilePaths)
        {
            // Find the name of the first file and use it as our base.
            string OutputPath = Path.GetDirectoryName(LogFilePaths[0]);
            string BaseFileName = Path.GetFileNameWithoutExtension(LogFilePaths[0]);
            string FinalFileName = Path.Combine(OutputPath, $"CombinedLogs_{BaseFileName}.shimLog");

            // Now load the files in one by one and combine their output.
            string[] TotalContent = Array.Empty<string>();
            TotalContent = LogFilePaths.SelectMany(FileObj =>
            {
                try { return File.ReadAllLines(FileObj); }
                catch {
                    ViewModelLogger.WriteLog("ERROR! FAILED TO PARSE IN ONE OR MORE LOG FILES!", LogType.ErrorLog);
                    return Array.Empty<string>();
                }
            }).ToArray();

            // Write final output contents now.
            ViewModelLogger.WriteLog($"WRITING A TOTAL OF {TotalContent.Length} NEW FILE LINES OUT TO OUR OUTPUT LOCATION NOW...", LogType.InfoLog);
            File.WriteAllLines(FinalFileName, TotalContent);
            return FinalFileName;
        }
        /// <summary>
        /// Loads the contents of an input log file object from a given path and stores them into the view.
        /// </summary>
        /// <param name="NewLogFile"></param>
        internal bool LoadLogContents(string NewLogFile)
        {
            // Log information, load contents, store values.
            ViewModelLogger.WriteLog("LOADING NEW LOG FILE CONTENTS NOW...", LogType.InfoLog);
            FulcrumLogReviewView CastView = this.BaseViewControl as FulcrumLogReviewView;
            
            try
            {
                // Make sure a file is loaded
                this.LoadedLogFile = NewLogFile;
                if (string.IsNullOrWhiteSpace(this.LoadedLogFile)) {
                    ViewModelLogger.WriteLog("NO LOG FILE LOADED! LOAD A LOG FILE BEFORE TRYING TO USE THIS METHOD!", LogType.InfoLog);
                    throw new FileNotFoundException("FAILED TO LOCATE THE DESIRED FILE! ENSURE ONE IS LOADED FIRST!");
                }

                // Log passed and return output. Store onto view content.
                this.LogFileContents = File.ReadAllText(this.LoadedLogFile);
                CastView.Dispatcher.Invoke(() => {
                    CastView.FilteringLogFileTextBox.Text = this.LoadedLogFile;
                    CastView.ReplayLogInputContent.Text = this.LogFileContents;
                });

                // Return passed
                this.InputParsed = false;
                this._lastBuiltExpressionsFile = null;
                ViewModelLogger.WriteLog("PROCESSED NEW LOG CONTENT INTO THE MAIN VIEW OK!", LogType.InfoLog);
                return true;
            }
            catch (Exception Ex)
            {
                // Log failed to load and set our contents to just "Failed to Load!" with the exception stack trace.
                ViewModelLogger.WriteLog("FAILED TO LOAD NEW LOG FILE! VIEW IS SHOWING STACK TRACE NOW!", LogType.InfoLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN IS BEING LOGGED BELOW.", Ex);

                // Store new values.
                this.LoadedLogFile = $"Failed to Load File: {Path.GetFileName(this.LoadedLogFile)}!";
                this.LogFileContents = Ex.Message + "\n" + "STACK TRACE:\n" + Ex.StackTrace;
                CastView.Dispatcher.Invoke(() => {
                    CastView.FilteringLogFileTextBox.Text = this.LoadedLogFile;
                    CastView.ReplayLogInputContent.Text = this.LogFileContents;
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
        internal bool ParseLogContents(out ObservableCollection<PassThruExpression> OutputExpressions)
        {
            try
            {
                // Build command split log contents first. 
                ViewModelLogger.WriteLog("PROCESSING LOG LINES INTO EXPRESSIONS NOW...", LogType.InfoLog);
                var SplitLogContent = this.SplitLogToCommands(LogFileContents);
                ViewModelLogger.WriteLog($"SPLIT CONTENTS INTO A TOTAL OF {SplitLogContent.Length} CONTENT SET OBJECTS", LogType.WarnLog);

                // Start by building PTExpressions from input string object sets.
                ViewModelLogger.WriteLog("PROCESSING LOG LINES INTO PT EXPRESSION OBJECTS FOR BINDING NOW...", LogType.InfoLog);
                var ExpressionSet = SplitLogContent.Select(LineSet =>
                {
                    // Split our output content here and then build a type for the expressions
                    if (LineSet.Length == 0) return null;
                    string[] SplitLines = LineSet.Split('\n');
                    var ExpressionType = SplitLines.GetTypeFromLines();

                    // Build expression class object and tick our progress
                    var NextClassObject = ExpressionType.GetRegexClassFromCommand(SplitLines);
                    this.ParsingProgress = (int)(SplitLogContent.ToList().IndexOf(LineSet) + 1 / (double)SplitLogContent.Length);

                    // Return the built expression object
                    return NextClassObject;
                }).Where(ExpObj => ExpObj != null).ToArray();

                // Convert the expression set into a list of file strings now and return list built.
                this.ParsingProgress = 100;
                this._lastBuiltExpressionsFile = ExpressionSet.SaveExpressionsFile(this.LoadedLogFile);
                if (this._lastBuiltExpressionsFile == "") throw new InvalidOperationException("FAILED TO FIND OUT NEW EXPRESSIONS CONTENT!");
                ViewModelLogger.WriteLog($"GENERATED A TOTAL OF {ExpressionSet.Length} EXPRESSION OBJECTS!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"SAVED EXPRESSIONS TO NEW FILE OBJECT NAMED: {this._lastBuiltExpressionsFile}!", LogType.InfoLog);
                OutputExpressions = new ObservableCollection<PassThruExpression>(ExpressionSet); this.InputParsed = true;
                return true;
            }
            catch (Exception Ex)
            {
                // Log failures, return nothing
                ViewModelLogger.WriteLog("FAILED TO GENERATE NEW EXPRESSION SETUP FROM INPUT CONTENT!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("EXCEPTION IS BEING LOGGED BELOW", Ex);
                OutputExpressions = null; this.InputParsed = false;
                return false;
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches the AvalonEdit object for text matching what we want.
        /// </summary>
        /// <param name="TextToFind"></param>
        internal void SearchForText(string TextToFind)
        {
            // Make sure transformer is built
            if (LogFilteringHelper == null) return;
            this.LogFilteringHelper.SearchForText(TextToFind);
        }
        /// <summary>
        /// Toggles the current contents of the log viewer based on the bool trigger for it.
        /// </summary>
        internal bool ToggleViewerContents()
        {
            try
            {
                // Start by getting our string values needed for the desired file.
                ViewModelLogger.WriteLog("PULLING IN NEW CONTENT FOR A DESIRED FILE OBJECT OUTPUT NOW!", LogType.WarnLog);
                string NewLogContents = this.ShowingParsed ? File.ReadAllText(this.LoadedLogFile) : File.ReadAllText(this._lastBuiltExpressionsFile);

                // Once pulled in, load our content values out.
                this.ShowingParsed = !this.ShowingParsed;
                FulcrumLogReviewView CastView = this.BaseViewControl as FulcrumLogReviewView;
                ViewModelLogger.WriteLog("FILE CONTENT PARSED OK! STORING TO VIEW NOW...", LogType.InfoLog);
                CastView.Dispatcher.Invoke(() => {
                    CastView.ReplayLogInputContent.Text = NewLogContents;
                    CastView.FilteringLogFileTextBox.Text = this.ShowingParsed ? this._lastBuiltExpressionsFile : this.LoadedLogFile;
                });

                // Toggle the showing parsed value.
                ViewModelLogger.WriteLog("IMPORTED CONTENT WITHOUT ISSUES! RETURNING NOW.", LogType.InfoLog);
                return true;
            }
            catch (Exception LoadEx)
            {
                // Log failures. Return false.
                ViewModelLogger.WriteLog("FAILED TO LOAD IN NEW CONTENTS FOR OUR FILE ENTRIES!");
                ViewModelLogger.WriteLog("EXCEPTIONS ARE BEING LOGGED BELOW", LoadEx);
                return false;
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Splits an input content string into a set fo PT Command objects which are split into objects.
        /// </summary>
        /// <param name="FileContents">Input file object content</param>
        /// <returns>Returns a set of file objects which contain the PT commands from a file.</returns>
        private string[] SplitLogToCommands(string FileContents)
        {
            // Build regex objects to help split input content into sets.
            var TimeRegex = new Regex(PassThruRegexModelShare.PassThruTime.ExpressionPattern);
            var StatusRegex = new Regex(PassThruRegexModelShare.PassThruStatus.ExpressionPattern);

            // Make an empty array of strings and then begin splitting.
            List<string> OutputLines = new List<string>();
            for (int CharIndex = 0; CharIndex < FileContents.Length;)
            {
                // Find the first index of a time entry and the close command index.
                int TimeStartIndex = TimeRegex.Match(FileContents, CharIndex).Index;
                var ErrorCloseMatch = StatusRegex.Match(FileContents, TimeStartIndex);
                int ErrorCloseIndex = ErrorCloseMatch.Index + ErrorCloseMatch.Length;

                // Take the difference in End/Start as our string length value.
                string NextCommand = FileContents.Substring(TimeStartIndex, ErrorCloseIndex - TimeStartIndex);
                if (OutputLines.Contains(NextCommand)) break;

                // If it was found in the list already, then we break out of this loop to stop adding dupes.
                if (ErrorCloseIndex < CharIndex) break;
                CharIndex = ErrorCloseIndex; OutputLines.Add(NextCommand);
            }

            // Return the built set of commands.
            return OutputLines.ToArray();
        }
    }
}
