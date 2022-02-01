using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using FulcrumInjector.FulcrumLogic.InjectorPipes.PipeEvents;
using FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.FIlteringFormatters;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters;
using ICSharpCode.AvalonEdit;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels
{
    public class FulcrumDllOutputLogViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorDllOutputViewModelLogger")) ?? new SubServiceLogger("InjectorDllOutputViewModelLogger");

        // Private control values
        private bool _hasOutput;
        private string[] _sessionLogs;

        // Public values for our view to bind onto 
        public bool HasOutput { get => _hasOutput; set => PropertyUpdated(value); }
        public string[] SessionLogs { get => _sessionLogs; set => PropertyUpdated(value); }

        // Helper for editing Text box contents
        public LogOutputFilteringHelper LogFilteringHelper { get; set; }
        public InjectorOutputSyntaxHelper InjectorSyntaxHelper { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumDllOutputLogViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP INJECTOR TEST VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Build default value for session log files.
            ViewModelLogger.WriteLog("BUILDING EMPTY ARRAY FOR SESSION LOG FILES NOW...", LogType.WarnLog);
            this.SessionLogs = Array.Empty<string>();

            // Build log content helper and return
            ViewModelLogger.WriteLog("SETUP NEW DLL INJECTION OUTPUT LOG VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"STORED NEW VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
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
        /// Event object to run when the injector output gets new content.
        /// </summary>
        /// <param name="PipeInstance">Pipe object calling these events</param>
        /// <param name="EventArgs">The events themselves.</param>
        internal void OnPipeReaderContentProcessed(object PipeInstance, FulcrumPipeDataReadEventArgs EventArgs)
        {
            // Attach output content into our session log box.
            FulcrumDllOutputLogView ViewCast = this.BaseViewControl as FulcrumDllOutputLogView;
            if (ViewCast == null) ViewModelLogger.WriteLog("WARNING: CAST VIEW ENTRY WAS NULL!", LogType.TraceLog); 
            else ViewCast?.Dispatcher.Invoke(() => { ViewCast.DebugRedirectOutputEdit.Text += EventArgs.PipeDataString + "\n"; });
        }
    }
}
