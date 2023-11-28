using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Authentication;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FulcrumEmailService.EmailServiceModels;
using FulcrumJson;
using System.Xml.Linq;
using FulcrumService;
using FulcrumSupport;
using SharpLogging;
using Newtonsoft.Json;

namespace FulcrumEmailService
{
    /// <summary>
    /// Class used for sending emails out to our client applications and users
    /// </summary>
    public partial class FulcrumEmail : FulcrumServiceBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields for our email service instance
        private static FulcrumEmail _serviceInstance;                   // Instance of our service object
        private static readonly object _serviceLock = new();            // Lock object for building service instances

        // Private configuration objects for service setup
        private EmailServiceSettings _serviceConfig;                    // Settings configuration for our service
        private MailAddress[] _emailRecipientAddresses;                 // Backing field for email service configuration
        private EmailSmtpConfiguration _stmpConfiguration;              // Configuration for SMTP Server connections
        private EmailBrokerConfiguration _emailSenderConfiguration;     // Configuration for the broker instance itself

        #endregion //Fields

        #region Properties

        // Public facing property holding our authorization state for the email client 
        public bool IsEmailClientAuthorized { get; private set; }

        // Public facing collection of email address properties used to configure sending outgoing messages
        public SmtpClient SendingClient { get; private set; }
        public MailAddress EmailSenderAddress => new(this._emailSenderConfiguration.ReportSenderEmail);
        public MailAddress[] DefaultRecipientAddresses
        {
            private set => _emailRecipientAddresses = value;
            get
            {
                // Make sure the list of emails is not null, clean it, and return it out
                this._emailRecipientAddresses ??= Array.Empty<MailAddress>();
                List<MailAddress> OutputList = this._emailRecipientAddresses
                    .GroupBy(MailObj => MailObj.ToString())
                    .Select(MailObj => MailObj.First())
                    .ToList();

                // Return our output email address list here
                return OutputList.ToArray();
            }
        }
        public FileInfo[] DefaultMessageAttachmentFiles { get; private set; }

        // Computed properties based on the SMTP and Broker configuration
        public bool SmtpSetupConfigured => this._stmpConfiguration != null;
        public bool EmialBrokerConfigured => this._emailSenderConfiguration != null;

        #endregion //Properties

        #region Structs and Classes
        
        /// <summary>
        /// Public class object holding information and configuration for a message to be sent using this email service
        /// </summary>
        public class FulcrumMessage
        {
            #region Custom Events
            #endregion // Custom Events

            #region Fields
            #endregion // Fields

            #region Properties

            // Public facing properties for our email object sender and recipients
            public string SenderAddress { get; internal set; }
            public List<string> RecipientsList { get; set; }

            // Public facing properties about our email object content
            public string MessageSubject { get; set; }
            public string MessageBodyContent { get; set; }
            public List<string> MessageAttachments { get; set; }

            #endregion // Properties

            #region Structs and Classes
            #endregion // Structs and Classes

            // --------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Builds a new FulcrumMessage object to be used for sending messages
            /// </summary>
            internal FulcrumMessage()
            {
                // Configure new default values for the recipients, attachments, and build a subject
                this.RecipientsList = new List<string>();
                this.MessageAttachments = new List<string>();
                this.MessageSubject = $"Fulcrum Reporting - {DateTime.Now:f}";
            }
        }

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private CTOR for building a new singleton of our email broker client
        /// </summary>
        /// <param name="ServiceSettings">Optional settings object for our service configuration</param>
        internal FulcrumEmail(EmailServiceSettings ServiceSettings = null) : base(ServiceTypes.EMAIL_SERVICE)
        {
            // Check if we're consuming this service instance or not
            if (this.IsServiceClient)
            {
                // If we're a client, just log out that we're piping commands across to our service and exit out
                this._serviceLogger.WriteLog("WARNING! EMAIL SERVICE IS BEING BOOTED IN CLIENT CONFIGURATION!", LogType.WarnLog);
                this._serviceLogger.WriteLog("ALL COMMANDS/ROUTINES EXECUTED ON THE DRIVE SERVICE WILL BE INVOKED USING THE HOST SERVICE!", LogType.WarnLog);
                return;
            }

            // Log we're building this new service and log out the name we located for it
            this._serviceLogger.WriteLog("SPAWNING NEW EMAIL SERVICE!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"PULLED IN A NEW SERVICE NAME OF {this.ServiceName}", LogType.InfoLog);

            // Pull our settings configuration for the service here 
            this._serviceConfig = ServiceSettings ?? ValueLoaders.GetConfigValue<EmailServiceSettings>("FulcrumServices.FulcrumEmailService");
            this._serviceLogger.WriteLog("PULLED BASE SERVICE CONFIGURATION VALUES CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"SERVICE NAME: {this._serviceConfig.ServiceName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SERVICE ENABLED: {this._serviceConfig.ServiceEnabled}", LogType.TraceLog);

            // Log out information about our service configuration here
            this._emailSenderConfiguration = this._serviceConfig.SenderConfiguration;
            this._serviceLogger.WriteLog("PULLED CONFIGURATION FOR EMAIL SMTP CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"SENDER NAME: {this._emailSenderConfiguration.ReportSenderName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SENDER NAME:  {this._emailSenderConfiguration.ReportSenderName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"INCLUDE SERVICE LOGS:  {(this._emailSenderConfiguration.IncludeServiceLogs ? "YES" : "NO")}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"INCLUDE INJECTOR LOGS: {(this._emailSenderConfiguration.IncludeInjectorLog ? "YES" : "NO")}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"DEFAULT RECIPIENT: {string.Join("," , this._emailSenderConfiguration.DefaultReportRecipients)}", LogType.TraceLog);

            // Log out information about our SMTP configuration here
            this._stmpConfiguration = this._serviceConfig.SmtpServerSettings;
            this._serviceLogger.WriteLog("PULLED CONFIGURATION FOR EMAIL SMTP CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"SMTP PORT: {this._stmpConfiguration.ServerPort}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SMTP SERVER: {this._stmpConfiguration.ServerName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SMTP TIMEOUT: {this._stmpConfiguration.ServerTimeout}", LogType.TraceLog);

            // Log information about passed output here.
            this._serviceLogger.WriteLog($"EMAILS WILL BE SENT FROM USER {this._emailSenderConfiguration.ReportSenderName} ({this.EmailSenderAddress}) WHEN USING THIS BROKER INSTANCE", LogType.InfoLog);
            this._serviceLogger.WriteLog($"OUR DEFAULT RECIPIENT WILL BE SEEN AS {string.Join(",", this._emailSenderConfiguration.DefaultReportRecipients)} FOR OUTPUT REPORTS", LogType.TraceLog);
        }
        /// <summary>
        /// Static CTOR for an email broker instance. Simply pulls out the new singleton instance for our email broker
        /// </summary>
        /// <param name="ForceInit">When true, we force rebuild the requested service instance</param>
        /// <returns>The instance for our broker singleton</returns>
        public static Task<FulcrumEmail> InitializeEmailService(bool ForceInit = false)
        {
            // Make sure we actually want to use this watchdog service 
            var ServiceConfig = ValueLoaders.GetConfigValue<EmailServiceSettings>("FulcrumServices.FulcrumEmailService");
            if (!ServiceConfig.ServiceEnabled) {
                _serviceInitLogger.WriteLog("WARNING! EMAIL SERVICE IS TURNED OFF IN OUR CONFIGURATION FILE! NOT BOOTING IT", LogType.WarnLog);
                return null;
            }

            // Spin up a new injector email service here if needed           
            _serviceInitLogger.WriteLog($"SPAWNING A NEW EMAIL SERVICE INSTANCE NOW...", LogType.WarnLog); 
            return Task.Run(() =>
            {
                // Lock our service object for thread safe operations
                lock (_serviceLock)
                {
                    // Check if we need to force rebuilt this service or not
                    if (_serviceInstance != null && !ForceInit) {
                        _serviceInitLogger.WriteLog("FOUND EXISTING EMAIL SERVICE INSTANCE! RETURNING IT NOW...");
                        return _serviceInstance;
                    }

                    // Build and boot a new service instance for our watchdog
                    _serviceInstance = new FulcrumEmail(ServiceConfig);
                    _serviceInitLogger.WriteLog("SPAWNED NEW INJECTOR EMAIL SERVICE OK!", LogType.InfoLog);

                    // Return the service instance here
                    return _serviceInstance;
                }
            });
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts the service up and builds an email helper process
        /// </summary>
        /// <param name="StartupArgs">NOT USED!</param>
        protected override void OnStart(string[] StartupArgs)
        {
            try
            {
                // Log out what type of service is being configured currently
                this._serviceLogger.WriteLog($"BOOTING NEW {this.GetType().Name} SERVICE NOW...", LogType.WarnLog);
                this._serviceLogger.WriteLog($"CONFIGURING NEW EMAIL/SMTP CONNECTION FOR INJECTOR SERVICE...", LogType.InfoLog);

                // Authorize our git client here if needed
                if (!this._authorizeEmailClient())
                    throw new AuthenticationException("Error! Failed to authorize SMTP client for the MEAT Inc Organization!");

                // Log out that our service has been booted without issues
                this._serviceLogger.WriteLog("EMAIL SERVICE HAS BEEN CONFIGURED AND BOOTED CORRECTLY!", LogType.InfoLog);
            }
            catch (Exception StartWatchdogEx)
            {
                // Log out the failure and exit this method
                this._serviceLogger.WriteLog("ERROR! FAILED TO BOOT NEW EMAIL SERVICE INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE START ROUTINE IS LOGGED BELOW", StartWatchdogEx);
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawns a new FulcrumMessage object used to configure emails to send out using this service
        /// </summary>
        /// <returns></returns>
        public FulcrumMessage CreateFulcrumMessage()
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(CreateFulcrumMessage));
                return PipeAction.PipeCommandResult as FulcrumMessage;
            }

            // Log out that we're creating a new mail message object here
            this._serviceLogger.WriteLog("BUILDING NEW MAIL MESSAGE FOR EMAIL SERVICE NOW...", LogType.InfoLog);

            // Build a new message object to configure and return
            FulcrumMessage OutputMessage = new FulcrumMessage();
            OutputMessage.SenderAddress = this._emailSenderConfiguration.ReportSenderEmail;
            OutputMessage.MessageBodyContent = this._emailSenderConfiguration.DefaultEmailBodyText;
            OutputMessage.MessageAttachments.AddRange(this.DefaultMessageAttachmentFiles.Select(FileObj => FileObj.FullName));
            OutputMessage.RecipientsList.AddRange(this.DefaultRecipientAddresses.Select(RecipientAddress => RecipientAddress.Address));

            // Once built, return our mail message 
            return OutputMessage;
        }
        /// <summary>
        /// Sends out the resulting report email object when this is called.
        /// </summary>
        /// <param name="MessageToSend">The message object being sent out</param>
        /// <returns>True if the message is sent. False if not.</returns>
        public bool SendFulcrumMessage(FulcrumMessage MessageToSend)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(SendFulcrumMessage), MessageToSend);
                return bool.Parse(PipeAction.PipeCommandResult.ToString());
            }

            // Log information about the startup of this new message object.
            this._serviceLogger.WriteLog($"PREPARING TO SEND OUT A NEW MESSAGE TO {MessageToSend.RecipientsList.Count} RECIPIENTS TITLED {MessageToSend.MessageSubject}", LogType.WarnLog);
            this._serviceLogger.WriteLog("BODY CONTENT OBJECT IS BEING APPENDED INTO A MAILMESSAGE OBJECT NOW...", LogType.WarnLog);

            // Clean up our attachments here
            MessageToSend.MessageAttachments.AddRange(this.DefaultMessageAttachmentFiles.Select(AttachmentFile => AttachmentFile.FullName));
            MessageToSend.MessageAttachments = MessageToSend.MessageAttachments
                .GroupBy(FileName => FileName.ToString())
                .Select(FileName => FileName.First())
                .Where(File.Exists)
                .ToList();

            // Update the content of our message with a final output for log file entries and names.
            var OutputFilesTupleSet = MessageToSend.MessageAttachments.Select(FileObj =>
            {
                // Spawn a new file info for the attachment given
                FileInfo AttachmentInfo = new FileInfo(FileObj);

                // Build a new tuple object from the given file and return it.
                string FileName = AttachmentInfo.Name;
                string FileSizeFormatted = AttachmentInfo.Length.ToFileSize();
                string TimeLastModified = AttachmentInfo.LastWriteTime.ToString("f");
                return new Tuple<string, string, string>(FileName, FileSizeFormatted, TimeLastModified);
            }).ToArray();
            this._serviceLogger.WriteLog("BUILT NEW TUPLE ARRAY FOR ALL FILE OBJECTS IN USE CORRECTLY!", LogType.InfoLog);

            // Build our output table object as a text block using the converter class.
            string OutputFileTable =
                "Session Log File Attachments\n" + OutputFilesTupleSet.ToStringTable(
                new[] { "File Name", "Size", "Last Modified" },
                FileObj => FileObj.Item1,
                FileObj => FileObj.Item2,
                FileObj => FileObj.Item3
            );

            // Log the output table object here and build out the MailMessage.
            MessageToSend.MessageBodyContent += $"\n\n{string.Concat(Enumerable.Repeat("=", 75))}\n\n{OutputFileTable}";
            this._serviceLogger.WriteLog("BUILT NEW OUTPUT TABLE CORRECTLY! ENTIRE MESSAGE OBJECT AND OUTPUT TABLE IS LOGGED BELOW!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"\n{OutputFileTable}", LogType.TraceLog);

            // Ensure we've got at least once recipient added in here
            MessageToSend.RecipientsList.AddRange(this.DefaultRecipientAddresses.Select(RecipientAddress => RecipientAddress.Address));
            MessageToSend.RecipientsList = MessageToSend.RecipientsList
                .GroupBy(MailObj => MailObj.ToString())
                .Select(MailObj => MailObj.First())
                .ToList();

            // Parse our list of recipients and see which ones are valid email addresses
            Regex SendingRegex = new Regex(@"([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)");
            var MatchedEmails = SendingRegex.Matches(string.Join(",", MessageToSend.RecipientsList));
            List<MailAddress> RecipientAddresses = MatchedEmails
                .Cast<Match>()
                .Select(AddressMatch => new MailAddress(AddressMatch.Value))
                .ToList();

            // Check to make sure we've got at least one recipient here
            if (MatchedEmails.Count == 0) {
                this._serviceLogger.WriteLog("ERROR! NO RECIPIENTS WERE FOUND FOR MAIL MESSAGE CONTENT!", LogType.ErrorLog);
                return false;
            }

            // Build a new message and append all of our attachments and recipients here
            MailMessage OutputMessage = new MailMessage();
            OutputMessage.Body = MessageToSend.MessageBodyContent;
            OutputMessage.Subject = MessageToSend.MessageSubject;
            OutputMessage.Sender = new MailAddress(MessageToSend.SenderAddress);
            foreach (var RecipientAddress in RecipientAddresses) OutputMessage.To.Add(RecipientAddress);
            foreach (var MessageAttachment in MessageToSend.MessageAttachments) OutputMessage.Attachments.Add(new Attachment(MessageAttachment));

            // Log information out about our mail message built and send it out once built
            this._serviceLogger.WriteLog("BUILT NEW OUTPUT MAIL MESSAGE FROM INPUT FULCRUM MESSAGE OK!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"MESSAGE OBJECT CONTAINS {OutputMessage.To.Count} RECIPIENTS AND {OutputMessage.Attachments.Count} ATTACHMENTS!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"RECIPIENTS: {string.Join(",", OutputMessage.To.Select(Recipient => Recipient.Address))}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"ATTACHMENTS: {string.Join(",", OutputMessage.Attachments.Select(Attachment => Attachment.Name))}", LogType.TraceLog);

            try
            {
                // Ensure our SMTP server instance has been configured correctly.
                if (!this.SmtpSetupConfigured)
                    throw new InvalidOperationException("Error! SMTP Client configuration is not setup!");

                // Now fire it off using our SMTP Server instance.
                this._serviceLogger.WriteLog($"SENDING OUTPUT MESSAGE NOW...", LogType.WarnLog);
                if (this._authorizeEmailClient()) this.SendingClient.Send(OutputMessage);
                else throw new AuthenticationException("Error! Failed to authenticate an email client!");
                this._serviceLogger.WriteLog($"MESSAGE WAS SENT CORRECTLY TO ALL REQUESTED RECIPIENTS!", LogType.InfoLog);

                // Return passed once done
                return true;
            }
            catch (Exception MailEx)
            {
                // Log failures, set the overall output value to false.
                this._serviceLogger.WriteLog($"ERROR! FAILED TO SEND OUTPUT MAIL MESSAGE TO RECIPIENTS!", LogType.ErrorLog);
                this._serviceLogger.WriteException("EMAIL EXCEPTION IS BEING LOGGED BELOW.", MailEx);
                return false;
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper method used to configure a new SMTP client for sending emails out 
        /// </summary>
        /// <returns>True if the client is authorized. False if it is not</returns>
        private bool _authorizeEmailClient()
        {
            try
            {
                // Check if we're configured or not already 
                if (this.IsEmailClientAuthorized) return true;

                // First build a new SMTP Client.
                this._serviceLogger.WriteLog("ATTEMPTING TO CONNECT TO OUR SMTP SERVER NOW...", LogType.WarnLog);
                this.SendingClient = new SmtpClient
                {
                    // Setup name and port values and timeout value.
                    Host = this._stmpConfiguration.ServerName,
                    Port = this._stmpConfiguration.ServerPort,
                    Timeout = this._stmpConfiguration.ServerPort,

                    // SSL Configuration
                    EnableSsl = true,
                    Credentials = new NetworkCredential(EmailSenderAddress.Address, this._emailSenderConfiguration.ReportSenderPassword),

                    // BUG: This is causing some type of auth issues. Removing for testing.
                    // DeliveryMethod = SmtpDeliveryMethod.Network
                };

                // Return passed and the built client.
                this._serviceLogger.WriteLog("BUILT NEW SMTP CLIENT OBJECT OK! EMAIL CONFIGURATION HAS BEEN COMPLETED!", LogType.InfoLog);
                this.IsEmailClientAuthorized = true;
                return true;
            }
            catch (Exception SetupEx)
            {
                // Log the failure output
                this._serviceLogger.WriteLog("FAILED TO CONFIGURE OUR NEW SMTP CLIENT! THIS IS A SERIOUS ISSUE!", LogType.ErrorLog);
                this._serviceLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW", SetupEx);
                return false;   
            }
        }
    }
}