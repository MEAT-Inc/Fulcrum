using FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SharpExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using static FulcrumInjector.FulcrumViewSupport.FulcrumUpdater;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters
{
    /// <summary>
    /// JSON Converter for the Fulcrum update configuration object/class
    /// </summary>
    internal class UpdaterConfigJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert this object or not.
        /// </summary>
        /// <param name="ObjectType">The type of object we're trying to convert</param>
        /// <returns>True if the object can be serialized, false if not</returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(FulcrumUpdaterConfiguration); }
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
            FulcrumUpdaterConfiguration UpdaterConfig = ValueObject as FulcrumUpdaterConfiguration;

            // Build a dynamic output object using the properties of our update configuration
            // Encrypt "UpdaterUserName", "UpdaterSecretKey"
            var OutputObject = JObject.FromObject(new
            {
                UpdaterConfig.ForceUpdateReady,
                UpdaterConfig.UpdaterOrgName,
                UpdaterConfig.UpdaterRepoName, 
                UpdaterUserName = StringEncryptor.Encrypt(UpdaterConfig.UpdaterUserName),
                UpdaterSecretKey = StringEncryptor.Encrypt(UpdaterConfig.UpdaterSecretKey),
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
            bool ForceUpdateReady = InputObject[nameof(FulcrumUpdaterConfiguration.ForceUpdateReady)].Value<bool>();
            string UpdaterOrgName = InputObject[nameof(FulcrumUpdaterConfiguration.UpdaterOrgName)].Value<string>();
            string UpdaterRepoName = InputObject[nameof(FulcrumUpdaterConfiguration.UpdaterRepoName)].Value<string>();
            string UpdaterUserName = StringEncryptor.Decrypt(InputObject[nameof(FulcrumUpdaterConfiguration.UpdaterUserName)].Value<string>());
            string UpdaterSecretKey = StringEncryptor.Decrypt(InputObject[nameof(FulcrumUpdaterConfiguration.UpdaterSecretKey)].Value<string>());

            // Return built output object
            return new FulcrumUpdaterConfiguration()
            {
                // Store the properties of our configuration here and exit out
                UpdaterOrgName = UpdaterOrgName,
                UpdaterRepoName = UpdaterRepoName,
                UpdaterUserName = UpdaterUserName,
                ForceUpdateReady = ForceUpdateReady,
                UpdaterSecretKey = UpdaterSecretKey,
            };
        }
    }
}
