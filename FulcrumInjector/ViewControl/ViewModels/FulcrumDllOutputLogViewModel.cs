using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.AppLogic;
using FulcrumInjector.JsonHelpers;
using FulcrumInjector.ViewControl.Views;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.ViewModels
{
    public class FulcrumDllOutputLogViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorDllOutputViewModelLogger")) ?? new SubServiceLogger("InjectorDllOutputViewModelLogger");

        // Private control values
        private bool _usingRegex;
        private bool _noResultsOnSearch;

        // Public values for our view to bind onto 
        public bool UsingRegex { get => _usingRegex; set => PropertyUpdated(value); }
        public bool NoResultsOnSearch { get => _noResultsOnSearch; set => PropertyUpdated(value); }

        // Helper for editing Text box contents
        public readonly AvalonEditFilteringHelpers LogContentHelper;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumDllOutputLogViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP INJECTOR TEST VIEW BOUND VALUES NOW...", LogType.WarnLog);
         
            // Pull editor off the view base
            var DebugViewCast = this.BaseViewControl as FulcrumDebugLoggingView;
            var PulledEditBox = DebugViewCast.DebugRedirectOutputEdit;
            ViewModelLogger.WriteLog("CAST VIEW TO TYPE OF DEBUG LOG VIEWER AND EXTRACTED TEXTEDIT OK!", LogType.InfoLog);

            // Build log content helper and return
            this.LogContentHelper = new AvalonEditFilteringHelpers(PulledEditBox);
            ViewModelLogger.WriteLog("SETUP NEW DLL INJECTION OUTPUT LOG VALUES OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches the AvalonEdit object for text matching what we want.
        /// </summary>
        /// <param name="TextToFind"></param>
        internal void SearchForText(string TextToFind)
        {
            // Setup new transformer helper
            var OutputTransformer = this.LogContentHelper.SearchForText(TextToFind);

            // Store values here
            this.UsingRegex = OutputTransformer.UseRegex;
            this.NoResultsOnSearch = OutputTransformer.NoMatches;
        }
    }
}
