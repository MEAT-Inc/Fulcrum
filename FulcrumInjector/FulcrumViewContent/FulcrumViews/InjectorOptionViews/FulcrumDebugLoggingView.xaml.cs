using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorOptionViewModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorOptionViews
{
    /// <summary>
    /// Interaction logic for FulcrumInjectorDebugLoggingView.xaml
    /// </summary>
    public partial class FulcrumDebugLoggingView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        internal FulcrumDebugLoggingViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumDebugLoggingView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = FulcrumConstants.FulcrumDebugLoggingViewModel ?? new FulcrumDebugLoggingViewModel(this);

            // Initialize new UI Component
            InitializeComponent();

            // Setup the output logging helper once the component is alive
            this.ViewModel.ConfigureOutputHighlighter();
            this._viewLogger.WriteLog("BUILT NEW LOG CONTENT FORMATTER OK!", LogType.InfoLog);

            // Store our View model as the current context and log out some information
            this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR THE DEBUG LOGGING REVIEW VIEW OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumDebugLoggingView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Refresh logger list each time this view is opened
            this.ViewModel.LoggerNamesFound = this.ViewModel.BuildLoggerNamesList();
            this._viewLogger.WriteLog("REFRESHED ENTRIES OK! SHOWING THEM NOW...", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

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
        /// Takes the selected logger object and filters log lines to only contain those from the given logger
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoggerNameComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Build filtering helper using the provided selection object
            ComboBox CastSendingBox = (ComboBox)sender;

            // Check for zero or no selection
            if (CastSendingBox.SelectedIndex < 0)
            {
                this.ViewModel?.FilterByLoggerName(null);
                this._viewLogger.WriteLog("REMOVED FILTER OBJECTS SINCE SELECTED INDEX WAS OUT OF RANGE!");
                return;
            }

            // Now setup new filtering rule.
            string SelectedLoggerName = CastSendingBox.SelectedItem?.ToString();
            this._viewLogger.WriteLog($"CONFIGURING NEW FILTERING RULE FOR LOGGER NAME {SelectedLoggerName}...");
            this.ViewModel.FilterByLoggerName(SelectedLoggerName);
        }
        /// <summary>
        /// Event handler used to open the current log file for this injector session in an external application
        /// </summary>
        /// <param name="sender">The sending control for this action</param>
        /// <param name="e">Events fired along with the action</param>
        private void OpenLogFileExternal_OnClick(object sender, RoutedEventArgs e)
        {
            // Find our log file name to view first
            string LogFileName = SharpLogBroker.LogFilePath;
            this._viewLogger.WriteLog($"OPENING UP LOG FILE {LogFileName} IN THE DEFAULT VIEWING APP NOW...");

            // If VS code doesn't exist, then default to notepad here
            string VsCodePath_32 = "C:\\Program Files (x86)\\Microsoft VS Code\\Code.exe";
            string VsCodePath_64 = "C:\\Program Files\\Microsoft VS Code\\Code.exe";
            if (File.Exists(VsCodePath_32))
            {
                // Log which VS code instance was found and start it up to view our log file
                this._viewLogger.WriteLog("FOUND VS CODE (32 BIT)! OPENING OUR LOG FILE NOW...");
                Process.Start(VsCodePath_32, LogFileName);
            }
            else if (File.Exists(VsCodePath_64))
            {
                // Log which VS code instance was found and start it up to view our log file
                this._viewLogger.WriteLog("FOUND VS CODE (64 BIT)! OPENING OUR LOG FILE NOW...");
                Process.Start(VsCodePath_64, LogFileName);
            }
            else
            {
                // If no VS Code instances exist, boot it up using notepad
                this._viewLogger.WriteLog("NO VS CODE INSTALL WAS FOUND! OPENING LOG FILE IN NOTEPAD...");
                Process.Start("notepad.exe", LogFileName);
            }
        }
        /// <summary>
        /// Event handler used to open the current log file for this injector session in a new
        /// standalone window which will only hold the log file viewer
        /// </summary>
        /// <param name="sender">The sending control for this action</param>
        /// <param name="e">Events fired along with the action</param>
        private void OpenLogFileWindow_OnClick(object sender, RoutedEventArgs e)
        {
            // For now just log out this isn't supported
            this._viewLogger.WriteLog("ERROR! OPENING STANDALONE LOG FILE WINDOWS IS NOT YET SUPPORTED!", LogType.WarnLog);
        }
    }
}
