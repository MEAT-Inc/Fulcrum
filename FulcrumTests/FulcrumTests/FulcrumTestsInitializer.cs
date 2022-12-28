using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpLogger;
using SharpLogger.LoggerObjects;

namespace InjectorTests.FulcrumTests
{
    /// <summary>
    /// Injector testing init class.
    /// Contains the init/setup routine used to build a new injector test suite
    /// </summary>
    [TestClass]
    public class FulcrumTestsInitializer
    {
        /// <summary>
        /// Overload/Init routine for the injector tests.
        /// This will run before the first call of the injector test classes
        /// </summary>
        /// <param name="Context">Test Context</param>
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext Context)
        {
            // Store a new configuration file for the injector setup routines 
            string ConfigFileName = "FulcrumInjectorSettings.json";
            string ConfigFilePath = Directory.GetCurrentDirectory();
            JsonConfigFiles.SetNewAppConfigFile(ConfigFileName, ConfigFilePath);

            // Configure the log broker instance classes here if needed
            int MinLoggingLevel = 0;
            int MaxLoggingLevel = 5;
            string AppName = "FulcrumTesting";
            string LoggingPath = Path.Combine(Directory.GetCurrentDirectory(), "FulcrumLogging");

            // Configure a logging session instance here and setup our broker instance
            LogBroker.ConfigureLoggingSession(AppName, LoggingPath, MinLoggingLevel, MaxLoggingLevel);
            BaseLogger.SetFlushTrigger(MaxLoggingLevel); LogBroker.BrokerInstance.FillBrokerPool();
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts an input collection of test files into a text table object
        /// </summary>
        /// <param name="TestFiles">The files to convert into a text table object</param>
        /// <returns>The built text holding the values of these input files in a text table format</returns>
        public static string FulcrumFilesAsTextTable(IEnumerable<FulcrumInjectorFile> TestFiles)
        {
            // Build the final output string values and return them here
            List<string> TitleStrings = new List<string>()
            {
                "Log File Name", "Log File Size", "Log File Length",    // Values for input log files
                "Expressions File Name", "Expressions File Size",       // Values for expressions
                "Simulation File Name", "Simulation File Size",         // Values for simulations
            };

            // Now build a tuple output object for all of our values here
            Tuple<string, string, string, string, string, string, string>[] FileValues = TestFiles.Select(FileObject =>
            {
                // Build a list of output values for the file content
                List<string> FileOutputValues = new List<string>();

                // Store the values for this file and update title values if needed
                var LogFileInfos = FileObject.GetLogFileValues().Skip(1);
                var ExpressionFileInfos = FileObject.GetExpressionsFileValues().Skip(1).Take(2);
                var SimulationFileInfos = FileObject.GetSimulationFileValues().Skip(1).Take(2);

                // Store all of the output values here
                FileOutputValues.AddRange(LogFileInfos.Select(FileInfoObj => FileInfoObj.Item2));
                FileOutputValues.AddRange(ExpressionFileInfos.Select(FileInfoObj => FileInfoObj.Item2));
                FileOutputValues.AddRange(SimulationFileInfos.Select(FileInfoObj => FileInfoObj.Item2));

                // Now build the output tuple for this file object and return it once done
                var OutputValues = new Tuple<string, string, string, string, string, string, string>(
                    FileOutputValues[0],
                    FileOutputValues[1],
                    FileOutputValues[2],
                    FileOutputValues[3],
                    FileOutputValues[4],
                    FileOutputValues[5],
                    FileOutputValues[6]
                );

                // Return the built tuple here 
                return OutputValues;
            }).ToArray();

            // Now invoke the text table extension method for our object values located
            string OutputTableString = FileValues.ToStringTable(
                TitleStrings.ToArray(),
                FileValue => FileValue.Item1,
                FileValue => FileValue.Item2,
                FileValue => FileValue.Item3,
                FileValue => FileValue.Item4,
                FileValue => FileValue.Item5,
                FileValue => FileValue.Item6,
                FileValue => FileValue.Item7);

            // Return the built string value now
            return OutputTableString;
        }
    }
}
