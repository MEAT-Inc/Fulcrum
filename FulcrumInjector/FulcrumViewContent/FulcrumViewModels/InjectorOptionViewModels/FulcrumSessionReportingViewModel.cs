using System;
using System.Linq;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using SharpLogging;
using SharpPipes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// Session reporting ViewModel used for sending cleaned up session information out to my email
    /// Email is defined in the app settings.
    /// </summary>
    internal class FulcrumSessionReportingViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for our public properties
        private bool _canModifyMessage = true;
        private bool _showEmailInfoText = true;
        private FulcrumEmailBroker _sessionReportSender;

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool CanModifyMessage { get => _canModifyMessage; set => PropertyUpdated(value); }
        public bool ShowEmailInfoText { get => _showEmailInfoText; set => PropertyUpdated(value); }
        public FulcrumEmailBroker SessionReportSender { get => _sessionReportSender; set => PropertyUpdated(value); }

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
            this.ShowEmailInfoText = true;
            if (_generateEmailBroker(out var NewSender)) this.SessionReportSender = NewSender;
            else throw new InvalidOperationException("FAILED TO CONFIGURE NEW EMAIL HELPER OBJECT!");

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
                FilesLocated.Length == 0 ? 
                    "NO FILES WERE LOCATED ON THE VIEW MODEL OBJECT!" : 
                    $"FOUND A TOTAL OF {FilesLocated.Length} SESSION LOG FILES!", 
                FilesLocated.Length == 0 ?
                    LogType.WarnLog :
                    LogType.InfoLog);

            // Append them into our list of reports and return.
            foreach (var MessageAttachment in FilesLocated) {
                this.SessionReportSender.AddMessageAttachment(MessageAttachment);
                this.ViewModelLogger.WriteLog($"APPENDED ATTACHMENT FILE: {MessageAttachment} TO REPORT", LogType.TraceLog);
            }

            // Return information and return out.
            this.ViewModelLogger.WriteLog("RETURNING OUTPUT FROM THE SESSION LOG EXTRACTION ROUTINE NOW...", LogType.InfoLog);
            return FilesLocated;
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

        /// <summary>
        /// Builds a new email reporting broker for the given settings values.
        /// </summary>
        /// <param name="BuiltSender">Sender object built</param>
        /// <returns>True if built ok. False if not.</returns>
        private bool _generateEmailBroker(out FulcrumEmailBroker BuiltSender)
        {
            try
            {
                // Pull in new settings values for sender and default receivers.
                this.ViewModelLogger.WriteLog("PULLING IN NEW VALUES FOR BROKER OBJECT AND CONSTRUCTING IT", LogType.InfoLog);
                var EmailConfigObject = ValueLoaders.GetConfigValue<dynamic>("FulcrumConstants.InjectorEmailConfiguration.SenderConfiguration");
                string SendName = EmailConfigObject.ReportSenderName;
                string SendEmail = EmailConfigObject.ReportSenderEmail;
                string SendPassword = EmailConfigObject.ReportSenderPassword;

                // Build broker first
                this.ViewModelLogger.WriteLog("PULLED IN NEW INFORMATION VALUES FOR OUR RECIPIENT AND SENDERS CORRECTLY! BUILDING BROKER NOW...", LogType.InfoLog);
                BuiltSender = new FulcrumEmailBroker(SendName, SendEmail, SendPassword);

                // Now try and authorize the client for a google address.
                this.ViewModelLogger.WriteLog("PULLING IN SMTP CONFIG VALUES AND AUTHORIZING CLIENT FOR USE NOW...", LogType.WarnLog);
                var SmtpConfigObject = ValueLoaders.GetConfigValue<dynamic>("FulcrumConstants.InjectorEmailConfiguration.SmtpServerSettings");
                var SmtpServerPort = (int)SmtpConfigObject.ServerPort;
                var SmtpServerName = (string)SmtpConfigObject.ServerName;
                var SmtpServerTimeout = (int)SmtpConfigObject.ServerTimeout;

                // Store configuration values for client and then authorize it.
                BuiltSender.StoreSmtpConfiguration(SmtpServerName, SmtpServerPort, SmtpServerTimeout);
                if (BuiltSender.AuthenticateSmtpClient()) this.ViewModelLogger.WriteLog("AUTHORIZED NEW CLIENT CORRECTLY! READY TO PROCESS AND SEND REPORTS!", LogType.InfoLog);
                else throw new InvalidOperationException("FAILED TO AUTHORIZE SMTP CLIENT BROKER ON THE REPORT SENDING OBJECT!");
                return true;
            }
            catch (Exception BuildBrokerEx)
            {
                this.ViewModelLogger.WriteLog("FAILED TO CONSTRUCT A NEW BROKER FOR EMAIL CONTENTS! THIS IS STRANGE!", LogType.WarnLog);
                this.ViewModelLogger.WriteException("EXCEPTION THROWN IS BEING LOGGED BELOW.", BuildBrokerEx);
                BuiltSender = null; return false;
            }
        }
    }
}
