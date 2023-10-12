using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using static FulcrumInjector.FulcrumViewSupport.FulcrumDriveBroker;
using static FulcrumInjector.FulcrumViewSupport.FulcrumUpdater;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters
{
    /// <summary>
    /// JSON Converter for the DriveBroker Configuration configuration objects
    /// </summary>
    internal class DriveExplorerConfigJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert this object or not.
        /// </summary>
        /// <param name="ObjectType">The type of object we're trying to convert</param>
        /// <returns>True if the object can be serialized, false if not</returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(DriveExplorerConfiguration); }
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
            DriveExplorerConfiguration ExplorerConfig = ValueObject as DriveExplorerConfiguration;

            // Build a dynamic output object using the properties of our update configuration
            // Encrypt "client_id", "project_id", "client_secret" 
            var OutputObject = JObject.FromObject(new
            {
                client_id = StringEncryptor.Encrypt(ExplorerConfig.ClientId),
                project_id = StringEncryptor.Encrypt(ExplorerConfig.ProjectId),
                auth_uri = ExplorerConfig.AuthUri,
                token_uri = ExplorerConfig.TokenUri,
                auth_provider_x509_cert_url = ExplorerConfig.AuthProvider,
                client_secret = StringEncryptor.Decrypt(ExplorerConfig.ClientSecret),
                redirect_uris = ExplorerConfig.RedirectUris,
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
            string ClientId = StringEncryptor.Decrypt(InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.ClientId))].Value<string>());
            string ProjectId = StringEncryptor.Decrypt(InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.ProjectId))].Value<string>());
            string AuthUri = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.AuthUri))].Value<string>();
            string TokenUri = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.TokenUri))].Value<string>();
            string AuthProvider = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.AuthProvider))].Value<string>();
            string ClientSecret = StringEncryptor.Decrypt(InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.ClientSecret))].Value<string>());
            string[] RedirectUris = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.RedirectUris))].ToObject<string[]>();

            // Return built output object
            return new DriveExplorerConfiguration()
            {
                // Store the properties of our configuration here and exit out
                ClientId = ClientId,
                ProjectId = ProjectId,
                AuthUri = AuthUri,
                TokenUri = TokenUri, 
                AuthProvider = AuthProvider,
                ClientSecret = ClientSecret,
                RedirectUris = RedirectUris
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
                throw new NullReferenceException($"Error! Could not find JSON property for property {PropName} on type {nameof(DriveExplorerConfiguration)}!");

            // Pull the name of the property attribute and return it out
            return JsonPropAttribute.PropertyName;
        }
    }
}
