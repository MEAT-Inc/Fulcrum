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
            this.NoCommandConfiguredPlaceholder.Visibility = SendingComboBox.SelectedIndex < 0 ?
                Visibility.Visible : Visibility.Collapsed;
            this.ViewLogger.WriteLog("TOGGLED VISIBILITY OF THE NO COMMAND PLACEHOLDER!", LogType.TraceLog);

            // Generate our control set for the selected command object
            this.ViewLogger.WriteLog("SETTING UP COMMAND CONFIG VALUES NOW...", LogType.InfoLog);
            string CurrentCommandName = SendingComboBox.SelectedItem.ToString();
            var BuiltUIControls =  this.ViewModel.GenerateCommandConfigElements(CurrentCommandName);

            // Store the controls on our items collection inside the viewer object now
            this.PassThruCommandArgsViewer.ItemsSource = BuiltUIControls;
            this.ViewLogger.WriteLog($"BUILT A TOTAL OF {BuiltUIControls.Length} CONTROL SETS FOR OUR COMMAND CONFIG!");
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

            // Now Execute our commands using the args built
            // TODO: BUILD LOGIC FOR RUNNING COMMANDS!
            var PopulatedArgControls = this.PassThruCommandArgsViewer.ItemsSource;

            // Reset monitoring if needed here
            if (ShouldMonitor) CurrentHwInfo.StartVehicleMonitoring();
            SendingButton.IsEnabled = true;
        }
    }
}
