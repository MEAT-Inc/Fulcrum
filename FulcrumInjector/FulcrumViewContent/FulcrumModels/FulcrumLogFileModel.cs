using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels
{
    /// <summary>
    /// Internal class holding the values for our different types of log files supported
    /// </summary>
    internal class FulcrumLogFileModel
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger instance for this file model object and the backing fields for properties
        private readonly LogFileType _inputFileType;            // The backing field for our input file type
        private readonly SharpLogger _fileModelLogger;          // Logger instance for this file object

        #endregion //Fields

        #region Properties

        // Public facing properties about the input file object used to spawn this instance
        public string InputLogFile { get; private set; }                        // Path to the input file for this model
        public string InputLogFolder { get; private set; }                      // The path to the folder for our input file
        public LogFileType InputFileType => this._inputFileType;                // The type of log file provided in to this model

        // Public facing properties holding the path values for all of our file types
        public string PassThruLogFile { get; private set; }                     // The set path to our PassThru base log file
        public string ExpressionsFile { get; private set; }                     // The set path to our built Expressions file
        public string SimulationsFile { get; private set; }                     // The set path to our built Simulations file

        // Public facing properties about the log types built/ready to use
        public bool HasPassThruLogFile => File.Exists(this.PassThruLogFile);    // Tells us if the PassThru base log file exists
        public bool HasExpressionsFile => File.Exists(this.ExpressionsFile);    // Tells us if the built Expressions file exists
        public bool HasSimulationsFile => File.Exists(this.SimulationsFile);    // Tells us if the built Simulations file exists

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration that holds our file type information objects
        /// </summary>
        public enum LogFileType
        {
            // The different types of extensions for each file type supported
            [Description("")] UNKNOWN_FILE,                // Default parse value for unknown file extensions
            [Description("txt")] PASSTHRU_FILE,            // Text file extension (Mainly PassThru logs)
            [Description("ptExp")] EXPRESSIONS_FILE,       // Expressions generation output files
            [Description("ptSim")] SIMULATIONS_FILE        // Simulation generation output files (Also JSON)
        }

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for a new file object. Takes in the log file path to open.
        /// Defaults to a PassThru log file unless the Expressions or simulations file type is found
        /// </summary>
        /// <param name="LogFilePath">Path to the log file to load for this model</param>
        /// <exception cref="FileLoadException">Thrown when the file being loaded in does not have a valid extension</exception>
        public FulcrumLogFileModel(string LogFilePath)
        {
            // If the input file is not real, then throw a failure out now
            if (!File.Exists(LogFilePath))
                throw new FileNotFoundException($"Error! Could not find log file {LogFilePath}!");

            // Store our new log file path value and setup a logger instance
            this.InputLogFile = LogFilePath;
            this.InputLogFolder = new FileInfo(this.InputLogFile).DirectoryName;
            string LoggerName = $"LogFileModel_{Path.GetFileNameWithoutExtension(this.InputLogFile)}";
            this._fileModelLogger = new SharpLogger(LoggerActions.UniversalLogger, LoggerName);

            // Try and find our log file type now
            string InputPathExtension = Path.GetExtension(this.InputLogFile);
            if (!Enum.TryParse(InputPathExtension, out this._inputFileType))
            {
                // Store the unknown file type and setup our pass thru base file
                this._inputFileType = LogFileType.UNKNOWN_FILE;
                this._fileModelLogger.WriteLog($"WARNING! FILE {this.InputLogFile} HAS AN INVALID EXTENSION ({InputPathExtension})!", LogType.WarnLog);
            }

            // Now store our log file as a desired file type here
            string InputTypeString = this.InputFileType.ToDescriptionString();
            switch (this.InputFileType)
            {
                // Store the input path as a base log file
                case LogFileType.UNKNOWN_FILE:
                case LogFileType.PASSTHRU_FILE:
                    this.PassThruLogFile = this.InputLogFile;
                    break;

                // Store the input path as expressions file
                case LogFileType.EXPRESSIONS_FILE:
                    this.ExpressionsFile = this.InputLogFile;
                    break;
                
                // Store the input path as a simulation file
                case LogFileType.SIMULATIONS_FILE:
                    this.SimulationsFile = this.InputLogFile;
                    break;
                
                // For default cases, throw a failure since we've got invalid enum values
                default: throw new InvalidEnumArgumentException($"Error! File type {InputTypeString} is not a valid file type!");
            }

            // Log out that we built a new file model object correctly for this file instance
            this._fileModelLogger.WriteLog($"STORED FILE TYPE OF {InputTypeString} FOR INPUT FILE {this.InputLogFile}", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Looks to load in our file content for a given file type if it's real
        /// </summary>
        /// <param name="FileTypeToLoad">The type of log file being loaded in</param>
        /// <returns>The content of our log file requested or nothing if not loaded</returns>
        public string LoadLogContent(LogFileType FileTypeToLoad)
        {
            // Check the file type and load it in now
            this._fileModelLogger.WriteLog($"LOADING LOG FILE CONTENT FOR TYPE {FileTypeToLoad}...");
            string PathToRead = this.InputFileType switch
            {
                LogFileType.UNKNOWN_FILE => this.PassThruLogFile,
                LogFileType.PASSTHRU_FILE => this.PassThruLogFile,
                LogFileType.EXPRESSIONS_FILE => this.ExpressionsFile,
                LogFileType.SIMULATIONS_FILE => this.SimulationsFile,
                _ => throw new ArgumentOutOfRangeException($"Error! File type {FileTypeToLoad} WAS INVALID!")
            };

            // Log out the path we're trying to load in now
            this._fileModelLogger.WriteLog($"ATTEMPTING TO LOAD LOG FILE {PathToRead} NOW...", LogType.TraceLog);
            if (string.IsNullOrWhiteSpace(PathToRead) || !File.Exists(PathToRead))
            {
                // Log that no file content could be loaded in and exit out
                this._fileModelLogger.WriteLog($"ERROR! FILE {PathToRead} COULD NOT BE FOUND ON THE SYSTEM!", LogType.ErrorLog);
                return string.Empty;
            }

            // If we did find the file, read the content of it in now
            string LoadedFileContent = File.ReadAllText(PathToRead);
            int LogFileLength = LoadedFileContent.Split(new[] { "\r", "\n" }, StringSplitOptions.None).Length;
            this._fileModelLogger.WriteLog($"LOADED IN A TOTAL OF {LogFileLength} LOG LINES FOR INPUT FILE {PathToRead}", LogType.InfoLog);

            // Return out the loaded file contents now 
            return LoadedFileContent;
        }
    }
}
