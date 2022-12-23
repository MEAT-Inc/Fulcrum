using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
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
            // Log building expression log command line sets now
            ExpLogger.WriteLog($"SPLITTING INPUT LOG FILE {this.LogFileName} INTO COMMAND LINE SETS NOW...", LogType.InfoLog);

            // Match all our starting and ending command lines and store them all here
            var TimeMatches = Regex.Matches(this.LogFileContents, PassThruRegexModelShare.PassThruTime.ExpressionPattern, RegexOptions.Compiled); 
            var StatusMatches = Regex.Matches(this.LogFileContents, PassThruRegexModelShare.PassThruStatus.ExpressionPattern, RegexOptions.Compiled);

            // Now build a tuple set for all the match objects found
            int MatchCount = Math.Min(TimeMatches.Count, StatusMatches.Count);
            List<Tuple<Match, Match>> PairedMatches = new List<Tuple<Match, Match>>();

            // Loop all the match sets and store their values as pairs now
            for (int MatchIndex = 0; MatchIndex < MatchCount; MatchIndex++)
            {
                // Store the matches for our time and status values here
                Match TimeMatch = TimeMatches[MatchIndex]; 
                Match StatusMatch = StatusMatches[MatchIndex];

                // Insert a new tuple into our list of values here
                PairedMatches.Add(new(TimeMatch, StatusMatch));
            }

            // Now iterate all the match objects and store the content between their index values here
            List<string> OutputLines = Enumerable.Repeat(string.Empty, MatchCount).ToList();
            Parallel.ForEach(PairedMatches, MatchSet =>
            {
                // Store the time regex and the status regex matches as local values
                var TimeMatch = MatchSet.Item1;
                var StatusMatch = MatchSet.Item2;

                // If we failed to find a match for our status value, move on
                if (!StatusMatch.Success) return;

                // Find the first index of a time entry and the close command index.
                int TimeStartIndex = TimeMatch.Index;
                int ErrorCloseStart = StatusMatch.Index;
                int ErrorCloseLength = StatusMatch.Length;

                // Now find the end of our length for the match object
                int ErrorCloseIndex = ErrorCloseStart + ErrorCloseLength;

                // Take the difference in End/Start as our string length value.
                if (ErrorCloseIndex - TimeStartIndex < 0) ErrorCloseIndex = LogFileContents.Length;
                string NextCommand = LogFileContents.Substring(TimeStartIndex, ErrorCloseIndex - TimeStartIndex);

                // If it was found in the list already, then we break out of this loop to stop adding dupes.
                int MatchSetIndex = PairedMatches.IndexOf(MatchSet);
                lock (OutputLines) OutputLines.Insert(MatchSetIndex, NextCommand);

                // Progress Updating Action if the bool is set to do so.
                if (!UpdateParseProgress) return;
                double CurrentProgress = ((double)MatchSetIndex / (double)PairedMatches.Count) * 100.00;
                FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = (int)CurrentProgress;
            });

            // Reorder the log line sets 

            // Log done building log command line sets
            ExpLogger.WriteLog($"DONE BUILDING LOG LINE SETS FROM INPUT FILE {this.LogFileName}!", LogType.InfoLog);
            ExpLogger.WriteLog($"BUILT A TOTAL OF {OutputLines.Count} LOG LINE SETS OK!", LogType.InfoLog);

            // Return the built set of commands.
            this.LogFileContentsSplit = OutputLines.ToArray();
            return this.LogFileContentsSplit;
        }
        /// <summary>
        /// Generates our expression sets based on the given value input
        /// </summary>
        /// <returns>A collection of expression objects built during this routine</returns>
        public PassThruExpression[] GenerateExpressionSet(bool UpdateParseProgress = false)
        {
            // Log Generating Expressions now
            ExpLogger.WriteLog($"GENERATING EXPRESSION SET FOR INPUT FILE {this.LogFileName}...", LogType.InfoLog);

            // Build a list to hold our output objects here
            var BuiltExpressionsList = new List<PassThruExpression>();
            Parallel.ForEach(this.LogFileContentsSplit, LineSet =>
            {
                // If no line content is found, then just move onto the next iteration
                if (LineSet.Length == 0) return;
                
                // Split our output content here and then build a type for the expressions
                string[] SplitLines = LineSet.Split('\n');
                var ExpressionType = SplitLines.GetTypeFromLines();

                try 
                {
                    // Build expression class object and tick our progress
                    var NextClassObject = ExpressionType.GetRegexClassFromCommand(SplitLines);
                    if (!UpdateParseProgress) lock (BuiltExpressionsList) BuiltExpressionsList.Add(NextClassObject);

                    // Update progress, return built object
                    int IndexOfLine = LogFileContents.LastIndexOf(LineSet);
                    double CurrentProgress = (IndexOfLine / (double)LogFileContents.Length) * 100.00;
                    FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = (int)CurrentProgress;
                    lock (ExpressionsBuilt) BuiltExpressionsList.Add(NextClassObject);
                }
                catch (Exception ParseEx)
                {
                    // Log failures out and find out why the fails happen
                    ExpLogger.WriteLog($"FAILED TO PARSE A COMMAND ENTRY! FIRST LINE OF COMMANDS {SplitLines[0]}", LogType.WarnLog);
                    ExpLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", ParseEx, new[] { LogType.WarnLog, LogType.TraceLog });
                }
            });

            // Store and return the expressions generated and stop our timer
            this.ExpressionsBuilt = BuiltExpressionsList.ToArray();
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
            string OutputFolder = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorLogging.DefaultExpressionsPath");
            string FinalOutputPath =
                Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(BaseFileName)) + ".ptExp";

            // Get a logger object for saving expression sets.
            string LoggerName = $"{Path.GetFileNameWithoutExtension(BaseFileName)}_ExpressionsLogger";
            var ExpressionLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith(LoggerName)) ?? new SubServiceLogger(LoggerName);

            // Find output path and then build final path value.             
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

                // TODO: Figure out why I had this code in here...
                // Check to see if we aren't in the default location
                //if (BaseFileName.Contains(Path.DirectorySeparatorChar) && !BaseFileName.Contains("FulcrumLogs"))
                //{
                //    // Find the base path, get the file name, and copy it into here.
                //    string LocalDirectory = Path.GetDirectoryName(BaseFileName);
                //    string CopyLocation = Path.Combine(LocalDirectory, Path.GetFileNameWithoutExtension(FinalOutputPath)) + ".ptExp";
                //    File.Copy(FinalOutputPath, CopyLocation, true);
                //
                //    // Remove the Expressions Logger. Log done and return
                //    ExpressionLogger.WriteLog("DONE LOGGING OUTPUT CONTENT! RETURNING OUTPUT VALUES NOW");
                //    this.ExpressionsFile = CopyLocation;
                //    return CopyLocation;
                //}

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

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Imports an expression file and converts it into a list of expression objects
        /// </summary>
        /// <returns>A temporary file name which contains the contents of our log file.</returns>
        public static string ImportExpressionSet(string InputFilePath)
        {
            // Read the contents of the file and store them. Split them out based on the expression splitting line entries
            string InputExpressionContent = File.ReadAllText(InputFilePath);
            string[] ExpressionStringsSplit = Regex.Split(InputExpressionContent, @"=+\n\n=+");

            // Now find JUST the log file content values and store them.
            string[] LogLinesPulled = ExpressionStringsSplit.Select(ExpressionEntrySet =>
            {
                // Regex match our content values desired
                string RegexLogLinesFound = Regex.Replace(ExpressionEntrySet, @"=+|\+=+\+\s+(?>\|[^\r\n]+\s+)+\+=+\+\s+", string.Empty);
                string[] SplitRegexLogLines = RegexLogLinesFound
                    .Split('\n')
                    .Where(LogLine =>
                        LogLine.Length > 3 &&
                        !LogLine.Contains("No Parameters") &&
                        !LogLine.Contains("No Messages Found!") &&
                        !string.IsNullOrWhiteSpace(LogLine))
                    .Select(LogLine => LogLine.Substring(3))
                    .ToArray();

                // Now trim the padding edges off and return
                string OutputRegexStrings = string.Join("\n", SplitRegexLogLines);
                return OutputRegexStrings;
            }).ToArray();

            // Convert pulled strings into one whole object. Convert the log content into an expression here
            string CombinedOutputLogLines = string.Join("\n", LogLinesPulled);
            string OutputLogFileDirectory = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorLogging.DefaultConversionsPath");
            string ConvertedLogFilePath = Path.Combine(OutputLogFileDirectory, "ExpressionImport_" + Path.GetFileName(Path.ChangeExtension(InputFilePath, ".txt")));

            // Remove old files and write out the new contents
            if (File.Exists(ConvertedLogFilePath)) File.Delete(ConvertedLogFilePath);
            if (!Directory.Exists(OutputLogFileDirectory)) Directory.CreateDirectory(OutputLogFileDirectory);
            File.WriteAllText(ConvertedLogFilePath, CombinedOutputLogLines);

            // Return the built file path
            return ConvertedLogFilePath;
        }
    }
}
