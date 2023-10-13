using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumEncryption;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters
{
    /// <summary>
    /// Base class object type used to perform encrypted or decrypted JSON read and write routines
    /// </summary>
    internal class EncryptionJsonConverter<TObjectType> : JsonConverter<TObjectType>
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Protected field which sets if encryption routines are enabled or not for the converter
        protected bool _useEncryption;

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Private enumeration used to set routine types for encrypting or decrypting values
        /// </summary>
        protected enum UpdateTypes
        {
            // Supported update routine types
            NOT_DEFINED,    // NOT DEFINED - Default
            ENCRYPT,        // Encrypt member info values
            DECRYPT         // Decrypt member info values
        }

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for this JSON converter. Defaults encryption to on for reading and writing JSON values
        /// </summary>
        protected EncryptionJsonConverter()
        {
            // Store our encryption configuration state
            this._useEncryption = true;
        }
        /// <summary>
        /// CTOR for this JSON converter. Allows us to specify encryption state
        /// </summary>
        /// <param name="UseEncryption">When true, output is encrypted</param>
        protected EncryptionJsonConverter(bool UseEncryption = true)
        {
            // Store our encryption configuration state
            this._useEncryption = UseEncryption;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, TObjectType ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is EncryptionJsonConverter<TObjectType> CastConverter) this._useEncryption = CastConverter._useEncryption;

            // If Encryption is disabled, just serialize and exit out
            if (!this._useEncryption) {
                JWriter.WriteRaw(JsonConvert.SerializeObject(ValueObject, Formatting.Indented));
                return;
            }

            // If encryption is enabled, run the update routine on the input object and reset our encryption state
            TObjectType EncryptedCopy = this._updateEncryptionMembers(UpdateTypes.ENCRYPT, ValueObject);
            JWriter.WriteRaw(JsonConvert.SerializeObject(EncryptedCopy, Formatting.Indented));
            this._useEncryption = OriginalEncryptionState;
        }
        /// <summary>
        /// Reads the JSON object input from a string
        /// </summary>
        /// <param name="JReader">The JReader being used to read our input JSON content</param>
        /// <param name="ObjectType">The type of object we're trying to build form the input JSON</param>
        /// <param name="ExistingValue">An existing object to update values for based on our new object</param>
        /// <param name="HasExistingValue">Sets if we've got an existing value for the object or not</param>
        /// <param name="JSerializer">Serializer settings for the reader input</param>
        /// <returns>The object built from the input JSON content</returns>
        public override TObjectType ReadJson(JsonReader JReader, Type ObjectType, TObjectType ExistingValue, bool HasExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is EncryptionJsonConverter<TObjectType> CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Based on our encryption state, decrypt property values if needed and store the built object
            TObjectType OutputObject = this._useEncryption 
                ? this._updateEncryptionMembers(UpdateTypes.DECRYPT, InputObject.ToObject<TObjectType>())
                : InputObject.ToObject<TObjectType>();

            // Reset our encryption state and return the object converted
            this._useEncryption = OriginalEncryptionState;
            return OutputObject;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper method used to pull the name of a JSON property from an attribute.
        /// This only looks at the type of object that our JSON Converter supports
        /// </summary>
        /// <param name="PropertyName">Name of the property to pull from</param>
        /// <returns>The name of the JSON property located</returns>
        protected string _findJsonPropName(string PropertyName)
        {
            // Find the JSON property attribute for the given property name
            if (typeof(TObjectType).GetProperties()
                    .Where(PropObj => PropObj.Name.Contains(PropertyName))
                    .Select(PropObj => PropObj.GetCustomAttribute(typeof(JsonPropertyAttribute)))
                    .FirstOrDefault() is not JsonPropertyAttribute JsonPropAttribute)
                throw new NullReferenceException($"Error! Could not find JSON property for property {PropertyName} on type {nameof(TObjectType)}!");

            // Pull the name of the property attribute and return it out
            return JsonPropAttribute.PropertyName;
        }
        /// <summary>
        /// Private helper method used to encrypt properties and fields on an object
        /// </summary>
        /// <param name="ValueObject">The object to encrypt fields and properties on</param>
        /// <returns>The encrypted object</returns>
        /// <exception cref="InvalidDataException">Thrown when a member of the object is invalid</exception>
        protected TObjectType _updateEncryptionMembers(UpdateTypes UpdateType, TObjectType ValueObject)
        {
            // Serialize and deserialize our input object to clone it before modifying values
            string SerializedInputObject = JsonConvert.SerializeObject(ValueObject);
            TObjectType ClonedValueObject = JsonConvert.DeserializeObject<TObjectType>(SerializedInputObject);

            // Find all of our property objects and encrypt their values as needed
            List<MemberInfo> MembersToUpdate = new List<MemberInfo>();
            MembersToUpdate.AddRange((FieldInfo[])typeof(TObjectType)
                .GetFields()
                .Where(FieldObj => FieldObj.GetCustomAttribute<EncryptedValueAttribute>() != null));
            MembersToUpdate.AddRange((PropertyInfo[])typeof(TObjectType)
                .GetProperties()
                .Where(PropObj => PropObj.GetCustomAttribute<EncryptedValueAttribute>() != null));

            // Update all needed properties/fields to be encrypted or decrypted
            foreach (var InputMember in MembersToUpdate)
            {
                // Check if it's a field or property and set our value
                if (InputMember is not PropertyInfo && InputMember is not FieldInfo)
                    throw new InvalidDataException("Error! Can not infer object serialization routine from MemberInfo!");

                // Switch on the type of member being updated and apply encryption/decryption and store the value built
                switch (InputMember)
                {
                    // Update the member as a property info object
                    case PropertyInfo InputProperty:
                    {
                        // Encrypt or decrypt the value and set the property value for our input object
                        string InputValue = InputProperty.GetValue(ClonedValueObject).ToString();
                        string UpdatedValue = UpdateType == UpdateTypes.ENCRYPT 
                            ? FulcrumEncryptor.Encrypt(InputValue)
                            : FulcrumEncryptor.Decrypt(InputValue);

                        // Update the value on our input object here
                        InputProperty.SetValue(ClonedValueObject, UpdatedValue);
                        break;
                    }

                    // Update the member as a field info object
                    case FieldInfo InputField:
                    {
                        // Encrypt or decrypt the value and set the property value for our input object
                        string InputValue = InputField.GetValue(ClonedValueObject).ToString();
                        string UpdatedValue = UpdateType == UpdateTypes.ENCRYPT
                            ? FulcrumEncryptor.Encrypt(InputValue)
                            : FulcrumEncryptor.Decrypt(InputValue);

                        // Update the value on our input object here
                        InputField.SetValue(ClonedValueObject, UpdatedValue);
                        break;
                    }
                }
            }

            // Return the updated cloned copy of our input object here
            return ClonedValueObject;
        }
    }
}
