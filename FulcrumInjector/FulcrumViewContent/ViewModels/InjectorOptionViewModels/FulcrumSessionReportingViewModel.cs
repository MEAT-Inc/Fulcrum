using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.EmailReporting;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
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
        private bool _showEmailInfoText = true;
        private SessionReportEmailBroker _sessionReportSender;

        // Public values for our view to bind onto 
        public bool ShowEmailInfoText { get => _showEmailInfoText; set => PropertyUpdated(value); }
        public SessionReportEmailBroker SessionReportSender { get => _sessionReportSender; set => PropertyUpdated(value); }

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
            if (GenerateEmailBroker(out var NewSender)) this.SessionReportSender = NewSender;
            else throw new InvalidOperationException("FAILED TO CONFIGURE NEW EMAIL HELPER OBJECT!");

            // Log passed. Build in main log file.
            ViewModelLogger.WriteLog("EMAIL REPORT BROKER HAS BEEN BUILT OK AND BOUND TO OUR VIEW CONTENT!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"ATTACHED MAIN LOG FILE NAMED: {this.AppendDefaultLogFile()} OK!", LogType.InfoLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW VIEW MODEL FOR EMAIL BROKER VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("REPORT EMAIL BROKER IS NOW READY FOR USE AND REPORT SENDING!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Appends the current injector log into the email object for including it.
        /// </summary>
        /// <returns>Name of the main log file.</returns>
        private string AppendDefaultLogFile()
        {
            // Log information, find the main log file name, and include it in here.
            ViewModelLogger.WriteLog("INCLUDING MAIN LOG FILE FROM NLOG OUTPUT IN THE LIST OF ATTACHMENTS NOW!", LogType.WarnLog);

            // Get file name. Store and return it.
            string LogFileName = LogBroker.MainLogFileName;
            this.SessionReportSender.AddMessageAttachment(LogFileName);
            ViewModelLogger.WriteLog($"ATTACHED NEW FILE NAMED {LogFileName} INTO SESSION ATTACHMENTS CORRECTLY!", LogType.InfoLog);
            return LogFileName;
        }

        /// <summary>
        /// Builds a new email reporting broker for the given settings values.
        /// </summary>
        /// <param name="BuiltSender">Sender object built</param>
        /// <returns>True if built ok. False if not.</returns>
        private bool GenerateEmailBroker(out SessionReportEmailBroker BuiltSender)
        { 
            try
            {
                // Pull in new settings values for sender and default receivers.
                ViewModelLogger.WriteLog("PULLING IN NEW VALUES FOR BROKER OBJECT AND CONSTRUCTING IT", LogType.InfoLog);
                var EmailConfigObject = ValueLoaders.GetConfigValue<dynamic>("FulcrumInjectorConstants.InjectorEmailConfiguration.SenderConfiguration");
                string SendName = EmailConfigObject.ReportSenderName;
                string SendEmail = EmailConfigObject.ReportSenderEmail;
                string SendPassword = EmailConfigObject.ReportSenderPassword;

                // Build broker first
                ViewModelLogger.WriteLog("PULLED IN NEW INFORMATION VALUES FOR OUR RECIPIENT AND SENDERS CORRECTLY! BUILDING BROKER NOW...", LogType.InfoLog);
                BuiltSender = new SessionReportEmailBroker(SendName, SendEmail, SendPassword);

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
        /// Updates the content of the email body passed in to include the names and paths of the attached file content on the output.
        /// </summary>
        /// <param name="MessageSubject">Subject of the message</param>
        /// <param name="MessageBodyContent">Body of the message</param>
        /// <returns>True if sent ok. false if not.</returns>
        public bool SendDiagnosticReport(string MessageSubject, string MessageBodyContent)
        {
            // Return true if this is sent OK. Temp return true for testing.
            ViewModelLogger.WriteLog("SENDING EMAIL OBJECT TO DESIRED RECIPIENTS NOW...", LogType.WarnLog);
            return true;
        }
    }
}
