using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation;
using SharpSimulator.SimulationObjects;

namespace InjectorTests
{
    /// <summary>
    /// Structure used to help track testing results and states
    /// </summary>
    public class FulcrumInjectorFile
    {
        // Input file path and name for our log file object 
        public readonly string LogFile;                                       // Full path to our log file
        public readonly string LogFileName;                                   // Name of the log file without the path
        public readonly int LogFileSize;                                      // Size of the log file in bytes
        public readonly int LogFileLength;                                    // Number of lines in the log file 
        public readonly string LogFileContents;                               // Contents of the log file read in
                                                                              
        // Output content and information for file Expressions                
        public string ExpressionsFile { get; private set; }                   // Built output expressions file
        public string ExpressionsFileName { get; private set; }               // Name of the built output expressions file
        public string ExpressionsFileContents { get; private set; }           // Content of the built expressions file
        public int ExpressionsFileSize { get; private set; }                  // Size of the built expressions file
        public int ExpressionsFileLength { get; private set; }                // Length of the built expressions file
        public PassThruExpression[] ExpressionsBuilt { get; private set; }    // Built expressions objects for our log file
                                                                              
        // Output content and information for file Simulations                
        public string SimulationFile { get; private set; }                    // Built output simulations file
        public string SimulationFileName { get; private set; }                // Name of the built output simulations file
        public string SimulationFileContents { get; private set; }            // Content of the built simulations file
        public int SimulationFileSize { get; private set; }                   // Size of the built simulations file
        public int SimulationFileLength { get; private set; }                 // Length of the built simulations file
        public SimulationChannel[] SimulationChannels { get; private set; }   // Built simulations channels for our log file

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of an injector test log file for testing parse routines
        /// </summary>
        /// <param name="InputLogFile">Full path to our input log file instance</param>
        /// <exception cref="FileNotFoundException">Thrown when the input file object is not found</exception>
        public FulcrumInjectorFile(string InputLogFile)
        {
            // Check if the file exists and store it on our instance here
            if (!File.Exists(InputLogFile))
                throw new FileNotFoundException($"Error! Failed to find input file named {InputLogFile}!");

            // Store the log file name and properties now
            this.LogFile = InputLogFile;
            this.LogFileName = Path.GetFileName(this.LogFile);
            this.LogFileContents = File.ReadAllText(this.LogFile);
            this.LogFileLength = this.LogFileContents.Length;
            this.LogFileSize = File.ReadAllBytes(this.LogFile).Length;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a tuple of values holding information about our input log file
        /// </summary>
        /// <returns>The information needed about our input log file object</returns>
        public IEnumerable<Tuple<string, string>> GetLogFileValues()
        {
            // Build and return a new tuple set of objects for the values for our input log file
            return new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Log File", this.LogFile),
                new Tuple<string, string>("Log File Name", this.LogFileName),
                new Tuple<string, string>("Log File Size", ((long)this.LogFileSize).ToFileSize()),
                new Tuple<string, string>("Log File Length", $"{this.LogFileLength} {(this.LogFileLength == 1 ? "Line" : "Lines")}"),
            };
        }
        /// <summary>
        /// Builds a tuple of values holding information about our output expressions file
        /// </summary>
        /// <returns>The information needed about our expressions file object</returns>
        public IEnumerable<Tuple<string, string>> GetExpressionsFileValues()
        {
            // Build and return a new tuple set of objects for the values for our input log file
            return new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Expressions File", this.ExpressionsFile ?? "N/A"),
                new Tuple<string, string>("Expressions File Name", this.ExpressionsFileName ?? "N/A" ),
                new Tuple<string, string>("Expressions File Size", ((long)this.ExpressionsFileSize).ToFileSize()),
                new Tuple<string, string>("Expressions File Length", $"{this.ExpressionsFileLength} {(this.ExpressionsFileLength == 1 ? "Line" : "Lines")}"),
            };
        }
        /// <summary>
        /// Builds a tuple of values holding information about our output simulation file
        /// </summary>
        /// <returns>The information needed about our simulation file object</returns>
        public IEnumerable<Tuple<string, string>> GetSimulationFileValues()
        {
            // Build and return a new tuple set of objects for the values for our input log file
            return new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Simulation File", this.SimulationFile ?? "N/A"),
                new Tuple<string, string>("Simulation File Name", this.SimulationFileName ?? "N/A"),
                new Tuple<string, string>("Simulation File Size", ((long)this.SimulationFileSize).ToFileSize()),
                new Tuple<string, string>("Simulation File Length", $"{this.SimulationFileLength} {(this.SimulationFileLength == 1 ? "Line" : "Lines")}"),
            };
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Takes in a new expression file path and the expressions built from it and stores them on our class
        /// </summary>
        /// <param name="ExpressionsFilePath">Path to the expressions file</param>
        /// <param name="BuiltExpressions">The built expressions values</param>
        public void StoreExpressionsResults(string ExpressionsFilePath, PassThruExpression[] BuiltExpressions)
        {
            // Store the new expressions values on this instance and exit out
            this.ExpressionsBuilt = BuiltExpressions;
            if (!File.Exists(ExpressionsFilePath))
                throw new FileNotFoundException($"Error! Failed to find input file named {ExpressionsFilePath}!");

            // Store the expressions file name and properties now
            this.ExpressionsFile = ExpressionsFilePath;
            this.ExpressionsFileName = Path.GetFileName(this.ExpressionsFile);
            this.ExpressionsFileContents = File.ReadAllText(this.ExpressionsFile);
            this.ExpressionsFileLength = this.ExpressionsFileContents.Length;
            this.ExpressionsFileSize = File.ReadAllBytes(this.ExpressionsFile).Length;
        }
        /// <summary>
        /// Takes in a new expression file path and the simulation built from it and stores them on our class
        /// </summary>
        /// <param name="SimulationFilePath">Path to the simulation file</param>
        /// <param name="BuiltChannels">The built simulation values</param>
        public void StoreSimulationResults(string SimulationFilePath, SimulationChannel[] BuiltChannels)
        {
            // Store the new simulation values on this instance and exit out
            this.SimulationChannels = BuiltChannels;
            if (!File.Exists(SimulationFilePath))
                throw new FileNotFoundException($"Error! Failed to find input file named {SimulationFilePath}!");

            // Store the simulation file name and properties now
            this.SimulationFile = SimulationFilePath;
            this.SimulationFileName = Path.GetFileName(this.SimulationFile);
            this.SimulationFileContents = File.ReadAllText(this.SimulationFile);
            this.SimulationFileLength = this.SimulationFileContents.Length;
            this.SimulationFileSize = File.ReadAllBytes(this.SimulationFile).Length;
        }
    }
}
