﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews
{
    /// <summary>
    /// Interaction logic for FulcrumInstalledOEAppsView.xaml
    /// </summary>
    public partial class FulcrumInstalledOeAppsView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        public FulcrumInstalledOeAppsViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new OE App status view object
        /// </summary>
        public FulcrumInstalledOeAppsView()
        {
            // Spawn a new logger and setup our view model
            FulcrumConstants.FulcrumInstalledOeAppsView = this;
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = FulcrumConstants.FulcrumInstalledOeAppsViewModel ??= new FulcrumInstalledOeAppsViewModel(this);

            // Initialize new UI Component
            InitializeComponent();

            // Setup our data context and log information out
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR CURRENTLY INSTALLED OE APPLICATION INFORMATION OUTPUT OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Opens or closes an OE app based on our current selected object.
        /// </summary>
        /// <param name="SendingButton">Button object</param>
        /// <param name="ButtonEventArgs">Event args for the button</param>
        private void ControlOeApplicationButton_OnClick(object SendingButton, RoutedEventArgs ButtonEventArgs)
        {
            // Build selected object output here.
            int SelectedIndexValue = this.InstalledAppsListView.SelectedIndex;
            if (SelectedIndexValue == -1) return;

            // Setup some default content values for states/status values
            bool ExecutedAction = false;
            Process BootedProcess = null;
            string KilledAppNAme = string.Empty;
            this._viewLogger.WriteLog("PROCESSING OE APP CONTROL BUTTON EVENT NOW...");

            // Make sure we're able to either boot or kill the process at this point
            if (!this.ViewModel.CanKillApp && !this.ViewModel.CanBootApp)
                throw new InvalidOperationException("FAILED TO CONFIGURE START OR KILL COMMANDS OF AN OE APP OBJECT!");

            // Check the view model of our object instance. If Can boot then boot. If can kill then kill
            if (this.ViewModel.CanKillApp) {
                ExecutedAction = this.ViewModel.KillOeApplication(out KilledAppNAme);
                this._viewLogger.WriteLog($"KILLED OE APPLICATION {KilledAppNAme} CORRECTLY!");
            }
            else if (this.ViewModel.CanBootApp) {
                ExecutedAction = this.ViewModel.LaunchOeApplication(out BootedProcess);
                this._viewLogger.WriteLog($"BOOTED OE APPLICATION {KilledAppNAme} CORRECTLY!");
            }

            // Pull in the current object from our sender.
            Button SenderButton = (Button)SendingButton;
            Brush DefaultColor = SenderButton.Background;
            bool WasBooted = string.IsNullOrWhiteSpace(KilledAppNAme);

            // Now setup temp values for booted or not.
            Task.Run(() =>
            {
                // Invoke via Dispatcher
                Dispatcher.Invoke(() =>
                {
                    // Show our new temp values. Set content based on if the command passed or failed.
                    SenderButton.Content = ExecutedAction 
                        ? WasBooted 
                            ? $"Booted {this.ViewModel.RunningApp.OEAppName} OK!"
                            : $"Killed {(string.IsNullOrWhiteSpace(KilledAppNAme) ? "OE Application" : KilledAppNAme)} OK!" 
                        : WasBooted 
                            ? $"Failed To Boot {this.ViewModel.SelectedApp.OEAppName}!" 
                            : $"Failed To Kill {this.ViewModel.SelectedApp.OEAppName}!";

                    // Set background value here.
                    SenderButton.Click -= this.ControlOeApplicationButton_OnClick;
                    SenderButton.Background = ExecutedAction ? Brushes.DarkGreen : Brushes.DarkRed;
                });

                // Wait for 3.5 Seconds
                Thread.Sleep(3500);

                // Invoke via Dispatcher
                Dispatcher.Invoke(() =>
                {
                    // Reset button values 
                    SenderButton.Background = DefaultColor;
                    SenderButton.Click += this.ControlOeApplicationButton_OnClick;

                    // Set content values here.  If the command passed, then show the inverse of the last shown state.
                    SenderButton.Content = ExecutedAction 
                        ? WasBooted
                            ? $"Terminate {this.ViewModel.SelectedApp.OEAppName}" 
                            : $"Launch {this.ViewModel.SelectedApp.OEAppName}" 
                        : WasBooted 
                            ? $"Launch {this.ViewModel.SelectedApp.OEAppName}" 
                            : $"Terminate {this.ViewModel.SelectedApp.OEAppName}";
                });
            });
        }
        /// <summary>
        /// Event handler fired when the user selects an object inside the OE Apps list view
        /// </summary>
        /// <param name="Sender">The list view that sent this change</param>
        /// <param name="E">Event args fired along with the event</param>
        private void InstalledAppsListView_OnSelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            // Pull the newly selected OE App model object 
            if (Sender is not ListView SendingListView) return;
            var SelectedApp = SendingListView.SelectedItem as FulcrumInstalledOeAppsViewModel.FulcrumOeApplication;

            // Log out what application is being controlled here
            this._viewLogger.WriteLog(SelectedApp != null
                ? $"SETTING CURRENT OE APP MODEL TO {SelectedApp.OEAppName}"
                : "CLEARING SELECTED OE APPLICATION MODEL AND RESETTING CONTROL BUTTON");

            // Store the app model on the ViewModel and update our button for controlling apps
            this.ViewModel.SetTargetOeApplication(SelectedApp);
            if (SelectedApp == null)
            {
                // If no app is selected, reset our button to idle
                this.btnOeApControl.IsEnabled = false;
                this.btnOeApControl.Content = "Select An OE Application";
                return;
            }
            
            // Update the button content based on the currently selected App model
            this.btnOeApControl.IsEnabled = this.ViewModel.CanBootApp || this.ViewModel.CanKillApp;
            if (this.ViewModel.CanKillApp) this.btnOeApControl.Content = $"Terminate {SelectedApp.OEAppName}";
            else 
            {
                // Show our button state as launch or missing based on boot status
                this.btnOeApControl.Content = this.ViewModel.CanBootApp
                    ? $"Launch {SelectedApp.OEAppName}"
                    : $"{SelectedApp.OEAppName} Is Missing!";
            }
        }

        /// <summary>
        /// Event handler to fire when the user requests to edit the list of installed OE Apps
        /// </summary>
        /// <param name="Sender">Sending control for this event</param>
        /// <param name="E">Event arguments fired along with this event</param>
        private void btnEditOeApps_OnClick(object Sender, RoutedEventArgs E)
        {
            // Check our edit mode state here
            if (this.ViewModel.IsEditMode) {
                this._viewLogger.WriteLog("TURNING OFF EDIT MODE FOR OE APPS NOW...", LogType.InfoLog);
                this.ViewModel.IsEditMode = false;
            }
             
            // Toggle edit mode on our view model
            this._viewLogger.WriteLog("STARTING EDIT MODE ON OUR VIEW MODEL FOR OE APPS NOW...", LogType.InfoLog);
            this.ViewModel.IsEditMode = true;
        }
        /// <summary>
        /// Event handler to fire when the user requests to add a new OE app to our list
        /// </summary>
        /// <param name="Sender">Sending control for this event</param>
        /// <param name="E">Event arguments fired along with this event</param>
        private void btnAddOeApp_OnClick(object Sender, RoutedEventArgs E)
        {
            // Make sure we've got a valid main window to open our edit content from
            if (FulcrumConstants.FulcrumMainWindow == null) {
                this._viewLogger.WriteLog("ERROR! VIEW CONTENT FOR MAIN WINDOW ON OUR CONSTANTS SHARE WAS NULL!", LogType.ErrorLog);
                return;
            }

            // Log out what app is being edited once we've got it selected
            this._viewLogger.WriteLog("TRIGGERING NEW ADD APP ROUTINE FOR AN OE APPLICATION...");
            FulcrumConstants.FulcrumMainWindow.EditOeAppFlyout.IsOpen = true;
            FulcrumConstants.FulcrumMainWindow.EditOeApplicationView.CreateNewApplication();
        }
        /// <summary>
        /// Event handler to fire when the user requests to edit the currently selected OE application
        /// </summary>
        /// <param name="Sender">Sending control for this event</param>
        /// <param name="E">Event arguments fired along with this event</param>
        private void btnEditSelectedApp_OnClick(object Sender, RoutedEventArgs E)
        {
            // Pull our selected OE app and pass it into our edit routine
            var SelectedApp = this.ViewModel.SelectedApp;
            if (SelectedApp == null) {
                this._viewLogger.WriteLog("ERROR! CAN NOT FIRE OE APP EDIT ROUTINE WHEN NO APP IS SELECTED!", LogType.ErrorLog);
                return;
            }

            // Make sure we've got a valid main window to open our edit content from
            if (FulcrumConstants.FulcrumMainWindow == null) {
                this._viewLogger.WriteLog("ERROR! VIEW CONTENT FOR MAIN WINDOW ON OUR CONSTANTS SHARE WAS NULL!", LogType.ErrorLog);
                return;
            }
            // Log out what app is being edited once we've got it selected
            this._viewLogger.WriteLog("TRIGGERING NEW EDIT ROUTINE FOR SELECTED OE APPLICATION...");
            this._viewLogger.WriteLog($"OE APP BEING UPDATED: {SelectedApp.OEAppName}");

            // Open our flyout to show the edit window and trigger it edit the current app selected
            FulcrumConstants.FulcrumMainWindow.EditOeAppFlyout.IsOpen = true;
            if (!FulcrumConstants.FulcrumMainWindow.EditOeApplicationView.SetOeApplication(ref SelectedApp))
            {
                // If the result is false no changes are made. Exit out
                FulcrumConstants.FulcrumMainWindow.EditOeAppFlyout.IsOpen = false;
                this._viewLogger.WriteLog("ERROR! FAILED TO OPEN A NEW EDIT WINDOW FOR AN OE APPLICATION!", LogType.WarnLog);
                this._viewLogger.WriteLog($"RETURNING OUT OF EDIT ROUTINE FOR OE APP {SelectedApp.OEAppName} WITHOUT CHANGES!", LogType.WarnLog);
                return;
            }
        }
        /// <summary>
        /// Event handler to fire when the user requests to delete the currently selected OE application
        /// </summary>
        /// <param name="Sender">Sending control for this event</param>
        /// <param name="E">Event arguments fired along with this event</param>
        private void btnDeleteOeApp_OnClick(object Sender, RoutedEventArgs E)
        {
            // TODO: Build logic for deleting OE applications
            // throw new NotImplementedException();
        }
        /// <summary>
        /// Event handler to fire when the user requests to discard changes to the list of OE apps
        /// </summary>
        /// <param name="Sender">Sending control for this event</param>
        /// <param name="E">Event arguments fired along with this event</param>
        private void btnDiscardOeAppChanges_OnClick(object Sender, RoutedEventArgs E)
        {
            this._viewLogger.WriteLog("TURNING OFF EDIT MODE FOR OE APPS NOW...", LogType.InfoLog);
            this._viewLogger.WriteLog("DISCARDING CHANGES FOR OE APPS LIST NOW...", LogType.InfoLog);
            this.ViewModel.IsEditMode = false;
        }
    }
}
