using System;
using System.Collections.Generic;
using System.Linq;
using FulcrumDriveService.DriveServiceModels;
using FulcrumEncryption;
using FulcrumJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumDriveService.JsonConverters
{
    /// <summary>
    /// JSON Converter for the DriveBroker Configuration configuration objects
    /// </summary>
    internal class DriveServiceSettingsJsonConverter : EncryptionJsonConverter<DriveServiceSettings>
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
        public DriveServiceSettingsJsonConverter() : base(true) { }
        /// <summary>
        /// CTOR for this JSON converter. Allows us to specify encryption state
        /// </summary>
        /// <param name="UseEncryption">When true, output is encrypted</param>
        public DriveServiceSettingsJsonConverter(bool UseEncryption = true) : base(UseEncryption) { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, DriveServiceSettings ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is DriveServiceSettingsJsonConverter CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Build a dynamic output object using the properties of our drive service settings
            // Encrypt "GoogleDriveId"
            var OutputObject = JObject.FromObject(new
            {
                ValueObject.ServiceEnabled,
                ValueObject.ServiceName,
                ValueObject.ApplicationName,
                GoogleDriveId = this._useEncryption
                    ? FulcrumEncryptor.Encrypt(ValueObject.GoogleDriveId)
                    : ValueObject.GoogleDriveId,
                ValueObject.ExplorerConfiguration,
                ValueObject.ExplorerAuthorization
            });

            // Now write this built object and reset our encryption state if needed
            JWriter.WriteRaw(JsonConvert.SerializeObject(OutputObject, Formatting.Indented));
            this._useEncryption = OriginalEncryptionState;
        }
        /// <summary>
        /// Reads the JSON object input from a string
        /// </summary>
        /// <param name="JReader">The JReader being used to read our input JSON content</param>
        /// <param name="ObjectType">The type of object we're trying to build form the input JSON</param>
        /// <param name="ExistingValue">An existing object to update values for based on our new object</param>
        /// <param name="JSerializer">Serializer settings for the reader input</param>
        /// <returns>The object built from the input JSON content</returns>}
        public override DriveServiceSettings ReadJson(JsonReader JReader, Type ObjectType, DriveServiceSettings ExistingValue, bool HasExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is DriveServiceSettingsJsonConverter CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Read in our properties for the JObject and build a configuration from them
            string ServiceName = InputObject[nameof(DriveServiceSettings.ServiceName)].Value<string>();
            bool ServiceEnabled = InputObject[nameof(DriveServiceSettings.ServiceEnabled)].Value<bool>();
            string GoogleDriveId = InputObject[nameof(DriveServiceSettings.GoogleDriveId)].Value<string>();
            string ApplicationName = InputObject[nameof(DriveServiceSettings.ApplicationName)].Value<string>();
            DriveAuthorization DriveAuth = InputObject[nameof(DriveServiceSettings.ExplorerAuthorization)].Value<DriveAuthorization>();
            DriveConfiguration DriveConfig = InputObject[nameof(DriveServiceSettings.ExplorerConfiguration)].Value<DriveConfiguration>();

            // Build a new output object using our pulled properties
            var OutputObject = new DriveServiceSettings()
            {
                ServiceName = ServiceName,
                ServiceEnabled = ServiceEnabled,
                ApplicationName = ApplicationName,
                GoogleDriveId = this._useEncryption ? FulcrumEncryptor.Decrypt(GoogleDriveId) : GoogleDriveId,
                ExplorerAuthorization = DriveAuth,
                ExplorerConfiguration = DriveConfig
            };

            // Reset our encryption state and return the built object
            this._useEncryption = OriginalEncryptionState;
            return OutputObject;
        }
    }
}
