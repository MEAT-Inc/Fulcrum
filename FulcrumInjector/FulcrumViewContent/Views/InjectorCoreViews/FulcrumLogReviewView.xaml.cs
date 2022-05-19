using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.FIlteringFormatters;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters;
using NLog;
using NLog.Config;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for FulcrumLogReviewView.xaml
    /// </summary>
    public partial class FulcrumLogReviewView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorLogReviewViewLogger")) ?? new SubServiceLogger("InjectorLogReviewViewLogger");

        // ViewModel object to bind onto
        public FulcrumLogReviewViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for DLL Logging output view
        /// </summary>
        public FulcrumLogReviewView()
        {
            // Build new ViewModel object
            InitializeComponent();
            this.ViewModel = FulcrumConstants.FulcrumLogReviewViewModel ?? new FulcrumLogReviewViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumLogReviewView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Setup coloring helper.
            this.ViewModel.LogFilteringHelper ??= new LogOutputFilteringHelper(this.ReplayLogInputContent);
            this.ViewModel.InjectorSyntaxHelper ??= new InjectorOutputSyntaxHelper(this.ReplayLogInputContent);
            this.ViewLogger.WriteLog("SETUP A NEW LOG FILE READING OBJECT TO PROCESS INPUT LOG FILES FOR REVIEW OK!", LogType.WarnLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Action processing for when clicking the load file button for the injector.
        /// </summary>
        /// <param name="SendingButton"></param>
        /// <param name="SenderEventArgs"></param>
        private void LoadInjectorLogFile_OnClick(object SendingButton, RoutedEventArgs SenderEventArgs)
        {
            // Start by setting the sending button content to "Loading..." and disable it.
            Button SenderButton = (Button)SendingButton;
            string DefaultContent = SenderButton.Content.ToString();
            var DefaultColor = SenderButton.Background;

            // Log information about opening appending box and begin selection
            this.ViewLogger.WriteLog("OPENING NEW FILE SELECTION DIALOGUE FOR APPENDING OUTPUT FILES NOW...", LogType.InfoLog);
            using OpenFileDialog SelectAttachmentDialog = new OpenFileDialog()
            {
                Multiselect = true,
                CheckFileExists = true,
                CheckPathExists = true,
                RestoreDirectory = true,
                AutoUpgradeEnabled = true,
                Filter = Debugger.IsAttached ? "All Files (*.*)|*.*" : "Injector Logs (*.shimLog)|*.shimLog|All Files (*.*)|*.*",
                InitialDirectory = Debugger.IsAttached ?
                    "C:\\Drewtech\\logs" :
                    ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.FulcrumInjectorLogging.DefaultLoggingPath")
            };

            // Now open the dialog and allow the user to pick some new files.
            this.ViewLogger.WriteLog("OPENING NEW DIALOG OBJECT NOW...", LogType.WarnLog);
            if (SelectAttachmentDialog.ShowDialog() != DialogResult.OK || SelectAttachmentDialog.FileNames.Length == 0) {
                // Log failed, set no file, reset sending button and return.
                this.ViewLogger.WriteLog("FAILED TO SELECT A NEW FILE OBJECT! EXITING NOW...", LogType.ErrorLog);
                return;
            }

            // Invoke this to keep UI Alive
            Grid ParentGrid = SenderButton.Parent as Grid;
            ParentGrid.IsEnabled = false;
            SenderButton.Content = "Loading...";

            // Run this in the background for smoother operation
            Task.Run(() =>
            {
                // Check if we have multiple files. 
                string FileToLoad = SelectAttachmentDialog.FileNames.Length == 1 ?
                    SelectAttachmentDialog.FileName :
                    this.ViewModel.CombineLogFiles(SelectAttachmentDialog.FileNames);

                // Store new file object value. Validate it on the ViewModel object first.
                bool LoadResult = this.ViewModel.LoadLogContents(FileToLoad);
                if (LoadResult) this.ViewLogger.WriteLog("PROCESSED OUTPUT CONTENT OK! READY TO PARSE", LogType.InfoLog);
                else this.ViewLogger.WriteLog("FAILED TO SPLIT INPUT CONTENT! THIS IS FATAL!", LogType.ErrorLog);

                // Enable grid, remove click command.
                Task.Run(() =>
                {
                    // Show new temp state
                    Dispatcher.Invoke(() =>
                    {
                        ParentGrid.IsEnabled = true;
                        SenderButton.Content = LoadResult ? "Loaded File!" : "Failed!";
                        SenderButton.Background = LoadResult ? Brushes.DarkGreen : Brushes.DarkRed;
                        SenderButton.Click -= LoadInjectorLogFile_OnClick;
                        this.ViewerContentComboBox.SelectedIndex = 0;
                    });

                    // Wait for 3.5 Seconds
                    Thread.Sleep(3500);
                    Dispatcher.Invoke(() =>
                    {
                        // Reset button values 
                        SenderButton.Content = DefaultContent;
                        SenderButton.Background = DefaultColor;
                        SenderButton.Click += LoadInjectorLogFile_OnClick;

                        // Log information
                        this.ViewLogger.WriteLog("RESET SENDING BUTTON CONTENT VALUES OK! RETURNING TO NORMAL OPERATION NOW.", LogType.WarnLog);
                    });
                });
            });
        }
        /// <summary>
        /// Runs the processing command instance object commands for log processing.
        /// </summary>
        /// <param name="SendingButton"></param>
        /// <param name="ButtonEventArgs"></param>
        private void BuildExpressionsButton_OnClick(object SendingButton, RoutedEventArgs ButtonEventArgs)
        {
            // Setup default values and parse out content values
            var Defaults = this.ProcessingActionStarted(SendingButton);
            this.ViewLogger.WriteLog("STARTING PROCESSING FOR PARSE CONTENT VALUES NOW...");

            // Run this as a task to keep our UI Alive
            this.ViewLogger.WriteLog("PROCESSING CONTENTS IN THE BACKGROUND NOW.", LogType.InfoLog);
            Task.Run(() =>
            {
                // Store result from processing
                bool ParseResult = this.ViewModel.ParseLogContents(out _);
                this.ViewLogger.WriteLog("PROCESSING INPUT CONTENT IS NOW COMPLETE!", LogType.InfoLog);

                // Invoke via dispatcher
                Dispatcher.Invoke(() => this.ProcessingActionFinished(ParseResult, SendingButton, Defaults));
                this.ViewLogger.WriteLog("INPUT CONTENT PARSING IS NOW COMPLETE!", LogType.InfoLog);
            });
        }
        /// <summary>
        /// Builds a simulation out of the currently processed log file contents.
        /// </summary>
        /// <param name="SendingButton"></param>
        /// <param name="ButtonEventArgs"></param>
        private void BuildSimulationButton_OnClick(object SendingButton, RoutedEventArgs ButtonEventArgs)
        {
            // Start by pulling in the expression values from our view model object and then pass them into our parser
            var Defaults = this.ProcessingActionStarted(SendingButton);
            this.ViewLogger.WriteLog("BUILDING SIMULATION FILE NOW...");

            // TODO: RUN THE CURRENT CONTENTS INTO THE SIMULATION HELPER PROCESSES AND BUILD OUTPUT!
            this.ViewLogger.WriteLog("PROCESSING CONTENTS IN THE BACKGROUND NOW.", LogType.InfoLog);
            Task.Run(() =>
            {
                // Store result from processing if it's not yet done on the view model
                if (this.ViewModel.ExpressionsBuilt == false) 
                    if (!this.ViewModel.ParseLogContents(out _)) {
                        this.ProcessingActionFinished(false, SendingButton, Defaults);
                        return;
                    }

                // Now build our simulation object here
                bool SimResult = this.ViewModel.BuildLogSimulation(out var SimGenerator); 
                this.ViewLogger.WriteLog("PROCESSING INPUT CONTENT IS NOW COMPLETE!", LogType.InfoLog);

                // Invoke via dispatcher
                Dispatcher.Invoke(() => this.ProcessingActionFinished(SimResult, SendingButton, Defaults));
                this.ViewLogger.WriteLog("SIMULATION PROCESSING IS NOW COMPLETE!", LogType.InfoLog);
            });
        }


        /// <summary>
        /// Sets up a processing window to show while an operation is processing
        /// </summary>
        /// <param name="SendingButton"></param>
        private object[] ProcessingActionStarted(object SendingButton)
        {
            // Start by turning off the grid for all sending buttons.
            this.ViewLogger.WriteLog("PROCESSED CLICK FOR PROCESSING COMMAND! DISABLING BUTTONS AND PREPARING TO PROCESS FILE NOW", LogType.WarnLog);

            // Pull the button object
            Button SenderButton = (Button)SendingButton;
            string DefaultContent = SenderButton.Content.ToString();
            var DefaultColor = SenderButton.Background; SenderButton.Content = "Processing...";

            // Open the processing flyout
            this.ProcessingFlyout.IsOpen = true;
            Grid ParentGrid = SenderButton.Parent as Grid;
            ViewerContentComboBox.IsEnabled = false; ParentGrid.IsEnabled = false;

            // Return the built object values for shutdown later on
            return new object[] { DefaultContent, DefaultColor };
        }
        /// <summary>
        /// Processing action stopped or ended
        /// </summary>
        /// <param name="ProcessResult">Result of processing</param>
        /// <param name="SendingButton"></param>
        /// <param name="DefaultValues"></param>
        private void ProcessingActionFinished(bool ProcessResult, object SendingButton, object[] DefaultValues)
        {
            // Dispatcher invoked for content
            this.Dispatcher.Invoke(() =>
            {
                // Get Button Contents
                Button SenderButton = (Button)SendingButton;
                var DefaultColor = DefaultValues[1];
                var DefaultContent = DefaultValues[0];

                // Get Grid object
                Grid ParentGrid = SenderButton.Parent as Grid; 
                ViewerContentComboBox.IsEnabled = false; ParentGrid.IsEnabled = false;

                // Close the processing flyout
                this.ProcessingFlyout.IsOpen = false;

                // Enable grid, show result on buttons
                ParentGrid.IsEnabled = true;
                ViewerContentComboBox.IsEnabled = true;
                SenderButton.Content = ProcessResult ? "Processed!" : "Failed!";
                SenderButton.Background = ProcessResult ? Brushes.DarkGreen : Brushes.DarkRed;

                // Reset values here.
                Task.Run(() =>
                {
                    // Wait for 3.5 Seconds
                    Thread.Sleep(3500);
                    Dispatcher.Invoke(() =>
                    {
                        // Reset values and log information
                        SenderButton.Content = DefaultContent;
                        SenderButton.Background = (Brush)DefaultColor;
                    });

                    // Log information about being done
                    this.ViewLogger.WriteLog("RESET SENDING BUTTON CONTENT VALUES OK! RETURNING TO NORMAL OPERATION NOW.", LogType.WarnLog);
                });
            });
        }


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
        /// Toggles format output for syntax outlining when writing new entries into our log files.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private async void SyntaxHighlightingButton_OnClick(object Sender, RoutedEventArgs E)
        {
            // Build new button object.
            Button SendButton = (Button)Sender;
            SendButton.Content = "Toggling...";
            SendButton.Background = Brushes.DarkOrange;

            // Async toggle button content and output format.
            await Task.Run(() =>
            {
                // Check the current state and toggle it.
                if (this.ViewModel.InjectorSyntaxHelper.IsHighlighting)
                    this.ViewModel.InjectorSyntaxHelper.StopColorHighlighting();
                else this.ViewModel.InjectorSyntaxHelper.StartColorHighlighting();
            });

            // Now apply new values to our button.
            SendButton.Background = this.ViewModel.InjectorSyntaxHelper.IsHighlighting ? Brushes.DarkGreen : Brushes.DarkRed;
            SendButton.Content = this.ViewModel.InjectorSyntaxHelper.IsHighlighting ? "Syntax Highlighting: ON" : "Syntax Highlighting: OFF";

            // Log toggle result.
            this.ViewLogger.WriteLog($"TOGGLED HIGHLIGHTING STATE OK! NEW STATE IS {this.ViewModel.InjectorSyntaxHelper.IsHighlighting}", LogType.InfoLog);
        }
        /// <summary>
        /// Changes the processing output actions of the comobox so it can show new values in the viewer
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void ViewerContentComboBox_OnSelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            // Start by finding what value is selected in our combobox.
            ComboBox CastBox = (ComboBox)Sender;
            int SelectedBoxIndex = CastBox.SelectedIndex;
            this.ViewLogger?.WriteLog("TOGGLING VIEW CONTENTS FOR PROCESSING OUTPUT VIEWER NOW...", LogType.InfoLog);

            // Now apply the new content based on what's in the box.
            FulcrumLogReviewViewModel.ViewerStateType DesiredState = FulcrumLogReviewViewModel.ViewerStateType.NoContent;
            if (SelectedBoxIndex == 0) DesiredState = FulcrumLogReviewViewModel.ViewerStateType.ShowingLogFile;
            if (SelectedBoxIndex == 1) DesiredState = FulcrumLogReviewViewModel.ViewerStateType.ShowingExpressions;
            if (SelectedBoxIndex == 2) DesiredState = FulcrumLogReviewViewModel.ViewerStateType.ShowingSimulation;

            // Now toggle the state value
            string DefaultValue = FilteringLogFileTextBox.Text;
            if (this.ViewModel.ToggleViewerContents(DesiredState)) this.ViewLogger?.WriteLog("PROCESSED REQUEST CORRECTLY! SHOWING VIEW CONTENT AS EXPECTED!");
            else
            {
                // Set to failed 
                FilteringLogFileTextBox.Foreground = Brushes.Red;
                FilteringLogFileTextBox.FontWeight = FontWeights.Bold;
                FilteringLogFileTextBox.Text = $"Failed To Load {DesiredState.ToDescriptionString()}! Did you build it?";

                // Reset the selected item value
                ComboBoxItem CastItem = (ComboBoxItem)E.RemovedItems[0];
                int IndexToSet = CastBox.Items.IndexOf(CastItem);
                CastBox.SelectedIndex = IndexToSet;

                // Now Reset values
                Task.Run(() =>
                {
                    Thread.Sleep(3500);
                    Dispatcher.Invoke(() => FilteringLogFileTextBox.Text = DefaultValue);
                });
            }
        }
    }
}
