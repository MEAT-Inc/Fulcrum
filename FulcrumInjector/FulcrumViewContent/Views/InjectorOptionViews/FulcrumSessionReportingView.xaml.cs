using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels;
using ICSharpCode.AvalonEdit;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorOptionViews
{
    /// <summary>
    /// Interaction logic for FulcrumSessionReportingView.xaml
    /// </summary>
    public partial class FulcrumSessionReportingView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("FulcrumSessionReportingViewLogger")) ?? new SubServiceLogger("FulcrumSessionReportingViewLogger");

        // ViewModel object to bind onto
        public FulcrumSessionReportingViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumSessionReportingView()
        {
            // Build new ViewModel object
            InitializeComponent();
            this.ViewModel = InjectorConstants.FulcrumSessionReportingViewModel ?? new FulcrumSessionReportingViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumSessionReportingView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Force show help menu and build email temp text
            this.EmailBodyTextContent.Text = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorEmailConfiguration.DefaultEmailBodyText");
            this.ViewLogger.WriteLog("STORED DEFAULT EMAIL TEXT INTO THE VIEW OBJECT CORRECTLY!", LogType.InfoLog);

            // Log done building new ViewModel.
            this.ToggleEmailPaneInfoButton_OnClick(null, null);
            this.ReportAttachmentFiles.ItemsSource = this.ViewModel.SessionReportSender.MessageAttachmentFiles;
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR EMAIL REPORTING OUTPUT OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Shows or hides the email information on the view object. 
        /// </summary>
        /// <param name="SendingButton"></param>
        /// <param name="EventArgs"></param>
        private void ToggleEmailPaneInfoButton_OnClick(object SendingButton, RoutedEventArgs EventArgs)
        {
            // Start by logging button was clicked then flipping the value around.
            this.ViewLogger.WriteLog("PROCESSED A BUTTON CLICK TO TOGGLE VISIBILITY OF OUR EMAIL PANE HELP TEXT", LogType.WarnLog);
            this.ViewLogger.WriteLog($"CURRENTLY SET INFORMATION VISIBILITY STATE IS {this.ViewModel.ShowEmailInfoText}", LogType.TraceLog);

            // Log and update information
            this.ViewModel.ShowEmailInfoText = !this.ViewModel.ShowEmailInfoText;
            this.ViewLogger.WriteLog("UPDATED VIEW CONTENT VALUES CORRECTLY! GRIDS SHOULD HAVE RESIDED AS EXPECTED", LogType.InfoLog);
            this.ViewLogger.WriteLog($"NEWLY SET INFORMATION VISIBILITY STATE IS {this.ViewModel.ShowEmailInfoText}", LogType.TraceLog);
        }


        /// <summary>
        /// Send email button for the report sender
        /// </summary>
        /// <param name="SendButton"></param>
        /// <param name="SendButtonArgs"></param>
        private async void SendEmailButton_OnClick(object SendButton, RoutedEventArgs SendButtonArgs)
        {
            // Log building new email object.
            this.ViewLogger.WriteLog("BUILDING NEW EMAIL OBJECT TO SEND OUT FOR OUR REPORT GENERATION NOW...", LogType.WarnLog);

            // Get our subject line, the body content, and then pass it over to our sender on the view model.
            string SendingSubject = this.EmailSubjectText.Text;
            if (SendingSubject.Length == 0) SendingSubject = $"Session Report - {DateTime.Now.ToString("F")}";
            else { SendingSubject = SendingSubject + $" (Session Report - {DateTime.Now.ToString("F")})"; }
            this.ViewLogger.WriteLog($"REPORT SESSION SUBJECT: {SendingSubject}", LogType.InfoLog);
            this.ViewLogger.WriteLog("STORED NEW SUBJECT BACK INTO OUR VIEW OBJECT!", LogType.InfoLog);
            this.EmailSubjectText.Text = SendingSubject;

            // Now get the body contents and pass them into our VM for processing and sending.
            Button SendingButton = (Button)SendButton;
            string BodyContents = this.EmailBodyTextContent.Text;
            this.ViewLogger.WriteLog($"BODY CONTENT OF SENDING OBJECT IS SEEN AS: {BodyContents}", LogType.TraceLog);
            this.ViewLogger.WriteLog("SENDING EMAIL OBJECT TO VIEW MODEL FOR FINAL PROCESS AND SEND ROUTINE!", LogType.InfoLog);
            await Task.Run(() =>
            {
                // Toggle buttons and textbox use and run the send routine
                Dispatcher.Invoke(() => {
                    SendingButton.IsEnabled = false;
                    this.EmailSubjectText.IsEnabled = false;
                    this.EmailBodyTextContent.IsEnabled = false;
                });

                // Rend out the message request here.
                var SendTime = new Stopwatch(); SendTime.Start();
                this.ViewModel.SessionReportSender.SendReportMessage(SendingSubject, BodyContents);
                this.ViewLogger.WriteLog($"SENDING ROUTINE HAS COMPLETED! SEND ROUTINE TOOK {SendTime.Elapsed.ToString("g")} TO SEND MESSAGES", LogType.InfoLog);

                // Turn everything back on.
                Dispatcher.Invoke(() => {
                    SendingButton.IsEnabled = true;
                    this.EmailSubjectText.IsEnabled = true;
                    this.EmailBodyTextContent.IsEnabled = true;
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
            Regex SendingRegex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            var MatchedEmails = SendingRegex.Matches(NewTextContent);
            if (MatchedEmails.Count == 0) return;

            // Clear out all current address values and then add them back in one at a time.
            this.ViewModel.SessionReportSender.RemoveRecipient();
            foreach (Match AddressMatch in MatchedEmails) { this.ViewModel.SessionReportSender.AddRecipient(AddressMatch.Value); }
            string NewAddressString = string.Join(",", this.ViewModel.SessionReportSender.EmailRecipientAddresses.Select(MailAddress => MailAddress.Address));

            // Now remove address values that don't fly here.
            BoxObject.Text = NewAddressString;
            this.ViewLogger.WriteLog($"CURRENT EMAILS: {NewAddressString}", LogType.TraceLog);
            this.ViewLogger.WriteLog("UPDATED EMAIL ENTRY TEXTBOX CONTENTS TO REFLECT ONLY VALID EMAILS!", LogType.InfoLog);
        }
        /// <summary>
        /// Attaches a new file entry into our list of files by showing a file selection dialogue
        /// </summary>
        /// <param name="AttachFileButton"></param>
        /// <param name="AttachFileEventArgs"></param>
        private void AddReportAttachmentButton_OnClick(object AttachFileButton, RoutedEventArgs AttachFileEventArgs)
        {
            // Log information about opening appending box and begin selection
            this.ViewLogger.WriteLog("OPENING NEW FILE SELECTION DIALOGUE FOR APPENDING OUTPUT FILES NOW...", LogType.InfoLog);
            using OpenFileDialog SelectAttachmentDialog = new OpenFileDialog()
            {
                Multiselect = true,
                CheckFileExists = true,
                CheckPathExists = true,
                RestoreDirectory = true,
                AutoUpgradeEnabled = true,
                InitialDirectory = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.FulcrumInjectorLogging.DefaultLoggingPath")
            };

            // Now open the dialog and allow the user to pick some new files.
            this.ViewLogger.WriteLog("OPENING NEW DIALOG OBJECT NOW...", LogType.WarnLog);
            if (SelectAttachmentDialog.ShowDialog() != DialogResult.OK || SelectAttachmentDialog.FileNames.Length == 0) {
                this.ViewLogger.WriteLog("FAILED TO SELECT A NEW FILE OBJECT! EXITING NOW...", LogType.ErrorLog);
                this.ReportAttachmentFiles.ItemsSource = this.ViewModel.SessionReportSender.MessageAttachmentFiles;
                return;
            }

            // Now pull our file objects out and store them on our viewModel
            foreach (var FilePath in SelectAttachmentDialog.FileNames) {
                this.ViewLogger.WriteLog($"ATTACHING FILE OBJECT: {FilePath}", LogType.TraceLog);
                this.ViewModel.SessionReportSender.AddMessageAttachment(FilePath);
            }

            // Store content for the listbox using the VM Email broker and log complete.
            this.ReportAttachmentFiles.ItemsSource = this.ViewModel.SessionReportSender.MessageAttachmentFiles;
            this.ViewLogger.WriteLog("DONE APPENDING NEW FILE INSTANCES. ATTACHMENTS LISTBOX SHOULD BE UPDATED WITH NEW INFORMATION NOW", LogType.InfoLog);
        }
        /// <summary>
        /// Event handler for when a button is clicked to remove the selected attachment.
        /// </summary>
        /// <param name="RemoveFileButton"></param>
        /// <param name="RemoveFileButtonEventArgs"></param>
        private void RemoveAttachmentButton_OnClick(object RemoveFileButton, RoutedEventArgs RemoveFileButtonEventArgs)
        {
            // Get the index of the sending button and then find out what string content goes along with it.
            this.ViewLogger.WriteLog("TRYING TO FIND PARENT GRID AND SENDING TEXT NAME NOW...", LogType.WarnLog);
            try
            {
                // Find selected index and file name. If failed, throw a null ref exception
                var SendButton = (Button)RemoveFileButton; Grid ParentGrid = SendButton.Parent as Grid;
                TextBlock FileNameBlock = ParentGrid?.Children.OfType<TextBlock>().FirstOrDefault();
                if (FileNameBlock == null) throw new NullReferenceException("FAILED TO FIND FILE BLOCK OBJECT FOR NAME TO REMOVE!");

                // Find possible index for file object.
                this.ViewLogger.WriteLog($"FILE OBJECT TEXT WAS FOUND AND PARSED! LOOKING FOR FILE {FileNameBlock.Text}", LogType.InfoLog);
                int FileIndex = this.ViewModel.SessionReportSender.MessageAttachmentFiles
                    .ToList()
                    .FindIndex(FileObj => string.Equals(FileObj.Name, FileNameBlock.Text, StringComparison.CurrentCultureIgnoreCase));

                // Now ensure the index is valid
                if (FileIndex > this.ViewModel.SessionReportSender.MessageAttachmentFiles.Length || FileIndex < 0) {
                    this.ViewLogger.WriteLog("INDEX WAS OUT OF RANGE FOR INPUT TEXT VALUE! THIS IS FATAL!", LogType.ErrorLog);
                    throw new IndexOutOfRangeException("INDEX FOR SELECTED OBJECT IS OUTSIDE BOUNDS OF VIEWMODEL SENDING BROKER!");
                }

                // Store string for name of the file.
                string FileNameToRemove = this.ViewModel.SessionReportSender.MessageAttachmentFiles[FileIndex].FullName;
                this.ViewLogger.WriteLog($"PULLED INDEX VALUE OK! TRYING TO REMOVE FILE NAMED {FileNameToRemove}", LogType.InfoLog);

                // Store modified view binding list into the view model for updating.
                this.ViewModel.SessionReportSender.RemoveMessageAttachment(FileNameToRemove);
                this.ReportAttachmentFiles.ItemsSource = this.ViewModel.SessionReportSender.MessageAttachmentFiles;
                this.ViewLogger.WriteLog("REQUEST FOR REMOVAL PROCESSED AND PASSED OK! RETURNING NOW", LogType.InfoLog);
            }
            catch (Exception Ex)
            {
                // Log failures and return out. Set the content for the listbox to the VM
                this.ReportAttachmentFiles.ItemsSource = this.ViewModel.SessionReportSender.MessageAttachmentFiles;
                this.ViewLogger.WriteLog("FAILED TO REMOVE FILE DUE TO AN EXCEPTION WHILE TRYING TO FIND FILE NAME OR VALUE!", LogType.ErrorLog);
                this.ViewLogger.WriteLog("EXCEPTION IS BEING LOGGED BELOW.", Ex);
            }
        }
    }
}
