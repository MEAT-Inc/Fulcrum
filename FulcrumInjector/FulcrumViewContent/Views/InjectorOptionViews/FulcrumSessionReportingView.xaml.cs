using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels;
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
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reacts to a new button click for adding an email entry into our list
        /// </summary>
        /// <param name="SendingTextBox">Sending button</param>
        /// <param name="EnteredKeyArgs">Event Args processed</param>
        private void AddAddressTextBox_KeyDown(object SendingTextBox, KeyEventArgs EnteredKeyArgs)
        {
            // When a key is pressed, if it's not the enter key move on.
            if (EnteredKeyArgs.Key != Key.Enter) return;

            // Get text of TextBox object and try to add address.
            TextBox BoxObject = (TextBox)SendingTextBox;
            bool AddedCorrectly = this.ViewModel.AppendNewAddress(BoxObject.Text.Trim());

            // If Added, Set text to empty. The listbox of address values will auto update


            // Else Set text to 'Invalid Email!', Highlight the box for 3 seconds in red and then reset to normal
        }
    }
}
