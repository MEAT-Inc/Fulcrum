using System.Windows.Controls;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels
{
    /// <summary>
    /// View model content for the About view on the injector application
    /// </summary>
    internal class FulcrumAboutThisAppViewModel : FulcrumViewModelBase
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
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="AboutAppUserControl">UserControl which holds the content for our About this app view</param>
        public FulcrumAboutThisAppViewModel(UserControl AboutAppUserControl) : base(AboutAppUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Log information and store values
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("SETTING UP TITLE VIEW BOUND VALUES NOW...", LogType.WarnLog);
        }
    }
}
