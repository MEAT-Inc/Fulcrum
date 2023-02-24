using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FulcrumInjector.FulcrumViewSupport.FulcrumJson.JsonHelpers;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for FulcrumSimulationPlaybackView.xaml
    /// </summary>
    public partial class FulcrumSimulationPlaybackView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        internal FulcrumSimulationPlaybackViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new view object instance for our simulation playback
        /// </summary>
        public FulcrumSimulationPlaybackView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = FulcrumConstants.FulcrumSimulationPlaybackViewModel ?? new FulcrumSimulationPlaybackViewModel(this);

            // Initialize new UI Component
            InitializeComponent();
            this._viewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumSimulationPlaybackView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new data context for our view model
            this.DataContext = this.ViewModel;

            // Check for hardware selection from the monitoring view
            var HardwareConfigView = FulcrumConstants.FulcrumInstalledHardwareViewModel;
            this.ViewModel.IsHardwareSetup = !(HardwareConfigView.SelectedDLL == null || string.IsNullOrEmpty(HardwareConfigView.SelectedDevice));
            this._viewLogger.WriteLog($"CURRENT HARDWARE STATE FOR SIMULATIONS: {this.ViewModel.IsHardwareSetup}");
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads in a new simulation file from our file box and stores it onto our view model
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void LoadSimulationButton_OnClick(object Sender, RoutedEventArgs E)
        {
            // Start by setting the sending button content to "Loading..." and disable it.
            Button SenderButton = (Button)Sender;
            string DefaultContent = SenderButton.Content.ToString();
            var DefaultColor = SenderButton.Background;

            // Log information about opening appending box and begin selection
            this._viewLogger.WriteLog("OPENING NEW FILE SELECTION DIALOGUE FOR APPENDING OUTPUT FILES NOW...", LogType.InfoLog);
            using var SelectAttachmentDialog = new System.Windows.Forms.OpenFileDialog()
            {
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true,
                RestoreDirectory = true,
                AutoUpgradeEnabled = true,
                Filter = Debugger.IsAttached ? "All Files (*.*)|*.*" : "Injector Simulations (*.ptSim)|*.ptSim|All Files (*.*)|*.*",
                InitialDirectory = Debugger.IsAttached ?
                    "C:\\Drewtech\\logs" :
                    ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorLogging.DefaultSimulationsPath")
            };

            // Now open the dialog and allow the user to pick some new files.
            this._viewLogger.WriteLog("OPENING NEW DIALOG OBJECT NOW...", LogType.WarnLog);
            if (SelectAttachmentDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK || SelectAttachmentDialog.FileNames.Length == 0) {
                // Log failed, set no file, reset sending button and return.
                this._viewLogger.WriteLog("FAILED TO SELECT A NEW FILE OBJECT! EXITING NOW...", LogType.ErrorLog);
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
                string FileToLoad = SelectAttachmentDialog.FileName;

                // Store new file object value. Validate it on the ViewModel object first.
                bool LoadResult = this.ViewModel.LoadSimulation(FileToLoad);
                if (LoadResult) this._viewLogger.WriteLog("LOADED SIMULATION FILE OK! READY TO PLAYBACK", LogType.InfoLog);
                else this._viewLogger.WriteLog("FAILED TO LOAD NEW SIMULATION FILE! THIS IS FATAL", LogType.ErrorLog);

                // Enable grid, remove click command.
                Task.Run(() =>
                {
                    // Show new temp state
                    Dispatcher.Invoke(() =>
                    {
                        ParentGrid.IsEnabled = true;
                        SenderButton.Content = LoadResult ? "Loaded File!" : "Failed!";
                        SenderButton.Background = LoadResult ? Brushes.DarkGreen : Brushes.DarkRed;
                        SenderButton.Click -= this.LoadSimulationButton_OnClick;
                    });

                    // Wait for 3.5 Seconds
                    Thread.Sleep(3500);
                    Dispatcher.Invoke(() =>
                    {
                        // Reset button values 
                        SenderButton.Content = DefaultContent;
                        SenderButton.Background = DefaultColor;
                        SenderButton.Click += this.LoadSimulationButton_OnClick;

                        // Log information
                        this._viewLogger.WriteLog("RESET SENDING BUTTON CONTENT VALUES OK! RETURNING TO NORMAL OPERATION NOW.", LogType.WarnLog);
                    });
                });
            });
        }
        /// <summary>
        /// Toggles the view of our editor for flyout values
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void ToggleSimulationEditor_OnClick(object Sender, RoutedEventArgs E)
        {
            // Toggle the view for our simulation editor flyout
            this.SimulationEditorFlyout.IsOpen = !this.SimulationEditorFlyout.IsOpen;
            this._viewLogger.WriteLog("TOGGLED SIMULATION EDITOR FLYOUT VALUE OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"NEW VALUE IS {this.SimulationEditorFlyout.IsOpen}", LogType.TraceLog);

            // Toggle the content of the sending button
            Button SendButton = (Button)Sender;
            SendButton.Content = this.SimulationEditorFlyout.IsOpen ?
                "Close Editor" : "Setup Simulation";
            this._viewLogger.WriteLog("TOGGLED EDITOR TOGGLE SENDING BUTTON CONTENT VALUES OK!", LogType.InfoLog);
        }
        /// <summary>
        /// Executes a new simulation playback routine
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void ToggleSimulationButton_OnClick(object Sender, RoutedEventArgs E)
        {
            // Start by checking if we have hardware selected for simulations on the hardware view page.
            this._viewLogger.WriteLog("FINDING CURRENTLY SELECTED HARDWARE FOR OUR SIMULATION HOST INSTANCE NOW...", LogType.InfoLog);
            var CurrentHwInfo = FulcrumConstants.FulcrumVehicleConnectionInfoViewModel;
            
            // Now using the given hardware, run our start simulation 
            if (!this.ViewModel.IsSimulationRunning)
            {
                // Invoke this on a new thread
                this.ViewModel.IsSimStarting = true;
                Task.Run(() =>
                {
                    // Stop monitoring and begin simulation reading
                    CurrentHwInfo.StopVehicleMonitoring();
                    this.ViewModel.StartSimulation(CurrentHwInfo.VersionType, CurrentHwInfo.SelectedDLL, CurrentHwInfo.SelectedDevice);
                    this.ViewModel.IsSimStarting = false;
                });

                // Exit out of this method here
                this._viewLogger.WriteLog("STARTED NEW SIMULATION INSTANCE OK!", LogType.InfoLog);
                return;
            }

            // If the simulation was running already, then stop it.
            Task.Run(() =>
            {
                // Stop simulation playback and restart monitoring if needed
                this.ViewModel.StopSimulation();
                CurrentHwInfo.StartVehicleMonitoring();

                // Log done and exit out of this routine
                this._viewLogger.WriteLog("STOPPED SIMULATION SESSION WITHOUT ISSUES!", LogType.WarnLog);
            });
        }
    }
}
