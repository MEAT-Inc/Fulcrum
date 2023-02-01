using System.IO;
using System.Linq;
using FulcrumInjector.FulcrumViewSupport.FulcrumJson.JsonConverters;
using Newtonsoft.Json;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Models
{
    /// <summary>
    /// Model object of our OE Applications installed on the system.
    /// </summary>
    [JsonConverter(typeof(OeApplicationJsonConverter))]
    public class OeApplicationModel
    {
        // Logger object.
        private static SubServiceLogger ModelLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("OeApplicationModelLogger", LoggerActions.SubServiceLogger);

        // Properties about an OE Application
        public string OEAppName { get; private set; }
        public string OEAppPath { get; private set; }
        public string OEAppVersion { get; private set; }
        public string OEAppCommand { get; private set; }
        public string[] OEAppPathList { get; private set; }
        public bool IsAppUsable => File.Exists(OEAppPath);

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns hyphenated string object for this app instance
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return $"{OEAppName} - {OEAppPath} - {OEAppVersion} - {OEAppCommand}"; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new OE application object from a given set of values.
        /// </summary>
        public OeApplicationModel(string Name, string Path, string Version = "N/A", string BatLaunchCommand = null, string[] PathSet = null)
        {
            // Store values. Append into our list of models.
            this.OEAppName = Name;
            this.OEAppPath = Path;
            this.OEAppVersion = Version;
            this.OEAppPathList = PathSet ?? new[] { this.OEAppPath };
            this.OEAppCommand = BatLaunchCommand ?? $"cmd.exe /C \"{OEAppPath}\"";

            // Log built new app instance.
            ModelLogger.WriteLog($"BUILT NEW OE APP: {this}", LogType.TraceLog);
        }
    }
}
