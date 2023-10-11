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

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Event handler to fire when the user changes the text in either of the key value text boxes
        /// </summary>
        /// <param name="Sender">Textbox that sent this event</param>
        /// <param name="E">Event args fired along with the text changed event</param>
        private void tbKeyValue_OnChanged(object Sender, TextChangedEventArgs E)
        {
            // Check the state of both text boxes and enable or disable the save button
            this.btnConfirmEncryptionSettings.IsEnabled =
                !string.IsNullOrWhiteSpace(this.tbAuthKeyValue.Text) &&
                !string.IsNullOrWhiteSpace(this.tbCryptoKeyValue.Text);
        }

        /// <summary>
        /// Event handler used to process a save configuration request for our encryption settings
        /// </summary>
        /// <param name="Sender">The sending button that fired this event</param>
        /// <param name="E">EventArgs fired along with the click event</param>
        private void btnConfirmEncryptionSettings_OnClick(object Sender, RoutedEventArgs E)
        {
            // TODO: Configure logic for setting injector encryption values
        }
        /// <summary>
        /// Event handler used to process a close injector request when the user cancels configuration
        /// </summary>
        /// <param name="Sender">The sending button that fired this event</param>
        /// <param name="E">EventArgs fired along with the click event</param>
        private void btnCloseInjectorApplication_OnClick(object Sender, RoutedEventArgs E)
        {
            // Log out that we're just closing this window out and exit the application
            this._viewLogger.WriteLog("PROCESSED AN EXIT REQUEST FROM THE ENCRYPTION CONFIGURATION WINDOW!", LogType.WarnLog);
            this._viewLogger.WriteLog("EXITING APPLICATION WITHOUT CONFIGURING ENCRYPTION KEYS NOW...", LogType.WarnLog);
            Environment.Exit(0);
        }
    }
}
