using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews
{
    /// <summary>
    /// Interaction logic for FulcrumConnectedVehicleInfoView.xaml
    /// </summary>
    public partial class FulcrumVehicleConnectionInfoView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        internal FulcrumVehicleConnectionInfoViewModel ViewModel { get; set; }
        
        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a connected vehicle view control
        /// </summary>
        public FulcrumVehicleConnectionInfoView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = new FulcrumVehicleConnectionInfoViewModel(this);

            // Initialize new UI Component
            InitializeComponent();
            this._viewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumVehicleConnectionInfoView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR VEHICLE CONNECTION INFORMATION OUTPUT OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button control click for when we toggle auto ID on or off manually.
        /// This does NOT control the setting value for it.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void TriggerAutoIdRoutine_OnClick(object Sender, RoutedEventArgs E)
        {
            // Trigger our updating routine.
            this._viewLogger.WriteLog("ATTEMPTING MANUAL TRIGGER FOR AUTO ID NOW...", LogType.InfoLog);
            Task.Run(() =>
            {
                this.ViewModel.AutoIdRunning = true;
                if (!this.ViewModel.ReadVoltageAndVin()) this._viewLogger.WriteLog("FAILED TO PULL VIN OR VOLTAGE VALUE!", LogType.ErrorLog);
                else this._viewLogger.WriteLog("PULLED AND POPULATED NEW VOLTAGE AND VIN VALUES OK!", LogType.InfoLog);
                this.ViewModel.AutoIdRunning = false;

                // Log routine done and exit out.
                this._viewLogger.WriteLog("ROUTINE COMPLETED! CHECK UI CONTENT AND LOG ENTRIES ABOVE TO SEE HOW THE OUTPUT LOOKS", LogType.InfoLog);
            });
        }
    }
}
