﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using SharpExpressions;
using SharpLogging;
using SharpSimulator;

// Static using for the LogFileTypes enumeration
using LogFileTypes = FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels.FulcrumLogFileModel.LogFileTypes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels
{
    /// <summary>
    /// Internal class holding the values for our different types of log files supported
    /// </summary>
    internal class FulcrumLogFileSet
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger instance for this file model object and the backing fields for properties
        private readonly SharpLogger _fileModelLogger;

        // Backing fields which hold information about built file content
        private PassThruExpression[] _generatedExpressions = Array.Empty<PassThruExpression>();
        private PassThruSimulationChannel[] _generatedChannels = Array.Empty<PassThruSimulationChannel>();

        #endregion //Fields

        #region Properties

        // Public facing properties holding the path values for all of our file types
        public FulcrumLogFileModel PassThruLogFile { get; private set; }                    // The set path to our PassThru base log file
        public FulcrumLogFileModel ExpressionsFile { get; private set; }                    // The set path to our built Expressions file
        public FulcrumLogFileModel SimulationsFile { get; private set; }                    // The set path to our built Simulations file

        // Public facing properties which will hold our expressions and simulations content once generated
        public PassThruExpression[] GeneratedExpressions => this._generatedExpressions;     // Collection of all built expression objects
        public PassThruSimulationChannel[] GeneratedChannels => this._generatedChannels;    // Collection of all built simulation objects

        // Public facing properties holding information about the built output files
        public bool HasPassThruLog => this.PassThruLogFile?.LogFileExists ?? false;         // Tells us if the pass thru log file exists
        public bool HasExpressions => this.ExpressionsFile?.LogFileExists ?? false;         // Tells us if the expressions log file exists
        public bool HasSimulations => this.SimulationsFile?.LogFileExists ?? false;         // Tells us if the simulations log file exists

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for a new file object. Takes in the log file path to open.
        /// Defaults to a PassThru log file unless the Expressions or simulations file type is found
        /// </summary>
        /// <param name="LogFilePath">Path to the log file to load for this model</param>
        /// <exception cref="FileLoadException">Thrown when the file being loaded in does not have a valid extension</exception>
        public FulcrumLogFileSet()
        {
            // Spawn our new logger instance and exit out
            this._fileModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Stores a new log file model value onto our instance for this log file model
        /// </summary>
        /// <param name="LogFileModel">The log model object holding information about a file</param>
        /// <returns>True if the new log file is stored. False if it is not</returns>
        public bool StorePassThruLogFile(FulcrumLogFileModel LogFileModel)
        {
            // Make sure the log file exists and it's not one of our current files
            if (string.IsNullOrWhiteSpace(LogFileModel.LogFilePath))
            {
                // Log that no valid path was given and exit out
                this._fileModelLogger.WriteLog("ERROR! A VALID FILE PATH MUST BE PROVIDED!", LogType.ErrorLog);
                return false;
            }
            if (!File.Exists(LogFileModel.LogFilePath))
            {
                // Log the file isn't real and exit out
                this._fileModelLogger.WriteLog($"ERROR! INPUT LOG FILE {LogFileModel.LogFilePath} DOES NOT EXIST ON OUR SYSTEM!", LogType.ErrorLog);
                return false;
            }

            // Log our file exists and that we're able to use it and then find the file type
            this._fileModelLogger.WriteLog($"ATTEMPTING TO STORE LOG FILE {LogFileModel.LogFilePath} ON A LOG FILE MODEL");
            this._fileModelLogger.WriteLog("FILE PATH HAS BEEN VALIDATED AND CONFIRMED TO EXISTS!", LogType.InfoLog);

            // Now try and find the log file type for the given path extension
            LogFileTypes LogFileType = Path.GetExtension(LogFileModel.LogFilePath) switch
            {
                "txt" => LogFileTypes.PASSTHRU_FILE,
                "ptExp" => LogFileTypes.EXPRESSIONS_FILE,
                "ptSim" => LogFileTypes.SIMULATIONS_FILE,
                _ => LogFileTypes.UNKNOWN_FILE
            };

            // Now based on the type of log file, store it if possible
            this._fileModelLogger.WriteLog($"FOUND FILE TYPE TO BE {LogFileType}! ATTEMPTING TO STORE IT ON OUR INSTANCE NOW...");
            switch (LogFileType)
            {
                // Store the input path as a base log file
                case LogFileTypes.UNKNOWN_FILE:
                case LogFileTypes.PASSTHRU_FILE:
                    this.PassThruLogFile = LogFileModel;
                    this._fileModelLogger.WriteLog("STORED PROVIDED FILE AS A PASSTHRU FILE!", LogType.InfoLog);
                    return true;

                // Store the input path as expressions file
                case LogFileTypes.EXPRESSIONS_FILE:
                    this.ExpressionsFile = LogFileModel;
                    this._fileModelLogger.WriteLog("STORED PROVIDED FILE AS AN EXPRESSIONS FILE!", LogType.InfoLog);
                    return true;

                // Store the input path as a simulation file
                case LogFileTypes.SIMULATIONS_FILE:
                    this.SimulationsFile = LogFileModel;
                    this._fileModelLogger.WriteLog("STORED PROVIDED FILE AS A SIMULATIONS FILE!", LogType.InfoLog);
                    return true;

                // For default cases, throw a failure since we've got invalid enum values
                default: throw new InvalidEnumArgumentException($"Error! File type {LogFileType} is not a valid file type!");
            }
        }
        /// <summary>
        /// Stores a new expressions file path on our log set instance and stores new expressions objects for it
        /// </summary>
        /// <param name="ExpressionsModel">The path to the input expressions file</param>
        /// <param name="BuiltExpressions">The expressions objects built</param>
        /// <returns>True if the expressions file is saved on our instance. False if not</returns>
        public bool StoreExpressionsFile(FulcrumLogFileModel ExpressionsModel, IEnumerable<PassThruExpression> BuiltExpressions)
        {
            // Store the new file path value and update our backing expressions 
            if (!this.StorePassThruLogFile(ExpressionsModel))
            {
                // Log name saving failed and return false
                this._fileModelLogger.WriteLog("ERROR! FAILED TO SAVE OUR NEW EXPRESSIONS FILE NAME TO THIS MODEL SET!", LogType.ErrorLog);
                return false;
            }

            // Return the result of the name store routine
            this._generatedExpressions = BuiltExpressions.ToArray(); 
            this._fileModelLogger.WriteLog($"STORED EXPRESSIONS PATH {ExpressionsModel.LogFilePath} OK!", LogType.InfoLog);
            this._fileModelLogger.WriteLog($"SAVED A TOTAL OF {this._generatedExpressions.Length} EXPRESSION OBJECTS");
            return true;
        }
        /// <summary>
        /// Stores a new simulation file path on our log set instance and stores new simulation channel objects for it
        /// </summary>
        /// <param name="SimulationsModel">The path to the input simulations file</param>
        /// <param name="BuiltSimulations">The simulation channels objects built</param>
        /// <returns>True if the simulations file is saved on our instance. False if not</returns>
        public bool StoreSimulationsFile(FulcrumLogFileModel SimulationsModel, IEnumerable<PassThruSimulationChannel> BuiltSimulations)
        {
            // Store the new file path value and update our backing expressions 
            if (!this.StorePassThruLogFile(SimulationsModel))
            {
                // Log name saving failed and return false
                this._fileModelLogger.WriteLog("ERROR! FAILED TO SAVE OUR NEW EXPRESSIONS FILE NAME TO THIS MODEL SET!", LogType.ErrorLog);
                return false;
            }

            // Return the result of the name store routine
            this._generatedChannels = BuiltSimulations.ToArray();
            this._fileModelLogger.WriteLog($"STORED SIMULATIONS PATH {SimulationsModel.LogFilePath} OK!", LogType.InfoLog);
            this._fileModelLogger.WriteLog($"SAVED A TOTAL OF {this._generatedChannels.Length} SIMULATION CHANNEL OBJECTS");
            return true;
        }
    }
}
