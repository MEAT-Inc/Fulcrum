using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.Models
{
    /// <summary>
    /// Model object of our OE Applications installed on the system.
    /// </summary>
    public class OeApplicationModel
    {
        // Logger object.
        private static SubServiceLogger ModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("OeApplicationModelLogger")) ?? new SubServiceLogger("OeApplicationModelLogger");

        // Properties about an OE Application
        public string OEAppName { get; set; }
        public string OEAppPath { get; set; }
        public string OEAppVersion { get; set; }
        public string OEAppLauncherCommand { get; set; }

        // Bool which checks if this app is usable or not.
        public bool IsAppUsable => File.Exists(OEAppPath);

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns hyphenated string object for this app instance
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return $"{OEAppName} - {OEAppPath} - {OEAppVersion} - {OEAppLauncherCommand}"; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new OE application object from a given set of values.
        /// </summary>
        public OeApplicationModel(string Name, string Path, string Version = "N/A", string BatLaunchCommand = "NO_COMMAND")
        {
            // Store values. Append into our list of models.
            this.OEAppName = Name;
            this.OEAppPath = Path;
            this.OEAppVersion = Version;
            this.OEAppLauncherCommand = BatLaunchCommand;
            ModelLogger.WriteLog($"BUILT NEW OE APP INSTANCE: {this.ToString()}");
        }
    }
}
