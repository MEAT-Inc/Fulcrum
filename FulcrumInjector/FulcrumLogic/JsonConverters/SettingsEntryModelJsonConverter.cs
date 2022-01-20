using System;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumInjector.FulcrumLogic.JsonConverters
{
    /// <summary>
    /// JSON Converter for settings object entries
    /// </summary>
    /// <typeparam name="TSettingType"></typeparam>
    public class SettingsEntryModelJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert this object or not.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType.GetType() == typeof(SettingsEntryModel); }

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
            SettingsEntryModel CastSettingEntry = ValueObject as SettingsEntryModel;

            // Build a dynamic output object
            var OutputObject = JObject.FromObject(new
            {
                CastSettingEntry.SettingName,           // Setting Name
                CastSettingEntry.SettingValue,          // Setting Value
                CastSettingEntry.TypeOfControl,         // Setting UI Control Type
                CastSettingEntry.SettingDescription,    // Description of the setting
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
            if (InputObject.HasValues == false) { return default; }

            // Select the array of paths here.
            string SettingName = InputObject["SettingName"].Value<string>();
            object SettingValue = InputObject["SettingValue"].Value<object>();
            string SettingDescription = InputObject["SettingDescription"].Value<string>();
            Enum.TryParse(InputObject["SettingControlType"].Value<string>(), out ControlTypes SettingControlType);

            // Return built output object
            return new SettingsEntryModel(SettingName, SettingValue, SettingControlType, SettingDescription);
        }
    }
}
