using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.DebugLogFormatters;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.FilteringFormatters;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.InjectorSyntaxFormatters;
using ICSharpCode.AvalonEdit;
using NLog;
using NLog.Config;
using SharpLogging;
using SharpPipes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for InjectorDllOutputLogView.xaml
    /// </summary>
    public partial class FulcrumDllOutputLogView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        internal FulcrumDllOutputLogViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for DLL Logging output view
        /// </summary>
        public FulcrumDllOutputLogView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = FulcrumConstants.FulcrumDllOutputLogViewModel ?? new FulcrumDllOutputLogViewModel(this);

            // Initialize new UI Component
            InitializeComponent();

            // Configure filtering and coloring instances here.
            this.ViewModel.ConfigureOutputHighlighter();
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR FULCRUM DLL OUTPUT OK!", LogType.InfoLog);

            // Setup our data context and log information out
            this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR THE INJECTOR DLL OUTPUT VIEW OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches for the provided text values
        /// </summary>
        /// <param name="SendingTextBox"></param>
        /// <param name="TextChangedArgs"></param>
        private void LogFilteringTextBox_OnTextChanged(object SendingTextBox, TextChangedEventArgs TextChangedArgs)
        {
            // Get the current text entry value and pass it over to the VM for actions.
            var FilteringTextBox = (TextBox)SendingTextBox;
            string TextToFilter = FilteringTextBox.Text;

            // Run the search and show method on the view model
            Task.Run(() =>
            {
                // Disable TextBox for the duration of the task
                Dispatcher.Invoke(() => FilteringTextBox.IsEnabled = false);
                ViewModel.SearchForText(TextToFilter);
                Dispatcher.Invoke(() => FilteringTextBox.IsEnabled = true);
            });
        }
        /// <summary>
        /// Updates has content value on the view model when text is changed.
        /// </summary>
        /// <param name="SendingTextBox"></param>
        /// <param name="TextChangedArgs"></param>
        private void DebugRedirectOutputEdit_OnTextChanged(object SendingTextBox, EventArgs TextChangedArgs)
        {
            // Check the content value. If empty, set hasContent to false.
            TextEditor DebugEditor = (TextEditor)SendingTextBox;
            this.ViewModel.HasOutput = DebugEditor.Text.Trim().Length != 0;
        }
        /// <summary>
        /// Toggles format output for syntax outlining when writing new entries into our log files.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void SyntaxHighlightingButton_OnClick(object Sender, RoutedEventArgs E)
        {
            // Build new button object.
            Button SendButton = (Button)Sender;
            SendButton.Content = "Toggling...";
            SendButton.Background = Brushes.DarkOrange;

            // Now apply new values to our button inside a task object to keep our UI alive
            Task.Run(() =>
            {
                // First toggle the highlighting state on the view model
                bool IsHighlighting = this.ViewModel.UpdateSyntaxHighlighting();

                // Now update our controls based on the state of the highlighter
                Dispatcher.Invoke(() => SendButton.Background = IsHighlighting
                    ? Brushes.DarkGreen
                    : Brushes.DarkRed);
                Dispatcher.Invoke(() => SendButton.Content = IsHighlighting
                    ? "Syntax Highlighting: ON"
                    : "Syntax Highlighting: OFF");

                // Log toggle result.
                this._viewLogger.WriteLog($"TOGGLED HIGHLIGHTING STATE OK! NEW STATE IS {IsHighlighting}", LogType.InfoLog);
            });
        }
    }
}
