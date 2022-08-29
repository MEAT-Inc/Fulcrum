using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for FulcrumNetworkAnalysisView.xaml
    /// </summary>
    public partial class FulcrumNetworkAnalysisView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorNetworkAnalysisViewLogger")) ?? new SubServiceLogger("InjectorNetworkAnalysisViewLogger");

        // ViewModel object to bind onto
        public FulcrumNetworkAnalysisViewModel ViewModel { get; set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new view object instance for our simulation playback
        /// </summary>
        public FulcrumNetworkAnalysisView()
        {
            InitializeComponent();
            this.ViewModel = FulcrumConstants.FulcrumNetworkAnalysisViewModel ?? new FulcrumNetworkAnalysisViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumNetworkAnalysisView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Processes events for when the index of our command object is changed.
        /// </summary>
        /// <param name="Sender">Combobox sending this request</param>
        /// <param name="E">Events fired along with the combobox changed</param>
        private void PassThruCommandComboBox_OnSelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            // Start by checking the index. If it's 0 then disable our execute buttons
            ComboBox SendingComboBox = (ComboBox)Sender;

            // Generate our control set for the selected command object
            this.ViewLogger.WriteLog("SETTING UP COMMAND CONFIG VALUES NOW...", LogType.InfoLog);
            this.ViewModel.CurrentJ2534CommandName = SendingComboBox.SelectedItem.ToString();

            // Store the controls on our items collection inside the viewer object now
            PassThruCommandArgsViewer.ItemsSource = this.ViewModel.GenerateCommandConfigElements();
            this.ViewLogger.WriteLog($"STORED NEW COMMAND NAME: {this.ViewModel.CurrentJ2534CommandName}", LogType.InfoLog);
            this.ViewLogger.WriteLog($"BUILT A TOTAL OF {PassThruCommandArgsViewer.Items.Count} CONTROL SETS FOR OUR COMMAND CONFIG!");
            this.ViewLogger.WriteLog("STORED CONTROLS OK! CONTENT SHOULD BE DISPLAYED ON OUR VIEW NOW...", LogType.InfoLog);
        }
        /// <summary>
        /// Processes a command execution request for the network testing view
        /// </summary>
        /// <param name="Sender">Sending button for this command</param>
        /// <param name="E">Event args fired along with the button click action</param>
        private void ExecutePassThruCommand_Click(object Sender, RoutedEventArgs E)
        {
            // Stop the vehicle monitoring routine on the Connection View if it's currently running
            var CurrentHwInfo = FulcrumConstants.FulcrumVehicleConnectionInfoViewModel;
            bool ShouldMonitor = CurrentHwInfo.IsMonitoring;
            if (ShouldMonitor) CurrentHwInfo.StopVehicleMonitoring();

            // Toggle the sending button to be disabled when the button is clicked
            Button SendingButton = (Button)Sender;
            SendingButton.IsEnabled = false;

            // Get the control objects on the view for our arguments and store their values here
            List<object> CurrentArgValues = new List<object>();
            foreach (var ControlObject in this.PassThruCommandArgsViewer.Items)
            {
                // Get the grid holding the name and value field object
                Grid CastControlGrid = (Grid)ControlObject;
                foreach (var GridChildObject in CastControlGrid.Children)
                {
                    // TODO: FIGURE OUT HOW TO DO REQUIRED OR NOT CHECKING
                    // Find the type of our control and get the value from it.
                    if (GridChildObject.GetType() == typeof(TextBox))
                    {
                        // Cast the control, get the value, store it, and move on.
                        TextBox CastInputBox = (TextBox)GridChildObject;
                        CurrentArgValues.Add(CastInputBox.Text.Trim());
                    }
                    else if (GridChildObject.GetType() == typeof(ComboBox))
                    {
                        // Cast the control, get the value, store it, and move on.
                        ComboBox CastInputBox = (ComboBox)GridChildObject;
                        CurrentArgValues.Add(CastInputBox.SelectedItem.ToString().Trim());
                    }
                    else if (GridChildObject.GetType() == typeof(Grid))
                    {
                        // Cast the control get all the child values and store them as an array
                        Grid CastInputBox = (Grid)GridChildObject;
                        foreach (var ChildGridChild in CastInputBox.Children)
                        {
                            // Get the value and store it   
                            if (ChildGridChild.GetType() == typeof(TextBox))
                            {
                                // Cast the control, get the value, store it, and move on.
                                TextBox ChildGridCastInputBox = (TextBox)GridChildObject;
                                CurrentArgValues.Add(ChildGridCastInputBox.Text.Trim());
                            }
                            else if (ChildGridChild.GetType() == typeof(ComboBox))
                            {
                                // Cast the control, get the value, store it, and move on.
                                ComboBox ChildGridCastInputBox = (ComboBox)GridChildObject;
                                CurrentArgValues.Add(ChildGridCastInputBox.SelectedItem.ToString().Trim());
                            }
                        }
                    }
                    else { throw new InvalidOperationException("INVALID CONTROL TYPE IDENTIFIED! FAILED TO PULL IN ONE OR MORE ARGS!"); }
                }
            }

            // Using this list of controls, invoke the current method using a sharp session object on the view model.
            // TODO: Build logic to invoke args onto the selected method object

            // Reset monitoring if needed here
            if (ShouldMonitor) CurrentHwInfo.StartVehicleMonitoring();
            SendingButton.IsEnabled = true;
        }
    }
}
