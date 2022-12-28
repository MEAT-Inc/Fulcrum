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
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger object and private helpers
        private readonly SubServiceLogger _expressionsLogger;

        // Input objects for this class instance to build simulations
        public readonly string LogFileName;         // Name of the input log file to use for conversion
        public readonly string LogFileContents;     // The input content of our log file as it was seen in the file 

        #endregion // Fields

        #region Properties

        // Expressions file output information
        public string ExpressionsFile { get; private set; }                     // Path to the newly built expressions file
        public string[] LogFileContentsSplit { get; private set; }              // Split input log file content based on commands
        public string[] ExpressionContentSplit { get; private set; }            // The Expressions file content split out based on commands
        public PassThruExpression[] ExpressionsBuilt { get; private set; }      // The actual expressions objects built for the input log file

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new expressions generator from a given input content file set.
        /// </summary>
        /// <param name="LogFileName"></param>
        /// <param name="LogFileContents"></param>
        public ExpressionsGenerator(string LogFileName, string LogFileContents = null)
        {
            // Store our File nam e and contents here
            this.LogFileName = LogFileName;
            this.LogFileContents = LogFileContents ?? File.ReadAllText(LogFileName);
            this._expressionsLogger = new SubServiceLogger($"ExpressionsLogger_{Path.GetFileNameWithoutExtension(this.LogFileName)}");
            this._expressionsLogger.WriteLog("BUILT NEW SETUP FOR AN EXPRESSIONS GENERATOR OK! READY TO BUILD OUR EXPRESSIONS FILE!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Splits an input content string into a set fo PT Command objects which are split into objects.
        ///
        /// PreReqs:
        ///     1) Load in the log file at the path specified in the CTOR of this class
        ///     2) Store the log contents in a class variable
        ///
        /// Execution:
        ///     1) Log bullshit about this process
        ///     2) Load the regex for time string parsing and compile it (for spood)
        ///     3) Process all matches in the log file content and split into an array of substrings
        ///     4) Setup empty lists for the following value types
        ///        - Output Commands - Strings of the log lines pulled for each PT command
        ///        - Output Expressions - Built Expression objects from the input log file
        ///        - Expressions File Content - Holds all the expression objects as strings
        ///     5) Loop all of the matches found in step 3 and run the following operations
        ///        - Pull a match object and get the index of it
        ///        - Get the index of the next match (or the end of the file) and find the length of our substring
        ///        - Pull a substring value from the input log contents and store it in the Output Commands list
        ///        - Using that substring, build an expression object if the log line content is supported
        ///        - Once an expression is built, convert it to a string and store the value of it
        ///     6) Check if progress updating should be done or not and do it if needed
        ///     7) Clean up our output list objects and prune null values out.
        ///     8) Store the built values on this class instance to return out our built expression objects
        ///     9) Log completed building and return the collection of built expressions
        /// </summary>
        /// <param name="UpdateParseProgress">When true, progress on the injector log review window will be updated</param>
        /// <returns>Returns a set of file objects which contain the PT commands from a file.</returns>
        public PassThruExpression[] GenerateLogExpressions(bool UpdateParseProgress = false)
        {
            // Log building expression log command line sets now
            if (UpdateParseProgress) FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = 0;
            this._expressionsLogger.WriteLog($"CONVERTING INPUT LOG FILE {this.LogFileName} INTO AN EXPRESSION SET NOW...", LogType.InfoLog);

            // Store our regex matches and regex object for the time string values located here
            var TimeRegex = new Regex(PassThruRegexModelShare.PassThruTime.ExpressionPattern, RegexOptions.Compiled);
            var TimeMatches = TimeRegex.Matches(this.LogFileContents).Cast<Match>().ToArray();

            // Build an output list of lines for content, find our matches from a built expressions Regex, and generate output lines
            var OutputCommands = Enumerable.Repeat(string.Empty, TimeMatches.Length).ToArray();
            var OutputFileContent = Enumerable.Repeat(string.Empty, TimeMatches.Length).ToArray();
            var OutputExpressions = Enumerable.Repeat(new PassThruExpression(), TimeMatches.Length).ToArray();

            // Store an int value to track our loop count based on the number of iterations built now
            int LoopsCompleted = 0;

            // Loop all the time matches in order and find the index of the next one. Take all content between the index values found
            ParallelOptions ParseOptions = new ParallelOptions() { MaxDegreeOfParallelism = 3 };
            Parallel.For(0, TimeMatches.Length, ParseOptions, MatchIndex =>
            {
                // Pull our our current match object here and store it
                Match CurrentMatch = TimeMatches[MatchIndex];

                // Store the index and the string value of this match object here
                string FileSubString = string.Empty;
                int StartingIndex = CurrentMatch.Index;
                string MatchContents = CurrentMatch.Value;

                try
                {
                    // Find the index values of the next match now and store it to
                    Match NextMatch = MatchIndex + 1 == TimeMatches.Length
                        ? TimeMatches[MatchIndex]
                        : TimeMatches[MatchIndex + 1];
                        
                    // Pull a substring of our file contents here and store them now
                    int EndingIndex = NextMatch.Index;
                    int FileSubstringLength = EndingIndex - StartingIndex;
                    FileSubString = this.LogFileContents.Substring(StartingIndex, FileSubstringLength);
                    OutputCommands[MatchIndex] = FileSubString;

                    // If we've got the zero messages error line, then just return on
                    bool IsEmpty = string.IsNullOrWhiteSpace(FileSubString);
                    bool HasBuffEmpty = FileSubString.Contains("16:BUFFER_EMPTY");
                    bool HasComplete = FileSubString.Contains("PTReadMsgs() complete");
                    if (!IsEmpty && !HasBuffEmpty && !HasComplete)
                    {
                        // Take the split content values, get our ExpressionType, and store the built expression object here
                        PassThruCommandType ExpressionType = FileSubString.GetPtTypeFromLines();
                        PassThruExpression NextClassObject = ExpressionType.GetRegexClassFromCommand(FileSubString);
                        OutputExpressions[MatchIndex] = NextClassObject;

                        // Now store the expression object as a string for our output file content values
                        string ExpressionString = NextClassObject.ToString();
                        OutputFileContent[MatchIndex] = ExpressionString;
                    }
                }
                catch (Exception GenerateExpressionEx)
                {
                    // Log failures out and find out why the fails happen then move to our progress routine or move to next iteration
                    this._expressionsLogger.WriteLog($"FAILED TO GENERATE AN EXPRESSION FROM INPUT COMMAND {MatchContents} (Index: {MatchIndex})!", LogType.WarnLog);
                    this._expressionsLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", GenerateExpressionEx, new[] { LogType.WarnLog, LogType.TraceLog });
                }

                // Update progress values if needed now
                if (!UpdateParseProgress) return;
            
                // Get the new progress value and update our UI value with it
                int OldProgress = FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress;
                int CurrentProgress = (int)((double)LoopsCompleted++ / (double)TimeMatches.Length * 100.00);
                if (OldProgress != CurrentProgress) FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = CurrentProgress;
            });

            // Prune all null values off the array of expressions
            OutputFileContent = OutputFileContent.Where(ExpressionLines => !string.IsNullOrWhiteSpace(ExpressionLines)).ToArray();
            OutputExpressions = OutputExpressions.Where(ExpressionObj => ExpressionObj.TypeOfExpression != PassThruCommandType.NONE).ToArray();

            // Log done building log command line sets and expressions
            this._expressionsLogger.WriteLog($"DONE BUILDING EXPRESSION SETS FROM INPUT FILE {this.LogFileName}!", LogType.InfoLog);
            this._expressionsLogger.WriteLog($"BUILT A TOTAL OF {OutputExpressions.Length} LOG LINE SETS OK!", LogType.InfoLog);

            // Return the built set of commands.
            this.ExpressionsBuilt = OutputExpressions.ToArray();
            this.LogFileContentsSplit = OutputCommands.ToArray();
            this.ExpressionContentSplit = OutputFileContent.ToArray();
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
            string FinalOutputPath = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(BaseFileName)) + ".ptExp";

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
                ExpressionLogger.WriteLog("COMBINING EXPRESSION OBJECTS INTO AN OUTPUT FILE NOW...", LogType.WarnLog);
                if (this.ExpressionContentSplit == null)
                {
                    // If we've got content to write but no string values, then build them here
                    if (this.ExpressionsBuilt == null) throw new InvalidOperationException("ERROR! CAN NOT SAVE AN EXPRESSIONS FILE THAT HAS NOT BEEN GENERATED!");
                     this.ExpressionContentSplit = this.ExpressionsBuilt.Select(ExpressionObj => ExpressionObj.ToString()).ToArray();
                }

                // Log information and write output.
                ExpressionLogger.WriteLog($"CONVERTED INPUT OBJECTS INTO A TOTAL OF {this.ExpressionContentSplit.Length} LINES OF TEXT!", LogType.WarnLog);
                ExpressionLogger.WriteLog("WRITING OUTPUT CONTENTS NOW...", LogType.WarnLog);
                File.WriteAllText(FinalOutputPath, string.Join("\n", this.ExpressionContentSplit));
                ExpressionLogger.WriteLog("DONE WRITING OUTPUT EXPRESSIONS CONTENT!");

                // Check to see if we aren't in the default location. If not, store the file in both the input spot and the injector directory
                if (BaseFileName.Contains(Path.DirectorySeparatorChar) && !BaseFileName.Contains("FulcrumLogs"))
                {
                    // Find the base path, get the file name, and copy it into here.
                    string LocalDirectory = Path.GetDirectoryName(BaseFileName);
                    string CopyLocation = Path.Combine(LocalDirectory, Path.GetFileNameWithoutExtension(FinalOutputPath)) + ".ptExp";
                    File.Copy(FinalOutputPath, CopyLocation, true);

                    // Remove the Expressions Logger. Log done and return
                    ExpressionLogger.WriteLog("COPIED OUTPUT EXPRESSIONS FILE INTO THE BASE EXPRESSION FILE LOCATION!");
                }

                // Store the path to our final expressions file and exit out
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
