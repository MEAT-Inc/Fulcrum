using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruRegex;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews;
using FulcrumInjector.FulcrumViewSupport.AppStyleSupport.AvalonEditHelpers;
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
        private string _loadedLogFile = "";
        private string _logFileContents = "";
        private double _parsingProgress = 0.00;

        private string[] _logFileCommands;

        // Public values for our view to bind onto 
        public string LoadedLogFile { get => _loadedLogFile; set => PropertyUpdated(value); }
        public string LogFileContents { get => _logFileContents; set => PropertyUpdated(value); }
        public double ParsingProgress { get => _parsingProgress; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumLogReviewViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP INJECTOR LOG REVIEW VIEW BOUND VALUES NOW...", LogType.WarnLog);
            
            // Build log content helper and return
            ViewModelLogger.WriteLog("SETUP NEW DLL LOG REVIEW OUTPUT VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"STORED NEW VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads the contents of an input log file object from a given path and stores them into the view.
        /// </summary>
        /// <param name="InputLogFile"></param>
        internal bool LoadLogFileContents()
        {
            // Log information, load contents, store values.
            ViewModelLogger.WriteLog("LOADING NEW LOG FILE CONTENTS NOW...", LogType.InfoLog);
            FulcrumLogReviewView CastView = this.BaseViewControl as FulcrumLogReviewView;
            
            try
            {
                // Make sure a file is loaded
                if (string.IsNullOrWhiteSpace(this.LoadedLogFile)) {
                    ViewModelLogger.WriteLog("NO LOG FILE LOADED! LOAD A LOG FILE BEFORE TRYING TO USE THIS METHOD!", LogType.InfoLog);
                    throw new FileNotFoundException("FAILED TO LOCATE THE DESIRED FILE! ENSURE ONE IS LOADED FIRST!");
                }

                // Log passed and return output.
                this.LogFileContents = File.ReadAllText(this.LoadedLogFile);

                // Store lines here.
                CastView.Dispatcher.Invoke(() => {
                    CastView.LoadedLogFileTextBox.Text = this.LoadedLogFile;
                    CastView.ReplayLogInputContent.Text = this.LogFileContents;
                });
                
                // Return passed
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
                    CastView.LoadedLogFileTextBox.Text = this.LoadedLogFile;
                    CastView.ReplayLogInputContent.Text = this.LogFileContents;
                });

                // Return split content
                return false;   
            }
        }

        /// <summary>
        /// Splits out the input command lines into a set of PTObjects.
        /// </summary>
        /// <param name="CommandLines"></param>
        /// <returns></returns>
        internal bool ProcessLogContents(out ObservableCollection<PassThruExpression> OutputExpressions)
        {
            // Build command split log contents first. 
            try
            {
                ViewModelLogger.WriteLog("PROCESSING LOG LINES INTO EXPRESSIONS NOW...", LogType.InfoLog);
                var SplitLogContent = PassThruExpressionHelpers.SplitLogToCommands(LogFileContents);
                ViewModelLogger.WriteLog($"SPLIT CONTENTS INTO A TOTAL OF {SplitLogContent.Length} CONTENT SET OBJECTS", LogType.WarnLog);

                // Start by building PTExpressions from input string object sets.
                ViewModelLogger.WriteLog("PROCESSING LOG LINES INTO PTEXPRESSION OBJECTS FOR BINDING NOW...", LogType.InfoLog);
                var ExpressionSet = SplitLogContent.Select(LineSet =>
                {
                    // Split our output content here and then build a type for the expressions
                    string[] SplitLines = LineSet.Split('\n');
                    var ExpressionType = PassThruExpressionHelpers.GetTypeFromLines(SplitLines);

                    // Build expression class object and tick our progress
                    var NextClassObject = ExpressionType.ToRegexClass(SplitLines);
                    this.ParsingProgress = (double)(SplitLogContent.ToList().IndexOf(LineSet) + 1 / SplitLogContent.Length);

                    // Return the built expression object
                    return NextClassObject;
                }).ToArray();

                // Convert the expression set into a list of file strings now and return list built.
                string BuiltExpressionFile = ExpressionSet.SaveExpressionsToFile(Path.GetFileName(LoadedLogFile));
                ViewModelLogger.WriteLog($"GENERATED A TOTAL OF {ExpressionSet.Length} EXPRESSION OBJECTS!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"SAVED EXPRESSIONS TO NEW FILE OBJECT NAMED: {BuiltExpressionFile}!", LogType.InfoLog);
                OutputExpressions = new ObservableCollection<PassThruExpression>(ExpressionSet);
                return true;
            }
            catch (Exception Ex)
            {
                // Log failures, return nothing
                ViewModelLogger.WriteLog("FAILED TO GENERATE NEW EXPRESSION SETUP FROM INPUT CONTENT!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("EXCEPTION IS BEING LOGGED BELOW", Ex);
                OutputExpressions = null;
                return false;
            }
        }
    }
}
