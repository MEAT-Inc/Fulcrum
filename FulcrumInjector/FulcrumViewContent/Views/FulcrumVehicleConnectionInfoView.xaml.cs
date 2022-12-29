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
using FulcrumInjector.FulcrumViewContent.ViewModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Views
{
    /// <summary>
    /// Interaction logic for FulcrumConnectedVehicleInfoView.xaml
    /// </summary>
    public partial class FulcrumVehicleConnectionInfoView : UserControl
    {        
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("FulcrumSessionReportingViewLogger", LoggerActions.SubServiceLogger);

        // ViewModel object to bind onto
        public FulcrumVehicleConnectionInfoViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a connected vehicle view control
        /// </summary>
        public FulcrumVehicleConnectionInfoView()
        {
            // Initialize new UI Component
            InitializeComponent();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);

            // Build new ViewModel object
            Dispatcher.InvokeAsync(() => this.ViewModel = new FulcrumVehicleConnectionInfoViewModel());
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumVehicleConnectionInfoView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR VEHICLE CONNECTION INFORMATION OUTPUT OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button control click for when we toggle auto ID on or off manually.
        /// This does NOT control the setting value for it.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void TriggerAutoIdRoutine_OnClick(object Sender, RoutedEventArgs E)
        {
            // Trigger our updating routine.
            this.ViewLogger.WriteLog("ATTEMPTING MANUAL TRIGGER FOR AUTO ID NOW...", LogType.InfoLog);
            Task.Run(() =>
            {
                this.ViewModel.AutoIdRunning = true;
                if (!this.ViewModel.ReadVoltageAndVin()) this.ViewLogger.WriteLog("FAILED TO PULL VIN OR VOLTAGE VALUE!", LogType.ErrorLog);
                else this.ViewLogger.WriteLog("PULLED AND POPULATED NEW VOLTAGE AND VIN VALUES OK!", LogType.InfoLog);
                this.ViewModel.AutoIdRunning = false;

                // Log routine done and exit out.
                this.ViewLogger.WriteLog("ROUTINE COMPLETED! CHECK UI CONTENT AND LOG ENTRIES ABOVE TO SEE HOW THE OUTPUT LOOKS", LogType.InfoLog);
            });
        }
    }
}
