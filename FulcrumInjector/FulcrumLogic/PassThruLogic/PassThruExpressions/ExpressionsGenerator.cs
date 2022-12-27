using System;
using System.Collections.Generic;
using System.Data;
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
using SharpWrapper.PassThruTypes;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions
{
    /// <summary>
    /// Class used to build an expressions setup object from an input log file.
    /// </summary>
    public class ExpressionsGenerator
    {
        // Logger object and private helpers
        private readonly SubServiceLogger _expressionsLogger;

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
            this._expressionsLogger = new SubServiceLogger($"ExpressionsLogger_{Path.GetFileNameWithoutExtension(this.LogFileName)}");
            this._expressionsLogger.WriteLog("BUILT NEW SETUP FOR AN EXPRESSIONS GENERATOR OK! READY TO BUILD OUR EXPRESSIONS FILE!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Splits an input content string into a set fo PT Command objects which are split into objects.
        /// </summary>
        /// <param name="UpdateParseProgress">When true, progress on the injector log review window will be updated</param>
        /// <returns>Returns a set of file objects which contain the PT commands from a file.</returns>
        public PassThruExpression[] GenerateLogExpressions(bool UpdateParseProgress = false)
        {
            // Log building expression log command line sets now
            this._expressionsLogger.WriteLog($"CONVERTING INPUT LOG FILE {this.LogFileName} INTO AN EXPRESSION SET NOW...", LogType.InfoLog);

            // Store our regex matches and regex object for the time string values located here
            var TimeRegex = new Regex(PassThruRegexModelShare.PassThruTime.ExpressionPattern, RegexOptions.Compiled);
            var TimeMatches = TimeRegex.Matches(this.LogFileContents).Cast<Match>().ToArray();

            // Build an output list of lines for content, find our matches from a built expressions Regex, and generate output lines
            var OutputExpressions = new List<PassThruExpression>();
            while (OutputExpressions.Count != TimeMatches.Length) OutputExpressions.Add(null);
            var OutputCommands = Enumerable.Repeat(string.Empty, TimeMatches.Length).ToList();

            // Loop all the time matches in order and find the index of the next one. Take all content between the index values found
            Parallel.For(0, TimeMatches.Length, MatchIndex =>
            {
                // Pull our our current match object here and store it
                var CurrentMatch = TimeMatches[MatchIndex];

                // Store the index and the string value of this match object here
                string FileSubString = string.Empty;
                int StartingIndex = CurrentMatch.Index;
                string MatchContents = CurrentMatch.Value;

                try
                { 
                    // Find the index values of the next match now and store it to
                    var NextMatch = MatchIndex + 1 == TimeMatches.Length
                        ? TimeMatches[MatchIndex]
                        : TimeMatches[MatchIndex + 1];

                    // Pull a substring of our file contents here and store them now
                    int EndingIndex = NextMatch.Index;
                    int FileSubstringLength = EndingIndex - StartingIndex;
                    FileSubString = this.LogFileContents.Substring(StartingIndex, FileSubstringLength);
                    OutputCommands[MatchIndex] = FileSubString;

                    // Take the split content values, get our ExpressionType, and store the built expression object here
                    var ExpressionType = FileSubString.GetPtTypeFromLines();
                    var NextClassObject = ExpressionType.GetRegexClassFromCommand(FileSubString);
                    OutputExpressions[MatchIndex] = NextClassObject;
                }
                catch (Exception GenerateExpressionEx)
                {
                    // Log failures out and find out why the fails happen then move to our progress routine or move to next iteration
                    this._expressionsLogger.WriteLog($"FAILED TO GENERATE AN EXPRESSION FROM INPUT COMMAND {MatchContents} (Index: {MatchIndex})!", LogType.WarnLog);
                    this._expressionsLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", GenerateExpressionEx, new[] { LogType.WarnLog, LogType.TraceLog });
                }

                // Update progress values if needed now
                if (!UpdateParseProgress) return;
                lock (OutputCommands)
                {
                    // Get the new progress value and update our UI value with it
                    int BuiltValues = OutputExpressions.Count(CommandObj => CommandObj != null);
                    double CurrentProgress = ((double)BuiltValues / (double)TimeMatches.Length) * 100.00;
                    FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = (int)CurrentProgress;
                }
            });

            // Log done building log command line sets and expressions
            this._expressionsLogger.WriteLog($"DONE BUILDING EXPRESSION SETS FROM INPUT FILE {this.LogFileName}!", LogType.InfoLog);
            this._expressionsLogger.WriteLog($"BUILT A TOTAL OF {OutputExpressions.Count} LOG LINE SETS OK!", LogType.InfoLog);

            // Return the built set of commands.
            this.ExpressionsBuilt = OutputExpressions.ToArray();
            this.LogFileContentsSplit = OutputCommands.ToArray();
            return this.ExpressionsBuilt;
        }
        /// <summary>
        /// Takes an input set of PTExpressions and writes them to a file object desired.
        /// </summary>
        /// <param name="BaseFileName">Input file name to save our output expressions file content as</param>
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
