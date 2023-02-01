using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpLogger;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpExpressions;
using SharpSimulator;

namespace InjectorTests.FulcrumTests
{
    /// <summary>
    /// Test class fixture used to test the expressions generator objects and routines
    /// </summary>
    [TestClass]
    public class FulcrumSimulationsTests
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// The startup routine which builds and imports all of our test log files for this session
        /// </summary>
        [TestInitialize]
        public void SetupSimulationsTests()
        {
            // Invoke a new logging setup here first
            FulcrumTestHelpers.FulcrumLoggingInit();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Test method for building all expressions files for our input log file objects 
        /// </summary>
        [TestCategory("Simulation Generation")]
        [TestMethod("Generate All Simulations")]
        public void GenerateAllSimulationFiles()
        {
            // Pull in all our file objects to loop through first
            var FulcrumTestFiles = FulcrumTestHelpers.FulcrumInputFileConfig();

            // Loop all of our built test file instances and attempt to split their contents now using a generator object
            string[] LogFileNames = FulcrumTestFiles.Keys.ToArray();
            Parallel.ForEach(LogFileNames, LogFileName =>
            {
                // Build a new generator for the file instance and store the output values
                var TestFileObject = FulcrumTestFiles[LogFileName];
                string LogFileContent = TestFileObject.LogFileContents;

                // Build our expressions files now for each file instance
                var ExpGenerator = PassThruExpressionsGenerator.LoadPassThruLogFile(TestFileObject.LogFile);
                var BuiltExpressions = ExpGenerator.GenerateLogExpressions();
                var ExpressionsFileName = ExpGenerator.SaveExpressionsFile(TestFileObject.LogFile);

                // Now build a simulation generator and invoke the build routine
                var SimGenerator = new PassThruSimulationGenerator(TestFileObject.LogFileName, BuiltExpressions);
                var BuiltSimulationChannels = SimGenerator.GenerateLogSimulation();
                var SimulationFileName = SimGenerator.SaveSimulationFile(TestFileObject.LogFile);

                // Check some conditions for the simulation file routine
                Assert.IsTrue(File.Exists(ExpressionsFileName));
                Assert.IsTrue(File.Exists(SimulationFileName));
                Assert.IsTrue(BuiltExpressions != null && BuiltExpressions.Length != 0);
                Assert.IsTrue(BuiltSimulationChannels != null && BuiltSimulationChannels.Length != 0);

                // Lock the collection of log file objects and update it
                lock (FulcrumTestFiles)
                {
                    // Store the expression generation results and the simulation generation results
                    FulcrumTestFiles[LogFileName].StoreExpressionsResults(ExpressionsFileName, BuiltExpressions);
                    FulcrumTestFiles[LogFileName].StoreSimulationResults(SimulationFileName, BuiltSimulationChannels);
                }
            });

            // Once done, print all of the file object text tables
            var FileObjects = FulcrumTestFiles.Values.ToArray();
            string FilesAsStrings = FulcrumTestHelpers.FulcrumFilesAsTextTable(FileObjects);
            LogBroker.Logger.WriteLog("\n\nGeneration Test Complete! Printing out Simulation Results Now.." + FilesAsStrings);
        }
    }
}
