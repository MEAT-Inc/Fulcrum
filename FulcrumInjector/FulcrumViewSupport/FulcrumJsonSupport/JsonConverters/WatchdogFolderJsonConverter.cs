using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.WatchdogModels;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters
{
    /// <summary>
    /// Helper methods for converting our watchdog folder JSON values into objects
    /// </summary>
    internal class WatchdogFolderJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if the object can be converted or not.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(WatchdogFolder); }
        /// <summary>
        /// Writes JSON output
        /// </summary>
        /// <param name="JWriter"></param>
        /// <param name="ValueObject"></param>
        /// <param name="JSerializer"></param>
        public override void WriteJson(JsonWriter JWriter, object? ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }
            WatchdogFolder WatchedFolder = ValueObject as WatchdogFolder;

            // Build a dynamic output object
            var OutputObject = JObject.FromObject(new
            {
                FolderPath = WatchedFolder.WatchedDirectoryPath,
                FileFilters = WatchedFolder.WatchedFileFilters.ToArray()
            });

            // Now write this built object.
            JWriter.WriteRaw(JsonConvert.SerializeObject(OutputObject, Formatting.Indented));
        }
        /// <summary>
        /// Reads the JSON object input from a string
        /// </summary>
        /// <param name="JReader"></param>
        /// <param name="ObjectType"></param>
        /// <param name="ExistingValue"></param>
        /// <param name="JSerializer"></param>
        /// <returns></returns>
        public override object? ReadJson(JsonReader JReader, Type ObjectType, object? ExistingValue, JsonSerializer JSerializer)
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
