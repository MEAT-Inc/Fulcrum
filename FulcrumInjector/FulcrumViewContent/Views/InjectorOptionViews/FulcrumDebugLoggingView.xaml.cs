using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.FIlteringFormatters;
using NLog;
using NLog.Config;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorOptionViews
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
            // Build new ViewModel object
            InitializeComponent();
            this.ViewModel = InjectorConstants.FulcrumDebugLoggingViewModel ?? new FulcrumDebugLoggingViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);

            // Configure the new Logging Output Target.
            var CurrentConfig = LogManager.Configuration;
            try { CurrentConfig.RemoveTarget("DebugLoggingRedirectTarget"); }
            catch { ViewLogger.WriteLog("NO TARGETS MATCHING DEFINED TYPE WERE FOUND! THIS IS A GOOD THING", LogType.InfoLog); }
            ConfigurationItemFactory.Default.Targets.RegisterDefinition("DebugLoggingRedirectTarget", typeof(DebugLoggingRedirectTarget));
            CurrentConfig.AddRuleForAllLevels(new DebugLoggingRedirectTarget(this.DebugRedirectOutputEdit));
            LogManager.ReconfigExistingLoggers();
            this.ViewLogger.WriteLog("BUILT INSTANCE FOR OUR DLL OUTPUT DEBUG LOGGING VIEW OK!", LogType.InfoLog);
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

            // Log Added new target output ok
            this.ViewLogger.WriteLog("INJECTOR HAS REGISTERED OUR DEBUGGING REDIRECT OBJECT OK!", LogType.WarnLog);
            this.ViewLogger.WriteLog("ALL LOG OUTPUT WILL APPEND TO OUR DEBUG VIEW ALONG WITH THE OUTPUT FILES NOW!", LogType.WarnLog);

            // Configure pipe instances here.
            this.ViewModel.LogContentHelper = new LogOutputFilteringHelper(this.DebugRedirectOutputEdit);
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
