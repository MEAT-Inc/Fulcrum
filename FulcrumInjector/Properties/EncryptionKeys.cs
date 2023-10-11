using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using SharpLogging;

namespace FulcrumInjector.Properties
{
    /// <summary>
    /// Static class instance holding our encryption key values for encrypting and decrypting string values
    /// 
    /// NOTE: If you're using a public copy of this source code, you will NOT be able to run this program without encryption keys!
    ///       You can get these keys by reaching out to zack.walsh@meatinc.autos. This was done to we can store secrets in the AppSettings.json
    ///       file without risking leaving their contents exposed to the public. Thanks for understanding.
    /// </summary>
    public static class EncryptionKeys
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private static logger object for the encryption keys class
        private static readonly SharpLogger _encryptionKeyLogger;

        #endregion // Fields

        #region Properties

        // Public static key objects for our encryption routines
        public static byte[] AutorizationKey { get; private set; } = 
        {
            // TODO: Insert the pre-defined Authorization key bytes in here!
            // Reach out to zack.walsh@meatinc.autos for these keys if you're helping develop this application!
        };
        public static byte[] CryptographicKey { get; private set; } = 
        {
            // TODO: Insert the pre-defined Cryptographic key bytes in here!
            // Reach out to zack.walsh@meatinc.autos for these keys if you're helping develop this application!
        };

        // Public static properties with helpful configuration information 
        public static bool IsEncryptionConfigured => 
            AutorizationKey.Length == StringEncryptor.KeyByteSize && 
            CryptographicKey.Length > StringEncryptor.KeyByteSize;

        #endregion // Properties

        #region Structs and Classes
        
        /// <summary>
        /// Private enumeration used to determine what type of keys are in use
        /// </summary>
        private enum KeyTypes
        {
            // Authorization and Cryptographic key types
            [Description("UNDEFINED")] NO_KEY_TYPE,
            [Description("AUTHORIZATION")] AUTHORIZATION_KEY, 
            [Description("CRYPTOGRAPHIC")] CRYPTOGRAPHIC_KEY,
        }

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Static CTOR for the encryption keys class
        /// </summary>
        static EncryptionKeys()
        {
            // Configure our logger instance and exit out
            _encryptionKeyLogger = new SharpLogger(LoggerActions.UniversalLogger, "EncryptionKeysLogger");
            _encryptionKeyLogger.WriteLog("ENCRYPTION KEY LOGGER HAS BEEN CONFIGURED!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method used to set our authorization key
        /// </summary>
        /// <param name="AuthKey">The authorization key we want to store</param>
        /// <returns>True if the key is stored. False if it is not</returns>
        public static bool SetAuthorizationKey(byte[] AuthKey)
        {
            // Store the authorization key using the set key helper method
            return _setEncryptionKey(AuthKey, KeyTypes.AUTHORIZATION_KEY);
        }
        /// <summary>
        /// Helper method used to set our cryptographic key
        /// </summary>
        /// <param name="CryptoKey">The cryptographic key we want to store</param>
        /// <returns>True if the key is stored. False if it is not</returns>
        public static bool SetCryptographicKey(byte[] CryptoKey)
        {
            // Store the authorization key using the set key helper method
            return _setEncryptionKey(CryptoKey, KeyTypes.AUTHORIZATION_KEY);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method used to set an encryption key on this static class
        /// </summary>
        /// <param name="EncryptionKey">The encryption key key we want to store</param>
        /// <returns>True if the key is stored. False if it is not</returns>
        private static bool _setEncryptionKey(byte[] EncryptionKey, KeyTypes KeyType)
        {
            // Store the type of key being used here
            string KeyTypeString = KeyType.ToDescriptionString();

            // Check our length for the key value first
            if (EncryptionKey == null) {
                _encryptionKeyLogger.WriteLog($"ERROR! {KeyTypeString} KEY PROVIDED WAS NULL!", LogType.ErrorLog);
                return false;
            }
            if (EncryptionKey.Length != StringEncryptor.KeyByteSize) {
                _encryptionKeyLogger.WriteLog($"ERROR! {KeyTypeString} KEY WAS {EncryptionKey.Length}! KEY SHOULD BE {StringEncryptor.KeyByteSize} BYTES!", LogType.ErrorLog);
                return false;
            }

            // Convert the key to a string value and store it
            string KeyString = BitConverter.ToString(EncryptionKey).Replace("-", ",");
            KeyString = Regex.Replace(KeyString, @"([0-9A-Z]{2})", "0x$1 ");

            // Store they key on the needed property based on the type provided
            switch (KeyType)
            {
                // For Authorization Keys
                case KeyTypes.AUTHORIZATION_KEY:
                    AutorizationKey = EncryptionKey;
                    break;

                // For Cryptographic Keys
                case KeyTypes.CRYPTOGRAPHIC_KEY:
                    CryptographicKey = EncryptionKey; 
                    break;

                // Default case we just return out false
                default:
                    _encryptionKeyLogger.WriteLog($"ERROR! KEY TYPE {KeyType.ToDescriptionString()} IS NOT VALID!");
                    return false;
            }

            // Log out what key we stored and return passed
            _encryptionKeyLogger.WriteLog($"STORED {KeyTypeString} ENCRYPTION KEY: {KeyString}");
            return true;
        }
    }
}
