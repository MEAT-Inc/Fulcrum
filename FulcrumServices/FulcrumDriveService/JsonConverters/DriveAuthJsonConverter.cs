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
    /// JSON Converter for the DriveBroker Authorization configuration objects
    /// </summary>
    public class DriveAuthJsonConverter : EncryptionJsonConverter<DriveAuthorization>
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
        public DriveAuthJsonConverter() : base(true) { }
        /// <summary>
        /// CTOR for this JSON converter. Allows us to specify encryption state
        /// </summary>
        /// <param name="UseEncryption">When true, output is encrypted</param>
        public DriveAuthJsonConverter(bool UseEncryption = true) : base(UseEncryption) { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, DriveAuthorization ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is DriveAuthJsonConverter CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Build a dynamic output object using the properties of our authorization configuration
            // Encrypt "project_id", "private_key_id", "private_key", "client_email", "client_id", "client_x509_cert_url"
            var OutputObject = JObject.FromObject(new
            {
                type = ValueObject.Type,
                project_id = this._useEncryption
                    ? FulcrumEncryptor.Encrypt(ValueObject.ProjectId)
                    : ValueObject.ProjectId,
                private_key_id = this._useEncryption
                    ? FulcrumEncryptor.Encrypt(ValueObject.PrivateKeyId)
                    : ValueObject.PrivateKeyId,
                private_key = this._useEncryption
                    ? FulcrumEncryptor.Encrypt(ValueObject.PrivateKey.Replace("\\n", "\n")).Trim()
                    : ValueObject.PrivateKey.Replace("\\n", "\n").Trim(),
                client_email = this._useEncryption
                    ? FulcrumEncryptor.Encrypt(ValueObject.ClientEmail)
                    : ValueObject.ClientEmail,
                client_id = this._useEncryption 
                    ? FulcrumEncryptor.Encrypt(ValueObject.ClientId)
                    : ValueObject.ClientId,
                auth_uri = ValueObject.AuthUri,
                token_uri = ValueObject.TokenUri,
                auth_provider_x509_cert_url = ValueObject.AuthProviderUrl,
                client_x509_cert_url = this._useEncryption
                    ? FulcrumEncryptor.Encrypt(ValueObject.ClientCertUrl)
                    : ValueObject.ClientCertUrl,
                universe_domain = ValueObject.UniverseDomain,
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
        /// <returns>The object built from the input JSON content</returns>
        public override DriveAuthorization ReadJson(JsonReader JReader, Type ObjectType, DriveAuthorization ExistingValue, bool HasExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is DriveAuthJsonConverter CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Read in our properties for the JObject and build a configuration from them
            string Type = InputObject[this._findJsonPropName(nameof(DriveAuthorization.Type))].Value<string>();
            string ProjectId = InputObject[this._findJsonPropName(nameof(DriveAuthorization.ProjectId))].Value<string>();
            string PrivateKeyId = InputObject[this._findJsonPropName(nameof(DriveAuthorization.PrivateKeyId))].Value<string>();
            string PrivateKey = InputObject[this._findJsonPropName(nameof(DriveAuthorization.PrivateKey))].Value<string>();
            string ClientEmail = InputObject[this._findJsonPropName(nameof(DriveAuthorization.ClientEmail))].Value<string>();
            string ClientId = InputObject[this._findJsonPropName(nameof(DriveAuthorization.ClientId))].Value<string>();
            string AuthUri = InputObject[this._findJsonPropName(nameof(DriveAuthorization.AuthUri))].Value<string>();
            string TokenUri = InputObject[this._findJsonPropName(nameof(DriveAuthorization.TokenUri))].Value<string>();
            string AuthProviderUrl = InputObject[this._findJsonPropName(nameof(DriveAuthorization.AuthProviderUrl))].Value<string>();
            string ClientCertUrl = InputObject[this._findJsonPropName(nameof(DriveAuthorization.ClientCertUrl))].Value<string>();
            string UniverseDomain = InputObject[this._findJsonPropName(nameof(DriveAuthorization.UniverseDomain))].Value<string>();

            // Build a new output object using our pulled properties
            var OutputObject = new DriveAuthorization()
            {
                // Store the properties of our configuration here and exit out
                Type = Type,
                ProjectId = this._useEncryption ? FulcrumEncryptor.Decrypt(ProjectId) : ProjectId,
                PrivateKeyId = this._useEncryption ? FulcrumEncryptor.Decrypt(PrivateKeyId) : PrivateKeyId,
                PrivateKey = (this._useEncryption ? FulcrumEncryptor.Decrypt(PrivateKey) : PrivateKey).Replace("\\n", "\n").Trim(),
                ClientEmail = this._useEncryption ? FulcrumEncryptor.Decrypt(ClientEmail) : ClientEmail,
                ClientId = this._useEncryption ? FulcrumEncryptor.Decrypt(ClientId) : ClientId,
                AuthUri = AuthUri,
                TokenUri = TokenUri,
                AuthProviderUrl = AuthProviderUrl,
                ClientCertUrl = this._useEncryption ? FulcrumEncryptor.Decrypt(ClientCertUrl) : ClientCertUrl,
                UniverseDomain = UniverseDomain
            };

            // Reset our encryption state and return the built object
            this._useEncryption = OriginalEncryptionState;
            return OutputObject;
        }
    }
}
