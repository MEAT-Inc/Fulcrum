using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.FilteringFormatters;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.InjectorSyntaxFormatters;
using NLog.Config;
using NLog;
using SharpLogging;
using SharpPipes;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.DebugLogFormatters;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// ViewModel for the DLL output content view
    /// </summary>
    internal class FulcrumDllOutputLogViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Helpers for editing Text box contents
        private LogOutputFilteringHelper _logFilteringHelper;
        private InjectorOutputSyntaxHelper _injectorSyntaxHelper;
        private readonly string _dllContentTargetName = "LiveInjectorOutputTarget";

        // Private backing fields for our public properties
        private bool _hasOutput;
        private List<string> _sessionLogs;

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool HasOutput { get => _hasOutput; set => PropertyUpdated(value); }
        public List<string> SessionLogs { get => _sessionLogs; private set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="DllOutputUserControl">UserControl which holds the content for the DLL output view</param>
        public FulcrumDllOutputLogViewModel(UserControl DllOutputUserControl) : base(DllOutputUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP INJECTOR TEST VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Build default value for session log files.
            this.SessionLogs = new List<string>();
            this.ViewModelLogger.WriteLog("BUILT NEW EMPTY ARRAY FOR SESSION LOG FILES NOW...");

            // Build event for our pipe objects to process new pipe content into our output box
            PassThruPipeReader ReaderPipe = PassThruPipeReader.AllocatePipe();
            ReaderPipe.PipeDataProcessed += this._onPipeDataProcessed;
            this.ViewModelLogger.WriteLog("ALLOCATED NEW READER PIPE WITHOUT ISSUES! (THANK FUCKIN GOD)", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("STORED NEW EVENT BROKER FOR PIPE READING DATA PROCESSED OK!", LogType.InfoLog);

            // Build log content helper and return
            this.ViewModelLogger.WriteLog("SETUP NEW DLL INJECTION OUTPUT LOG VALUES OK!");
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches the AvalonEdit object for text matching what we want.
        /// </summary>
        /// <param name="TextToFind"></param>
        public void SearchForText(string TextToFind)
        {
            // Make sure transformer is built
            if (_logFilteringHelper == null) return;
            this._logFilteringHelper.SearchForText(TextToFind);
        }
        /// <summary>
        /// Updates/toggles on or off our syntax highlighting for the output content in the DLL logging window
        /// </summary>
        public bool UpdateSyntaxHighlighting()
        {
            // Check the current state and toggle it.
            if (this._injectorSyntaxHelper.IsHighlighting)
                this._injectorSyntaxHelper.StopColorHighlighting();
            else this._injectorSyntaxHelper.StartColorHighlighting();

            // Return out if we're highlighting or not
            return this._injectorSyntaxHelper.IsHighlighting;
        }
        /// <summary>
        /// Builds and returns a new log content helper object
        /// </summary>
        /// <returns>The log content helper object built out from our view content</returns>
        public void ConfigureOutputHighlighter()
        {
            // Log information and store values 
            this.ViewModelLogger.WriteLog("SETTING UP DEBUG LOG TARGETS FOR UI LOGGING NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Make sure the view content exists first and that it's been setup correctly
            if (this.BaseViewControl is not FulcrumDllOutputLogView CastViewContent)
                throw new InvalidOperationException($"Error! View content type was {this.BaseViewControl.GetType().Name}");

            // Configure the new Logging Output Target.
            if (LogManager.Configuration.FindTargetByName(this._dllContentTargetName) != null)
            {
                // Log that we've already got a helper instance and exit out
                this.ViewModelLogger.WriteLog($"WARNING! ALREADY FOUND AN EXISTING TARGET MATCHING THE NAME {this._dllContentTargetName}!", LogType.WarnLog);
                this.ViewModelLogger.WriteLog("RECONFIGURING LOGGERS AND EXITING OUT", LogType.WarnLog);
            }
            else
            {
                // Log that we didn't find any targets to use for this Debug review window and build one
                this.ViewModelLogger.WriteLog("NO TARGETS MATCHING DEFINED TYPE WERE FOUND! THIS IS A GOOD THING", LogType.InfoLog);

                // Register our new target instance based ont the debug logging target type
                ConfigurationItemFactory.Default.Targets.RegisterDefinition(this._dllContentTargetName, typeof(InjectorOutputSyntaxHelper));
                LogManager.Configuration.AddRuleForAllLevels(new InjectorOutputSyntaxHelper(CastViewContent.DebugRedirectOutputEdit));
                LogManager.ReconfigExistingLoggers();

                // Store the new formatter on this class instance and log results out
                this.ViewModelLogger.WriteLog("INJECTOR HAS REGISTERED OUR DLL OUTPUT LOGGING REDIRECT OBJECT OK!", LogType.WarnLog);
                this.ViewModelLogger.WriteLog("ALL LOG OUTPUT WILL APPEND TO OUR DEBUG VIEW ALONG WITH THE OUTPUT FILES NOW!", LogType.WarnLog);
            }

            // Configure our new output Logging format helper and store it on this window
            this._injectorSyntaxHelper = new InjectorOutputSyntaxHelper(CastViewContent.DebugRedirectOutputEdit);
            this._logFilteringHelper = new LogOutputFilteringHelper(CastViewContent.DebugRedirectOutputEdit);
            this.ViewModelLogger.WriteLog("CONFIGURED DLL OUTPUT VIEW CONTROL VALUES AND LOGGING TARGETS OK!", LogType.InfoLog);
        }

        /// <summary>
        /// Private event handler to fire when a reader pipe instance processes new content 
        /// </summary>
        /// <param name="PassThruPiper">The sending pipe object</param>
        /// <param name="PipeEventArgs">The sending event args fired along with this pipe data event</param>
        private void _onPipeDataProcessed(object PassThruPiper, PassThruPipe.PipeDataEventArgs PipeEventArgs)
        {
            // Make sure the view content exists first and that it's been setup correctly
            if (this.BaseViewControl is not FulcrumDllOutputLogView CastViewContent)
                throw new InvalidOperationException($"Error! View content type was {this.BaseViewControl.GetType().Name}");

            // Write the new content out to our DLL view control and check to see if we've got a file name
            CastViewContent?.Dispatcher.Invoke(() => { CastViewContent.DebugRedirectOutputEdit.Text += PipeEventArgs.PipeDataString + "\n"; });

            // If we've found a potential file, then store it now
            if (!PipeEventArgs.PipeDataString.Contains("Session Log File")) return;
            string ParsedLogFileName = PipeEventArgs.PipeDataString.Split(':').Last();
            if (!File.Exists(ParsedLogFileName)) this.ViewModelLogger.WriteLog($"WARNING! POTENTIAL SESSION LOG FILE {ParsedLogFileName} COULD NOT BE FOUND!", LogType.WarnLog);
            else
            {
                // Setup and store the new log file name if needed
                this.SessionLogs.Add(ParsedLogFileName);
                this.SessionLogs = this.SessionLogs.Distinct().ToList();
                this.ViewModelLogger.WriteLog($"FOUND LOG FILE NAMED {ParsedLogFileName} AND VALIDATED IT EXISTS ON THE SYSTEM!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog("LOCATED LOG FILE WILL BE STORED ON THE OUTPUT LOG FILES TO BE INCLUDED IN SESSION REPORTS");

                // Log out all of the session log files we've found so far
                string ProcessedStrings = string.Join(" | ", this.SessionLogs);
                this.ViewModelLogger.WriteLog($"{this.SessionLogs.Count} SESSION FILES HAVE BEEN PARSED FROM DLL OUTPUT SO FAR", LogType.TraceLog);
                this.ViewModelLogger.WriteLog($"LOG FILES PROCESSED: {ProcessedStrings}", LogType.TraceLog);
            }
        }
    }
}
