using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels;
using ICSharpCode.AvalonEdit;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

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
            // Init component. Build new VM object
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

            // Store temp text into our email body.
            this.EmailBodyTextContent.Text = "Dearest Neo,\n\nPlease fix your broken software. I thought this was supposed to make my life easier?\n\nWith Love,\nA Pissed Off Tech";
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
            if (NewTextContent.Length == 0) { this.ViewModel.RemoveAddress(); }
            Regex SendingRegex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$"); 
            var MatchedEmails = SendingRegex.Matches(NewTextContent);
            if (MatchedEmails.Count == 0) return; 

            // Clear out all current address values and then add them back in one at a time.
            this.ViewModel.RemoveAddress();
            foreach (Match AddressMatch in MatchedEmails) { this.ViewModel.AppendAddress(AddressMatch.Value); }
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

            // Get parent grid object here.
            // Button SendingButtonCast = (Button)SendingButton;
            // Grid ParentSendingGrid = SendingButtonCast.Parent as Grid;
            // this.ViewLogger.WriteLog("PULLED GRID INSTANCE FOR OUR PARENT OBJECT OK! SETTING NEW VALUES NOW", LogType.InfoLog);

            // Set grid content values.
            // var LastRowInstance = ParentSendingGrid.RowDefinitions.LastOrDefault();
            // if (!this.ViewModel.ShowEmailInfoText) ParentSendingGrid.RowDefinitions.Remove(LastRowInstance);
            // else { ParentSendingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); }
            // this.ViewLogger.WriteLog("UPDATED VIEW CONTENT VALUES CORRECTLY! GRIDS SHOULD HAVE RESIDED AS EXPECTED", LogType.InfoLog);
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
        /// <summary>
        /// Attaches a new file entry into our list of files by showing a file selection dialogue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddReportAttachmentButton_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO: Write logic for adding a new file object into the report list of attachments for our email
        }

    }
}
