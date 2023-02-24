using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.Views
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
        internal FulcrumPipeStatusViewModel ViewModel { get; set; }

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
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = new FulcrumPipeStatusViewModel(this)
            {
                // Store default values for our pipe states
                ReaderPipeState = "Loading...",
                WriterPipeState = "Loading..."
            };

            // Init component. Build new VM object
            InitializeComponent();
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumPipeStatusView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel and setup our pipe watchdogs
            this.DataContext = this.ViewModel;
            Dispatcher.InvokeAsync(() =>
            {
                this.ViewModel.SetupPipeStateWatchdogs();
                this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES AND WATCHDOGS OK!", LogType.InfoLog);
            });
        }
    }
}
