using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            // Configure pipe instances here.
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR OE APP INSTALLS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Checks the currently selected item on the listbox and sets the button state for controlling OE Apps.
        /// </summary>
        /// <param name="SendingObject"></param>
        /// <param name="SelectionChangedArgs"></param>
        private void InstalledAppsListView_OnSelectionChanged(object SendingObject, SelectionChangedEventArgs SelectionChangedArgs)
        {
            // Pull in the current object from our sender.
            ListBox SendingListBox = (ListBox)SendingObject;
            int SelectedIndexValue = SendingListBox.SelectedIndex;
            this.ViewLogger.WriteLog($"PULLED IN NEW SELECTED INDEX VALUE OF AN OE APP AS {SelectedIndexValue}", LogType.InfoLog);
            if (SelectedIndexValue == -1 || SelectedIndexValue > this.ViewModel.InstalledOeApps.Count) {
                this.ViewLogger.WriteLog("ERROR! INDEX WAS OUT OF RANGE FOR POSSIBLE OE APP OBJECTS!", LogType.ErrorLog);
                return;
            }

            // Now using this index value, find our current model object.
            this.ViewModel.SetCurrentOeApplication(this.ViewModel.InstalledOeApps[SelectedIndexValue]);
            this.ViewLogger.WriteLog("SELECTED A NEW OE APPLICATION OBJECT OK! READY TO CONTROL IS ASSUMING VALUES FOR THE APP ARE VALID", LogType.WarnLog);
            this.ViewLogger.WriteLog($"OE APPLICATION LOADED IS {ViewModel.SelectedAppModel.OEAppName} AT PATH {ViewModel.SelectedAppModel.OEAppPath}");
        }
        /// <summary>
        /// Opens or closes an OE app based on our current selected object.
        /// </summary>
        /// <param name="SendingButton">Button object</param>
        /// <param name="ButtonEventArgs">Event args for the button</param>
        private void ControlOeApplicationButton_OnClick(object SendingButton, RoutedEventArgs ButtonEventArgs)
        {
            // Check the view model of our object instance. If Can boot then boot. If can kill then kill.
            if (this.ViewModel.CanBootApp) {
                this.ViewLogger.WriteLog("BOOTING NEW OE APP OBJECT NOW!", LogType.WarnLog);
                this.ViewModel.LaunchOeApplication(out var BuiltProcess);
                this.ViewLogger.WriteLog($"PROCESS ID FOR APP BUILT: {BuiltProcess.Id}", LogType.InfoLog);
            }

            // If we can kill the app
            if (this.ViewModel.CanKillApp) {
                this.ViewLogger.WriteLog("KILLING RUNNING OE APP INSTANCE NOW...", LogType.WarnLog);
                this.ViewModel.KillOeApplication();
                this.ViewLogger.WriteLog("KILLED OE APP OK!", LogType.InfoLog);
            }

            // If we can't do either of these, then throw an exception
            this.ViewLogger.WriteLog("FAILED TO BUILD NEW COMMAND FOR APP START OR STOP! THIS IS FATAL!", LogType.ErrorLog);
            throw new InvalidOperationException("FAILED TO CONFIGURE START OR KILL COMMANDS OF AN OE APP OBJECT!");
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
