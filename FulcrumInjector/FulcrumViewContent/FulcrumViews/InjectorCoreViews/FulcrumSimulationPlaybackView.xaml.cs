using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumJson;
using SharpLogging;
using SharpSimulator;
using SharpWrapper.PassThruTypes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews
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
        public FulcrumSimulationPlaybackViewModel ViewModel { get; set; }

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

            // Setup our data context and log information out
            // this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR THE SIMULATION PLAYBACK VIEW OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumSimulationPlaybackView_OnLoaded(object sender, RoutedEventArgs e)
        {
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
                    ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorResources.FulcrumSimulationsPath")
            };

            // Now open the dialog and allow the user to pick some new files.
            this._viewLogger.WriteLog("OPENING NEW DIALOG OBJECT NOW...", LogType.WarnLog);
            if (SelectAttachmentDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK || SelectAttachmentDialog.FileNames.Length == 0) 
            {
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
                if (!LoadResult) this._viewLogger.WriteLog("FAILED TO LOAD NEW SIMULATION FILE! THIS IS FATAL", LogType.ErrorLog); 
                else this._viewLogger.WriteLog("LOADED SIMULATION FILE OK! READY TO PLAYBACK", LogType.InfoLog);

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

                        // If the load routine passed, show the configuration flyout
                        if (LoadResult && !this.SimulationEditorFlyout.IsOpen) 
                            this.ToggleSimulationEditor_OnClick(this.btnToggleSimEditor, null);
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
                "Close Configuration" : "Setup Simulation";
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
                // If the simulation configuration is not defined, open the viewer
                if (this.ViewModel.LoadedConfiguration == null) {
                    this.ToggleSimulationEditor_OnClick(this.btnToggleSimEditor, null);
                    return;
                }

                // Invoke this on a new thread
                Dispatcher.Invoke(() => this.ViewModel.IsSimStarting = true);
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
                this.ViewModel.SimEventsProcessed = Array.Empty<EventArgs>();

                // Log done and exit out of this routine
                this._viewLogger.WriteLog("STOPPED SIMULATION SESSION WITHOUT ISSUES!", LogType.WarnLog);
            });
        }

        /// <summary>
        /// Event handler to fire when the selected simulation configuration is updated/changed
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void cboSimConfiguration_OnSelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            // Pull our selected sim configuration and store it on the view model.
            this.ViewModel.LoadedConfiguration = (PassThruSimulationConfiguration)E.AddedItems[0];
            this.ViewModel.PropertyUpdated(E.AddedItems[0], nameof(this.ViewModel.LoadedConfiguration));
            this._viewLogger.WriteLog($"UPDATED CURRENT SIMULATION CONFIGURATION TO {this.ViewModel.LoadedConfiguration.ConfigurationName}!");
        }

        /// <summary>
        /// Event handler to fire when the user clicks the new configuration button.
        /// This will toggle edit mode and generate a new dummy configuration value set
        /// </summary>
        /// <param name="Sender">Object which fired this event</param>
        /// <param name="E">Arguments fired along with this event</param>
        private void btnNewSimulationConfig_OnClick(object Sender, RoutedEventArgs E)
        {
            // Toggle edit mode on our view model
            this.ViewModel.IsNewConfig = true;
            this.ViewModel.IsEditingConfig = true;
            this._viewLogger.WriteLog("BUILDING AND STORING NEW CONFIGURATION FOR SIMULATION PLAYBACK NOW");

            // Build a new configuration object for the view model to bind onto. Apply config values to it as well
            this.ViewModel.CustomConfiguration = new PassThruSimulationConfiguration("My Configuration") {
                ReaderConfigs = new PassThruStructs.SConfigList(1) {
                    ConfigList = new List<PassThruStructs.SConfig>() {
                        new() {
                            SConfigValue = 1,
                            SConfigParamId = ConfigParamId.CAN_MIXED_FORMAT
                        }
                    }
                }
            };
        }
        /// <summary>
        /// Event handler to fire when the user clicks the edit configuration button.
        /// This will toggle edit mode and show new editing UI controls
        /// </summary>
        /// <param name="Sender">Object which fired this event</param>
        /// <param name="E">Arguments fired along with this event</param>
        private void btnEditSimulationConfig_OnClick(object Sender, RoutedEventArgs E)
        {
            // Toggle edit mode on our view model
            this.ViewModel.IsEditingConfig = true;
            this._viewLogger.WriteLog("TOGGLING EDIT MODE FOR CURRENT SIMULATION OBJECT");
        }

        /// <summary>
        /// Event handler to fire when the user clicks the delete configuration button.
        /// This will toggle edit mode and remove the current configuration routine from our settings store
        /// </summary>
        /// <param name="Sender">Object which fired this event</param>
        /// <param name="E">Arguments fired along with this event</param>
        private void btnDeleteSimulationConfig_OnClick(object Sender, RoutedEventArgs E)
        {
            // TODO: Build logic for removing these configurations
            this.ViewModel.IsEditingConfig = false;
        }
        /// <summary>
        /// Event handler to fire when the user clicks the save configuration button.
        /// This will toggle edit mode and write the current configuration values out to our settings file
        /// </summary>
        /// <param name="Sender">Object which fired this event</param>
        /// <param name="E">Arguments fired along with this event</param>
        private void btnSaveSimulationConfig_OnClick(object Sender, RoutedEventArgs E)
        {
            // TODO: Build logic for saving a new configuration
            this.ViewModel.IsEditingConfig = false;
        }
        /// <summary>
        /// Event handler to fire when the user clicks the discard changes button.
        /// This will toggle edit mode and discard any changes to the configuration currently loaded
        /// </summary>
        /// <param name="Sender">Object which fired this event</param>
        /// <param name="E">Arguments fired along with this event</param>
        private void btnDiscardSimConfigChanges_OnClick(object Sender, RoutedEventArgs E)
        {
            // TODO: Build logic for discarding changes to a configuration
            this.ViewModel.IsEditingConfig = false;
        }

        /// <summary>
        /// Event handler to fire when the user tries to add a new message filter to a configuration
        /// </summary>
        /// <param name="Sender">Object which fired this event</param>
        /// <param name="E">Arguments fired along with this event</param>
        private void btnCreateMessageFilter_OnClick(object Sender, RoutedEventArgs E)
        {
            // TODO: Configure logic for adding filters
        }
        /// <summary>
        /// Event handler to fire when the user tries to delete an existing message filter for a configuration
        /// </summary>
        /// <param name="Sender">Object which fired this event</param>
        /// <param name="E">Arguments fired along with this event</param>
        private void btnDeleteMessageFilter_OnClick(object Sender, RoutedEventArgs E)
        {
            // TODO: Configure logic for removing filters
        }
    }
}
