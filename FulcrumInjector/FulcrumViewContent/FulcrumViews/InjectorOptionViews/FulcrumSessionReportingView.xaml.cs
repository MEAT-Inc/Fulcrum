using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorOptionViewModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using SharpLogging;
using SharpPipes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorOptionViews
{
    /// <summary>
    /// Interaction logic for FulcrumSessionReportingView.xaml
    /// </summary>
    public partial class FulcrumSessionReportingView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        public FulcrumSessionReportingViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumSessionReportingView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = FulcrumConstants.FulcrumSessionReportingViewModel ?? new FulcrumSessionReportingViewModel(this);

            // Append new session log files.
            this.ViewModel.AppendSessionLogFiles();
            this._viewLogger.WriteLog("STORED SESSION LOG FILES INTO EMAIL ATTACHMENTS OK!", LogType.InfoLog);

            // Build event for our pipe objects to process new pipe content into our output box
            PassThruPipeReader ReaderPipe = PassThruPipeReader.AllocatePipe();
            ReaderPipe.PipeDataProcessed += this.ViewModel.OnPipeReaderContentProcessed;
            this._viewLogger.WriteLog("STORED NEW EVENT HELPER FOR PROCESSING LOG CONTENTS ON PIPE DATA OUTPUT!", LogType.InfoLog);

            // Initialize new UI Component
            InitializeComponent();
            
            // Setup our data context and log information out
            // this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR THE SESSION REPORTING VIEW OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumSessionReportingView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Force show help menu and build email temp text
            if (this.EmailBodyTextContent.Text.Length == 0)
            {
                this.EmailBodyTextContent.Text = ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorEmailConfiguration.DefaultEmailBodyText");
                this._viewLogger.WriteLog("STORED DEFAULT EMAIL TEXT INTO THE VIEW OBJECT CORRECTLY!", LogType.InfoLog);
            }

            // Log done building new ViewModel.
            if (this.ViewModel.ShowEmailInfoText) this.ToggleEmailPaneInfoButton_OnClick(null, null);
            this.ReportAttachmentFiles.ItemsSource = this.ViewModel.SessionReportSender.MessageAttachmentFiles;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR EMAIL REPORTING OUTPUT OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Shows or hides the email information on the view object. 
        /// </summary>
        /// <param name="SendingButton"></param>
        /// <param name="EventArgs"></param>
        private void ToggleEmailPaneInfoButton_OnClick(object SendingButton, RoutedEventArgs EventArgs)
        {
            // Start by logging button was clicked then flipping the value around.
            this._viewLogger.WriteLog("PROCESSED A BUTTON CLICK TO TOGGLE VISIBILITY OF OUR EMAIL PANE HELP TEXT", LogType.WarnLog);
            this._viewLogger.WriteLog($"CURRENTLY SET INFORMATION VISIBILITY STATE IS {this.ViewModel.ShowEmailInfoText}", LogType.TraceLog);

            // Log and update information
            this.ViewModel.ShowEmailInfoText = !this.ViewModel.ShowEmailInfoText;
            this._viewLogger.WriteLog("UPDATED VIEW CONTENT VALUES CORRECTLY! GRIDS SHOULD HAVE RESIDED AS EXPECTED", LogType.InfoLog);
            this._viewLogger.WriteLog($"NEWLY SET INFORMATION VISIBILITY STATE IS {this.ViewModel.ShowEmailInfoText}", LogType.TraceLog);
        }
        /// <summary>
        /// Send email button for the report sender
        /// </summary>
        /// <param name="SendButton"></param>
        /// <param name="SendButtonArgs"></param>
        private async void SendEmailButton_OnClick(object SendButton, RoutedEventArgs SendButtonArgs)
        {
            // Log building new email object.
            this._viewLogger.WriteLog("BUILDING NEW EMAIL OBJECT TO SEND OUT FOR OUR REPORT GENERATION NOW...", LogType.WarnLog);

            // Get our subject line, the body content, and then pass it over to our sender on the view model.
            string SendingSubject = this.EmailSubjectText.Text;
            if (SendingSubject.Length == 0) SendingSubject = $"Session Report - {DateTime.Now.ToString("F")}";
            else { SendingSubject += $" (Session Report - {DateTime.Now.ToString("F")})"; }
            this._viewLogger.WriteLog($"REPORT SESSION SUBJECT: {SendingSubject}", LogType.InfoLog);
            this._viewLogger.WriteLog("STORED NEW SUBJECT BACK INTO OUR VIEW OBJECT!", LogType.InfoLog);

            // Now get the body contents and pass them into our VM for processing and sending.
            Button SendingButton = (Button)SendButton;
            Brush SendingDefaultColor = SendingButton.Background;
            string BodyContents = this.EmailBodyTextContent.Text;
            this._viewLogger.WriteLog($"BODY CONTENT OF SENDING OBJECT IS SEEN AS: {BodyContents}", LogType.TraceLog);
            this._viewLogger.WriteLog("SENDING EMAIL OBJECT TO VIEW MODEL FOR FINAL PROCESS AND SEND ROUTINE!", LogType.InfoLog);

            // Set Can modify to false to turn off controls.
            bool SendPassed = await Task.Run(() =>
            {
                // Toggle Can Modify
                this.ViewModel.CanModifyMessage = false;

                // Toggle buttons and textbox use and run the send routine
                Dispatcher.Invoke(() => { 
                    SendingButton.IsEnabled = false;
                    SendingButton.Content = "Sending...";
                    SendingButton.Background = Brushes.DarkOrange;
                });

                // Rend out the message request here.
                var SendTime = new Stopwatch(); SendTime.Start();
                bool SendResult = this.ViewModel.SessionReportSender.SendMessage(SendingSubject, BodyContents);
                this._viewLogger.WriteLog($"SENDING ROUTINE HAS COMPLETED! SEND ROUTINE TOOK {SendTime.Elapsed.ToString("g")} TO SEND MESSAGES", LogType.InfoLog);
                this._viewLogger.WriteLog($"RESULT FROM SEND ROUTINE WAS: {SendResult}", LogType.WarnLog);

                // Turn can modify back on.
                this.ViewModel.CanModifyMessage = true;
                return SendResult;
            });

            // Now set the send button based on the result.
            SendingButton.IsEnabled = true;
            SendingButton.Click -= SendEmailButton_OnClick;
            SendingButton.Content = SendPassed ? "Sent!" : "Failed!";
            SendingButton.Background = SendPassed ? Brushes.DarkGreen : Brushes.DarkRed;
            Task.Run(() =>
            {
                // Now wait 3 seconds and reset our colors to default values.
                Thread.Sleep(3000);
                Dispatcher.Invoke(() =>
                {
                    SendingButton.Content = "Send";
                    SendingButton.Background = SendingDefaultColor;
                    SendingButton.Click += SendEmailButton_OnClick;
                    this._viewLogger.WriteLog("RESET SENDING BUTTON CONTENT VALUES OK!", LogType.InfoLog);
                });
            });
        }
        /// <summary>
        /// Reacts to a new button click for adding an email entry into our list
        /// </summary>
        /// <param name="SendingTextBox">Sending button</param>
        /// <param name="TextChangedArgs">Changed text arguments</param>
        private void AddressListTextBox_OnChanged(object SendingTextBox, TextChangedEventArgs TextChangedArgs)
        {
            // Get text of TextBox object and try to add address.
            var BoxObject = (TextBox)SendingTextBox;
            string NewTextContent = BoxObject.Text;

            // Check if there's even an email to parse out. If none, remove all.
            if (NewTextContent.Length == 0) { this.ViewModel.SessionReportSender.RemoveRecipient(); }
            Regex SendingRegex = new Regex(@"([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)");
            var MatchedEmails = SendingRegex.Matches(NewTextContent);
            if (MatchedEmails.Count == 0) {
                this.SendMessageButton.IsEnabled = false;
                return;
            }

            // Clear out all current address values and then add them back in one at a time.
            this.ViewModel.SessionReportSender.RemoveRecipient();
            foreach (Match AddressMatch in MatchedEmails) { this.ViewModel.SessionReportSender.AddRecipient(AddressMatch.Value); }
            string NewAddressString = string.Join(",", this.ViewModel.SessionReportSender.EmailRecipientAddresses.Select(MailAddress => MailAddress.Address));

            // Now remove address values that don't fly here.
            this._viewLogger.WriteLog($"CURRENT EMAILS: {NewAddressString}", LogType.TraceLog);
            this._viewLogger.WriteLog("UPDATED EMAIL ENTRY TEXTBOX CONTENTS TO REFLECT ONLY VALID EMAILS!", LogType.InfoLog);
            this.SendMessageButton.IsEnabled = this.ViewModel.SessionReportSender.EmailRecipientAddresses.Length != 0;
        }
        /// <summary>
        /// Attaches a new file entry into our list of files by showing a file selection dialogue
        /// </summary>
        /// <param name="AttachFileButton"></param>
        /// <param name="AttachFileEventArgs"></param>
        private void AddReportAttachmentButton_OnClick(object AttachFileButton, RoutedEventArgs AttachFileEventArgs)
        {
            // Log information about opening appending box and begin selection
            this._viewLogger.WriteLog("OPENING NEW FILE SELECTION DIALOGUE FOR APPENDING OUTPUT FILES NOW...", LogType.InfoLog);
            using var SelectAttachmentDialog = new System.Windows.Forms.OpenFileDialog()
            {
                Multiselect = true,
                CheckFileExists = true,
                CheckPathExists = true,
                RestoreDirectory = true,
                AutoUpgradeEnabled = true,
                InitialDirectory = ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorLogging.DefaultLoggingPath")
            };

            // Now open the dialog and allow the user to pick some new files.
            this._viewLogger.WriteLog("OPENING NEW DIALOG OBJECT NOW...", LogType.WarnLog);
            if (SelectAttachmentDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK || SelectAttachmentDialog.FileNames.Length == 0) {
                this._viewLogger.WriteLog("FAILED TO SELECT A NEW FILE OBJECT! EXITING NOW...", LogType.ErrorLog);
                this.ReportAttachmentFiles.ItemsSource = this.ViewModel.SessionReportSender.MessageAttachmentFiles;
                return;
            }

            // Now pull our file objects out and store them on our viewModel
            foreach (var FilePath in SelectAttachmentDialog.FileNames) {
                this._viewLogger.WriteLog($"ATTACHING FILE OBJECT: {FilePath}", LogType.TraceLog);
                this.ViewModel.SessionReportSender.AddMessageAttachment(FilePath);
            }

            // Store content for the listbox using the VM Email broker and log complete.
            this.ReportAttachmentFiles.ItemsSource = this.ViewModel.SessionReportSender.MessageAttachmentFiles;
            this._viewLogger.WriteLog("DONE APPENDING NEW FILE INSTANCES. ATTACHMENTS LISTBOX SHOULD BE UPDATED WITH NEW INFORMATION NOW", LogType.InfoLog);
        }
        /// <summary>
        /// Event handler for when a button is clicked to remove the selected attachment.
        /// </summary>
        /// <param name="RemoveFileButton"></param>
        /// <param name="RemoveFileButtonEventArgs"></param>
        private void RemoveAttachmentButton_OnClick(object RemoveFileButton, RoutedEventArgs RemoveFileButtonEventArgs)
        {
            // Get the index of the sending button and then find out what string content goes along with it.
            if (!this.ViewModel.CanModifyMessage) return;
            this._viewLogger.WriteLog("TRYING TO FIND PARENT GRID AND SENDING TEXT NAME NOW...", LogType.WarnLog);
            try
            {
                // Find selected index and file name. If failed, throw a null ref exception
                var SendButton = (Button)RemoveFileButton; Grid ParentGrid = SendButton.Parent as Grid;
                TextBlock FileNameBlock = ParentGrid?.Children.OfType<TextBlock>().FirstOrDefault();
                if (FileNameBlock == null) throw new NullReferenceException("FAILED TO FIND FILE BLOCK OBJECT FOR NAME TO REMOVE!");

                // Find possible index for file object.
                this._viewLogger.WriteLog($"FILE OBJECT TEXT WAS FOUND AND PARSED! LOOKING FOR FILE {FileNameBlock.Text}", LogType.InfoLog);
                int FileIndex = this.ViewModel.SessionReportSender.MessageAttachmentFiles
                    .ToList()
                    .FindIndex(FileObj => string.Equals(FileObj.Name, FileNameBlock.Text, StringComparison.CurrentCultureIgnoreCase));

                // Now ensure the index is valid
                if (FileIndex > this.ViewModel.SessionReportSender.MessageAttachmentFiles.Length || FileIndex < 0) {
                    this._viewLogger.WriteLog("INDEX WAS OUT OF RANGE FOR INPUT TEXT VALUE! THIS IS FATAL!", LogType.ErrorLog);
                    throw new IndexOutOfRangeException("INDEX FOR SELECTED OBJECT IS OUTSIDE BOUNDS OF VIEWMODEL SENDING BROKER!");
                }

                // Store string for name of the file.
                string FileNameToRemove = this.ViewModel.SessionReportSender.MessageAttachmentFiles[FileIndex].FullName;
                this._viewLogger.WriteLog($"PULLED INDEX VALUE OK! TRYING TO REMOVE FILE NAMED {FileNameToRemove}", LogType.InfoLog);

                // Store modified view binding list into the view model for updating.
                this.ViewModel.SessionReportSender.RemoveMessageAttachment(FileNameToRemove);
                this.ReportAttachmentFiles.ItemsSource = this.ViewModel.SessionReportSender.MessageAttachmentFiles;
                this._viewLogger.WriteLog("REQUEST FOR REMOVAL PROCESSED AND PASSED OK! RETURNING NOW", LogType.InfoLog);
            }
            catch (Exception Ex)
            {
                // Log failures and return out. Set the content for the listbox to the VM
                this.ReportAttachmentFiles.ItemsSource = this.ViewModel.SessionReportSender.MessageAttachmentFiles;
                this._viewLogger.WriteLog("FAILED TO REMOVE FILE DUE TO AN EXCEPTION WHILE TRYING TO FIND FILE NAME OR VALUE!", LogType.ErrorLog);
                this._viewLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW.", Ex);
            }
        }
    }
}
