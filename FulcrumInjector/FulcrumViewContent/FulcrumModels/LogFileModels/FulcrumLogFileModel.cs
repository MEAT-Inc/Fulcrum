using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels
{
    /// <summary>
    /// Structure which holds information about a log file path provided into the constructor for it
    /// </summary>
    internal class FulcrumLogFileModel
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Public facing fields holding information about our log file location
        public readonly string LogFileName;             // Name of the log file with an extension
        public readonly string LogFilePath;             // Path to our log file (fully qualified)
        public readonly string LogFileFolder;           // Folder our log file is found inside of 
        public readonly LogFileTypes LogFileType;       // The type of log file this struct contains

        #endregion //Fields

        #region Properties

        // Public facing properties holding information about the file itself
        public bool LogFileExists => File.Exists(this.LogFilePath);
        public FileInfo LogFileInfo => new FileInfo(this.LogFilePath);

        // Public facing properties holding our log file contents
        public string LogFileContents => this.LogFileExists ? File.ReadAllText(this.LogFilePath) : string.Empty;

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration that holds our file type information objects
        /// </summary>
        public enum LogFileTypes
        {
            // The different types of extensions for each file type supported
            [Description("")] UNKNOWN_FILE,                // Default parse value for unknown file extensions
            [Description("txt")] PASSTHRU_FILE,            // Text file extension (Mainly PassThru logs)
            [Description("ptExp")] EXPRESSIONS_FILE,       // Expressions generation output files
            [Description("ptSim")] SIMULATIONS_FILE        // Simulation generation output files (Also JSON)
        }

        #endregion //Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawns a new log file object instance and configures fields/properties of it
        /// </summary>
        /// <param name="InputLogPath">The path to the input log file object</param>
        public FulcrumLogFileModel(string InputLogPath)
        {
            // Setup our log file fields and properties now
            this.LogFilePath = InputLogPath;
            this.LogFileFolder = Path.GetDirectoryName(this.LogFilePath);
            this.LogFileName = Path.GetFileNameWithoutExtension(this.LogFilePath);
            this.LogFileType = Path.GetExtension(this.LogFilePath) switch
            {
                "txt" => LogFileTypes.PASSTHRU_FILE,
                "ptExp" => LogFileTypes.EXPRESSIONS_FILE,
                "ptSim" => LogFileTypes.SIMULATIONS_FILE,
                _ => LogFileTypes.UNKNOWN_FILE
            };
        }
    }
}
