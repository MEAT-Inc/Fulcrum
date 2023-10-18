using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport
{
    /// <summary>
    /// JSON Converter for the DriveBroker Configuration configuration objects
    /// </summary>
    internal class DriveConfigJsonConverter : EncryptionJsonConverter<DriveConfiguration>
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
        public DriveConfigJsonConverter() : base(true) { }
        /// <summary>
        /// CTOR for this JSON converter. Allows us to specify encryption state
        /// </summary>
        /// <param name="UseEncryption">When true, output is encrypted</param>
        public DriveConfigJsonConverter(bool UseEncryption = true) : base(UseEncryption) { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, DriveConfiguration ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is DriveConfigJsonConverter CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Build a dynamic output object using the properties of our explorer configuration
            // Encrypt "client_id", "project_id", "client_secret" 
            var OutputObject = JObject.FromObject(new
            {
                client_id = this._useEncryption 
                    ? FulcrumEncryptor.Encrypt(ValueObject.ClientId)
                    : ValueObject.ClientId,
                project_id = this._useEncryption 
                    ? FulcrumEncryptor.Encrypt(ValueObject.ProjectId) 
                    : ValueObject.ProjectId,
                auth_uri = ValueObject.AuthUri,
                token_uri = ValueObject.TokenUri,
                auth_provider_x509_cert_url = ValueObject.AuthProvider,
                client_secret = this._useEncryption 
                    ? FulcrumEncryptor.Decrypt(ValueObject.ClientSecret)
                    : ValueObject.ClientSecret,
                redirect_uris = ValueObject.RedirectUris,
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
        public override DriveConfiguration ReadJson(JsonReader JReader, Type ObjectType, DriveConfiguration ExistingValue, bool HasExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is DriveConfigJsonConverter CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Read in our properties for the JObject and build a configuration from them
            string ClientId = InputObject[this._findJsonPropName(nameof(DriveConfiguration.ClientId))].Value<string>();
            string ProjectId = InputObject[this._findJsonPropName(nameof(DriveConfiguration.ProjectId))].Value<string>();
            string AuthUri = InputObject[this._findJsonPropName(nameof(DriveConfiguration.AuthUri))].Value<string>();
            string TokenUri = InputObject[this._findJsonPropName(nameof(DriveConfiguration.TokenUri))].Value<string>();
            string AuthProvider = InputObject[this._findJsonPropName(nameof(DriveConfiguration.AuthProvider))].Value<string>();
            string ClientSecret = InputObject[this._findJsonPropName(nameof(DriveConfiguration.ClientSecret))].Value<string>();
            string[] RedirectUris = InputObject[this._findJsonPropName(nameof(DriveConfiguration.RedirectUris))].ToObject<string[]>();

            // Build a new output object using our pulled properties
            var OutputObject = new DriveConfiguration()
            {
                // Store the properties of our configuration here and exit out
                ClientId = this._useEncryption ? FulcrumEncryptor.Decrypt(ClientId) : ClientId,
                ProjectId = this._useEncryption ? FulcrumEncryptor.Decrypt(ProjectId) : ProjectId,
                AuthUri = AuthUri,
                TokenUri = TokenUri,
                AuthProvider = AuthProvider,
                ClientSecret = this._useEncryption ? FulcrumEncryptor.Decrypt(ClientSecret) : ClientSecret,
                RedirectUris = RedirectUris
            };

            // Reset our encryption state and return the built object
            this._useEncryption = OriginalEncryptionState;
            return OutputObject;
        }
    }
}
