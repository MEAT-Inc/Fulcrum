using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumViewContent;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions
{
    /// <summary>
    /// Class used to build an expressions setup object from an input log file.
    /// </summary>
    public class ExpressionsGenerator
    {
        // Logger object and private helpers
        private readonly SubServiceLogger ExpLogger;

        // Input objects for this class instance to build simulations
        public readonly string LogFileName;
        public readonly string LogFileContents;
        public string[] LogFileContentsSplit { get; private set; }

        // Expressions file output information
        public string ExpressionsFile { get; private set; }
        public PassThruExpression[] ExpressionsBuilt { get; private set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new expressions generator from a given input content file set.
        /// </summary>
        /// <param name="LogFileName"></param>
        /// <param name="LogFileContents"></param>
        public ExpressionsGenerator(string LogFileName, string LogFileContents)
        {
            // Store our File nam e and contents here
            this.LogFileName = LogFileName;
            this.LogFileContents = LogFileContents;
            this.ExpLogger = new SubServiceLogger($"ExpressionsLogger_{Path.GetFileNameWithoutExtension(this.LogFileName)}");
            this.ExpLogger.WriteLog("BUILT NEW SETUP FOR AN EXPRESSIONS GENERATOR OK! READY TO BUILD OUR EXPRESSIONS FILE!", LogType.InfoLog);
        }

        /// <summary>
        /// Splits an input content string into a set fo PT Command objects which are split into objects.
        /// </summary>
        /// <param name="UpdateParseProgress">Action for progress updating</param>
        /// <returns>Returns a set of file objects which contain the PT commands from a file.</returns>
        public string[] SplitLogToCommands(bool UpdateParseProgress = false)
        {
            // Build regex objects to help split input content into sets.
            var TimeRegex = new Regex(PassThruRegexModelShare.PassThruTime.ExpressionPattern);
            var StatusRegex = new Regex(PassThruRegexModelShare.PassThruStatus.ExpressionPattern);

            // Make an empty array of strings and then begin splitting.
            List<string> OutputLines = new List<string>();
            for (int CharIndex = 0; CharIndex < LogFileContents.Length;)
            {
                // Find the first index of a time entry and the close command index.
                int TimeStartIndex = TimeRegex.Match(LogFileContents, CharIndex).Index;
                var ErrorCloseMatch = StatusRegex.Match(LogFileContents, TimeStartIndex);
                int ErrorCloseStart = ErrorCloseMatch.Index; int ErrorCloseLength = ErrorCloseMatch.Length;
                if (!ErrorCloseMatch.Success)
                {
                    // If we can't find the status close message, try and find it using the next command startup.
                    var NextTime = TimeRegex.Match(LogFileContents, TimeStartIndex + 1).Index;
                    ErrorCloseStart = NextTime; ErrorCloseLength = 0;
                }

                // Now find the end of our length for the match object
                int ErrorCloseIndex = ErrorCloseStart + ErrorCloseLength;

                // Take the difference in End/Start as our string length value.
                if (ErrorCloseIndex - TimeStartIndex < 0) ErrorCloseIndex = LogFileContents.Length;
                string NextCommand = LogFileContents.Substring(TimeStartIndex, ErrorCloseIndex - TimeStartIndex);

                // If it was found in the list already, then we break out of this loop to stop adding dupes.
                CharIndex = ErrorCloseIndex;
                OutputLines.Add(NextCommand);

                // Progress Updating Action if the bool is set to do so.
                if (!UpdateParseProgress) continue;
                double CurrentProgress = ((double)CharIndex / (double)LogFileContents.Length) * 100.00;
                FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = (int)CurrentProgress;
            }

            // Return the built set of commands.
            this.LogFileContentsSplit = OutputLines.ToArray();
            return this.LogFileContentsSplit;
        }
        /// <summary>
        /// Generates our expression sets based on the given value input
        /// </summary>
        /// <returns></returns>
        public PassThruExpression[] GenerateExpressionSet(bool UpdateParseProgress = false)
        {
            // Setup parsing routine here.
            var ExpressionSet = LogFileContentsSplit.Select(LineSet =>
            {
                // Split our output content here and then build a type for the expressions
                if (LineSet.Length == 0) return null;
                string[] SplitLines = LineSet.Split('\n');
                var ExpressionType = SplitLines.GetTypeFromLines();

                try
                {
                    // Build expression class object and tick our progress
                    var NextClassObject = ExpressionType.GetRegexClassFromCommand(SplitLines);
                    if (!UpdateParseProgress) return NextClassObject;

                    // Update progress, return built object
                    FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress =
                        ((int)(LogFileContentsSplit.ToList().IndexOf(LineSet) + 1 / (double)LogFileContentsSplit.Length)) * 2;
                    return NextClassObject;
                }
                catch (Exception ParseEx)
                {
                    // Log failures out and find out why the fails happen
                    ExpLogger.WriteLog($"FAILED TO PARSE A COMMAND ENTRY! FIRST LINE OF COMMANDS {SplitLines[0]}", LogType.WarnLog);
                    ExpLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", ParseEx, new[] { LogType.WarnLog, LogType.TraceLog });
                    return null;
                }
            }).Where(ExpObj => ExpObj != null).ToArray();

            // Store and return the expressions
            this.ExpressionsBuilt = ExpressionSet;
            return this.ExpressionsBuilt;
        }
        /// <summary>
        /// Takes an input set of PTExpressions and writes them to a file object desired.
        /// </summary>
        /// <param name="InputExpressions">Expression input objects</param>
        /// <returns>Path of our built expression file</returns>
        public string SaveExpressionsFile(string BaseFileName = "")
        {
            // First build our output location for our file.
            string OutputFolder = Path.Combine(LogBroker.BaseOutputPath, "FulcrumExpressions");
            string FinalOutputPath =
                Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(BaseFileName)) + ".ptExp";

            // Get a logger object for saving expression sets.
            string LoggerName = $"{Path.GetFileNameWithoutExtension(BaseFileName)}_ExpressionsLogger";
            var ExpressionLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith(LoggerName)) ?? new SubServiceLogger(LoggerName);

            // Find output path and then build final path value.             
            Directory.CreateDirectory(Path.Combine(LogBroker.BaseOutputPath, "FulcrumExpressions"));
            if (!Directory.Exists(Path.GetDirectoryName(FinalOutputPath))) { Directory.CreateDirectory(Path.GetDirectoryName(FinalOutputPath)); }
            ExpressionLogger.WriteLog($"BASE OUTPUT LOCATION FOR EXPRESSIONS IS SEEN TO BE {Path.GetDirectoryName(FinalOutputPath)}", LogType.InfoLog);

            // Log information about the expression set and output location
            ExpressionLogger.WriteLog($"SAVING A TOTAL OF {this.ExpressionsBuilt.Length} EXPRESSION OBJECTS NOW...", LogType.InfoLog);
            ExpressionLogger.WriteLog($"EXPRESSION SET IS BEING SAVED TO OUTPUT FILE: {FinalOutputPath}", LogType.InfoLog);

            try
            {
                // Now Build output string content from each expression object.
                ExpressionLogger.WriteLog("CONVERTING TO STRINGS NOW...", LogType.WarnLog);
                List<string> OutputExpressionStrings = this.ExpressionsBuilt
                    .SelectMany(InputObj => (InputObj + "\n").Split('\n'))
                    .ToList();

                // Log information and write output.
                ExpressionLogger.WriteLog($"CONVERTED INPUT OBJECTS INTO A TOTAL OF {OutputExpressionStrings.Count} LINES OF TEXT!", LogType.WarnLog);
                ExpressionLogger.WriteLog("WRITING OUTPUT CONTENTS NOW...", LogType.WarnLog);
                File.WriteAllText(FinalOutputPath, string.Join("\n", OutputExpressionStrings));

                // Check to see if we aren't in the default location
                if (BaseFileName.Contains(Path.DirectorySeparatorChar) && !BaseFileName.Contains("FulcrumLogs"))
                {
                    // Find the base path, get the file name, and copy it into here.
                    string LocalDirectory = Path.GetDirectoryName(BaseFileName);
                    string CopyLocation = Path.Combine(LocalDirectory, Path.GetFileNameWithoutExtension(FinalOutputPath)) + ".ptExp";
                    File.Copy(FinalOutputPath, CopyLocation, true);

                    // Remove the Expressions Logger. Log done and return
                    ExpressionLogger.WriteLog("DONE LOGGING OUTPUT CONTENT! RETURNING OUTPUT VALUES NOW");
                    this.ExpressionsFile = CopyLocation;
                    return CopyLocation;
                }

                // Remove the Expressions Logger. Log done and return
                ExpressionLogger.WriteLog("DONE LOGGING OUTPUT CONTENT! RETURNING OUTPUT VALUES NOW");
                this.ExpressionsFile = FinalOutputPath;
                return FinalOutputPath;
            }
            catch (Exception WriteEx)
            {
                // Log failures. Return an empty string.
                ExpressionLogger.WriteLog("FAILED TO SAVE OUR OUTPUT EXPRESSION SETS! THIS IS FATAL!", LogType.FatalLog);
                ExpressionLogger.WriteLog("EXCEPTION FOR THIS INSTANCE IS BEING LOGGED BELOW", WriteEx);

                // Return nothing.
                return string.Empty;
            }
        }
    }
}
