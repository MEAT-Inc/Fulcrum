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
    }
}
