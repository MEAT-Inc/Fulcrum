using System;
using System.IO;
using System.Linq;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport
{
    /// <summary>
    /// JSON Converter for converting input OE Scan App Path objects
    /// </summary>
    internal class FulcrumOeApplicationJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert this object or not.
        /// </summary>
        /// <param name="ObjectType">The type of object we're trying to convert</param>
        /// <returns>True if the object can be serialized, false if not</returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(FulcrumOeApplication); }
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
            FulcrumOeApplication CastApp = ValueObject as FulcrumOeApplication;

            // Build a dynamic output object
            var OutputObject = JObject.FromObject(new
            {
                CastApp.OEAppName,                      // App Name
                CastApp.OEAppVersion,                   // Version Number
                CastApp.OEAppCommand,                   // Launcher command
                OEAppPath = CastApp.OEAppPathList,      // Paths for application
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
            if (InputObject.HasValues == false) { return null; }

            // Select the array of paths here.
            string AppName = InputObject[nameof(FulcrumOeApplication.OEAppName)].Value<string>();
            string AppVersion = InputObject[nameof(FulcrumOeApplication.OEAppVersion)].Value<string>();
            string AppCommand = InputObject[nameof(FulcrumOeApplication.OEAppCommand)].Value<string>();
            string[] PathSet = JArray.FromObject(InputObject[nameof(FulcrumOeApplication.OEAppPath)]).ToObject<string[]>();

            // Find existing path value.
            string FinalAppPath = PathSet.Any(PathValue => File.Exists(PathValue)) ?
                PathSet.FirstOrDefault(File.Exists) :
                "NOT INSTALLED";

            // Build new app command object.
            AppCommand = FinalAppPath == "NOT INSTALLED" ?
                $"cmd.exe /C echo \"APPLICATION {AppName} IS NOT INSTALLED!\"" :
                !AppCommand.Contains("$OEAppPath$") ? AppCommand : AppCommand.Replace("$OEAppPath$", FinalAppPath);

            // Generate new output app model object.
            FulcrumOeApplication OutputApp = new FulcrumOeApplication(AppName, FinalAppPath, AppVersion, AppCommand, PathSet);
            return OutputApp;
        }
    }
}
