using System.IO;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels
{
    /// <summary>
    /// Model object of our OE Applications installed on the system.
    /// </summary>
    [JsonConverter(typeof(OeAppJsonConverter))]
    public class FulcrumOeAppModel
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields
        #endregion //Fields

        #region Properties

        // Properties about an OE Application
        public string OEAppName { get; private set; }
        public string OEAppPath { get; private set; }
        public string OEAppVersion { get; private set; }
        public string OEAppCommand { get; private set; }
        public string[] OEAppPathList { get; private set; }
        public bool IsAppUsable => File.Exists(OEAppPath);

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns hyphenated string object for this app instance
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return $"{OEAppName} - {OEAppPath} - {OEAppVersion} - {OEAppCommand}"; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new OE application object from a given set of values.
        /// </summary>
        public FulcrumOeAppModel(string Name, string Path, string Version = "N/A", string BatLaunchCommand = null, string[] PathSet = null)
        {
            // Store values. Append into our list of models.
            this.OEAppName = Name;
            this.OEAppPath = Path;
            this.OEAppVersion = Version;
            this.OEAppPathList = PathSet ?? new[] { this.OEAppPath };
            this.OEAppCommand = BatLaunchCommand ?? $"cmd.exe /C \"{OEAppPath}\"";
        }
    }
}
