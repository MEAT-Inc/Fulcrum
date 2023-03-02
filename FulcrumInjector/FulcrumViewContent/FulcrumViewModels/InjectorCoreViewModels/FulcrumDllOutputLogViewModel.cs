using System;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorOptionViews;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.DebugLogFormatters;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.FilteringFormatters;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.InjectorSyntaxFormatters;
using NLog.Config;
using NLog;
using SharpLogging;
using SharpPipes;
using System.Threading.Tasks;

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
        private string[] _sessionLogs;

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool HasOutput { get => _hasOutput; set => PropertyUpdated(value); }
        public string[] SessionLogs { get => _sessionLogs; set => PropertyUpdated(value); }

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
            this.SessionLogs = Array.Empty<string>();
            this.ViewModelLogger.WriteLog("BUILT NEW EMPTY ARRAY FOR SESSION LOG FILES NOW...");

            // Build event for our pipe objects to process new pipe content into our output box
            PassThruPipeReader ReaderPipe = PassThruPipeReader.AllocatePipe();
            ReaderPipe.PipeDataProcessed += this._onPipeReaderContentProcessed;
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
            var ExistingTarget = LogManager.Configuration.FindTargetByName(this._dllContentTargetName);
            if (ExistingTarget == null) this.ViewModelLogger.WriteLog("NO TARGETS MATCHING DEFINED TYPE WERE FOUND! THIS IS A GOOD THING", LogType.InfoLog);
            else
            {
                // Log that we've already got a helper instance and exit out
                this.ViewModelLogger.WriteLog($"WARNING! ALREADY FOUND AN EXISTING TARGET MATCHING THE NAME {this._dllContentTargetName}!", LogType.WarnLog);
                this.ViewModelLogger.WriteLog("REMOVING EXISTING INSTANCES OF OUR DEBUG LOG FORMATTER AND BUILDING A NEW ONE...", LogType.WarnLog);

                // Remove the old target and then build a new one
                LogManager.Configuration.RemoveTarget(ExistingTarget.Name);
            }

            // Log information, build new target output and return.
            ConfigurationItemFactory.Default.Targets.RegisterDefinition(this._dllContentTargetName, typeof(InjectorOutputSyntaxHelper));
            LogManager.Configuration.AddRuleForAllLevels(new InjectorOutputSyntaxHelper(CastViewContent.DebugRedirectOutputEdit));
            this._injectorSyntaxHelper = new InjectorOutputSyntaxHelper(CastViewContent.DebugRedirectOutputEdit);
            this._logFilteringHelper = new LogOutputFilteringHelper(CastViewContent.DebugRedirectOutputEdit);
            LogManager.ReconfigExistingLoggers();

            // Store the new formatter on this class instance and log results out
            this.ViewModelLogger.WriteLog("INJECTOR HAS REGISTERED OUR DEBUGGING REDIRECT OBJECT OK!", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("ALL LOG OUTPUT WILL APPEND TO OUR DEBUG VIEW ALONG WITH THE OUTPUT FILES NOW!", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES AND LOGGING TARGETS OK!", LogType.InfoLog);
        }

        /// <summary>
        /// Event object to run when the injector output gets new content.
        /// </summary>
        /// <param name="PipeInstance">Pipe object calling these events</param>
        /// <param name="EventArgs">The events themselves.</param>
        private void _onPipeReaderContentProcessed(object PipeInstance, PassThruPipe.PipeDataEventArgs EventArgs)
        {
            // Attach output content into our session log box.
            FulcrumDllOutputLogView ViewCast = this.BaseViewControl as FulcrumDllOutputLogView;
            if (ViewCast == null) this.ViewModelLogger.WriteLog("WARNING: CAST VIEW ENTRY WAS NULL!", LogType.TraceLog);
            else ViewCast?.Dispatcher.Invoke(() => { ViewCast.DebugRedirectOutputEdit.Text += EventArgs.PipeDataString + "\n"; });
        }
    }
}
