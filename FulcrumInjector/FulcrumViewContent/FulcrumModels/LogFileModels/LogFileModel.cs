using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels.DriveModels;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels.FulcrumModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using SharpLogging;

// Static using call for a google drive log file model
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels
{
    /// <summary>
    /// Base class model object for a log file being used in the injector application
    /// This holds information about a local log file or a Google Drive log file
    /// </summary>
    public class LogFileModel
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Static shared logger for log file models
        protected static SharpLogger _logFileLogger; 

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
            // Configure a new file logger if needed
            _logFileLogger ??= new SharpLogger(LoggerActions.UniversalLogger);

            // Setup our log file fields and properties now
            this.LogFilePath = InputLogPath;
            this.LogLocation = LogFileLocations.LOCAL_LOG;
            this.LogFileFolder = Path.GetDirectoryName(this.LogFilePath);
            this.LogFileName = Path.GetFileNameWithoutExtension(this.LogFilePath);
            this.LogFileType = Path.GetExtension(this.LogFilePath) switch
            {
                ".txt" => LogFileTypes.PASSTHRU_FILE,
                ".log" => LogFileTypes.PASSTHRU_FILE,
                ".shimLog" => LogFileTypes.PASSTHRU_FILE,
                ".ptExp" => LogFileTypes.EXPRESSIONS_FILE,
                ".ptSim" => LogFileTypes.SIMULATIONS_FILE,
                _ => LogFileTypes.UNKNOWN_FILE
            };
        }
        /// <summary>
        /// Spawns a new log file object instance and configures fields/properties of it
        /// </summary>
        /// <param name="InputDriveFile">The google drive file object built for our log file</param>
        protected LogFileModel(GoogleDriveFile InputDriveFile)
        {
            // Configure a new file logger if needed
            _logFileLogger ??= new SharpLogger(LoggerActions.UniversalLogger);

            // Setup our log file fields and properties now
            this.LogFilePath = InputDriveFile.Name;
            this.LogLocation = LogFileLocations.DRIVE_LOG;
            this.LogFileFolder = Path.GetDirectoryName(this.LogFilePath);
            this.LogFileName = Path.GetFileNameWithoutExtension(this.LogFilePath);
            this.LogFileType = Path.GetExtension(this.LogFilePath) switch
            {
                ".txt" => LogFileTypes.PASSTHRU_FILE,
                ".log" => LogFileTypes.PASSTHRU_FILE,
                ".shimLog" => LogFileTypes.PASSTHRU_FILE,
                ".ptExp" => LogFileTypes.EXPRESSIONS_FILE,
                ".ptSim" => LogFileTypes.SIMULATIONS_FILE,
                _ => LogFileTypes.UNKNOWN_FILE
            };
        }
    }
}
