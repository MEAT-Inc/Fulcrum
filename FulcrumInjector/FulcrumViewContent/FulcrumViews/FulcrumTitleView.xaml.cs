using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews
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
        internal FulcrumTitleViewModel ViewModel { get; set; }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view showing title information and the text for the version
        /// </summary>
        public FulcrumTitleView()
        {
            // Spawn a new logger and setup our view model
            this.ViewModel = new FulcrumTitleViewModel(this);
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Initialize new UI Component
            InitializeComponent();
            
            // Setup a new ViewModel and store our context
            this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("SETUP TITLE VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
    }
}
