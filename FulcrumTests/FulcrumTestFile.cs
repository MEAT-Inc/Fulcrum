using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation;
using SharpSimulator.SimulationObjects;

namespace InjectorTests
{
    /// <summary>
    /// Structure used to help track testing results and states
    /// </summary>
    internal class FulcrumTestFile
    {
        // Input file path and name for our log file object 
        public readonly string LogFile;             // Full path to our log file
        public readonly string LogFileName;         // Name of the log file without the path
        public readonly int LogFileSize;            // Size of the log file in bytes
        public readonly int LogFileLength;          // Number of lines in the log file 
        public readonly string LogFileContents;     // Contents of the log file read in

        // Processed content objects for our log file instance
        public PassThruExpression[] LogExpressions;          // Built expressions objects for our log file
        public SimulationChannel[] LogSimulationChannels;    // Built simulations channels for our log file

        // Output file names and paths for this log file
        public string ExpressionsFile;              // Built output expressions file
        public string SimulationFile;               // Built output simulations file

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of an injector test log file for testing parse routines
        /// </summary>
        /// <param name="InputLogFile">Full path to our input log file instance</param>
        /// <exception cref="FileNotFoundException">Thrown when the input file object is not found</exception>
        public FulcrumTestFile(string InputLogFile)
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
    }
}
