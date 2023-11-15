using System;
using System.Linq;
using FulcrumWatchdogService.WatchdogServiceModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumWatchdogService.JsonConverters
{
    /// <summary>
    /// Helper methods for converting our watchdog folder JSON values into objects
    /// </summary>
    internal class WatchdogFolderJsonConverter : JsonConverter<WatchdogFolder>
    {
        /// <summary>
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, WatchdogFolder? ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }
            
            // Build a dynamic output object
            var OutputObject = JObject.FromObject(new
            {
                FolderPath = ValueObject.WatchedDirectoryPath,
                FileFilters = ValueObject.WatchedFileFilters.ToArray()
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
        public override WatchdogFolder ReadJson(JsonReader JReader, Type ObjectType, WatchdogFolder ExistingValue, bool HasExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return null; }

            // Select the array of paths here.
            string WatchedPath = InputObject["FolderPath"].Value<string>();
            string[] WatchedFilters = JArray.FromObject(InputObject["FileFilters"]).ToObject<string[]>();
            
            // Generate new output watchdog folder object.
            WatchdogFolder ParsedFolder = new WatchdogFolder(WatchedPath, WatchedFilters);
            return ParsedFolder;
        }
    }
}
