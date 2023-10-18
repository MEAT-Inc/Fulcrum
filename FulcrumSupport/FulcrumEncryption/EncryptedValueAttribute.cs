using System;

namespace FulcrumEncryption
{
    /// <summary>
    /// Attribute which indicates an encryption state for a property or field on an object
    /// </summary>
    public class EncryptedValueAttribute : Attribute
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public property holding our encryption state
        public bool IsEncrypted { get; private set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new EncryptedValue attribute and sets encryption state
        /// </summary>
        /// <param name="IsEncrypted">When true, values are encrypted. Defaults to true</param>
        public EncryptedValueAttribute(bool IsEncrypted = true)
        {
            // Store encryption state and exit out 
            this.IsEncrypted = IsEncrypted;
        }
    }

}
