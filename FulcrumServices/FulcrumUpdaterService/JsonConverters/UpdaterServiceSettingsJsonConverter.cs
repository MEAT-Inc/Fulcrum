using System;
using System.Collections.Generic;
using System.Linq;
using FulcrumEncryption;
using FulcrumJson;
using FulcrumUpdaterService.UpdaterServiceModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumUpdaterService.JsonConverters
{
    /// <summary>
    /// JSON Converter for the Fulcrum update configuration object/class
    /// </summary>
    internal class UpdaterServiceSettingsJsonConverter : EncryptionJsonConverter<UpdaterServiceSettings>
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
        /// CTOR for this JSON converter. Defaults encryption to on for reading and writing JSON values
        /// </summary>
        public UpdaterServiceSettingsJsonConverter() : base(true) { }
        /// <summary>
        /// CTOR for this JSON converter. Allows us to specify encryption state
        /// </summary>
        /// <param name="UseEncryption">When true, output is encrypted</param>
        public UpdaterServiceSettingsJsonConverter(bool UseEncryption = true) : base(UseEncryption) { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, UpdaterServiceSettings ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is UpdaterServiceSettingsJsonConverter CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Build a dynamic output object using the properties of our update configuration
            // Encrypt "UpdaterUserName", "UpdaterSecretKey"
            var OutputObject = JObject.FromObject(new
            {
                ValueObject.ForceUpdateReady,
                ValueObject.UpdaterOrgName,
                ValueObject.UpdaterRepoName, 
                UpdaterUserName = this._useEncryption ? FulcrumEncryptor.Encrypt(ValueObject.UpdaterUserName) : ValueObject.UpdaterUserName,
                UpdaterSecretKey = this._useEncryption ? FulcrumEncryptor.Encrypt(ValueObject.UpdaterSecretKey) : ValueObject.UpdaterSecretKey,
            });

            // Now write this built object and reset our encryption state if needed
            JWriter.WriteRaw(JsonConvert.SerializeObject(OutputObject, Formatting.Indented));
            this._useEncryption =  OriginalEncryptionState; 
        }
        /// <summary>
        /// Reads the JSON object input from a string
        /// </summary>
        /// <param name="JReader">The JReader being used to read our input JSON content</param>
        /// <param name="ObjectType">The type of object we're trying to build form the input JSON</param>
        /// <param name="ExistingValue">An existing object to update values for based on our new object</param>
        /// <param name="JSerializer">Serializer settings for the reader input</param>
        /// <returns>The object built from the input JSON content</returns>
        public override UpdaterServiceSettings ReadJson(JsonReader JReader, Type ObjectType, UpdaterServiceSettings ExistingValue, bool HasExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is UpdaterServiceSettingsJsonConverter CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Read in our properties for the JObject and build a configuration from them
            bool ForceUpdateReady = InputObject[nameof(UpdaterServiceSettings.ForceUpdateReady)].Value<bool>();
            string UpdaterOrgName = InputObject[nameof(UpdaterServiceSettings.UpdaterOrgName)].Value<string>();
            string UpdaterRepoName = InputObject[nameof(UpdaterServiceSettings.UpdaterRepoName)].Value<string>();
            string UpdaterUserName = InputObject[nameof(UpdaterServiceSettings.UpdaterUserName)].Value<string>();
            string UpdaterSecretKey = InputObject[nameof(UpdaterServiceSettings.UpdaterSecretKey)].Value<string>();

            // Build a new output object using our pulled properties
            var OutputObject = new UpdaterServiceSettings()
            {
                // Store the properties of our configuration here and exit out
                ForceUpdateReady = ForceUpdateReady,
                UpdaterOrgName = UpdaterOrgName,
                UpdaterRepoName = UpdaterRepoName,
                UpdaterUserName = this._useEncryption ? FulcrumEncryptor.Decrypt(UpdaterUserName) : UpdaterUserName,
                UpdaterSecretKey = this._useEncryption ? FulcrumEncryptor.Decrypt(UpdaterSecretKey) : UpdaterSecretKey,
            };

            // Reset our encryption state and return the built object
            this._useEncryption = OriginalEncryptionState;
            return OutputObject;
        }
    }
}
