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
            // Invoke a new logging setup here first
            FulcrumTestHelpers.FulcrumLoggingInit();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Test method for building all expressions files for our input log file objects 
        /// </summary>
        [TestCategory("Expressions Generation")]
        [TestMethod("Generate All Expressions")]
        public void GenerateAllExpressionsFiles()
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
                ExpressionsGenerator GeneratorBuilt = new ExpressionsGenerator(LogFileName, LogFileContent);

                // Build our expressions files now for each file instance and save them out
                var BuiltExpressions = GeneratorBuilt.GenerateLogExpressions();
                var ExpressionsFileName = GeneratorBuilt.SaveExpressionsFile(TestFileObject.LogFile);

                // Check some conditions for our expressions file output and store the new values built
                Assert.IsTrue(File.Exists(ExpressionsFileName));
                Assert.IsTrue(BuiltExpressions != null && BuiltExpressions.Length != 0);

                // Lock the collection of log file objects and update it
                lock (FulcrumTestFiles)
                    FulcrumTestFiles[LogFileName].StoreExpressionsResults(ExpressionsFileName, BuiltExpressions);
            });

            // Once done, print all of the file object text tables
            var FileObjects = FulcrumTestFiles.Values.ToArray();
            string FilesAsStrings = FulcrumTestHelpers.FulcrumFilesAsTextTable(FileObjects);
            LogBroker.Logger.WriteLog("\n\nGeneration Test Complete! Printing out Expression Results Now.." + FilesAsStrings);
        }
    }
}