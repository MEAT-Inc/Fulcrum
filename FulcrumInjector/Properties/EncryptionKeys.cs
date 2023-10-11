using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.Properties
{
    /// <summary>
    /// Static class instance holding our encryption key values for encrypting and decrypting string values
    /// NOTE: If you're using a public copy of this source code, you will NOT be able to run this program without encryption keys!
    ///       You can get these keys by reaching out to zack.walsh@meatinc.autos. This was done to we can store secrets in the AppSettings.json
    ///       file without risking leaving their contents exposed to the public. Thanks for understanding.
    /// </summary>
    internal static class EncryptionKeys
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Static readonly fields for byte content/encryption
        public static readonly byte[] AuthKey = new byte[] { };
        public static readonly byte[] CryptoKey = new byte[] { };

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes
    }
}
