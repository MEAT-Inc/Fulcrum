using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.Properties;
using MahApps.Metro.Controls;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumControls
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
            try
            {
                // Regex match our byte values out here for both text boxes
                byte[] AuthBytes = this._convertKeyString(this.tbAuthKeyValue.Text);
                byte[] CryptoBytes = this._convertKeyString(this.tbCryptoKeyValue.Text);
                this._viewLogger.WriteLog("CONVERTED AND PARSED BOTH AUTH KEY AND CRYPTO KEY VALUES CORRECTLY!", LogType.InfoLog);

                // Now store the byte values on the encryption store and exit out
                if (!EncryptionKeys.SetAuthorizationKey(AuthBytes))
                    throw new InvalidOperationException("Error! Failed to set a new Authorization Key despite a valid parse!");
                if (!EncryptionKeys.SetCryptographicKey(CryptoBytes))
                    throw new InvalidOperationException("Error! Failed to set a new Cryptographic Key despite a valid parse!");

                // Once we've set our key values, we can exit out of this window
                this._viewLogger.WriteLog("STORED NEW AUTHORIZATION AND CRYPTOGRAPHIC KEYS CORRECTLY! CLOSING CONFIGURATION WINDOW NOW...", LogType.InfoLog);
                this.Close();
            }
            catch (Exception StoreKeysEx)
            {
                // Log out the exception thrown
                this._viewLogger.WriteLog("ERROR! FAILED TO STORE ENCRYPTION KEYS!", LogType.ErrorLog);
                this._viewLogger.WriteException("EXCEPTION THROWN IS BEING LOGGED BELOW", StoreKeysEx);

                // Show a message box indicating the provided key value is not valid
                MessageBox.Show(
                    $"Error! Please ensure key values provided are formatted correctly!\n\n{StoreKeysEx.Message}",
                    "Key Configuration Failed!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method used to convert an input string into a byte array
        /// </summary>
        /// <param name="InputKey">The input string we're trying to convert</param>
        /// <returns>The byte array of our input key value</returns>
        private byte[] _convertKeyString(string InputKey)
        {
            // Regex match our hex contents for the input string values here and convert it into a byte array
            MatchCollection ByteMatches = Regex.Matches(InputKey, @"([0-9A-Z]{2})");
            if (ByteMatches.Count == 0) return null; 

            // Store all the bytes pulled as a string and convert it into a byte array
            string KeyText = ByteMatches.Cast<Match>().Aggregate("", (Current, AuthByteMatch) => Current + AuthByteMatch.Value);
            return Enumerable.Range(0, KeyText.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(KeyText.Substring(x, 2), 16))
                .ToArray();
        }
    }
}
