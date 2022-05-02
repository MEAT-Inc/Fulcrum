﻿using System;
using System.Collections.Generic;
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
                Filter = "Injector Logs (*.shimLog)|*.shimLog|All Files (*.*)|*.*",
                InitialDirectory = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.FulcrumInjectorLogging.DefaultLoggingPath")
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
            Dispatcher.Invoke(() =>
            {
                // Get Grid object and toggle buttons
                ParentGrid.IsEnabled = false;
                SenderButton.Content = "Loading...";
                ToggleViewTextButton.IsEnabled = false;
            });

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
                    // Invoke via Dispatcher
                    Dispatcher.Invoke(() =>
                    {
                        // Show new temp state
                        ParentGrid.IsEnabled = true;
                        SenderButton.Content = LoadResult ? "Loaded File!" : "Failed!";
                        SenderButton.Background = LoadResult ? Brushes.DarkGreen : Brushes.DarkRed;
                        SenderButton.Click -= LoadInjectorLogFile_OnClick;
                    });

                    // Wait for 3.5 Seconds
                    Thread.Sleep(3500);

                    // Invoke via Dispatcher
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
        private void ProcessLogFileContent_OnClick(object SendingButton, RoutedEventArgs ButtonEventArgs)
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
                if (this.ViewModel.InputParsed == false && !this.ViewModel.ParseLogContents(out _)) {
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
            ToggleViewTextButton.IsEnabled = false; ParentGrid.IsEnabled = false;

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
            // Get Button Contents
            Button SenderButton = (Button)SendingButton;
            var DefaultColor = DefaultValues[1];
            var DefaultContent = DefaultValues[0];

            // Get Grid object
            Grid ParentGrid = SenderButton.Parent as Grid;
            ToggleViewTextButton.IsEnabled = false; ParentGrid.IsEnabled = false;

            // Enable grid, remove click command.
            Task.Run(() =>
            {
                // Invoke via Dispatcher
                Dispatcher.Invoke(() =>
                {
                    // Close the processing flyout
                    this.ProcessingFlyout.IsOpen = false;

                    // Enable grid, show result on buttons
                    ParentGrid.IsEnabled = true;
                    ToggleViewTextButton.IsEnabled = true;
                    SenderButton.Content = ProcessResult ? "Processed!" : "Failed!";
                    SenderButton.Background = ProcessResult ? Brushes.DarkGreen : Brushes.DarkRed;
                });

                // Wait for 3.5 Seconds
                Thread.Sleep(3500);

                // Invoke via Dispatcher
                Dispatcher.Invoke(() =>
                {
                    // Reset values and log information
                    SenderButton.Content = DefaultContent;
                    SenderButton.Background = (Brush)DefaultColor;
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
        /// Shows or hides the content inside the TextEditor to either show the parsed content view or the raw log input view.
        /// </summary>
        /// <param name="SenderButton"></param>
        /// <param name="E"></param>
        private void ToggleParsedContentButton_OnClick(object SenderButton, RoutedEventArgs E)
        {
            // Setup basic control values.
            MainActionLoadButtons.IsEnabled = false;
            Button ParseToggleButton = (Button)SenderButton;
            Brush DefaultColor = ParseToggleButton.Background;

            // Run the switch to show the new content value type here.
            ParseToggleButton.IsEnabled = false;
            bool LoadResult = this.ViewModel.ToggleViewerContents();
            this.ViewLogger.WriteLog("TOGGLED VIEW CONTENTS ON THE TEXTBOX VIEWER OBJECT OK!", LogType.InfoLog);

            // Enable grid, remove click command.
            Task.Run(() =>
            {
                // Invoke via Dispatcher
                Dispatcher.Invoke(() =>
                {
                    // Enable grid, show result on buttons
                    ParseToggleButton.IsEnabled = true;
                    MainActionLoadButtons.IsEnabled = true;
                    ParseToggleButton.Content = LoadResult ? "Loaded!" : "Failed!";
                    ParseToggleButton.Background = LoadResult ? Brushes.DarkGreen : Brushes.DarkRed;
                    ParseToggleButton.Click -= ToggleParsedContentButton_OnClick;
                });

                // Wait for 3.5 Seconds
                Thread.Sleep(3500);

                // Invoke via Dispatcher
                Dispatcher.Invoke(() =>
                {
                    // Reset values and log information
                    ParseToggleButton.Background = DefaultColor;
                    ParseToggleButton.Click += ToggleParsedContentButton_OnClick;
                    ParseToggleButton.Content = this.ViewModel.ShowingParsed ? "Show Raw Log Contents" : "Show Parsed Content";
                    this.ViewLogger.WriteLog("RESET SENDING BUTTON CONTENT VALUES OK! RETURNING TO NORMAL OPERATION NOW.", LogType.WarnLog);
                });
            });

            // Log failed. Return.
            this.ViewLogger.WriteLog("PROCESSED TOGGLE REQUEST OK! SHOWING DEFAULT VALUES AFTER CONTENT UPDATING IS COMPLETE!", LogType.WarnLog);
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

    }
}
