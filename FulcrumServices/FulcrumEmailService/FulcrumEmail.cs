using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
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

        // Private logger instance and backing fields for our email recipients
        private readonly SharpLogger _emailLogger;
        private static FulcrumEmail _serviceInstance;             // Static service instance object
        private MailAddress[] _emailRecipientAddresses;

        #endregion //Fields

        #region Properties

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
                    this._emailLogger.WriteLog("WARING! NO EMAILS ENTERED YET! RETURNING ONLY OUR DEFAULT MAIL RECIPIENT!", LogType.WarnLog);
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

        // Public facing configuration properties for our broker
        public EmailBrokerConfiguration EmailConfiguration { get; private set; }
        public EmailSmtpConfiguration EmailStmpConfiguration { get; private set; }

        // Computed properties based on CTOR arguments
        public bool EmialBrokerConfigured => this.EmailConfiguration != null;
        public string EmailSenderName => this.EmailConfiguration.ReportSenderName;
        public string EmailSenderPassword => this.EmailConfiguration.ReportSenderPassword;
        public MailAddress EmailSenderAddress => new(this.EmailConfiguration.ReportSenderEmail);
        public MailAddress DefaultRecipientAddress => new(this.EmailConfiguration.DefaultReportRecipient);

        // Computed properties based on the SMTP configuration
        public bool SmtpSetupConfigured => this.EmailStmpConfiguration != null;
        public int SmtpServerPort => this.EmailStmpConfiguration.ServerPort;
        public int SmtpServerTimeout => this.EmailStmpConfiguration.ServerTimeout;
        public string SmtpServerName => this.EmailStmpConfiguration.ServerName;

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private CTOR for building a new singleton of our email broker client
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when authentication of the SMTP client fails</exception>
        internal FulcrumEmail()
        {
            // Configure a new logger for our EmailBroker 
            this._emailLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Pull in new settings values for sender and default receivers.
            string ConfigKeyBase = "FulcrumConstants.InjectorEmailConfiguration";
            this._emailLogger.WriteLog("PULLING IN NEW CONFIGURATION VALUES FOR EMAIL BROKER NOW...", LogType.InfoLog);
            this.EmailConfiguration = ValueLoaders.GetConfigValue<EmailBrokerConfiguration>($"{ConfigKeyBase}.SenderConfiguration");

            // Now try and authorize the client for a google address.
            this._emailLogger.WriteLog("PULLING IN SMTP CONFIG VALUES AND AUTHORIZING CLIENT FOR USE NOW...", LogType.WarnLog);
            this.EmailStmpConfiguration = ValueLoaders.GetConfigValue<EmailSmtpConfiguration>($"{ConfigKeyBase}.SmtpServerSettings");

            try
            {
                // First build a new SMTP Client.
                this._emailLogger.WriteLog("ATTEMPTING TO CONNECT TO OUR SMTP SERVER NOW...", LogType.WarnLog);
                this.SendingClient = new SmtpClient
                {
                    // Setup name and port values and timeout value.
                    Host = this.SmtpServerName,
                    Port = this.SmtpServerPort,
                    Timeout = this.SmtpServerTimeout,

                    // SSL Configuration
                    EnableSsl = true,
                    Credentials = new NetworkCredential(EmailSenderAddress.Address, this.EmailSenderPassword),

                    // BUG: This is causing some type of auth issues. Removing for testing.
                    // DeliveryMethod = SmtpDeliveryMethod.Network
                };

                // Return passed and the built client.
                this._emailLogger.WriteLog("BUILT NEW SMTP CLIENT OBJECT OK! RETURNING IT NOW", LogType.InfoLog);
            }
            catch (Exception SetupEx)
            {
                // Log the failure output
                this._emailLogger.WriteLog("FAILED TO CONFIGURE OUR NEW SMTP CLIENT! THIS IS A SERIOUS ISSUE!", LogType.ErrorLog);
                this._emailLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW", SetupEx);
                throw SetupEx;
            }

            // Log information about passed output here.
            this._emailLogger.WriteLog($"EMAILS WILL BE SENT FROM USER {this.EmailSenderName} ({this.EmailSenderAddress}) WHEN USING THIS BROKER INSTANCE", LogType.InfoLog);
            this._emailLogger.WriteLog($"OUR DEFAULT RECIPIENT WILL BE SEEN AS {this.DefaultRecipientAddress} FOR OUTPUT REPORTS", LogType.TraceLog);
        }
        /// <summary>
        /// Static CTOR for an email broker instance. Simply pulls out the new singleton instance for our email broker
        /// </summary>
        /// <param name="ForceInit">When true, we force rebuild the requested service instance</param>
        /// <returns>The instance for our broker singleton</returns>
        public static FulcrumEmail InitializeEmailService(bool ForceInit = false)
        {
            // Build a static init logger for the service here
            SharpLogger ServiceInitLogger =
                SharpLogBroker.FindLoggers("ServiceInitLogger").FirstOrDefault()
                ?? new SharpLogger(LoggerActions.UniversalLogger, "ServiceInitLogger");

            // Spin up a new injector drive service here if needed           
            Task.Run(() =>
            {
                // Check if we need to force rebuilt this service or not
                if (_serviceInstance != null && !ForceInit) return;

                // Build and boot a new service instance for our watchdog
                _serviceInstance = new FulcrumEmail();
                _serviceInstance.OnStart(null);
            });

            // Log that we've booted this new service instance correctly and exit out
            ServiceInitLogger.WriteLog("SPAWNED NEW INJECTOR EMAIL SERVICE OK! BOOTING IT NOW...", LogType.WarnLog);
            ServiceInitLogger.WriteLog("BOOTED NEW INJECTOR EMAIL SERVICE OK!", LogType.InfoLog);

            // Return the built service instance 
            return _serviceInstance;
        }

        // --------------------------------------------------------------------------------------------------------------------------

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
                    this._emailLogger.WriteLog("ADDRESS WAS ALREADY IN OUR LIST OF RECIPIENTS! RETURNING FALSE", LogType.WarnLog);
                    return false;
                }

                // Add address here since it does not currently exist.
                this.EmailRecipientAddresses = this.EmailRecipientAddresses.Append(TempAddress).ToArray();
                this._emailLogger.WriteLog("NEW EMAIL PASSED IS A VALID UNIQUE EMAIL! ADDING TO OUR LIST NOW", LogType.InfoLog);
                return true;
            }
            catch
            {
                // If it failed, then return false.
                this._emailLogger.WriteLog("EMAIL PROVIDED WAS NOT A VALID EMAIL! RETURNING FALSE", LogType.WarnLog);
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
                this._emailLogger.WriteLog("REMOVING ALL RECIPIENTS FROM LIST OF ENTRIES NOW...", LogType.WarnLog);
                this.EmailRecipientAddresses = Array.Empty<MailAddress>();
                return true;
            }

            // Remove only the given name.
            if (this.EmailRecipientAddresses.All(EmailObj => !string.Equals(EmailObj.ToString(), RecipientAddress, StringComparison.CurrentCultureIgnoreCase)))
            {
                this._emailLogger.WriteLog($"NO EMAIL ADDRESS WITH THE VALUE {RecipientAddress} WAS FOUND!", LogType.WarnLog);
                return false;
            }

            // Remove value here and return.
            var StringAddresses = this.EmailRecipientAddresses.Select(EmailObj => EmailObj.ToString().ToUpper());
            if (!StringAddresses.ToList().Remove(RecipientAddress.ToUpper()))
            {
                this._emailLogger.WriteLog($"FAILED TO REMOVE REQUESED ADDRESS OF {RecipientAddress}!", LogType.WarnLog);
                return false;
            }

            // Now reset email list contents here.
            this.EmailRecipientAddresses = StringAddresses.Select(StringAddr => new MailAddress(StringAddr)).ToArray();
            this._emailLogger.WriteLog($"REMOVED ADDRESS NAME {RecipientAddress} CORRECTLY! STORING NEW ADDRESS SET NOW...", LogType.InfoLog);
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
            this._emailLogger.WriteLog($"ATTACHING FILE {PathToAttachment}", LogType.InfoLog);
            if (!File.Exists(PathToAttachment))
            {
                this._emailLogger.WriteLog("FILE OBJECT DOES NOT EXIST! CAN NOT ADD IT INTO OUR ATTACHMENT LIST!", LogType.ErrorLog);
                return false;
            }

            // Find existing if possible.
            if (MessageAttachmentFiles.Any(FileObj => FileObj.Name == Path.GetFileName(PathToAttachment)))
                return false;

            // Log total file count now and add into our list.
            this._emailLogger.WriteLog("APPENDING NEW FILE OBJECT INTO OUR LIST OF ATTACHMENTS NOW...", LogType.InfoLog);
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
                this._emailLogger.WriteLog("NO NAME FILTER WAS PROVIDED! REMOVING ALL ENTRIES FROM OUR MAIL LIST NOW...");
                this.MessageAttachmentFiles = Array.Empty<FileInfo>();
                return true;
            }

            // Check if the list exists and log file removing
            this.MessageAttachmentFiles ??= Array.Empty<FileInfo>();
            this._emailLogger.WriteLog($"REMOVING FILE NAME {NameToRemove}", LogType.InfoLog);
            if (UseFilter) this._emailLogger.WriteLog("WARNING: REGEX FILTERING WAS TURNED ON! USING IT NOW", LogType.WarnLog);

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
                this._emailLogger.WriteLog("NO FILES FOUND TO REMOVE! RETURNING FAILED NOW...", LogType.ErrorLog);
                return false;
            }

            // Log how many files to remove and pull them all out.
            this._emailLogger.WriteLog($"FILES TO PULL OUT OF THE LIST: {FilesToRemove.Count()}", LogType.InfoLog);
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
            this._emailLogger.WriteLog($"PREPARING TO SEND OUT A NEW MESSAGE TO {this.EmailRecipientAddresses.Length} RECIPIENTS TITLED {MessageSubject}", LogType.WarnLog);
            this._emailLogger.WriteLog("BODY CONTENT OBJECT IS BEING APPENDED INTO A MAILMESSAGE OBJECT NOW...", LogType.WarnLog);

            // Update the content of our message with a final output for log file entries and names.
            var OutputFilesTupleSet = this.MessageAttachmentFiles.Select(FileObj =>
            {
                // Build a new tuple object from the given file and return it.
                string FileName = FileObj.Name;
                string FileSizeFormatted = FileObj.Length.ToFileSize();
                string TimeLastModified = FileObj.LastWriteTime.ToString("f");
                return new Tuple<string, string, string>(FileName, FileSizeFormatted, TimeLastModified);
            }).ToArray();
            this._emailLogger.WriteLog("BUILT NEW TUPLE ARRAY FOR ALL FILE OBJECTS IN USE CORRECTLY!", LogType.InfoLog);

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
            this._emailLogger.WriteLog("BUILT NEW OUTPUT TABLE CORRECTLY! ENTIRE MESSAGE OBJECT AND OUTPUT TABLE IS LOGGED BELOW!", LogType.InfoLog);
            this._emailLogger.WriteLog($"\n{OutputFileTable}", LogType.TraceLog);

            // Build mail object and send it out.
            bool OverallStatus = true;
            foreach (var RecipientAddress in this.EmailRecipientAddresses)
            {
                // Build message, send it out, and move to the next one.
                this._emailLogger.WriteLog($"SENDING REPORT TO {RecipientAddress.Address}", LogType.TraceLog);
                MailMessage OutputMessage = new MailMessage(
                    this.EmailSenderAddress.Address,    // Sender
                    RecipientAddress.Address,            // Recipient
                    MessageSubject,                         // Message subject
                    MessageBodyContent                      // Body content for message.
                );

                // File in the attachment objects now.
                this._emailLogger.WriteLog("ATTACHING FILES TO MESSAGE OBJECT NOW...", LogType.WarnLog);
                foreach (var FileInstance in this.MessageAttachmentFiles)
                    OutputMessage.Attachments.Add(new Attachment(FileInstance.FullName));
                this._emailLogger.WriteLog("ATTACHMENT PROCESS PASSED WITHOUT ISSUES!", LogType.InfoLog);
                this._emailLogger.WriteLog($"MESSAGE OBJECT NOW CONTAINS A TOTAL OF {OutputMessage.Attachments.Count} ATTACHMENTS", LogType.TraceLog);

                try
                {
                    // Ensure our SMTP server instance has been configured correctly.
                    if (!this.SmtpSetupConfigured)
                        throw new InvalidOperationException("Error! SMTP Client configuration is not setup!");

                    // Now fire it off using our SMTP Server instance.
                    this._emailLogger.WriteLog($"SENDING OUTPUT MESSAGE TO RECIPIENT {RecipientAddress.Address} NOW...", LogType.WarnLog);
                    this.SendingClient.Send(OutputMessage);
                    this._emailLogger.WriteLog($"SENDING ROUTINE PASSED FOR MESSAGE OUTPUT TO CLIENT {RecipientAddress.Address}!", LogType.InfoLog);
                }
                catch (Exception MailEx)
                {
                    // Log failures, set the overall output value to false.
                    OverallStatus = false;
                    this._emailLogger.WriteLog($"FAILED TO INVOKE SENDING ROUTINE FOR MESSAGE TO BE SENT TO RECIPIENT {RecipientAddress.Address}!", LogType.ErrorLog);
                    this._emailLogger.WriteException("EMAIL EXCEPTION IS BEING LOGGED BELOW.", MailEx);
                }
            }

            // Return passed sending
            return OverallStatus;
        }
    }
}
