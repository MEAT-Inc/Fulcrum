using System;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using FulcrumEmailService;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using SharpLogging;
using SharpPipes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// Session reporting ViewModel used for sending cleaned up session information out to my email
    /// Email is defined in the app settings.
    /// </summary>
    public class FulcrumSessionReportingViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for our public properties
        private bool _canModifyMessage = true;          // Sets if we're able to modify the text of our email or not 
        private bool _showEmailInfoText = true;         // Sets if we're showing the help information or not 
        private FulcrumEmail _sessionReportSender;      // The sending service helper for sending emails

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool CanModifyMessage { get => _canModifyMessage; set => PropertyUpdated(value); }
        public bool ShowEmailInfoText { get => _showEmailInfoText; set => PropertyUpdated(value); }
        internal FulcrumEmail SessionReportSender { get => _sessionReportSender; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="ReportingUserControl">UserControl which holds the content for our reporting view</param>
        public FulcrumSessionReportingViewModel(UserControl ReportingUserControl) : base(ReportingUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Log information and store values 
            this.ViewModelLogger.WriteLog("SETTING UP REPORTING VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Build our new Email broker instance
            try { this._sessionReportSender = FulcrumEmail.InitializeEmailService().Result; }
            catch (Exception InitBrokerEx)
            {
                // Catch and log the broker creation exception and exit out
                this.ViewModelLogger.WriteLog("FAILED TO CONFIGURE A NEW EMAIL BROKER OBJECT!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW.", InitBrokerEx);
                throw InitBrokerEx;
            }

            // Log passed. Build in main log file and session logs if any.
            this.ViewModelLogger.WriteLog("EMAIL REPORT BROKER HAS BEEN BUILT OK AND BOUND TO OUR VIEW CONTENT!");
            this.ViewModelLogger.WriteLog($"ATTACHED MAIN LOG FILE NAMED: {this.AppendDefaultLogFiles()} OK!");
            this.ViewModelLogger.WriteLog("SETUP NEW VIEW MODEL FOR EMAIL BROKER VALUES OK!");
            this.ViewModelLogger.WriteLog("REPORT EMAIL BROKER IS NOW READY FOR USE AND REPORT SENDING!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Appends the current injector log into the email object for including it.
        /// </summary>
        /// <returns>Name of the main log file.</returns>
        public string AppendDefaultLogFiles()
        {
            // Log information, find the main log file name, and include it in here.
            this.ViewModelLogger.WriteLog("INCLUDING MAIN LOG FILE FROM NLOG OUTPUT IN THE LIST OF ATTACHMENTS NOW!", LogType.WarnLog);

            // Get file name. Store and return it
            string LogFileName = SharpLogBroker.LogFilePath;
            this.SessionReportSender.AddMessageAttachment(LogFileName);
            this.ViewModelLogger.WriteLog($"ATTACHED NEW FILE NAMED {LogFileName} INTO SESSION ATTACHMENTS CORRECTLY!", LogType.InfoLog);
            return LogFileName;
        }
        /// <summary>
        /// Pulls in the session logs from our DLL output view.
        /// </summary>
        /// <returns>Strings added</returns>
        public string[] AppendSessionLogFiles()
        {
            // Log information, pull view from constants and get values.
            this.ViewModelLogger.WriteLog("PULLING IN DLL SESSION LOG FILE ENTRIES NOW...", LogType.InfoLog);
            var FilesLocated = FulcrumConstants.FulcrumDllOutputLogViewModel?.SessionLogs;
            if (FilesLocated == null) {
                this.ViewModelLogger.WriteLog("ERROR! SESSION LOG OBJECT WAS NULL!", LogType.ErrorLog);
                return Array.Empty<string>();
            }

            // Check how many files we pulled and return.
            this.ViewModelLogger.WriteLog(
                FilesLocated.Count == 0 ? 
                    "NO FILES WERE LOCATED ON THE VIEW MODEL OBJECT!" : 
                    $"FOUND A TOTAL OF {FilesLocated.Count} SESSION LOG FILES!", 
                FilesLocated.Count == 0 ?
                    LogType.WarnLog :
                    LogType.InfoLog);

            // Append them into our list of reports and return.
            foreach (var MessageAttachment in FilesLocated) {
                this.SessionReportSender.AddMessageAttachment(MessageAttachment);
                this.ViewModelLogger.WriteLog($"APPENDED ATTACHMENT FILE: {MessageAttachment} TO REPORT", LogType.TraceLog);
            }

            // Return information and return out.
            this.ViewModelLogger.WriteLog("RETURNING OUTPUT FROM THE SESSION LOG EXTRACTION ROUTINE NOW...", LogType.InfoLog);
            return FilesLocated.ToArray();
        }
        /// <summary>
        /// Event object to run when the injector output gets new content.
        /// Pulls log files out from text and saves their contents here.
        /// </summary>
        /// <param name="PipeInstance">Pipe object calling these events</param>
        /// <param name="EventArgs">The events themselves.</param>
        public void OnPipeReaderContentProcessed(object PipeInstance, PassThruPipe.PipeDataEventArgs EventArgs)
        {
            // See if there's a new log file to contain and update here.
            if (!EventArgs.PipeDataString.Contains("Session Log File:")) return;

            // Store log file object name onto our injector constants here.
            string NextSessionLog = string.Join("", EventArgs.PipeDataString.Split(':').Skip(1));
            this.ViewModelLogger.WriteLog("STORING NEW FILE NAME VALUE INTO STORE OBJECT FOR REGISTERED OUTPUT FILES!", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"SESSION LOG FILE BEING APPENDED APPEARED TO HAVE NAME OF {NextSessionLog}", LogType.InfoLog);

            // Try and attach it to our output report helper.
            if (this.SessionReportSender.AddMessageAttachment(NextSessionLog)) this.ViewModelLogger.WriteLog("ATTACHED REPORT FILE OK!", LogType.InfoLog);
            else this.ViewModelLogger.WriteLog("FAILED TO ATTACH REPORT INTO OUR OUTPUT CONTENT!", LogType.ErrorLog);
        }
    }
}
