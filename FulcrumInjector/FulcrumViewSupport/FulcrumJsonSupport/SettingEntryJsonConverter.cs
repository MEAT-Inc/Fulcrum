using System;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels.SettingsModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpExpressions;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport
{
    /// <summary>
    /// JSON Converter for settings object entries
    /// </summary>
    internal class SettingEntryJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert this object or not.
        /// </summary>
        /// <param name="ObjectType">The type of object we're trying to convert</param>
        /// <returns>True if the object can be serialized, false if not</returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(FulcrumSettingEntryModel); }
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

            // Select the array of paths here.
            string SettingName = InputObject[nameof(FulcrumSettingEntryModel.SettingName)]?.Value<string>();
            object SettingValue = InputObject[nameof(FulcrumSettingEntryModel.SettingValue)]?.Value<object>();
            string SettingDescription = InputObject[nameof(FulcrumSettingEntryModel.SettingDescription)]?.Value<string>();
            Enum.TryParse(InputObject[nameof(FulcrumSettingEntryModel.SettingSection)]?.Value<string>(), out FulcrumSettingsCollection.SettingSectionTypes SettingSection);
            Enum.TryParse(InputObject[nameof(FulcrumSettingEntryModel.SettingControlType)]?.Value<string>(), out FulcrumSettingEntryModel.ControlTypes SettingControlType);

            // Double check that this is NOT a PassThruRegex value. If it is, then we apply a new Regex based on loaded values
            if (SettingName.Contains("Regex") && string.IsNullOrEmpty(SettingValue?.ToString()))
            {
                // If we've got a regex type here, find the needed expression value for it
                string RegexTypeName = SettingName
                    .Replace("Regex", string.Empty)
                    .Replace(" ", string.Empty);

                // Parse the regex type into an enumeration value and pull it from our store here
                var RegexType = (PassThruExpressionTypes)Enum.Parse(typeof(PassThruExpressionTypes), RegexTypeName);
                PassThruExpressionRegex FoundRegex = PassThruExpressionRegex.LoadedExpressions[RegexType];

                // Now using the found regex model object, we can store a new value for the setting entry
                string RegexPattern = FoundRegex.ExpressionRegex.ToString();
                string RegexGroups = $"*GROUPS_({string.Join(",", FoundRegex.ExpressionValueGroups)})*";
                SettingValue = $"{RegexPattern.Trim()} {RegexGroups.Trim()}";
            }

            // Return built output object
            return new FulcrumSettingEntryModel(
                SettingName,            // Name of the setting object
                SettingValue,           // Value of the setting object
                SettingControlType,     // The template type for the setting object
                SettingSection,         // The section this setting belongs to 
                SettingDescription);    // The description for this setting
        }
    }
}
