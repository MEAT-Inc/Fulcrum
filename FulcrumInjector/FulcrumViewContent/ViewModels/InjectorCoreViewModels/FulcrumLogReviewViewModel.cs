using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        // Public values for our view to bind onto 
        public string LoadedLogFile { get => _loadedLogFile; set => PropertyUpdated(value); }
        public string LogFileContents { get => _logFileContents; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumLogReviewViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP INJECTOR LOG REVIEW VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Setup dummy values for log file values.
            this.LoadedLogFile = "";
            this.LogFileContents = "";

            // Build log content helper and return
            ViewModelLogger.WriteLog("SETUP NEW DLL LOG REVIEW OUTPUT VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"STORED NEW VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads the contents of an input log file object from a given path and stores them into the view.
        /// </summary>
        /// <param name="InputLogFile"></param>
        internal void LoadLogFileContents(string InputLogFile)
        {
            // Log information, load contents, store values.
            ViewModelLogger.WriteLog("LOADING NEW LOG FILE CONTENTS NOW...", LogType.InfoLog);
            FulcrumLogReviewView CastView = this.BaseViewControl as FulcrumLogReviewView;

            try
            {
                // Log passed and return output.
                this.LoadedLogFile = InputLogFile;
                this.LogFileContents = File.ReadAllText(InputLogFile);

                // Store lines here.
                CastView.Dispatcher.Invoke(() =>
                {
                    CastView.LoadedLogFileTextBox.Text = this.LoadedLogFile;
                    CastView.ReplayLogInputContent.Text = this.LogFileContents;
                });

                // Log complete and return.
                ViewModelLogger.WriteLog($"LOADED CONTENTS OF LOG FILE {InputLogFile} CORRECTLY AND STORED ONTO VIEW INSTANCE!", LogType.InfoLog);
            }
            catch (Exception Ex)
            {
                // Log failed to load and set our contents to just "Failed to Load!" with the exception stack trace.
                ViewModelLogger.WriteLog("FAILED TO LOAD NEW LOG FILE! VIEW IS SHOWING STACK TRACE NOW!", LogType.InfoLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN IS BEING LOGGED BELOW.", Ex);

                // Store new values.
                this.LoadedLogFile = $"Failed to Load File: {Path.GetFileName(InputLogFile)}!";
                this.LogFileContents = Ex.Message + "\n" + "STACK TRACE:\n" + Ex.StackTrace;
                CastView.Dispatcher.Invoke(() =>
                {
                    CastView.LoadedLogFileTextBox.Text = this.LoadedLogFile;
                    CastView.ReplayLogInputContent.Text = this.LogFileContents;
                });
            }
        }
    }
}
