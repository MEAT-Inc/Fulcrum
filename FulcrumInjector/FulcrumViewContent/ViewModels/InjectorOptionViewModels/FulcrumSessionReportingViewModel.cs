using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic;
using FulcrumInjector.FulcrumLogic.FulcrumPipes.PipeEvents;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews;
using FulcrumInjector.FulcrumViewSupport;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// Session reporting ViewModel used for sending cleaned up session information out to my email
    /// Email is defined in the app settings.
    /// </summary>
    public class FulcrumSessionReportingViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SessionReportViewModelLogger")) ?? new SubServiceLogger("SessionReportViewModelLogger");

        // Private Control Values
        private bool _canModifyMessage = true;
        private bool _showEmailInfoText = true;
        private FulcrumEmailBroker _sessionReportSender;

        // Public values for our view to bind onto 
        public bool CanModifyMessage { get => _canModifyMessage; set => PropertyUpdated(value); }
        public bool ShowEmailInfoText { get => _showEmailInfoText; set => PropertyUpdated(value); }
        public FulcrumEmailBroker SessionReportSender { get => _sessionReportSender; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumSessionReportingViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP REPORTING VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Build our new Email broker instance
            this.ShowEmailInfoText = true;
            if (GenerateEmailBroker(out var NewSender)) this.SessionReportSender = NewSender;
            else throw new InvalidOperationException("FAILED TO CONFIGURE NEW EMAIL HELPER OBJECT!");

            // Log passed. Build in main log file and session logs if any.
            ViewModelLogger.WriteLog("EMAIL REPORT BROKER HAS BEEN BUILT OK AND BOUND TO OUR VIEW CONTENT!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"ATTACHED MAIN LOG FILE NAMED: {this.AppendDefaultLogFiles()} OK!", LogType.InfoLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW VIEW MODEL FOR EMAIL BROKER VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("REPORT EMAIL BROKER IS NOW READY FOR USE AND REPORT SENDING!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------
     
        /// <summary>
        /// Builds a new email reporting broker for the given settings values.
        /// </summary>
        /// <param name="BuiltSender">Sender object built</param>
        /// <returns>True if built ok. False if not.</returns>
        private bool GenerateEmailBroker(out FulcrumEmailBroker BuiltSender)
        {
            try
            {
                // Pull in new settings values for sender and default receivers.
                ViewModelLogger.WriteLog("PULLING IN NEW VALUES FOR BROKER OBJECT AND CONSTRUCTING IT", LogType.InfoLog);
                var EmailConfigObject = ValueLoaders.GetConfigValue<dynamic>("FulcrumInjectorConstants.InjectorEmailConfiguration.SenderConfiguration");
                string SendName = EmailConfigObject.ReportSenderName;
                string SendEmail = EmailConfigObject.ReportSenderEmail;

                // Convert our password in base64
                byte[] SenderPasswordBytes = Convert.FromBase64String(EmailConfigObject.ReportSenderPassword);
                string SendPassword = Encoding.UTF8.GetString(EmailConfigObject.ReportSenderPassword) + "!";

                // Build broker first
                ViewModelLogger.WriteLog("PULLED IN NEW INFORMATION VALUES FOR OUR RECIPIENT AND SENDERS CORRECTLY! BUILDING BROKER NOW...", LogType.InfoLog);
                BuiltSender = new FulcrumEmailBroker(SendName, SendEmail, SendPassword);

                // Now try and authorize the client for a google address.
                ViewModelLogger.WriteLog("PULLING IN SMTP CONFIG VALUES AND AUTHORIZING CLIENT FOR USE NOW...", LogType.WarnLog);
                var SmtpConfigObject = ValueLoaders.GetConfigValue<dynamic>("FulcrumInjectorConstants.InjectorEmailConfiguration.SmtpServerSettings");
                var SmtpServerPort = (int)SmtpConfigObject.ServerPort;
                var SmtpServerName = (string)SmtpConfigObject.ServerName;
                var SmtpServerTimeout = (int)SmtpConfigObject.ServerTimeout;

                // Store configuration values for client and then authorize it.
                BuiltSender.StoreSmtpConfiguration(SmtpServerName, SmtpServerPort, SmtpServerTimeout);
                if (BuiltSender.AuthenticateSmtpClient()) ViewModelLogger.WriteLog("AUTHORIZED NEW CLIENT CORRECTLY! READY TO PROCESS AND SEND REPORTS!", LogType.InfoLog);
                else throw new InvalidOperationException("FAILED TO AUTHORIZE SMTP CLIENT BROKER ON THE REPORT SENDING OBJECT!");
                return true;
            }
            catch (Exception BuildBrokerEx)
            {
                ViewModelLogger.WriteLog("FAILED TO CONSTRUCT A NEW BROKER FOR EMAIL CONTENTS! THIS IS STRANGE!", LogType.WarnLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN IS BEING LOGGED BELOW.", BuildBrokerEx);
                BuiltSender = null; return false;
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Appends the current injector log into the email object for including it.
        /// </summary>
        /// <returns>Name of the main log file.</returns>
        internal string AppendDefaultLogFiles()
        {
            // Log information, find the main log file name, and include it in here.
            ViewModelLogger.WriteLog("INCLUDING MAIN LOG FILE FROM NLOG OUTPUT IN THE LIST OF ATTACHMENTS NOW!", LogType.WarnLog);

            // Get file name. Store and return it
            string LogFileName = LogBroker.MainLogFileName;
            this.SessionReportSender.AddMessageAttachment(LogFileName);
            ViewModelLogger.WriteLog($"ATTACHED NEW FILE NAMED {LogFileName} INTO SESSION ATTACHMENTS CORRECTLY!", LogType.InfoLog);
            return LogFileName;
        }
        /// <summary>
        /// Pulls in the session logs from our DLL output view.
        /// </summary>
        /// <returns>Strings added</returns>
        internal string[] AppendSessionLogFiles()
        {
            // Log information, pull view from constants and get values.
            ViewModelLogger.WriteLog("PULLING IN DLL SESSION LOG FILE ENTRIES NOW...", LogType.InfoLog);
            var FilesLocated = FulcrumConstants.FulcrumDllOutputLogViewModel?.SessionLogs;
            if (FilesLocated == null) {
                ViewModelLogger.WriteLog("ERROR! SESSION LOG OBJECT WAS NULL!", LogType.ErrorLog);
                return Array.Empty<string>();
            }

            // Check how many files we pulled and return.
            ViewModelLogger.WriteLog(
                FilesLocated.Length == 0 ? 
                    "NO FILES WERE LOCATED ON THE VIEW MODEL OBJECT!" : 
                    $"FOUND A TOTAL OF {FilesLocated.Length} SESSION LOG FILES!", 
                FilesLocated.Length == 0 ?
                    LogType.WarnLog :
                    LogType.InfoLog);

            // Append them into our list of reports and return.
            foreach (var MessageAttachment in FilesLocated) {
                this.SessionReportSender.AddMessageAttachment(MessageAttachment);
                ViewModelLogger.WriteLog($"APPENDED ATTACHMENT FILE: {MessageAttachment} TO REPORT", LogType.TraceLog);
            }

            // Return information and return out.
            ViewModelLogger.WriteLog("RETURNING OUTPUT FROM THE SESSION LOG EXTRACTION ROUTINE NOW...", LogType.InfoLog);
            return FilesLocated;
        }
        /// <summary>
        /// Event object to run when the injector output gets new content.
        /// Pulls log files out from text and saves their contents here.
        /// </summary>
        /// <param name="PipeInstance">Pipe object calling these events</param>
        /// <param name="EventArgs">The events themselves.</param>
        internal void OnPipeReaderContentProcessed(object PipeInstance, FulcrumPipeDataReadEventArgs EventArgs)
        {
            // See if there's a new log file to contain and update here.
            if (!EventArgs.PipeDataString.Contains("Session Log File:")) return;

            // Store log file object name onto our injector constants here.
            string NextSessionLog = string.Join("", EventArgs.PipeDataString.Split(':').Skip(1));
            ViewModelLogger.WriteLog("STORING NEW FILE NAME VALUE INTO STORE OBJECT FOR REGISTERED OUTPUT FILES!", LogType.WarnLog);
            ViewModelLogger.WriteLog($"SESSION LOG FILE BEING APPENDED APPEARED TO HAVE NAME OF {NextSessionLog}", LogType.InfoLog);

            // Try and attach it to our output report helper.
            if (this.SessionReportSender.AddMessageAttachment(NextSessionLog)) ViewModelLogger.WriteLog("ATTACHED REPORT FILE OK!", LogType.InfoLog);
            else ViewModelLogger.WriteLog("FAILED TO ATTACH REPORT INTO OUR OUTPUT CONTENT!", LogType.ErrorLog);
        }
    }
}
