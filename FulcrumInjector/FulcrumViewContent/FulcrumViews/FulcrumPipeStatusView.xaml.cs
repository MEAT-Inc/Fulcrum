using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using FulcrumInjector.FulcrumViewSupport;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews
{
    /// <summary>
    /// Interaction logic for FulcrumPipeStatusView.xaml
    /// </summary>
    public partial class FulcrumPipeStatusView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        public FulcrumPipeStatusViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumPipeStatusView()
        {
            // Spawn a new logger and setup our view model
            FulcrumConstants.FulcrumPipeStatusView = this;
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = FulcrumConstants.FulcrumPipeStatusViewModel ??= new FulcrumPipeStatusViewModel(this);

            // Initialize new UI Component
            InitializeComponent();

            // Setup a new ViewModel and setup our pipe watchdogs in a background task
            Task.Run(() => this.ViewModel.SetupPipeStateWatchdogs());
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES AND WATCHDOGS OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
    }
}
