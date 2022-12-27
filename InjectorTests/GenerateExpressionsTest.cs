using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;

// Reference for the PassThruExpression objects and expression generator
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation;

namespace InjectorTests
{
    /// <summary>
    /// Test class fixture used to test the expressions generator objects and routines
    /// </summary>
    [TestClass]
    public class GenerateExpressionsTest
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Collection of objects used to track our input files and their results
        private readonly string _logFileFolder = @"\InjectorLogs";
        private Dictionary<string, InjectorTestFile> _logFilesImported;

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
        public void SetupExpressionsTests()
        {
            // Build a new dictionary to store our injector files first
            this._logFilesImported = new Dictionary<string, InjectorTestFile>();

            // Loop all the files found in our injector logs folder and import them for testing
            string[] InjectorFiles = Directory.GetFiles(this._logFileFolder);
            foreach (var InjectorFilePath in InjectorFiles)
            {
                // Build a new structure for our injector log file and store it on our class instance
                InjectorTestFile NextTestFile = new InjectorTestFile(InjectorFilePath);
                if (this._logFilesImported.ContainsKey(InjectorFilePath)) this._logFilesImported[InjectorFilePath] = NextTestFile;
                else this._logFilesImported.Add(InjectorFilePath, NextTestFile);
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Test method for importing and storing all log file contents needed for testing later on
        /// </summary>
        [TestMethod]
        public void GenerateAllCommandSets()
        {
            // Loop all of our built test file instances and attempt to split their contents now using a generator object
            Parallel.ForEach(this._logFilesImported, FileKeyValuePair =>
            {
                // Build a new generator for the file instance and store the output values
                string LogFileName = FileKeyValuePair.Key;
                string LogFileContent = FileKeyValuePair.Value.LogFileContents;
                ExpressionsGenerator GeneratorBuilt = new ExpressionsGenerator(LogFileName, LogFileContent);

                // Split our log file content and store it on our output instance now
                var BuiltSplitLines = GeneratorBuilt.GenerateCommandSets(false);
                Assert.IsTrue(BuiltSplitLines != null && BuiltSplitLines.Length != 0);
            });
        }
        /// <summary>
        /// Test method for building all expressions files for our input log file objects 
        /// </summary>
        [TestMethod]
        public void GenerateAllExpressions()
        {
            // Loop all of our built test file instances and attempt to split their contents now using a generator object
            Parallel.ForEach(this._logFilesImported, FileKeyValuePair =>
            {
                // Build a new generator for the file instance and store the output values
                string LogFileName = FileKeyValuePair.Key;
                string LogFileContent = FileKeyValuePair.Value.LogFileContents;
                ExpressionsGenerator GeneratorBuilt = new ExpressionsGenerator(LogFileName, LogFileContent);

                // Build our expressions files now for each file instance
                var BuiltExpressions = GeneratorBuilt.GenerateExpressionsSet();
                Assert.IsTrue(BuiltExpressions != null && BuiltExpressions.Length != 0);

                // Store the built expressions on the new test file instance here
                FileKeyValuePair.Value.LogExpressions = BuiltExpressions;
            });
        }
    }
}