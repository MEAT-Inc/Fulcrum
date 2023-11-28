using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using FulcrumEmailService;
using FulcrumInjector.FulcrumViewSupport;
using Google.Apis.Drive.v3.Data;
using SharpLogging;
using SharpPipes;

// Static using call for importing mail message objects
using FulcrumMessage = FulcrumEmailService.FulcrumEmail.FulcrumMessage;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// Session reporting ViewModel used for sending cleaned up session information out to my email
    /// Email is defined in the app settings.
    /// </summary>
    public class FulcrumSessionReportingViewModel : FulcrumViewModelBase
    {
        #region Custom Events

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

            // Try and attach it to our output report helper. Build our mail message first if needed here
            if (this.SessionMessage == null && this._sessionReportSender != null) this.SessionMessage = this._sessionReportSender.CreateFulcrumMessage();
            if (this.SessionMessage.MessageAttachments.Contains(NextSessionLog)) this.ViewModelLogger.WriteLog($"SKIPPING DUPLICATE ATTACHMENT {NextSessionLog}", LogType.WarnLog);
            else
            {
                // Add the file and log the path of it out here
                this.SessionMessage.MessageAttachments.Add(NextSessionLog);
                this.ViewModelLogger.WriteLog($"APPENDED ATTACHMENT FILE: {NextSessionLog} TO REPORT", LogType.TraceLog);
            }
        }

        #endregion // Custom Events

        #region Fields

        // Private backing fields for our public properties
        private bool _canModifyMessage = true;          // Sets if we're able to modify the text of our email or not 
        private bool _showEmailInfoText = true;         // Sets if we're showing the help information or not 
        private FulcrumMessage _sessionMessage;         // The message object we're sending for this session
        private List<FileInfo> _attachmentInfos;        // Collection of FileInfo objects for message attachments
        private FulcrumEmail _sessionReportSender;      // The sending service helper for sending emails

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool CanModifyMessage { get => _canModifyMessage; set => PropertyUpdated(value); }
        public bool ShowEmailInfoText { get => _showEmailInfoText; set => PropertyUpdated(value); }

        // Properties holding information about our report sender and the message being sent
        public FulcrumMessage SessionMessage { get => _sessionMessage; set => PropertyUpdated(value); }
        public List<FileInfo> AttachmentInfos { get => _attachmentInfos; set => PropertyUpdated(value); }

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

            // Store a new mail message object for this session here 
            this.SessionMessage = this._sessionReportSender.CreateFulcrumMessage();
            this.ViewModelLogger.WriteLog("BUILT NEW FULCRUM EMAIL MESSAGE OBJECT CORRECTLY!", LogType.InfoLog);

            // Log passed. Build in main log file and session logs if any.
            this.ViewModelLogger.WriteLog("EMAIL REPORT BROKER HAS BEEN BUILT OK AND BOUND TO OUR VIEW CONTENT!");
            this.ViewModelLogger.WriteLog("SETUP NEW VIEW MODEL FOR EMAIL BROKER VALUES OK!");
            this.ViewModelLogger.WriteLog("REPORT EMAIL BROKER IS NOW READY FOR USE AND REPORT SENDING!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls in the session logs from our DLL output view.
        /// </summary>
        /// <returns>Strings added</returns>
        public bool AppendSessionLogFiles()
        {
            // Log information, pull view from constants and get values.
            this.ViewModelLogger.WriteLog("PULLING IN DLL SESSION LOG FILE ENTRIES NOW...", LogType.InfoLog);
            var FilesLocated = FulcrumConstants.FulcrumDllOutputLogViewModel?.SessionLogs ?? new List<string>();

            // Store our session log file on the mail message here
            if (this.SessionMessage.MessageAttachments.Contains(SharpLogBroker.LogFilePath)) this.ViewModelLogger.WriteLog($"SKIPPING DUPLICATE FILE {SharpLogBroker.LogFilePath} SINCE IT WAS ALREADY ADDED!", LogType.WarnLog);
            else
            {
                // Add the file and log the path of it out here
                this.SessionMessage.MessageAttachments.Add(SharpLogBroker.LogFilePath);
                this.ViewModelLogger.WriteLog($"APPENDED ATTACHMENT FILE: {SharpLogBroker.LogFilePath} INTO SESSION ATTACHMENTS CORRECTLY!", LogType.InfoLog);
            }

            // Check how many files we pulled and return.
            this.ViewModelLogger.WriteLog(
                FilesLocated.Count == 0 ? 
                    "NO FILES WERE LOCATED ON THE DLL OUTPUT VIEW MODEL OBJECT!" : 
                    $"FOUND A TOTAL OF {FilesLocated.Count} SESSION LOG FILES!", 
                FilesLocated.Count == 0 ?
                    LogType.WarnLog :
                    LogType.InfoLog);

            // Append them into our list of reports and return.
            foreach (var MessageAttachment in FilesLocated)
            {
                // Make sure we don't have this attachment already
                if (this.SessionMessage.MessageAttachments.Contains(MessageAttachment)) this.ViewModelLogger.WriteLog($"SKIPPING DUPLICATE FILE {MessageAttachment}", LogType.WarnLog);
                else
                {
                    // Add the file and log the path of it out here
                    this.SessionMessage.MessageAttachments.Add(MessageAttachment);
                    this.ViewModelLogger.WriteLog($"APPENDED ATTACHMENT FILE: {MessageAttachment} INTO SESSION ATTACHMENTS CORRECTLY", LogType.TraceLog);
                }
            }

            // Build a new view model bound file collection for attachments
            this.AttachmentInfos = this.SessionMessage.MessageAttachments.Select(FileName => new FileInfo(FileName)).ToList();
            this.ViewModelLogger.WriteLog("BUILT NEW FILE INFORMATION OBJECTS FOR ATTACHED FILES CORRECTLY!", LogType.InfoLog);

            // Return information and return out.
            return FilesLocated.Count != 0;
        }
        /// <summary>
        /// Sends our session message out to the requested recipients
        /// </summary>
        /// <returns>True if our message is sent, false if it is not</returns>
        public bool SendSessionMessage()
        {
            // Try and send our message object here
            this.ViewModelLogger.WriteLog("SENDING FULCRUM MESSAGE OBJECT USING EMAIL SERVICE NOW...", LogType.WarnLog);
            try
            {
                // Send our message out and log completed here
                if (!this._sessionReportSender.SendFulcrumMessage(this.SessionMessage))
                    throw new InvalidOperationException("Error! Failed to send FulcrumMessage!");

                // Log out that we've sent our message without issues
                this.SessionMessage = this._sessionReportSender.CreateFulcrumMessage();
                this.ViewModelLogger.WriteLog("SENT MAIL MESSAGE WITHOUT ISSUES!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog("BUILT NEW EMAIL MESSAGE OBJECT FOR NEXT REPORT CORRECTLY!", LogType.InfoLog);
                return true; 
            }
            catch (Exception SendMessageEx)
            {
                // Log out our exception and return failed here
                this.ViewModelLogger.WriteLog("ERROR! FAILED TO SEND MAIL MESSAGE USING EMAIL SERVICE!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION THROWN DURING SEND ROUTINE IS BEING LOGGED BELOW", SendMessageEx);
                return false;
            }
        }
    }
}
