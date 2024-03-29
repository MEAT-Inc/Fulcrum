﻿using System.Collections.Generic;
using System.Linq;
using SharpLogging;
using static FulcrumInjector.FulcrumViewSupport.FulcrumModels.LogFileModels.LogFile;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels.LogFileModels
{
    /// <summary>
    /// Base class model object for a log file set used in the injector application
    /// This holds information about a set of local log files or a Google Drive log file set
    /// </summary>
    public class LogFileSet : Dictionary<LogFileTypes, List<LogFile>>
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Static shared logger for this log file set
        protected static SharpLogger _logSetLogger;         

        // Primary/default/combined log file objects
        private LogFile _passThruLogFile;              // Primary PassThru log file when more than one exists
        private LogFile _expressionsFile;              // Primary expressions file when more than one exists
        private LogFile _simulationsFile;              // Primary simulations file when more than one exists

        #endregion // Fields

        #region Properties

        // Public facing properties holding the path values for all of our file types
        public LogFile PassThruLogFile
        {
            get => this._passThruLogFile;
            protected set
            {
                // Add this log file if needed and set it as our default
                if (!this[LogFileTypes.PASSTHRU_FILE].Contains(value))
                    this[LogFileTypes.PASSTHRU_FILE].Add(value);

                // Store this log file as the default one
                this._passThruLogFile = value; 
            }
        }                    
        public LogFile ExpressionsFile
        {
            get => this._expressionsFile;
            protected set
            {
                // Add this log file if needed and set it as our default
                if (!this[LogFileTypes.EXPRESSIONS_FILE].Contains(value))
                    this[LogFileTypes.EXPRESSIONS_FILE].Add(value);

                // Store this log file as the default one
                this._expressionsFile = value;
            }
        }                    
        public LogFile SimulationsFile
        {
            get => this._simulationsFile;
            protected set
            {
                // Add this log file if needed and set it as our default
                if (!this[LogFileTypes.SIMULATIONS_FILE].Contains(value))
                    this[LogFileTypes.SIMULATIONS_FILE].Add(value);

                // Store this log file as the default one
                this._simulationsFile = value;
            }
        }

        // Public facing properties containing information about our log file sets
        public int PassThruCount => this[LogFileTypes.PASSTHRU_FILE].Count;
        public int ExpressionsCount => this[LogFileTypes.EXPRESSIONS_FILE].Count;
        public int SimulationsCount => this[LogFileTypes.SIMULATIONS_FILE].Count;
        public int TotalLogCount => this.Sum(KeyObj => KeyObj.Value.Count);

        // Collection of all log files stored on the log file set
        public List<LogFile> LogSetFiles
        {
            get
            {
                // Build a combined list of all log files
                List<LogFile> OutputFiles = new List<LogFile>();
                OutputFiles.AddRange(this[LogFileTypes.PASSTHRU_FILE]);
                OutputFiles.AddRange(this[LogFileTypes.EXPRESSIONS_FILE]);
                OutputFiles.AddRange(this[LogFileTypes.SIMULATIONS_FILE]);

                // Return the list of combined log files
                return OutputFiles;
            }
        }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new log file set object.
        /// Sets up a collection for log files to be searched/stored on this object
        /// </summary>
        protected LogFileSet()
        {
            // Spawn our new logger instance
            _logSetLogger ??= new SharpLogger(LoggerActions.UniversalLogger);

            // Configure our dictionary of log types and exit out
            this.Add(LogFileTypes.PASSTHRU_FILE, new List<LogFile>());
            this.Add(LogFileTypes.EXPRESSIONS_FILE, new List<LogFile>());
            this.Add(LogFileTypes.SIMULATIONS_FILE, new List<LogFile>());
            this.Add(LogFileTypes.UNKNOWN_FILE, new List<LogFile>());
        }
    }
}
