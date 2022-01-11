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
        private string[] _emailAddressRecipients;
        private SessionReportEmailBroker _sessionReportSender;

        // Public values for our view to bind onto 
        public string[] EmailAddressRecipients { get => _emailAddressRecipients; private set => OnPropertyChanged(); }
        public SessionReportEmailBroker SessionReportSender { get => _sessionReportSender; private set => OnPropertyChanged(); }

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
            ViewModelLogger.WriteLog("EMAIL REPORT BROKER HAS BEEN BUILT OK AND BOUND TO OUR VIEW CONTENT!", LogType.InfoLog);

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
                string DefaultRecipient = EmailConfigObject.DefaultReportRecipient;

                // Build broker first
                ViewModelLogger.WriteLog("PULLED IN NEW INFORMATION VALUES FOR OUR RECIPIENT AND SENDERS CORRECTLY! BUILDING BROKER NOW...", LogType.InfoLog);
                BuiltSender = new SessionReportEmailBroker(SendName, SendEmail, SendPassword, DefaultRecipient);

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

        /// <summary>
        /// Adds a new email address into our list of emails to control
        /// </summary>
        /// <param name="AddressToAppend">Address to add</param>
        /// <returns>True if the address is added OK. False if not.</returns>
        public bool AppendNewAddress(string AddressToAppend)
        {
            // Try and add the address into a copy of our sending broker list.
            ViewModelLogger.WriteLog($"APPENDING ADDRESS {AddressToAppend} NOW...", LogType.InfoLog);
            if (this.SessionReportSender.AddNewRecipient(AddressToAppend))
            {
                // Now set our list of email objects for addresses and update the view.
                ViewModelLogger.WriteLog("UPDATED SESSION RECIPIENTS CORRECTLY!", LogType.InfoLog);
                this.EmailAddressRecipients = this.SessionReportSender.EmailRecipientAddresses.Select(EmailObj => EmailObj.ToString()).ToArray();
                ViewModelLogger.WriteLog($"VIEW CONTENTS NOW SHOWING CORRECT CONTENTS FOR A TOTAL OF {this.SessionReportSender.EmailRecipientAddresses.Length} EMAILS");
                return true;
            }

            // Log failed, return false and move on.
            ViewModelLogger.WriteLog("FAILED TO ADD NEW EMAIL INTO SESSION LIST! THIS IS CONCERNING!", LogType.ErrorLog);
            return false;   
        }
    }
}
