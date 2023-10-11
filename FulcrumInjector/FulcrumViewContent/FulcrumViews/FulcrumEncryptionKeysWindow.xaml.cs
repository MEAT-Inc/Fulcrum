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
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using FulcrumInjector.FulcrumViewSupport;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews
{
    /// <summary>
    /// Interaction logic for FulcrumEncryptionKeysWindow.xaml
    /// </summary>
    public partial class FulcrumEncryptionKeysWindow : MetroWindow
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new view object instance for our encryption key configuration routines
        /// </summary>
        public FulcrumEncryptionKeysWindow()
        {
            // Initialize new UI Component and configure the logger instance for it
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            InitializeComponent();
        }
        /// <summary>
        /// On loaded, we want to populate our view content with current key values if they exist
        /// </summary>
        /// <param name="SendingWindow">Sending object</param>
        /// <param name="EventArgs">Events attached to it.</param>
        private void FulcrumEncryptionKeysWindow_OnLoaded(object SendingWindow, RoutedEventArgs EventArgs)
        {
            // Pull up our version information and configure our view content
            this._viewLogger.WriteLog("REFRESHING BUILD CONFIGURATION INFORMATION NOW...", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

    }
}
