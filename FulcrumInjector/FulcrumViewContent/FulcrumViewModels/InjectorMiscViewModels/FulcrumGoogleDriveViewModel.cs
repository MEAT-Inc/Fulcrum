using SharpLogging;
using SharpSimulator;
using System;
using System.Windows.Controls;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels
{
    /// <summary>
    /// View model for the Google Drive viewing content used throughout the Injector application
    /// </summary>
    internal class FulcrumGoogleDriveViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new google drive view model
        /// </summary>
        /// <param name="GoogleDriveUserControl">UserControl which holds the content for our google drive view</param>
        public FulcrumGoogleDriveViewModel(UserControl GoogleDriveUserControl) : base(GoogleDriveUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP GOOGLE DRIVE VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Log completed building view model instance and exit out
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }
    }
}
