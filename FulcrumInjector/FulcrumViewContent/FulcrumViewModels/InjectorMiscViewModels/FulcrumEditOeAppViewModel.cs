using SharpLogging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

// Static using for OE application to reduce clutter in code
using FulcrumOeApp = FulcrumInjector.FulcrumViewContent.FulcrumViewModels.FulcrumInstalledOeAppsViewModel.FulcrumOeApplication;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels
{
    /// <summary>
    /// View model for the edit OE application flyout called from the OE apps list
    /// </summary>
    public class FulcrumEditOeAppViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for the selected OE application
        private FulcrumOeApp _selectedOeApp;        // The OE application we're modifying or adding

        #endregion // Fields

        #region Properties

        // Public facing properties for our view content to bind onto
        public FulcrumOeApp SelectedOeApp { get => this._selectedOeApp; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="EditOeAppFlyoutView">UserControl which holds the content for our edit OE app view</param>
        public FulcrumEditOeAppViewModel(UserControl EditOeAppFlyoutView) : base(EditOeAppFlyoutView)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP OE APPLICATION EDIT WINDOW VIEW MODEL NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

    }
}
