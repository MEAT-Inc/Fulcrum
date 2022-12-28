using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpLogger;

namespace InjectorTests.FulcrumTests
{
    /// <summary>
    /// Test class fixture used to test the expressions generator objects and routines
    /// </summary>
    [TestClass]
    public class FulcrumExpressionsTests
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Collection of objects used to track our input files and their results
        private Dictionary<string, FulcrumInjectorFile> _logFilesImported;
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
        public void SetupExpressionsTests()
        {
            // Build a new dictionary to store our injector files first
            this._logFilesImported = new Dictionary<string, FulcrumInjectorFile>();

            // Loop all the files found in our injector logs folder and import them for testing
            string[] InjectorFiles = Directory.GetFiles(this._logFileFolder);
            foreach (var InjectorFilePath in InjectorFiles)
            {
                // Build a new structure for our injector log file and store it on our class instance
                FulcrumInjectorFile NextInjectorFile = new FulcrumInjectorFile(InjectorFilePath);
                if (this._logFilesImported.ContainsKey(InjectorFilePath)) this._logFilesImported[InjectorFilePath] = NextInjectorFile;
                else this._logFilesImported.Add(InjectorFilePath, NextInjectorFile);
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Test method for building all expressions files for our input log file objects 
        /// </summary>
        [TestCategory("Expressions Generation")]
        [TestMethod("Generate All Expressions")]
        public void GenerateAllExpressionsFiles()
        {
            // Loop all of our built test file instances and attempt to split their contents now using a generator object
            string[] LogFileNames = this._logFilesImported.Keys.ToArray();
            Parallel.ForEach(LogFileNames, LogFile =>
            {
                // Build a new generator for the file instance and store the output values
                var TestFile = this._logFilesImported[LogFile];
                string LogFileContent = TestFile.LogFileContents;
                ExpressionsGenerator GeneratorBuilt = new ExpressionsGenerator(LogFile, LogFileContent);

                // Build our expressions files now for each file instance
                var BuiltExpressions = GeneratorBuilt.GenerateLogExpressions();
                Assert.IsTrue(BuiltExpressions != null && BuiltExpressions.Length != 0);

                // Save the expressions file and validate the content is real
                var ExpressionsFileName = GeneratorBuilt.SaveExpressionsFile(LogFile);
                Assert.IsTrue(File.Exists(ExpressionsFileName));

                // Lock the collection of log file objects and update it
                lock (this._logFilesImported)
                    this._logFilesImported[LogFile].StoreExpressionsResults(ExpressionsFileName, BuiltExpressions);
            });

            // Once done, print all of the file object text tables
            var FileObjects = this._logFilesImported.Values.ToArray();
            string FilesAsStrings = FulcrumTestsInitializer.FulcrumFilesAsTextTable(FileObjects);
            LogBroker.Logger.WriteLog("\n\nGeneration Test Complete! Printing out Expression Results Now.." + FilesAsStrings);
        }
    }
}