using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using MahApps.Metro.Controls;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.Views
{
    /// <summary>
    /// Interaction logic for TitleTextAndQuickActions.xaml
    /// </summary>
    public partial class FulcrumTitleView : UserControl
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Backing fields for view content configuration 
        private readonly SharpLogger _viewLogger;

        #endregion //Fields

        #region Properties

        // Public facing fields for view content configuration
        public FulcrumTitleViewModel ViewModel { get; set; }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view showing title information and the text for the version
        /// </summary>
        public FulcrumTitleView()
        {
            // Initialize new UI Component
            InitializeComponent();
            this.ViewModel = new FulcrumTitleViewModel();
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumTitleView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            ViewModel.SetupViewControl(this);
            DataContext = ViewModel;

            // Log booted title view
            this._viewLogger.WriteLog("SETUP TITLE VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
        }
    }
}
