using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
using FulcrumInjector.FulcrumViewSupport.AppStyleSupport.AvalonEditHelpers;
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
        private async void LoadInjectorLogFile_OnClick(object SendingButton, RoutedEventArgs SenderEventArgs)
        {
            // Start by setting the sending button content to "Loading..." and disable it.
            Button SenderButton = (Button)SendingButton;
            string DefaultContent = SenderButton.Content.ToString();
            SenderButton.Content = "Loading File...";
            SenderButton.IsEnabled = false;

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

                // Reset Sending button
                SenderButton.Content = DefaultContent;
                SenderButton.IsEnabled = true;
                return;
            }

            // Store new file object value. Validate it on the ViewModel object first.
            this.ViewModel.LoadedLogFile = SelectAttachmentDialog.FileName;
            if (!this.ViewModel.LoadLogFileContents(out var SplitContent))
            {
                // Log Failed and show buttons again
                var DefaultColor = SenderButton.Background;
                this.ViewLogger.WriteLog("FAILED TO SPLIT INPUT CONTENT! THIS IS FATAL!", LogType.ErrorLog);
                SenderButton.Content = "Failed!"; SenderButton.Click -= LoadInjectorLogFile_OnClick; SenderButton.Background = Brushes.DarkRed;

                // Show this for 3.5 Seconds and then reset.
                Dispatcher.InvokeAsync(() =>
                {
                    // Wait for 3.5 Seconds
                    Thread.Sleep(3500);

                    // Reset button values 
                    SenderButton.Content = DefaultContent;
                    SenderButton.Background = DefaultColor;
                    SenderButton.Click += LoadInjectorLogFile_OnClick;

                    // Log information
                    this.ViewLogger.WriteLog("RESET SENDING BUTTON CONTENT VALUES OK! RETURNING TO NORMAL OPERATION NOW.", LogType.WarnLog);
                });

                // Return failed.
                this.ViewLogger.WriteLog("ASYNC DISPATCHER FOR FAILED BUTTON CONTENT CONTROL IS RUNNING. RETURNING FROM METHOD NOW", LogType.ErrorLog);
                return;
            }

            // Parse Contents out now on the VM and show the please wait operation.
            await Task.Run(() =>
            {
                // Log information and parse content output.
                this.ViewLogger.WriteLog($"LOADED INPUT LOG FILE OBJECT: {SelectAttachmentDialog.FileName}", LogType.TraceLog); 
                this.ViewLogger.WriteLog("PROCESSING CONTENTS IN THE BACKGROUND NOW.", LogType.InfoLog);

                // Run the parse operation here.
                this.ViewModel.ProcessLogContents(SplitContent);
            });

            // Reset Sending Button now.
            SenderButton.Content = DefaultContent;
            SenderButton.IsEnabled = true;
        }
    }
}
