using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation;

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

        // Collection of objects used to track our input files and their results
        private Dictionary<string, FulcrumInjectorFile> _injectorLogFiles;
        private readonly string _logFileFolder = Path.Combine(Directory.GetCurrentDirectory(), @"FulcrumLogs");

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
            FulcrumTestHelpers.FulcrumTestInit();

            // Build a new dictionary to store our injector files first
            this._injectorLogFiles = new Dictionary<string, FulcrumInjectorFile>();

            // Loop all the files found in our injector logs folder and import them for testing
            string[] InjectorFiles = Directory.GetFiles(this._logFileFolder).Where(FileName => FileName.EndsWith(".txt")).ToArray();
            foreach (var InjectorFilePath in InjectorFiles)
            {
                // Build a new structure for our injector log file and store it on our class instance
                FulcrumInjectorFile NextInjectorFile = new FulcrumInjectorFile(InjectorFilePath);
                if (this._injectorLogFiles.ContainsKey(InjectorFilePath)) this._injectorLogFiles[InjectorFilePath] = NextInjectorFile;
                else this._injectorLogFiles.Add(InjectorFilePath, NextInjectorFile);
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Test method for building all expressions files for our input log file objects 
        /// </summary>
        [TestCategory("Simulation Generation")]
        [TestMethod("Generate All Simulations")]
        public void GenerateAllSimulationFiles()
        {
            // Loop all of our built test file instances and attempt to split their contents now using a generator object
            string[] LogFileNames = this._injectorLogFiles.Keys.ToArray();
            Parallel.ForEach(LogFileNames, LogFileName =>
            {
                // Build a new generator for the file instance and store the output values
                var TestFileObject = this._injectorLogFiles[LogFileName];
                string LogFileContent = TestFileObject.LogFileContents;

                // Build our expressions files now for each file instance
                ExpressionsGenerator ExpGenerator = new ExpressionsGenerator(TestFileObject.LogFile, LogFileContent);
                var BuiltExpressions = ExpGenerator.GenerateLogExpressions();
                var ExpressionsFileName = ExpGenerator.SaveExpressionsFile(TestFileObject.LogFile);

                // Now build a simulation generator and invoke the build routine
                SimulationGenerator SimGenerator = new SimulationGenerator(TestFileObject.LogFile, TestFileObject.ExpressionsBuilt);
                var GroupedExpressionSets = SimGenerator.GenerateGroupedIds();
                var BuiltSimulationChannels = SimGenerator.GenerateSimulationChannels();
                var SimulationFileName = SimGenerator.SaveSimulationFile(TestFileObject.LogFile);

                // Check some conditions for the simulation file routine
                Assert.IsTrue(File.Exists(ExpressionsFileName));
                Assert.IsTrue(File.Exists(SimulationFileName));
                Assert.IsTrue(BuiltExpressions != null && BuiltExpressions.Length != 0);
                Assert.IsTrue(GroupedExpressionSets != null && GroupedExpressionSets.Count != 0);
                Assert.IsTrue(BuiltSimulationChannels != null && BuiltSimulationChannels.Count != 0);

                // Lock the collection of log file objects and update it
                lock (this._injectorLogFiles)
                {
                    // Store the expression generation results and the simulation generation results
                    this._injectorLogFiles[LogFileName].StoreExpressionsResults(ExpressionsFileName, BuiltExpressions);
                    this._injectorLogFiles[LogFileName].StoreSimulationResults(SimulationFileName, BuiltSimulationChannels.Values.ToArray());
                }
            });

            // Once done, print all of the file object text tables
            var FileObjects = this._injectorLogFiles.Values.ToArray();
            string FilesAsStrings = FulcrumTestHelpers.FulcrumFilesAsTextTable(FileObjects);
            LogBroker.Logger.WriteLog("\n\nGeneration Test Complete! Printing out Simulation Results Now.." + FilesAsStrings);
        }
    }
}
