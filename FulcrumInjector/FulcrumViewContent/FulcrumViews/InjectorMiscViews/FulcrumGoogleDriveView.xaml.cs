using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorMiscViews
{
    /// <summary>
    /// Interaction logic for FulcrumGoogleDriveView.xaml
    /// </summary>
    public partial class FulcrumGoogleDriveView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        internal FulcrumGoogleDriveViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new view object instance for our simulation playback
        /// </summary>
        public FulcrumGoogleDriveView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = FulcrumConstants.FulcrumGoogleDriveViewModel ?? new FulcrumGoogleDriveViewModel(this);

            // Initialize new UI Component
            InitializeComponent();

            // Setup our data context and log information out
            this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR THE GOOGLE DRIVE VIEW OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumGoogleDriveView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Hook in a new event for the button click on the check for updates title button
            FulcrumConstants.FulcrumTitleView.btnAboutTheInjetor.Click += this.ToggleGoogleDriveFlyout_OnClick;
            this._viewLogger.WriteLog("HOOKED IN A NEW EVENT FOR THE ABOUT THIS APP BUTTON ON OUR TITLE VIEW!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button click event for the google drive icon. This will trigger our google drive view.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        internal void ToggleGoogleDriveFlyout_OnClick(object Sender, RoutedEventArgs E)
        {
            // Log processed and show if we have to.
            this._viewLogger.WriteLog("PROCESSED BUTTON CLICK FOR THE GOOGLE DRIVE ICON CORRECTLY!", LogType.WarnLog);
            if (FulcrumConstants.FulcrumMainWindow?.GoogleDriveFlyout == null) { _viewLogger.WriteLog("ERROR! GOOGLE DRIVE FLYOUT IS NULL!", LogType.ErrorLog); }
            else
            {
                // Toggle the information pane
                bool IsOpen = FulcrumConstants.FulcrumMainWindow.GoogleDriveFlyout.IsOpen;
                FulcrumConstants.FulcrumMainWindow.GoogleDriveFlyout.IsOpen = !IsOpen;
                this._viewLogger.WriteLog("PROCESSED VIEW TOGGLE REQUEST FOR GOOGLE DRIVE FLYOUT OK!", LogType.InfoLog);
            }
        }
    }
}
