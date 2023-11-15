using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using FulcrumInjector.FulcrumViewSupport;
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
            // Spawn a new logger and setup our view model
            FulcrumConstants.FulcrumTitleView = this;
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = FulcrumConstants.FulcrumTitleViewModel ??= new FulcrumTitleViewModel(this);

            // Initialize new UI Component
            InitializeComponent();
            
            // Setup a new ViewModel and store our context
            this._viewLogger.WriteLog("SETUP TITLE VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
    }
}
