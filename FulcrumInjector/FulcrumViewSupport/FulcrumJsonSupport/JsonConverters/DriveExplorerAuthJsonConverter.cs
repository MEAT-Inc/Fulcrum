using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static FulcrumInjector.FulcrumViewSupport.FulcrumDriveBroker;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters
{
    /// <summary>
    /// JSON Converter for the DriveBroker Authorization configuration objects
    /// </summary>
    internal class DriveExplorerAuthJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert this object or not.
        /// </summary>
        /// <param name="ObjectType">The type of object we're trying to convert</param>
        /// <returns>True if the object can be serialized, false if not</returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(DriveExplorerAuthorization); }
        /// <summary>
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, object? ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }
            DriveExplorerAuthorization ExplorerAuth = ValueObject as DriveExplorerAuthorization;

            // Build a dynamic output object using the properties of our update configuration
            // Encrypt "project_id", "private_key_id", "private_key", "client_email", "client_id", "client_x509_cert_url"
            var OutputObject = JObject.FromObject(new
            {
                type = ExplorerAuth.Type,
                project_id = StringEncryptor.Encrypt(ExplorerAuth.ProjectId),
                private_key_id = StringEncryptor.Encrypt(ExplorerAuth.PrivateKeyId.Replace("\\n", string.Empty)),
                private_key = StringEncryptor.Encrypt(ExplorerAuth.PrivateKey),
                client_email = StringEncryptor.Encrypt(ExplorerAuth.ClientEmail),
                client_id = StringEncryptor.Encrypt(ExplorerAuth.ClientId),
                auth_uri = ExplorerAuth.AuthUri,
                token_uri = ExplorerAuth.TokenUri,
                auth_provider_x509_cert_url = ExplorerAuth.AuthProviderUrl,
                client_x509_cert_url = StringEncryptor.Encrypt(ExplorerAuth.ClientCertUrl),
                universe_domain = ExplorerAuth.UniverseDomain,
            });

            // Now write this built object.
            JWriter.WriteRaw(JsonConvert.SerializeObject(OutputObject, Formatting.Indented));
        }
        /// <summary>
        /// Reads the JSON object input from a string
        /// </summary>
        /// <param name="JReader">The JReader being used to read our input JSON content</param>
        /// <param name="ObjectType">The type of object we're trying to build form the input JSON</param>
        /// <param name="ExistingValue">An existing object to update values for based on our new object</param>
        /// <param name="JSerializer">Serializer settings for the reader input</param>
        /// <returns>The object built from the input JSON content</returns>
        public override object? ReadJson(JsonReader JReader, Type ObjectType, object? ExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // Read in our properties for the JObject and build a configuration from them
            string Type = InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.Type))].Value<string>();
            string ProjectId = StringEncryptor.Decrypt(InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.ProjectId))].Value<string>());
            string PrivateKeyId = StringEncryptor.Decrypt(InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.PrivateKeyId))].Value<string>().Replace("\\n", string.Empty));
            string PrivateKey = StringEncryptor.Decrypt(InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.PrivateKey))].Value<string>());
            string ClientEmail = StringEncryptor.Decrypt(InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.ClientEmail))].Value<string>());
            string ClientId = StringEncryptor.Decrypt(InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.ClientId))].Value<string>());
            string AuthUri = InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.AuthUri))].Value<string>();
            string TokenUri = InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.TokenUri))].Value<string>();
            string AuthProviderUrl = InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.AuthProviderUrl))].Value<string>();
            string ClientCertUrl = StringEncryptor.Decrypt(InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.ClientCertUrl))].Value<string>());
            string UniverseDomain = InputObject[this._findJsonPropName(nameof(DriveExplorerAuthorization.UniverseDomain))].Value<string>();

            // Return built output object
            return new DriveExplorerAuthorization()
            {
                // Store the properties of our configuration here and exit out
                Type = Type,
                ProjectId = ProjectId,
                PrivateKeyId = PrivateKeyId,
                PrivateKey = PrivateKey,
                ClientEmail = ClientEmail,
                ClientId = ClientId,
                AuthUri = AuthUri,
                TokenUri = TokenUri,
                AuthProviderUrl = AuthProviderUrl,
                ClientCertUrl = ClientCertUrl,
                UniverseDomain = UniverseDomain
            };
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper method used to pull the name of a JSON property from an attribute.
        /// This only looks at the type of object that our JSON Converter supports
        /// </summary>
        /// <param name="PropName">Name of the property to pull from</param>
        /// <returns>The name of the JSON property located</returns>
        private string _findJsonPropName(string PropName)
        {
            // Find the JSON property attribute for the given property name
            if (typeof(DriveExplorerConfiguration).GetProperties()
                    .Where(PropObj => PropObj.Name.Contains(PropName))
                    .Select(PropObj => PropObj.GetCustomAttribute(typeof(JsonPropertyAttribute)))
                    .FirstOrDefault() is not JsonPropertyAttribute JsonPropAttribute)
                throw new NullReferenceException($"Error! Could not find JSON property for property {PropName} on type {nameof(DriveExplorerAuthorization)}!");

            // Pull the name of the property attribute and return it out
            return JsonPropAttribute.PropertyName;
        }
    }
}
