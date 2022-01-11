using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.EmailReporting
{
    /// <summary>
    /// Class used for sending emails out to our client applications and users
    /// </summary>
    public class SessionReportEmailBroker
    {
        // Logger object
        private SubServiceLogger EmailLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("ReportSendingBrokerLogger")) ?? new SubServiceLogger("ReportSendingBrokerLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        // Sender object information and address
        public readonly string EmailSenderName;
        public readonly MailAddress EmailSenderAddress;
        public readonly SecureString EmailSenderPassword;
        public readonly MailAddress DefaultRecipientAddress;

        // Recipients for the email being built and sent.
        private MailAddress[] _emailRecipientAddresses;
        public MailAddress[] EmailRecipientAddresses
        {
            private set => _emailRecipientAddresses = value;
            get
            {
                // Make sure the list of emails is not null. 
                if (this._emailRecipientAddresses == null && this.DefaultRecipientAddress != null)
                {
                    this.EmailLogger.WriteLog("WARING! NO EMAILS ENTERED YET! RETURNING ONLY OUR DEFAULT MAIL RECIPIENT!", LogType.WarnLog);
                    return new MailAddress[] { DefaultRecipientAddress };
                }

                // Combine the output address set with the current default address
                var OutputList = this.DefaultRecipientAddress == null ?
                    new List<MailAddress>() :
                    new List<MailAddress> { DefaultRecipientAddress };

                // Append in existing address values now.
                if (this._emailRecipientAddresses != null) OutputList.AddRange(this._emailRecipientAddresses);

                // Remove duplicates and return output
                OutputList = OutputList.GroupBy(MailObj => MailObj.ToString()).Select(MailObj => MailObj.First()).ToList();
                return OutputList.ToArray();
            }
        }

        // SMTP Client for Sending and properties for it.
        private bool _smtpSetupConfigured = false;   
        public int SmtpServerPort { get; private set; }
        public int SmtpServerTimeout { get; private set; }
        public string SmtpServerName { get; private set; }
        public SmtpClient SendingClient { get; private set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new email report broker object with the given sender information.
        /// </summary>
        /// <param name="SenderName">Name of sender</param>
        /// <param name="SenderEmail">Sender email</param>
        /// <param name="SenderPassword">Sender password</param>
        /// <param name="DefaultRecipient">Default recipient for emails</param>
        public SessionReportEmailBroker(string SenderName, string SenderEmail, string SenderPassword, string DefaultRecipient = null)
        {
            // Now build default settings values and log information
            this.EmailSenderName = SenderName;
            this.EmailSenderAddress = new MailAddress(SenderEmail);
            if (DefaultRecipient == null) this.EmailLogger.WriteLog("NOT INCLUDING A DEFAULT RECIPIENT ON THESE EMAILS!", LogType.WarnLog);
            else { this.DefaultRecipientAddress = new MailAddress(DefaultRecipient); }

            // Configure password entry and the SMTP Client here
            this.EmailSenderPassword = new SecureString();
            Array.ForEach(SenderPassword.ToArray(), this.EmailSenderPassword.AppendChar);

            // Log information about passed output here.
            this.EmailLogger.WriteLog($"EMAILS WILL BE SENT FROM USER {this.EmailSenderName} ({this.EmailSenderAddress}) WHEN USING THIS BROKER INSTANCE", LogType.InfoLog);
            this.EmailLogger.WriteLog($"PULLED IN A NEW PASSWORD VALUE OF {SenderPassword} TO USE FOR SENDING OPERATIONS", LogType.InfoLog);
            this.EmailLogger.WriteLog($"OUR DEFAULT RECIPIENT WILL BE SEEN AS {this.DefaultRecipientAddress} FOR OUTPUT REPORTS", LogType.TraceLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures new SMTP configuration information if needed.
        /// </summary>
        /// <param name="SMTPHost"></param>
        /// <param name="SMTPPort"></param>
        /// <param name="SMTPTimeout"></param>
        public void StoreSmtpConfiguration(string SMTPHost, int SMTPPort, int SMTPTimeout = 20000)
        {
            // Log information and store new values.
            this.SmtpServerName = SMTPHost;
            this.SmtpServerPort = SMTPPort;
            this.SmtpServerTimeout = SMTPTimeout;

            // Log information about the newly
            this._smtpSetupConfigured = true;
            this.EmailLogger.WriteLog("CONFIGURED NEW SMTP CLIENT LOGIN AND CONFIG VALUES!");
            this.EmailLogger.WriteLog($"HOST: {this.EmailSenderName} | PORT: {this.SmtpServerPort} | TIMEOUT: {this.SmtpServerTimeout}", LogType.TraceLog);
        }

        /// <summary>
        /// Authorizes the sender email passed into the CTOR and ensures we can use it.
        /// </summary>
        /// <param name="OutputClient">Built SMTP client object if this passed</param>
        /// <returns>True if authorized. False if not.</returns>
        public bool AuthenticateSmtpClient()
        {
            // Check if config values are built.
            if (!this._smtpSetupConfigured) {
                this.EmailLogger.WriteLog("SMTP CONFIGURATION VALUES ARE NOT YET BUILT! RETURNING NULL!", LogType.ErrorLog); 
                return false;
            }

            try
            {
                // First build a new SMTP Client.
                this.EmailLogger.WriteLog("ATTEMPTING TO CONNECT TO OUR SMTP SERVER NOW...", LogType.WarnLog);
                this.SendingClient = new SmtpClient
                {
                    // Setup name and port values and timeout value.
                    Host = this.SmtpServerName,
                    Port = this.SmtpServerPort,
                    Timeout = this.SmtpServerTimeout,

                    // SSL Configuration
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(EmailSenderAddress.Address, this.EmailSenderPassword),
                };

                // Return passed and the built client.
                this.EmailLogger.WriteLog("BUILT NEW SMTP CLIENT OBJECT OK! RETURNING IT NOW", LogType.InfoLog);
                return true;
            }
            catch (Exception SetupEx)
            {
                // Log the failure output
                this.EmailLogger.WriteLog("FAILED TO CONFIGURE OUR NEW SMTP CLIENT! THIS IS A SERIOUS ISSUE!", LogType.ErrorLog);
                this.EmailLogger.WriteLog("EXCEPTION IS BEING LOGGED BELOW", SetupEx); 
                return false;
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds a new recipient into our list of recipients.
        /// Skips the name value if it's already in the list.
        /// </summary>
        /// <param name="NextRecipient">Name to add in.</param>
        /// <returns>True if adding the user passed (Unique and format is right). False if not.</returns>
        public bool AddNewRecipient(string NextRecipient)
        {
            try
            {
                // Try and make this string an email
                var TempAddress = new MailAddress(NextRecipient);
                if (EmailRecipientAddresses.Any(EmailObj => Equals(EmailObj, TempAddress)))
                {
                    this.EmailLogger.WriteLog("ADDRESS WAS ALREADY IN OUR LIST OF RECIPIENTS! RETURNING FALSE", LogType.WarnLog);
                    return false;
                }

                // Add address here since it does not currently exist.
                this.EmailLogger.WriteLog("NEW EMAIL PASSED IS A VALID UNIQUE EMAIL! ADDING TO OUR LIST NOW", LogType.InfoLog);
                this.EmailRecipientAddresses = this.EmailRecipientAddresses.Append(TempAddress).ToArray();
                this.EmailLogger.WriteLog($"CURRENT EMAILS: {string.Join(",", this.EmailRecipientAddresses.Select(MailObj => MailObj.ToString()))}", LogType.TraceLog);
                return true;
            }
            catch
            {
                // If it failed, then return false.
                this.EmailLogger.WriteLog("EMAIL PROVIDED WAS NOT A VALID EMAIL! RETURNING FALSE", LogType.WarnLog);
                return false;
            }
        }
    }
}