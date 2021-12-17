using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.AppLogic;
using FulcrumInjector.AppLogic.AvalonEditHelpers;
using FulcrumInjector.JsonHelpers;
using FulcrumInjector.ViewControl.Views;
using ICSharpCode.AvalonEdit;
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
        public AvalonEditFilteringHelpers LogContentHelper;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumDllOutputLogViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP INJECTOR TEST VIEW BOUND VALUES NOW...", LogType.WarnLog);
            
            // Build log content helper and return
            ViewModelLogger.WriteLog("SETUP NEW DLL INJECTION OUTPUT LOG VALUES OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches the AvalonEdit object for text matching what we want.
        /// </summary>
        /// <param name="TextToFind"></param>
        internal void SearchForText(string TextToFind)
        {
            // Make sure transformer is built
            if (LogContentHelper == null) return;
            var OutputTransformer = this.LogContentHelper.SearchForText(TextToFind);

            // Store values here
            if (string.IsNullOrEmpty(TextToFind)) return;
            this.UsingRegex = OutputTransformer?.UseRegex ?? false;
            this.NoResultsOnSearch = OutputTransformer?.NoMatches ?? false;
        }
    }
}
