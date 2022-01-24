using System;
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
using FulcrumInjector.FulcrumLogic.InjectorPipes;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using Button = System.Windows.Controls.Button;
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
            this.ViewModel = InjectorConstants.FulcrumLogReviewViewModel ?? new FulcrumLogReviewViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);

            // TODO: Append new Transformers into this constructor to apply color filtering on the output view.
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

            // Log completed setup values ok
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
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true,
                RestoreDirectory = true,
                AutoUpgradeEnabled = true,
                Filter = "Injector Logs (*.shimLog)|*.shimLog|All Files (*.*)|*.*",
                InitialDirectory = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.FulcrumInjectorLogging.DefaultLoggingPath")
            };

            // Now open the dialog and allow the user to pick some new files.
            this.ViewLogger.WriteLog("OPENING NEW DIALOG OBJECT NOW...", LogType.WarnLog);
            if (SelectAttachmentDialog.ShowDialog() != DialogResult.OK || SelectAttachmentDialog.FileNames.Length == 0)
            {
                // Log failed, set no file, reset sending button and return.
                this.ViewLogger.WriteLog("FAILED TO SELECT A NEW FILE OBJECT! EXITING NOW...", LogType.ErrorLog);
                return;
            }
            
            // Get Grid object and toggle buttons
            ToggleViewTextButton.IsEnabled = false;
            Grid ParentGrid = SenderButton.Parent as Grid;
            SenderButton.Content = "Loading..."; ParentGrid.IsEnabled = false;

            // Store new file object value. Validate it on the ViewModel object first.
            bool LoadResult = this.ViewModel.LoadLogContents(SelectAttachmentDialog.FileName);
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
        }
        /// <summary>
        /// Runs the processing command instance object commands for log processing.
        /// </summary>
        /// <param name="SendingButton"></param>
        /// <param name="ButtonEventArgs"></param>
        private void ProcessLogFileContent_OnClick(object SendingButton, RoutedEventArgs ButtonEventArgs)
        {
            // Start by turning off the grid for all sending buttons.
            this.ViewLogger.WriteLog("PROCESSED CLICK FOR LOG PARSE COMMAND! DISABLING BUTTONS AND PREPARING TO PROCESS FILE NOW", LogType.WarnLog);

            // Pull the button object
            Button SenderButton = (Button)SendingButton;
            string DefaultContent = SenderButton.Content.ToString();
            var DefaultColor = SenderButton.Background;
            SenderButton.Content = "Processing...";

            // Open the processing flyout
            this.ProcessingFlyout.IsOpen = true;

            // Get Grid object
            Grid ParentGrid = SenderButton.Parent as Grid;
            ToggleViewTextButton.IsEnabled = false; ParentGrid.IsEnabled = false;

            // Parse Contents out now on the VM and show the please wait operation.
            Task.Run(() =>
            {
                // Log information and parse content output.
                this.ViewLogger.WriteLog($"LOADED INPUT LOG FILE OBJECT: {ViewModel.LoadedLogFile}", LogType.TraceLog);
                this.ViewLogger.WriteLog("PROCESSING CONTENTS IN THE BACKGROUND NOW.", LogType.InfoLog);

                // TODO: Pop open a flyout view here to show progress of these operations

                // Run the parse operation here.
                bool ProcessResult = this.ViewModel.ParseLogContents(out _);
                this.ViewLogger.WriteLog("DONE PROCESSING OUTPUT CONTENT FOR OUR EXPRESSION OBJECTS! READY TO DISPLAY ON OUR VIEW CONTENT", LogType.InfoLog);

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
                        SenderButton.Click -= ProcessLogFileContent_OnClick;
                    });

                    // Wait for 3.5 Seconds
                    Thread.Sleep(3500);

                    // Invoke via Dispatcher
                    Dispatcher.Invoke(() =>
                    {
                        // Reset values and log information
                        SenderButton.Content = DefaultContent;
                        SenderButton.Background = DefaultColor;
                        SenderButton.Click += ProcessLogFileContent_OnClick;
                        this.ViewLogger.WriteLog("RESET SENDING BUTTON CONTENT VALUES OK! RETURNING TO NORMAL OPERATION NOW.", LogType.WarnLog);
                    });
                });
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
    }
}
