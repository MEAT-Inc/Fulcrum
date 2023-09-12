using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;

// Static using call for a google drive log file model
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels
{
    /// <summary>
    /// Base class model object for a log file being used in the injector application
    /// This holds information about a local log file or a Google Drive log file
    /// </summary>
    internal class LogFileModel
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public facing properties holding information about our log file location
        public string LogFileName { get; private set; }               // Name of the log file with an extension
        public string LogFilePath { get; private set; }               // Path to our log file (fully qualified)
        public string LogFileFolder { get; private set; }             // Folder our log file is found inside of 
        public LogFileTypes LogFileType { get; private set; }         // The type of log file this struct contains
        public LogFileLocations LogLocation { get; private set; }     // Location of the log file object (Drive or Local)     

        // Properties of our log file objects
        public bool LogFileExists
        {
            get
            {
                // Return out based on the type of file built
                if (this is FulcrumLogFileModel LocalLogModel)
                    return LocalLogModel.LogFileExists;
                if (this is DriveLogFileModel DriveLogModel)
                    return DriveLogModel.LogFileExists;

                // Return false if casting fails
                return false;
            }
        }
        public string LogFileSize
        {
            get
            {
                // Return out based on the type of file built
                if (this is FulcrumLogFileModel LocalLogModel)
                    return LocalLogModel.LogFileSize;
                if (this is DriveLogFileModel DriveLogModel)
                    return DriveLogModel.LogFileSize;

                // Return false if casting fails
                return "N/A";
            }
        }

        #endregion // Properties

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
        /// <summary>
        /// Enumeration that holds our different locations for log files
        /// </summary>
        public enum LogFileLocations
        {
            [Description("Local File")] LOCAL_LOG,
            [Description("Google Drive")] DRIVE_LOG,
        }

        #endregion // Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawns a new log file object instance and configures fields/properties of it
        /// </summary>
        /// <param name="InputLogPath">The path to the input log file object</param>
        protected LogFileModel(string InputLogPath)
        {
            // Setup our log file fields and properties now
            this.LogFilePath = InputLogPath;
            this.LogLocation = LogFileLocations.LOCAL_LOG;
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
        /// <summary>
        /// Spawns a new log file object instance and configures fields/properties of it
        /// </summary>
        /// <param name="InputDriveFile">The google drive file object built for our log file</param>
        protected LogFileModel(GoogleDriveFile InputDriveFile)
        {
            // Setup our log file fields and properties now
            this.LogFilePath = InputDriveFile.Name;
            this.LogLocation = LogFileLocations.DRIVE_LOG;
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

    /// <summary>
    /// Class which holds information about a log file path from the local machine
    /// </summary>
    internal class FulcrumLogFileModel : LogFileModel
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields
        #endregion //Fields

        #region Properties

        // Public facing properties holding information about the file and the contents of it
        public bool LogFileExists => File.Exists(this.LogFilePath);
        public string LogFileSize => this.LogFileExists ? new FileInfo(this.LogFilePath).Length.ToFileSize() : "N/A";

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawns a new log file object instance and configures fields/properties of it
        /// </summary>
        /// <param name="InputLogPath">The path to the input log file object</param>
        public FulcrumLogFileModel(string InputLogPath) : base(InputLogPath) { }
    }
    /// <summary>
    /// Class which holds information about a log file path pulled in from our google drive
    /// </summary>
    internal class DriveLogFileModel : LogFileModel
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing field for our log object 
        private readonly GoogleDriveFile _inputLogModel;

        #endregion // Fields

        #region Properties

        // Public facing properties holding information about the log file instance
        public bool LogFileExists => (bool)!this._inputLogModel.Trashed;
        public string LogFileSize => this.LogFileExists ? this._inputLogModel.Size.Value.ToFileSize() : "N/A";

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawns a new log file object instance and configures fields/properties of it
        /// </summary>
        /// <param name="InputLogFile">The google drive log file we're building this log from</param>
        public DriveLogFileModel(GoogleDriveFile InputLogFile) : base(InputLogFile)
        {
            // Store the base log file object 
            this._inputLogModel = InputLogFile;
        }
    }
}
