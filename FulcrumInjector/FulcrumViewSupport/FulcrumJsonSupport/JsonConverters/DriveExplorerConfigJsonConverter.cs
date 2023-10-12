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
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public facing property used to determine if we encrypt the output content of our JSON object or not
        private readonly bool _useEncryption;

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for this JSON converter. Defaults encryption to on for reading and writing JSON values
        /// </summary>
        public DriveExplorerConfigJsonConverter()
        {
            // Store our encryption configuration state 
            this._useEncryption = true;
        }
        /// <summary>
        /// CTOR for this JSON converter. Allows us to specify encryption state
        /// </summary>
        /// <param name="UseEncryption">When true, output is encrypted</param>
        public DriveExplorerConfigJsonConverter(bool UseEncryption = true)
        {
            // Store our encryption configuration state 
            this._useEncryption = UseEncryption;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

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

            // Build a dynamic output object using the properties of our explorer configuration
            // Encrypt "client_id", "project_id", "client_secret" 
            var OutputObject = JObject.FromObject(new
            {
                client_id = this._useEncryption 
                    ? StringEncryptor.Encrypt(ExplorerConfig.ClientId)
                    : ExplorerConfig.ClientId,
                project_id = this._useEncryption 
                    ? StringEncryptor.Encrypt(ExplorerConfig.ProjectId) 
                    : ExplorerConfig.ProjectId,
                auth_uri = ExplorerConfig.AuthUri,
                token_uri = ExplorerConfig.TokenUri,
                auth_provider_x509_cert_url = ExplorerConfig.AuthProvider,
                client_secret = this._useEncryption 
                    ? StringEncryptor.Decrypt(ExplorerConfig.ClientSecret)
                    : ExplorerConfig.ClientSecret,
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
            string ClientId = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.ClientId))].Value<string>();
            string ProjectId = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.ProjectId))].Value<string>();
            string AuthUri = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.AuthUri))].Value<string>();
            string TokenUri = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.TokenUri))].Value<string>();
            string AuthProvider = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.AuthProvider))].Value<string>();
            string ClientSecret = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.ClientSecret))].Value<string>();
            string[] RedirectUris = InputObject[this._findJsonPropName(nameof(DriveExplorerConfiguration.RedirectUris))].ToObject<string[]>();

            // Return built output object
            return new DriveExplorerConfiguration()
            {
                // Store the properties of our configuration here and exit out
                ClientId = this._useEncryption ? StringEncryptor.Decrypt(ClientId) : ClientId,
                ProjectId = this._useEncryption ? StringEncryptor.Decrypt(ProjectId) : ProjectId,
                AuthUri = AuthUri,
                TokenUri = TokenUri, 
                AuthProvider = AuthProvider,
                ClientSecret = this._useEncryption ? StringEncryptor.Decrypt(ClientSecret) : ClientSecret,
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
