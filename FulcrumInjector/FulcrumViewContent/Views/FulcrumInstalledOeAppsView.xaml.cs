using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FulcrumInjector.FulcrumViewContent.Views
{
    /// <summary>
    /// Interaction logic for FulcrumInstalledOEAppsView.xaml
    /// </summary>
    public partial class FulcrumInstalledOeAppsView : UserControl
    {
        // Logger object.
      //  private SubServiceLogger ViewLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("InstalledOeAppsViewLogger", LoggerActions.SubServiceLogger);

        // ViewModel object to bind onto
        public FulcrumInstalledOeAppsViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new OE App status view object
        /// </summary>
        public FulcrumInstalledOeAppsView()
        {
            // Initialize new UI Component
            InitializeComponent();
         //   this.ViewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);

            // Build a new ViewModel object
            this.Dispatcher.InvokeAsync(() => this.ViewModel = new FulcrumInstalledOeAppsViewModel());
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumInstalledOeAppsView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;
          //  this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR CURRENTLY INSTALLED OE APPLICATION INFORMATION OUTPUT OK!", LogType.InfoLog);
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
            var SelectedObject = this.ViewModel.InstalledOeApps[SelectedIndexValue];
          //  this.ViewLogger.WriteLog($"PULLED IN NEW SELECTED INDEX VALUE OF AN OE APP AS {SelectedIndexValue}", LogType.InfoLog);
            if (SelectedIndexValue == -1 || SelectedIndexValue > this.ViewModel.InstalledOeApps.Count) {
             //   this.ViewLogger.WriteLog("ERROR! INDEX WAS OUT OF RANGE FOR POSSIBLE OE APP OBJECTS!", LogType.ErrorLog);
                return;
            }

            // Now using this index value, find our current model object.
            this.ViewModel.SetTargetOeApplication(SelectedObject);
        //    this.ViewLogger.WriteLog($"APP OBJECT SELECTED FOR TARGETING IS: {SelectedObject}", LogType.InfoLog);
        //    this.ViewLogger.WriteLog("SELECTED A NEW OE APPLICATION OBJECT OK! READY TO CONTROL IS ASSUMING VALUES FOR THE APP ARE VALID", LogType.WarnLog);

            // Check the view model of our object instance. If Can boot then boot. If can kill then kill
            bool RanCommand = false; bool WasBooted = this.ViewModel.CanBootApp;
            Process BootedProcess = null; FulcrumOeAppModel KilledApplication = null; 
            if (this.ViewModel.CanKillApp) RanCommand = this.ViewModel.KillOeApplication(out KilledApplication);
            else if (this.ViewModel.CanBootApp) RanCommand = this.ViewModel.LaunchOeApplication(out BootedProcess);
            else throw new InvalidOperationException("FAILED TO CONFIGURE START OR KILL COMMANDS OF AN OE APP OBJECT!");

            // Pull in the current object from our sender.
            Button SenderButton = (Button)SendingButton;
            Brush DefaultColor = SenderButton.Background;

            // Now setup temp values for booted or not.
            Task.Run(() =>
            {
                // Invoke via Dispatcher
                Dispatcher.Invoke(() =>
                {
                    // Show our new temp values. Set content based on if the command passed or failed.
                    SenderButton.Content = RanCommand ?
                        WasBooted ?
                            $"Booted {this.ViewModel.RunningAppModel.OEAppName} OK!" :
                            $"Killed {(KilledApplication == null ? "OE Application" : KilledApplication.OEAppName)} OK!" :
                        WasBooted ?
                            $"Failed To Boot OE Application!" :
                            $"Failed To Kill OE Application!";

                    // Set background value here.
                    SenderButton.Click -= this.ControlOeApplicationButton_OnClick;
                    SenderButton.Background = RanCommand ? Brushes.DarkGreen : Brushes.DarkRed;
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
                    SenderButton.Content = RanCommand ?
                        WasBooted ?
                            $"Terminate OE Application" :
                            $"Launch OE Application" :
                        WasBooted ?
                            $"Launch OE Application" :
                            $"Terminate OE Application";

                    // Log information
                  //  this.ViewLogger.WriteLog("RESET SENDING BUTTON CONTENT VALUES OK! RETURNING TO NORMAL OPERATION NOW.", LogType.WarnLog);
                });
            });

            // Log Passed output and return here
          //  this.ViewLogger.WriteLog("BUILT NEW COMMAND INSTANCE FOR OE APP OBJECT OK!", LogType.InfoLog);
          //  this.ViewLogger.WriteLog("TOGGLED CONTENT VALUES, AND TRIGGERED APP METHOD CORRECTLY!", LogType.InfoLog);
        }


        /// <summary>
        /// Event for a double click on an OE Application object.
        /// THIS IS NOT YET DONE!
        /// </summary>
        /// <param name="SendingGrid"></param>
        /// <param name="GridClickedArgs"></param>
        private void OEApplicationMouseDown_Twice(object SendingGrid, MouseButtonEventArgs GridClickedArgs)
        {
            // Check for a double click event action. If not, return out. If it is, show a new flyout object to allow user to modify the app object.
            bool DoubleClick = GridClickedArgs.LeftButton == MouseButtonState.Pressed && GridClickedArgs.ClickCount == 2;
         //   if (DoubleClick) this.ViewLogger.WriteLog("PROCESSED REQUEST TO CHANGE OE APP CONTENT! THIS IS NOT YET BUILT!");
        }
    }
}
