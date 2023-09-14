using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorMiscViews
{
    /// <summary>
    /// Interaction logic for AboutThisAppView.xaml
    /// </summary>
    public partial class FulcrumAboutThisAppView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        internal FulcrumAboutThisAppViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view showing title information and the text for the version
        /// </summary>
        public FulcrumAboutThisAppView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = new FulcrumAboutThisAppViewModel(this);

            // Initialize new UI component instance
            InitializeComponent();

            // Setup our data context and log information out
            this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("SETUP ABOUT THIS APP VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumAboutThisAppView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Hook in a new event for the button click on the check for updates title button
            FulcrumConstants.FulcrumTitleView.btnAboutTheInjetor.Click += this.ToggleAboutThisApplicationFlyout_OnClick;
            this._viewLogger.WriteLog("HOOKED IN A NEW EVENT FOR THE ABOUT THIS APP BUTTON ON OUR TITLE VIEW!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button click event for the settings gear. This will trigger our session settings view.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void ToggleAboutThisApplicationFlyout_OnClick(object Sender, RoutedEventArgs E)
        {
            // Log processed and show if we have to.
            this._viewLogger.WriteLog("PROCESSED BUTTON CLICK FOR ABOUT THIS APPLICATION ICON CORRECTLY!", LogType.WarnLog);
            if (FulcrumConstants.FulcrumMainWindow?.InformationFlyout == null) { _viewLogger.WriteLog("ERROR! INFORMATION FLYOUT IS NULL!", LogType.ErrorLog); }
            else
            {
                // Toggle the information pane
                bool IsOpen = FulcrumConstants.FulcrumMainWindow.InformationFlyout.IsOpen;
                FulcrumConstants.FulcrumMainWindow.InformationFlyout.IsOpen = !IsOpen;
                this._viewLogger.WriteLog("PROCESSED VIEW TOGGLE REQUEST FOR ABOUT THIS APP FLYOUT OK!", LogType.InfoLog);
            }
        }
    }
}
