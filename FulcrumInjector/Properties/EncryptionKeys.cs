using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        public static byte[] AutorizationKey { get; private set; } = new byte[]
        {
            // TODO: Insert the pre-defined Authorization key bytes in here!
            // Reach out to zack.walsh@meatinc.autos for these keys if you're helping develop this application!
        };
        public static byte[] CryptographicKey { get; private set; } = new byte[]
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
    }
}
