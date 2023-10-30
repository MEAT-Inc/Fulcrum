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
        private static FulcrumEmail _serviceInstance;         // Instance of our service object
        private static readonly object _serviceLock = new();    // Lock object for building service instances

        // Private configuration objects for service setup
        private EmailServiceSettings _serviceConfig;              // Settings configuration for our service
        private MailAddress[] _emailRecipientAddresses;           // Backing field for email service configuration
        private EmailSmtpConfiguration _stmpConfiguration;        // Configuration for SMTP Server connections
        private EmailBrokerConfiguration _emailConfiguration;     // Configuration for the broker instance itself

        #endregion //Fields

        #region Properties

        // Public facing property holding our authorization state for the email client 
        public bool IsEmailClientAuthorized { get; private set; }

        // Public facing collection of email address properties used to configure sending outgoing messages
        public SmtpClient SendingClient { get; private set; }
        public MailAddress[] EmailRecipientAddresses
        {
            private set => _emailRecipientAddresses = value;
            get
            {
                // Make sure the list of emails is not null. 
                if (this._emailRecipientAddresses == null && this.DefaultRecipientAddress != null)
                {
                    this._serviceLogger.WriteLog("WARING! NO EMAILS ENTERED YET! RETURNING ONLY OUR DEFAULT MAIL RECIPIENT!", LogType.WarnLog);
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
        public FileInfo[] MessageAttachmentFiles { get; private set; }

        // Computed properties based on the SMTP and Broker configuration
        public bool EmialBrokerConfigured => this._emailConfiguration != null;
        public bool SmtpSetupConfigured => this._stmpConfiguration != null;

        // Public properties holding information about sender addresses
        public MailAddress EmailSenderAddress => new(this._emailConfiguration.ReportSenderEmail);
        public MailAddress DefaultRecipientAddress => new(this._emailConfiguration.DefaultReportRecipient);

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private CTOR for building a new singleton of our email broker client
        /// </summary>
        /// <param name="ServiceSettings">Optional settings object for our service configuration</param>
        internal FulcrumEmail(EmailServiceSettings ServiceSettings = null) : base(ServiceTypes.EMAIL_SERVICE)
        {
            // Build and register a new watchdog logging target here for a file and the console
            this.ServiceLoggingTarget = LocateServiceFileTarget<FulcrumEmail>();
            this._serviceLogger.RegisterTarget(this.ServiceLoggingTarget);

            // Log we're building this new service and log out the name we located for it
            this._serviceLogger.WriteLog("SPAWNING NEW UPDATER SERVICE!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"PULLED IN A NEW SERVICE NAME OF {this.ServiceName}", LogType.InfoLog);

            // Pull our settings configuration for the service here 
            this._serviceConfig = ServiceSettings ?? ValueLoaders.GetConfigValue<EmailServiceSettings>("FulcrumServices.FulcrumEmailService");
            this._serviceLogger.WriteLog("PULLED BASE SERVICE CONFIGURATION VALUES CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"SERVICE NAME: {this._serviceConfig.ServiceName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SERVICE ENABLED: {this._serviceConfig.ServiceEnabled}", LogType.TraceLog);

            // Log out information about our service configuration here
            this._emailConfiguration = this._serviceConfig.SenderConfiguration;
            this._serviceLogger.WriteLog("PULLED CONFIGURATION FOR EMAIL SMTP CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"SENDER NAME: {this._emailConfiguration.ReportSenderName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SENDER NAME:  {this._emailConfiguration.ReportSenderName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"DEFAULT RECIPIENT: {this._emailConfiguration.DefaultReportRecipient}", LogType.TraceLog);

            // Log out information about our SMTP configuration here
            this._stmpConfiguration = this._serviceConfig.SmtpServerSettings;
            this._serviceLogger.WriteLog("PULLED CONFIGURATION FOR EMAIL SMTP CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"SMTP PORT: {this._stmpConfiguration.ServerPort}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SMTP SERVER: {this._stmpConfiguration.ServerName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SMTP TIMEOUT: {this._stmpConfiguration.ServerTimeout}", LogType.TraceLog);

            // Log information about passed output here.
            this._serviceLogger.WriteLog($"EMAILS WILL BE SENT FROM USER {this._emailConfiguration.ReportSenderName} ({this.EmailSenderAddress}) WHEN USING THIS BROKER INSTANCE", LogType.InfoLog);
            this._serviceLogger.WriteLog($"OUR DEFAULT RECIPIENT WILL BE SEEN AS {this.DefaultRecipientAddress} FOR OUTPUT REPORTS", LogType.TraceLog);
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
        /// <summary>
        /// Invokes a custom command routine for our service based on the int code provided to it.
        /// </summary>
        /// <param name="ServiceCommand">The command to execute on our service instance (128-255)</param>
        protected override void OnCustomCommand(int ServiceCommand)
        {
            try
            {
                // Check what type of command is being executed and perform actions accordingly.
                switch (ServiceCommand)
                {
                    // For any other command value or something that is not recognized
                    case 128:

                        // Log out the command help information for the user to read in the log file.
                        this._serviceLogger.WriteLog("----------------------------------------------------------------------------------------------------------------", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"                                FulcrumInjector Email Service Command Help", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"- The provided command value of {ServiceCommand} is reserved to show this help message.", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"- Enter any command number above 128 to execute an action on our service instance.", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"- Execute this command again with the service command ID 128 to get a list of all possible commands", LogType.InfoLog);
                        this._serviceLogger.WriteLog("", LogType.InfoLog);
                        this._serviceLogger.WriteLog("Help Commands", LogType.InfoLog);
                        this._serviceLogger.WriteLog("   Command 128:  Displays this help message", LogType.InfoLog);
                        this._serviceLogger.WriteLog("----------------------------------------------------------------------------------------------------------------", LogType.InfoLog);
                        return;
                }
            }
            catch (Exception SendCustomCommandEx)
            {
                // Log out the failure and exit this method
                this._serviceLogger.WriteLog("ERROR! FAILED TO INVOKE A CUSTOM COMMAND ON AN EXISTING EMAIL SERVICE INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE CUSTOM COMMAND ROUTINE IS LOGGED BELOW", SendCustomCommandEx);
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds a new recipient into our list of recipients.
        /// Skips the name value if it's already in the list.
        /// </summary>
        /// <param name="NextRecipient">Name to add in.</param>
        /// <returns>True if adding the user passed (Unique and format is right). False if not.</returns>
        public bool AddRecipient(string NextRecipient)
        {
            try
            {
                // Try and make this string an email
                var TempAddress = new MailAddress(NextRecipient);
                if (EmailRecipientAddresses.Any(EmailObj => Equals(EmailObj, TempAddress)))
                {
                    this._serviceLogger.WriteLog("ADDRESS WAS ALREADY IN OUR LIST OF RECIPIENTS! RETURNING FALSE", LogType.WarnLog);
                    return false;
                }

                // Add address here since it does not currently exist.
                this.EmailRecipientAddresses = this.EmailRecipientAddresses.Append(TempAddress).ToArray();
                this._serviceLogger.WriteLog("NEW EMAIL PASSED IS A VALID UNIQUE EMAIL! ADDING TO OUR LIST NOW", LogType.InfoLog);
                return true;
            }
            catch
            {
                // If it failed, then return false.
                this._serviceLogger.WriteLog("EMAIL PROVIDED WAS NOT A VALID EMAIL! RETURNING FALSE", LogType.WarnLog);
                return false;
            }
        }
        /// <summary>
        /// Clears out the list of recipients to use for sending emails.
        /// </summary>
        public bool RemoveRecipient(string RecipientAddress = null)
        {
            // Check if the entered recipient is null or not.
            if (RecipientAddress == null)
            {
                // Log Removing all.
                this._serviceLogger.WriteLog("REMOVING ALL RECIPIENTS FROM LIST OF ENTRIES NOW...", LogType.WarnLog);
                this.EmailRecipientAddresses = Array.Empty<MailAddress>();
                return true;
            }

            // Remove only the given name.
            if (this.EmailRecipientAddresses.All(EmailObj => !string.Equals(EmailObj.ToString(), RecipientAddress, StringComparison.CurrentCultureIgnoreCase)))
            {
                this._serviceLogger.WriteLog($"NO EMAIL ADDRESS WITH THE VALUE {RecipientAddress} WAS FOUND!", LogType.WarnLog);
                return false;
            }

            // Remove value here and return.
            var StringAddresses = this.EmailRecipientAddresses.Select(EmailObj => EmailObj.ToString().ToUpper());
            if (!StringAddresses.ToList().Remove(RecipientAddress.ToUpper()))
            {
                this._serviceLogger.WriteLog($"FAILED TO REMOVE REQUESED ADDRESS OF {RecipientAddress}!", LogType.WarnLog);
                return false;
            }

            // Now reset email list contents here.
            this.EmailRecipientAddresses = StringAddresses.Select(StringAddr => new MailAddress(StringAddr)).ToArray();
            this._serviceLogger.WriteLog($"REMOVED ADDRESS NAME {RecipientAddress} CORRECTLY! STORING NEW ADDRESS SET NOW...", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Puts a new file path into our list of attachment objects. 
        /// </summary>
        /// <param name="PathToAttachment">File to add</param>
        /// <returns>True if added. False if not.</returns>
        public bool AddMessageAttachment(string PathToAttachment)
        {
            // Check if the list exists at all and if the new path is in it.
            this.MessageAttachmentFiles ??= Array.Empty<FileInfo>();
            this._serviceLogger.WriteLog($"ATTACHING FILE {PathToAttachment}", LogType.InfoLog);
            if (!File.Exists(PathToAttachment))
            {
                this._serviceLogger.WriteLog("FILE OBJECT DOES NOT EXIST! CAN NOT ADD IT INTO OUR ATTACHMENT LIST!", LogType.ErrorLog);
                return false;
            }

            // Find existing if possible.
            if (MessageAttachmentFiles.Any(FileObj => FileObj.Name == Path.GetFileName(PathToAttachment)))
                return false;

            // Log total file count now and add into our list.
            this._serviceLogger.WriteLog("APPENDING NEW FILE OBJECT INTO OUR LIST OF ATTACHMENTS NOW...", LogType.InfoLog);
            this.MessageAttachmentFiles = this.MessageAttachmentFiles.Append(new FileInfo(PathToAttachment)).ToArray();
            return true;
        }
        /// <summary>
        /// Removes a file from the attachment list by passing in the name of it.
        /// If filtering is on it runs a regex on the names in the system checking if any match the pattern. 
        /// </summary>
        /// <param name="NameToRemove">Name of file to remove</param>
        /// <param name="UseFilter">Filtering on or off</param>
        /// <returns>True if one or more files get removed. False if not.</returns>
        public bool RemoveMessageAttachment(string NameToRemove = null, bool UseFilter = false)
        {
            // Check for a clear command.
            if (NameToRemove == null)
            {
                // Removes all entries if no value is given.
                this._serviceLogger.WriteLog("NO NAME FILTER WAS PROVIDED! REMOVING ALL ENTRIES FROM OUR MAIL LIST NOW...");
                this.MessageAttachmentFiles = Array.Empty<FileInfo>();
                return true;
            }

            // Check if the list exists and log file removing
            this.MessageAttachmentFiles ??= Array.Empty<FileInfo>();
            this._serviceLogger.WriteLog($"REMOVING FILE NAME {NameToRemove}", LogType.InfoLog);
            if (UseFilter) this._serviceLogger.WriteLog("WARNING: REGEX FILTERING WAS TURNED ON! USING IT NOW", LogType.WarnLog);

            // Find matches or the file object.
            var FilesToRemove = this.MessageAttachmentFiles.Where(FileObj =>
            {
                // Check if filtering or not.
                if (UseFilter && Regex.Match(FileObj.FullName, NameToRemove).Success) return true;
                return NameToRemove.ToUpper().Contains(FileObj.FullName.ToUpper());
            }).ToList();

            // Check if there's files to pull.
            if (!FilesToRemove.Any())
            {
                this._serviceLogger.WriteLog("NO FILES FOUND TO REMOVE! RETURNING FAILED NOW...", LogType.ErrorLog);
                return false;
            }

            // Log how many files to remove and pull them all out.
            this._serviceLogger.WriteLog($"FILES TO PULL OUT OF THE LIST: {FilesToRemove.Count()}", LogType.InfoLog);
            this.MessageAttachmentFiles = this.MessageAttachmentFiles.Where(FileObj => !FilesToRemove.Contains(FileObj)).ToArray();
            return true;
        }
        /// <summary>
        /// Sends out the resulting report email object when this is called.
        /// </summary>
        /// <param name="MessageSubject">Subject of the message</param>
        /// <param name="MessageBodyContent">Body of the message</param>
        /// <param name="IncludeAttachments">Attachments to include in the message.</param>
        /// <returns>True if the message is sent. False if not.</returns>
        public bool SendMessage(string MessageSubject, string MessageBodyContent, bool IncludeAttachments = true)
        {
            // Log information about the startup of this new message object.
            this._serviceLogger.WriteLog($"PREPARING TO SEND OUT A NEW MESSAGE TO {this.EmailRecipientAddresses.Length} RECIPIENTS TITLED {MessageSubject}", LogType.WarnLog);
            this._serviceLogger.WriteLog("BODY CONTENT OBJECT IS BEING APPENDED INTO A MAILMESSAGE OBJECT NOW...", LogType.WarnLog);

            // Update the content of our message with a final output for log file entries and names.
            var OutputFilesTupleSet = this.MessageAttachmentFiles.Select(FileObj =>
            {
                // Build a new tuple object from the given file and return it.
                string FileName = FileObj.Name;
                string FileSizeFormatted = FileObj.Length.ToFileSize();
                string TimeLastModified = FileObj.LastWriteTime.ToString("f");
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

            // Log the output table object here and build out the mailmessage.
            MessageBodyContent += $"\n\n{string.Concat(Enumerable.Repeat("=", 75))}\n\n{OutputFileTable}";
            this._serviceLogger.WriteLog("BUILT NEW OUTPUT TABLE CORRECTLY! ENTIRE MESSAGE OBJECT AND OUTPUT TABLE IS LOGGED BELOW!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"\n{OutputFileTable}", LogType.TraceLog);

            // Build mail object and send it out.
            bool OverallStatus = true;
            foreach (var RecipientAddress in this.EmailRecipientAddresses)
            {
                // Build message, send it out, and move to the next one.
                this._serviceLogger.WriteLog($"SENDING REPORT TO {RecipientAddress.Address}", LogType.TraceLog);
                MailMessage OutputMessage = new MailMessage(
                    this.EmailSenderAddress.Address,    // Sender
                    RecipientAddress.Address,            // Recipient
                    MessageSubject,                         // Message subject
                    MessageBodyContent                      // Body content for message.
                );

                // File in the attachment objects now.
                if (!IncludeAttachments) this._serviceLogger.WriteLog("WARNING! ATTACHMENTS WERE NOT REQUESTED!", LogType.WarnLog); 
                else 
                {
                    // Iterate all attachments and store them on our new mail message
                    this._serviceLogger.WriteLog("ATTACHING FILES TO MESSAGE OBJECT NOW...", LogType.WarnLog);
                    foreach (var FileInstance in this.MessageAttachmentFiles)
                        OutputMessage.Attachments.Add(new Attachment(FileInstance.FullName));

                    // Log out how many attachments we've built here
                    this._serviceLogger.WriteLog("ATTACHMENT PROCESS PASSED WITHOUT ISSUES!", LogType.InfoLog);
                    this._serviceLogger.WriteLog($"MESSAGE OBJECT NOW CONTAINS A TOTAL OF {OutputMessage.Attachments.Count} ATTACHMENTS", LogType.TraceLog);
                }

                try
                {
                    // Ensure our SMTP server instance has been configured correctly.
                    if (!this.SmtpSetupConfigured)
                        throw new InvalidOperationException("Error! SMTP Client configuration is not setup!");

                    // Now fire it off using our SMTP Server instance.
                    this._serviceLogger.WriteLog($"SENDING OUTPUT MESSAGE TO RECIPIENT {RecipientAddress.Address} NOW...", LogType.WarnLog);
                    if (this._authorizeEmailClient()) this.SendingClient.Send(OutputMessage);
                    else throw new AuthenticationException("Error! Failed to authenticate an email client!");
                    this._serviceLogger.WriteLog($"SENDING ROUTINE PASSED FOR MESSAGE OUTPUT TO CLIENT {RecipientAddress.Address}!", LogType.InfoLog);

                    // Clear out existing attachments from the message here
                    this.MessageAttachmentFiles = Array.Empty<FileInfo>();
                }
                catch (Exception MailEx)
                {
                    // Log failures, set the overall output value to false.
                    OverallStatus = false;
                    this._serviceLogger.WriteLog($"FAILED TO INVOKE SENDING ROUTINE FOR MESSAGE TO BE SENT TO RECIPIENT {RecipientAddress.Address}!", LogType.ErrorLog);
                    this._serviceLogger.WriteException("EMAIL EXCEPTION IS BEING LOGGED BELOW.", MailEx);
                }
            }

            // Return passed sending
            return OverallStatus;
        }

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
                    Credentials = new NetworkCredential(EmailSenderAddress.Address, this._emailConfiguration.ReportSenderPassword),

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