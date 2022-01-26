using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Views
{
    /// <summary>
    /// Interaction logic for FulcrumInstalledOEAppsView.xaml
    /// </summary>
    public partial class FulcrumInstalledOeAppsView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InstalledOeAppsViewLogger")) ?? new SubServiceLogger("InstalledOeAppsViewLogger");

        // ViewModel object to bind onto
        public FulcrumInstalledOeAppsViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new OE App status view object
        /// </summary>
        public FulcrumInstalledOeAppsView()
        {
            // Init component. Build new VM object
            InitializeComponent();
            this.Dispatcher.InvokeAsync(() => this.ViewModel = new FulcrumInstalledOeAppsViewModel());
            this.ViewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
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
            this.ViewLogger.WriteLog($"PULLED IN NEW SELECTED INDEX VALUE OF AN OE APP AS {SelectedIndexValue}", LogType.InfoLog);
            if (SelectedIndexValue == -1 || SelectedIndexValue > this.ViewModel.InstalledOeApps.Count) {
                this.ViewLogger.WriteLog("ERROR! INDEX WAS OUT OF RANGE FOR POSSIBLE OE APP OBJECTS!", LogType.ErrorLog);
                return;
            }

            // Now using this index value, find our current model object.
            this.ViewModel.SetTargetOeApplication(SelectedObject);
            this.ViewLogger.WriteLog($"APP OBJECT SELECTED FOR TARGETING IS: {SelectedObject}", LogType.InfoLog);
            this.ViewLogger.WriteLog("SELECTED A NEW OE APPLICATION OBJECT OK! READY TO CONTROL IS ASSUMING VALUES FOR THE APP ARE VALID", LogType.WarnLog);

            // Check the view model of our object instance. If Can boot then boot. If can kill then kill.
            bool RanCommand = false; bool WasBooted = this.ViewModel.CanBootApp;
            if (this.ViewModel.CanKillApp) RanCommand = this.ViewModel.KillOeApplication();
            else if (this.ViewModel.CanBootApp) RanCommand = this.ViewModel.LaunchOeApplication(out var BuiltProcess);
            else throw new InvalidOperationException("FAILED TO CONFIGURE START OR KILL COMMANDS OF AN OE APP OBJECT!");

            // Pull in the current object from our sender.
            Button SenderButton = (Button)SendingButton;
            Brush DefaultColor = SenderButton.Background;
            string DefaultContent = SenderButton.Content.ToString();

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
                            $"Killed {SelectedObject.OEAppName} OK!" :
                        WasBooted ?
                            $"Failed To Boot {SelectedObject.OEAppName}!" :
                            $"Failed To Kill {SelectedObject.OEAppName}!";

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
                    SenderButton.Content = DefaultContent;
                    SenderButton.Background = DefaultColor;
                    SenderButton.Click += this.ControlOeApplicationButton_OnClick;

                    // Log information
                    this.ViewLogger.WriteLog("RESET SENDING BUTTON CONTENT VALUES OK! RETURNING TO NORMAL OPERATION NOW.", LogType.WarnLog);
                });
            });

            // Log Passed output and return here
            this.ViewLogger.WriteLog("BUILT NEW COMMAND INSTANCE FOR OE APP OBJECT OK!", LogType.InfoLog);
            this.ViewLogger.WriteLog("TOGGLED CONTENT VALUES, AND TRIGGERED APP METHOD CORRECTLY!", LogType.InfoLog);
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
            if (DoubleClick) this.ViewLogger.WriteLog("PROCESSED REQUEST TO CHANGE OE APP CONTENT! THIS IS NOT YET BUILT!");
        }
    }
}
