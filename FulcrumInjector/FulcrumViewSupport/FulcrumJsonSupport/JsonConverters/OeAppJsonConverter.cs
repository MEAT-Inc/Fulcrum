using System;
using System.IO;
using System.Linq;
using FulcrumInjector.FulcrumViewContent.FulcrumModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters
{
    /// <summary>
    /// JSON Converter for converting input OE Scan App Path objects
    /// </summary>
    internal class OeAppJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if the object can be converted or not.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(FulcrumOeAppModel); }
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
            FulcrumOeAppModel CastApp = (FulcrumOeAppModel)ValueObject;

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
            string AppName = InputObject["OEAppName"].Value<string>();
            string AppVersion = InputObject["OEAppVersion"].Value<string>();
            string AppCommand = InputObject["OEAppCommand"].Value<string>();
            string[] PathSet = JArray.FromObject(InputObject["OEAppPath"]).ToObject<string[]>();

            // Find existing path value.
            string FinalAppPath = PathSet.Any(PathValue => File.Exists(PathValue)) ?
                PathSet.FirstOrDefault(File.Exists) :
                "NOT INSTALLED";

            // Build new app command object.
            AppCommand = FinalAppPath == "NOT INSTALLED" ?
                $"cmd.exe /C echo \"APPLICATION {AppName} IS NOT INSTALLED!\"" :
                !AppCommand.Contains("$OEAppPath$") ? AppCommand : AppCommand.Replace("$OEAppPath$", FinalAppPath);

            // Generate new output app model object.
            FulcrumOeAppModel OutputApp = new FulcrumOeAppModel(AppName, FinalAppPath, AppVersion, AppCommand, PathSet);
            return OutputApp;
        }
    }
}
