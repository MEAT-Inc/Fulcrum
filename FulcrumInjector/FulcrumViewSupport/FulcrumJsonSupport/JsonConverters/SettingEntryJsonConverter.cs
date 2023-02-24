using System;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpExpressions;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters
{
    /// <summary>
    /// JSON Converter for settings object entries
    /// </summary>
    internal class SettingEntryJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert this object or not.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(FulcrumSettingEntryModel); }
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
            FulcrumSettingEntryModel castSettingEntry = ValueObject as FulcrumSettingEntryModel;

            // Build a dynamic output object
            string TypeOfControlString = castSettingEntry.TypeOfControl.ToString();
            var OutputObject = JObject.FromObject(new
            {
                castSettingEntry.SettingName,                // Setting Name
                castSettingEntry.SettingValue,               // Setting Value
                castSettingEntry.SettingDescription,         // Description of the setting
                SettingControlType = TypeOfControlString,    // Setting UI Control Type
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
            string SettingName = InputObject["SettingName"]?.Value<string>();
            object SettingValue = InputObject["SettingValue"]?.Value<object>();
            string SettingDescription = InputObject["SettingDescription"]?.Value<string>();
            Enum.TryParse(InputObject["SettingControlType"]?.Value<object>()?.ToString(), out FulcrumSettingEntryModel.ControlTypes SettingControlType);

            // Double check that this is NOT a PassThruRegex value. If it is, then we apply a new Regex based on loaded values
            if (SettingName.Contains("Regex") && string.IsNullOrEmpty(SettingValue?.ToString()))
            {
                // If we've got a regex type here, find the needed expression value for it
                string RegexTypeName = SettingName
                    .Replace("Regex", string.Empty)
                    .Replace(" ", string.Empty);

                // Parse the regex type into an enumeration value and pull it from our store here
                var RegexType = (PassThruExpressionType)Enum.Parse(typeof(PassThruExpressionType), RegexTypeName);
                PassThruExpressionRegex FoundRegex = PassThruExpressionRegex.LoadedExpressions[RegexType];

                // Now using the found regex model object, we can store a new value for the setting entry
                string RegexPattern = FoundRegex.ExpressionRegex.ToString();
                string RegexGroups = $"*GROUPS_({string.Join(",", FoundRegex.ExpressionValueGroups)})*";
                SettingValue = $"{RegexPattern.Trim()} {RegexGroups.Trim()}";
            }

            // Return built output object
            return new FulcrumSettingEntryModel(SettingName, SettingValue, SettingControlType, SettingDescription);
        }
    }
}
