using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using SharpLogging;
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
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels;
using FulcrumInjector.FulcrumViewSupport;

// Static using for OE application to reduce clutter in code
using FulcrumOeApp = FulcrumInjector.FulcrumViewContent.FulcrumViewModels.FulcrumInstalledOeAppsViewModel.FulcrumOeApplication;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorMiscViews
{
    /// <summary>
    /// Interaction logic for FulcrumEditOeAppView.xaml
    /// </summary>
    public partial class FulcrumEditOeAppView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        public FulcrumEditOeAppViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new edit OE App view object
        /// </summary>
        public FulcrumEditOeAppView()
        {
            // Spawn a new logger and setup our view model
            this.ViewModel = new FulcrumEditOeAppViewModel(this);
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Initialize new UI Component
            InitializeComponent();

            // Setup our data context and log information out
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR OE APPLICATION EDIT WINDOW OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls up a new view for a created OE application and returns it out to the user
        /// </summary>
        /// <returns>The OE application built from this request</returns>
        public FulcrumOeApp CreateNewApplication()
        {
            // Log out that we're resetting our view content and set our app to edit here
            this._viewLogger.WriteLog("RESETTING OE VIEW CONTENT AND SHOWING NEW WINDOW FOR OE APP CREATION...", LogType.WarnLog);
            this.ViewModel.SelectedOeApp = new FulcrumOeApp("Application Name", "N/A");
            return this.ViewModel.SelectedOeApp;
        }
        /// <summary>
        /// Pulls up a new view for our requested OE application to allow us to edit it
        /// </summary>
        /// <param name="ApplicationToEdit">The application we're looking to edit</param>
        /// <returns>True if the application has been updated/changed. False if not</returns>
        public bool SetOeApplication(ref FulcrumOeApp ApplicationToEdit)
        {
            // Log out that we're resetting our view content and set our app to edit here
            this._viewLogger.WriteLog("RESETTING OE EDIT CONTENT AND SHOWING NEW WINDOW FOR EDITING...", LogType.WarnLog);
            this._viewLogger.WriteLog($"OE APPLICATION BEING EDITED: {ApplicationToEdit.OEAppName}");

            // Store the OE application in use on our view model and show this flyout
            this.ViewModel.SelectedOeApp = ApplicationToEdit;
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Event handler to fire when the user tries to save changes from inside the edit OE apps view window
        /// </summary>
        /// <param name="Sender">Sending control for this event</param>
        /// <param name="E">Event arguments fired along with this event</param>
        private void btnSaveOeApp_OnClick(object Sender, RoutedEventArgs E)
        {
            // TODO: Build logic for saving changes or created OE application objects in this class.
            this._viewLogger.WriteLog($"SAVING CHANGES FOR OE APPLICATION {this.ViewModel.SelectedOeApp.OEAppName}...", LogType.WarnLog);
            FulcrumConstants.FulcrumMainWindow.EditOeAppFlyout.IsOpen = false;
        }
        /// <summary>
        /// Event handler to fire when the user tries to close the edit OE apps view window`
        /// </summary>
        /// <param name="Sender">Sending control for this event</param>
        /// <param name="E">Event arguments fired along with this event</param>
        private void btnCloseEditWindow_OnClick(object Sender, RoutedEventArgs E)
        {
            // Close the edit window flyout and exit out
            this._viewLogger.WriteLog("CLOSING EDIT WINDOW FLYOUT WITHOUT SAVING CHANGES NOW...", LogType.WarnLog);
            FulcrumConstants.FulcrumMainWindow.EditOeAppFlyout.IsOpen = false;
        }
    }
}
