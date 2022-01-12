using System;
using System.Linq;
using System.Text.RegularExpressions;
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
            this.ViewModel.ShowEmailInfoText = !this.ViewModel.ShowEmailInfoText;
            this.EmailBodyTextContent.Text = 
                "Dearest Neo,\n\n" +
                "Please fix your broken software. I thought this was supposed to make my life easier?\n\n" +
                "With Love,\n" +
                "A Pissed Off Tech";

            // Log done building new ViewModel.
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR EMAIL REPORTING OUTPUT OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

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
            if (NewTextContent.Length == 0) { this.ViewModel.RemoveRecipient(); }
            Regex SendingRegex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$"); 
            var MatchedEmails = SendingRegex.Matches(NewTextContent);
            if (MatchedEmails.Count == 0) return; 

            // Clear out all current address values and then add them back in one at a time.
            this.ViewModel.RemoveRecipient();
            foreach (Match AddressMatch in MatchedEmails) { this.ViewModel.AddRecipient(AddressMatch.Value); }
            this.ViewLogger.WriteLog($"CURRENT EMAILS: {string.Join(",", this.ViewModel.EmailAddressRecipients)}", LogType.TraceLog);

            // Now remove address values that don't fly here.
            BoxObject.Text = string.Join(",", this.ViewModel.EmailAddressRecipients);
            this.ViewLogger.WriteLog("UPDATED EMAIL ENTRY TEXTBOX CONTENTS TO REFLECT ONLY VALID EMAILS!", LogType.InfoLog);
        }

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
                InitialDirectory = ValueLoaders.GetConfigValue<string>("FulcrumInjectorLogging.DefaultLoggingPath")
            };

            // Now open the dialog and allow the user to pick some new files.
            this.ViewLogger.WriteLog("OPENING NEW DIALOG OBJECT NOW...", LogType.WarnLog);
            if (SelectAttachmentDialog.ShowDialog() != DialogResult.OK || SelectAttachmentDialog.FileNames.Length == 0) {
                this.ViewLogger.WriteLog("FAILED TO SELECT A NEW FILE OBJECT! EXITING NOW...", LogType.ErrorLog);
                return;
            }

            // Now pull our file objects out and store them on our viewModel
            foreach (var FilePath in SelectAttachmentDialog.FileNames) {
                this.ViewLogger.WriteLog($"ATTACHING FILE OBJECT: {FilePath}", LogType.TraceLog);
                this.ViewModel.AddMessageAttachment(FilePath);
            }
            
            // Log information about done.
            this.ViewLogger.WriteLog("DONE APPENDING NEW FILE INSTANCES. ATTACHMENTS LISTBOX SHOULD BE UPDATED WITH NEW INFORMATION NOW", LogType.InfoLog);
            this.ViewLogger.WriteLog($"TOTAL OF {this.ViewModel.EmailMessageAttachments.Length} ATTACHMENTS ARE NOW BEING TRACKED", LogType.InfoLog);
        }
        /// <summary>
        /// Event handler for when a button is clicked to remove the selected attachment.
        /// </summary>
        /// <param name="RemoveFileButton"></param>
        /// <param name="RemoveFileButtonEventArgs"></param>
        private void RemoveAttachmentButton_OnClick(object RemoveFileButton, RoutedEventArgs RemoveFileButtonEventArgs)
        {
            // TODO: Build logic to remove file based on selected index.
        }

        /// <summary>
        /// Send email button for the report sender
        /// </summary>
        /// <param name="SendButton"></param>
        /// <param name="SendButtonArgs"></param>
        private void SendEmailButton_OnClick(object SendButton, RoutedEventArgs SendButtonArgs)
        {
            // TODO: Write logic to parse contents and build an output email to send.
        }
    }
}
