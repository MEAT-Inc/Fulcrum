using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorOptionViews;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.DebugLogFormatters;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.FilteringFormatters;
using NLog.Config;
using NLog;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// View model used to help render and register logging redirects
    /// </summary>
    public class FulcrumDebugLoggingViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Helper for editing Text box contents
        private LogOutputFilteringHelper _logContentHelper;
        private readonly string _logContentTargetName = "DebugLoggingRedirectTarget";

        // Private backing fields for our public properties
        private bool _usingRegex;
        private bool _noResultsOnSearch;
        private List<string> _loggerNamesFound;

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool UsingRegex { get => _usingRegex; set => PropertyUpdated(value); }
        public bool NoResultsOnSearch { get => _noResultsOnSearch; set => PropertyUpdated(value); }
        public List<string> LoggerNamesFound { get => _loggerNamesFound; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="DebugLoggingUserControl">User control which holds the view content for our debug logging view</param>
        public FulcrumDebugLoggingViewModel(UserControl DebugLoggingUserControl) : base(DebugLoggingUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Store logger names here
            this.LoggerNamesFound = this.BuildLoggerNamesList();
            this.ViewModelLogger.WriteLog("DONE CONFIGURING VIEW BINDING VALUES!");
            this.ViewModelLogger.WriteLog("SETUP NEW LOGGING VIEW AND MODIFICATION VALUES OK!");
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Generates a list of logger names from the log broker object
        /// </summary>
        /// <returns>Names of all loggers</returns>
        public List<string> BuildLoggerNamesList()
        {
            // Build filtering list and return them
            var PulledNames = new List<string>() { "--- Select A Logger ---" };
            this.ViewModelLogger.WriteLog("PULLING LOGGER NAMES FROM QUEUE NOW TO POPULATE DEBUG DROPDOWN...");
            PulledNames.AddRange(SharpLogBroker.LoggerPool.Select(LoggerObj => LoggerObj.LoggerName));

            // Return them here
            this.ViewModelLogger.WriteLog($"PULLED A TOTAL OF {PulledNames.Count} LOGGER NAMES OK!", LogType.InfoLog);
            return PulledNames;
        }
        /// <summary>
        /// Searches the AvalonEdit object for text matching what we want.
        /// </summary>
        /// <param name="TextToFind"></param>
        public void SearchForText(string TextToFind)
        {
            // Make sure transformer is built
            if (_logContentHelper == null) return;
            var OutputTransformer = this._logContentHelper.SearchForText(TextToFind);

            // Store values here
            if (string.IsNullOrEmpty(TextToFind)) return;
            this.UsingRegex = OutputTransformer?.UseRegex ?? false;
            this.NoResultsOnSearch = OutputTransformer?.NoMatches ?? false;
        }
        /// <summary>
        /// Filters log lines by logger name
        /// </summary>
        /// <param name="LoggerName"></param>
        public void FilterByLoggerName(string LoggerName)
        {
            // Find the content we need to based on the logger name
            this._logContentHelper?.FilterByText(LoggerName);
        }
        /// <summary>
        /// Builds and returns a new log content helper object
        /// </summary>
        /// <returns>The log content helper object built out from our view content</returns>
        public void ConfigureOutputHighlighter()
        {
            // Log information and store values 
            this.ViewModelLogger.WriteLog("SETTING UP DEBUG LOG TARGETS FOR UI LOGGING NOW...", LogType.WarnLog);
            
            // Make sure the view content exists first and that it's been setup correctly
            if (this.BaseViewControl is not FulcrumDebugLoggingView CastViewContent)
                throw new InvalidOperationException($"Error! View content type was {this.BaseViewControl.GetType().Name}");

            // Configure the new Logging Output Target.
            if (LogManager.Configuration.FindTargetByName(this._logContentTargetName) != null)
            {
                // Log that we've already got a helper instance and exit out
                this.ViewModelLogger.WriteLog($"WARNING! ALREADY FOUND AN EXISTING TARGET MATCHING THE NAME {this._logContentTargetName}!", LogType.WarnLog);
                this.ViewModelLogger.WriteLog("NOT ATTEMPTING TO REGISTER A NEW TARGET SINCE THAT WOULD BE A WASTE OF TIME (LIKE THIS LOG ENTRY)");
            }
            else
            {
                // Log that we didn't find any targets to use for this Debug review window and build one
                this.ViewModelLogger.WriteLog("NO TARGETS MATCHING DEFINED TYPE WERE FOUND! THIS IS A GOOD THING", LogType.InfoLog);

                // Register our new target instance based ont the debug logging target type
                ConfigurationItemFactory.Default.Targets.RegisterDefinition(this._logContentTargetName, typeof(DebugLoggingRedirectTarget));
                LogManager.Configuration.AddRuleForAllLevels(new DebugLoggingRedirectTarget(CastViewContent.DebugRedirectOutputEdit));
                LogManager.ReconfigExistingLoggers();

                // Store the new formatter on this class instance and log results out
                this.ViewModelLogger.WriteLog("INJECTOR HAS REGISTERED OUR DEBUGGING REDIRECT OBJECT OK!", LogType.WarnLog);
                this.ViewModelLogger.WriteLog("ALL LOG OUTPUT WILL APPEND TO OUR DEBUG VIEW ALONG WITH THE OUTPUT FILES NOW!", LogType.WarnLog);
            }

            // Hook an event to build our log highlighter when the view is loaded
            CastViewContent.Loaded += (_, _) =>
            {
                // Configure our new output Logging format helper and store it on this window
                this._logContentHelper ??= new LogOutputFilteringHelper(CastViewContent.DebugRedirectOutputEdit);
                LogManager.ReconfigExistingLoggers();

                // Log out that these routines are done and exit out
                this.ViewModelLogger.WriteLog($"{CastViewContent.GetType().Name} WAS LOADED AT {DateTime.Now:G}", LogType.TraceLog);
                this.ViewModelLogger.WriteLog($"CONFIGURED {CastViewContent.GetType().Name} VIEW CONTROL VALUES AND LOGGING TARGETS OK!", LogType.InfoLog);
            };
        }
    }
}