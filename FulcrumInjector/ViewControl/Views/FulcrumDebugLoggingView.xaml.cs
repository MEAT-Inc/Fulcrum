using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FulcrumInjector.AppLogic.AvalonEditHelpers;
using FulcrumInjector.ViewControl.ViewModels;
using NLog;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.Views
{
    /// <summary>
    /// Interaction logic for FulcrumInjectorDebugLoggingView.xaml
    /// </summary>
    public partial class FulcrumDebugLoggingView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("DebugLoggingViewLogger")) ?? new SubServiceLogger("DebugLoggingViewLogger");

        // ViewModel object to bind onto
        public FulcrumDebugLoggingViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumDebugLoggingView()
        {
            // Init component. Build new VM object
            InitializeComponent();
            this.ViewModel = new FulcrumDebugLoggingViewModel();
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumInjectorDebugLoggingView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Configure pipe instances here.
            this.ViewModel.LogContentHelper = new AvalonEditFilteringHelpers(this.DebugRedirectOutputEdit);
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES AND LOGGING TARGETS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches for the provided text values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LogFilteringTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the current text entry value and pass it over to the VM for actions.
            var FilteringTextBox = (TextBox)sender;
            string TextToFilter = FilteringTextBox.Text;

            // Run the search and show method on the view model
            await Task.Run(() =>
            {
                // Disable textbox for the duration of the task
                Dispatcher.Invoke(() => FilteringTextBox.IsEnabled = false);
                ViewModel.SearchForText(TextToFilter);
                Dispatcher.Invoke(() => FilteringTextBox.IsEnabled = true);
            });
        }


        /// <summary>
        /// Pulls in new loggers and shows them here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoggerNameComboBox_OnDropDownOpened(object sender, EventArgs e)
        {
            // Trigger refresh logger list
            this.ViewModel.BuildLoggerNamesList();
            ViewLogger.WriteLog("REFRESHED ENTRIES OK! SHOWING THEM NOW...", LogType.InfoLog);
        }
        /// <summary>
        /// Takes the selected logger object and filters log lines to only contain those from the given logger
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoggerNameComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Build filtering helper using the provided selection object
            ComboBox CastSendingBox = (ComboBox)sender;

            // Check for zero or no selection
            if (CastSendingBox.SelectedIndex <= 0)
            {
                ViewModel?.FilterByLoggerName(null);
                ViewLogger.WriteLog("REMOVED FILTER OBJECTS SINCE SELECTED INDEX WAS OUT OF RANGE!");
                return;
            }

            // Now setup new filtering rule.
            string SelectedLoggerName = CastSendingBox.SelectedItem?.ToString();
            ViewLogger.WriteLog($"CONFIGURING NEW FILTERING RULE FOR LOGGER NAME {SelectedLoggerName}...");
            ViewModel.FilterByLoggerName(SelectedLoggerName);
        }
    }
}
