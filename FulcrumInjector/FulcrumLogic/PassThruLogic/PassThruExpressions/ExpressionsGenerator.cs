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

        /// <summary>
        /// Splits an input content string into a set fo PT Command objects which are split into objects.
        /// </summary>
        /// <param name="UpdateParseProgress">Action for progress updating</param>
        /// <returns>Returns a set of file objects which contain the PT commands from a file.</returns>
        public string[] SplitLogToCommands(bool UpdateParseProgress = false)
        {
            // Log building expression log command line sets now
            this._expressionsLogger.WriteLog($"SPLITTING INPUT LOG FILE {this.LogFileName} INTO COMMAND LINE SETS NOW...", LogType.InfoLog);

            // Build an output list of lines for content, find our matches from a built expressions Regex, and generate output lines
            List<string> OutputCommands = new List<string>();
            var TimeRegex = new Regex(PassThruRegexModelShare.PassThruTime.ExpressionPattern, RegexOptions.Compiled);
            var TimeMatches = TimeRegex.Matches(this.LogFileContents).Cast<Match>().ToArray();

            // Loop all the time matches in order and find the index of the next one. Take all content between the index values found
            Parallel.For(0, TimeMatches.Length, MatchIndex =>
            {
                // Store the current match and the index of it  
                var CurrentMatch = TimeMatches[MatchIndex];
                int StartingIndex = CurrentMatch.Index;

                try
                {
                    // Check if we're at the end of our file or not
                    if (MatchIndex + 1 == TimeMatches.Length)
                    {
                        // Store the final substring value and store it in our output list
                        string FinalLogCommand = this.LogFileContents.Substring(StartingIndex);
                        lock (OutputCommands) OutputCommands.Add(FinalLogCommand);
                    }
                    else
                    {
                        // Find the index values of the next match now and store it
                        var NextMatch = TimeMatches[MatchIndex + 1];
                        int EndingIndex = NextMatch.Index;

                        // Pull a substring of our file contents here and store them now
                        int FileSubstringLength = EndingIndex - StartingIndex;
                        string FileContentSubstring = this.LogFileContents.Substring(StartingIndex, FileSubstringLength);
                        lock (OutputCommands) OutputCommands.Add(FileContentSubstring);
                    }
                }
                catch (Exception SplitLogContentEx)
                {
                    // Log failures out and find out why the fails happen then move to our progress routine or move to next iteration
                    this._expressionsLogger.WriteLog($"FAILED TO SPLIT A COMMAND LOG SET INTO AN EXPRESSION!", LogType.WarnLog);
                    this._expressionsLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", SplitLogContentEx, new[] { LogType.WarnLog, LogType.TraceLog });
                }

                // Update progress values if needed now
                if (!UpdateParseProgress) return;
                lock (OutputCommands)
                {
                    // Get the new progress value and update our UI value with it
                    double CurrentProgress = ((double)OutputCommands.Count / (double)TimeMatches.Length) * 100.00;
                    FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = (int)CurrentProgress;
                }
            });

            // Log done building log command line sets
            this._expressionsLogger.WriteLog($"DONE BUILDING LOG LINE SETS FROM INPUT FILE {this.LogFileName}!", LogType.InfoLog);
            this._expressionsLogger.WriteLog($"BUILT A TOTAL OF {OutputCommands.Count} LOG LINE SETS OK!", LogType.InfoLog);

            // Return the built set of commands.
            this.LogFileContentsSplit = OutputCommands.ToArray();
            return this.LogFileContentsSplit;
        }
        /// <summary>
        /// Generates our expression sets based on the given value input
        /// </summary>
        /// <returns>A collection of expression objects built during this routine</returns>
        public PassThruExpression[] GenerateExpressionSet(bool UpdateParseProgress = false)
        {
            // Log Generating Expressions now
            this._expressionsLogger.WriteLog($"GENERATING EXPRESSION SET FOR INPUT FILE {this.LogFileName}...", LogType.InfoLog);

            // Build a list to hold our output objects here
            var BuiltExpressionsList = new List<PassThruExpression>(this.LogFileContentsSplit.Length);
            while (BuiltExpressionsList.Count != this.LogFileContentsSplit.Length - 1)
                BuiltExpressionsList.Add(null);

            // Loop all of the file content lines split apart and store each new expression file from it
            Parallel.For(0, this.LogFileContentsSplit.Length, LineSetIndex => 
            {
                // If no line content is found, then just move onto the next iteration
                var LineSet = this.LogFileContentsSplit[LineSetIndex];
                string[] SplitLines = LineSet.Split('\n').ToArray();
                if (LineSet.Contains("16:BUFFER_EMPTY") || SplitLines.Length == 1) return;

                try
                {
                    // Take the split content values and get our ExpressionType as long as content was found
                    var ExpressionType = SplitLines.GetPtTypeFromLines();

                    // Build expression class object and tick our progress
                    var NextClassObject = ExpressionType.GetRegexClassFromCommand(SplitLines);
                    lock (BuiltExpressionsList) BuiltExpressionsList[LineSetIndex] = NextClassObject;

                    // Progress Updating Action if the bool is set to do so.
                    if (!UpdateParseProgress) return;

                    // Build a new progress value and then store it on the view model for our log review
                    lock (BuiltExpressionsList)
                    {
                        int NumberOfValues = BuiltExpressionsList.Count(OutputObj => OutputObj != null);
                        double CurrentProgress = ((double)NumberOfValues / (double)BuiltExpressionsList.Count) * 100.00;
                        FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = (int)CurrentProgress;
                    }
                }
                catch (Exception ParseEx)
                {
                    // Log failures out and find out why the fails happen
                    this._expressionsLogger.WriteLog($"FAILED TO PARSE A COMMAND ENTRY! FIRST LINE OF COMMAND SET: {SplitLines[0]}", LogType.WarnLog);
                    this._expressionsLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", ParseEx, new[] { LogType.WarnLog, LogType.TraceLog });
                }
            });
            
            // Log done building log command line sets
            this._expressionsLogger.WriteLog($"DONE BUILDING EXPRESSION OBJECTS FROM INPUT FILE {this.LogFileName}!", LogType.InfoLog);
            this._expressionsLogger.WriteLog($"BUILT A TOTAL OF {BuiltExpressionsList.Count} EXPRESSIONS CORRECTLY!", LogType.InfoLog);

            // Store and return the expressions generated and stop our timer
            this.ExpressionsBuilt = BuiltExpressionsList.ToArray();
            return this.ExpressionsBuilt;
        }
      
        /// <summary>
        /// Splits the input log file content into chunks by command issues and stores their results as a set of expressions
        /// </summary>
        /// <param name="UpdateParseProgress">When true, progress on the injector log review window will be updated</param>
        /// <returns>A collection of all the expression objects built out from our input log file</returns>
        public PassThruExpression[] SplitAndGenerateExpressions(bool UpdateParseProgress = false)
        {
            // Log Generating Expressions now
            this._expressionsLogger.WriteLog($"GENERATING EXPRESSION SET FOR INPUT FILE {this.LogFileName}...", LogType.InfoLog);

            // Start by splitting the input log content using our time regex object
            var TimeRegex = new Regex(PassThruRegexModelShare.PassThruTime.ExpressionPattern, RegexOptions.Compiled);
            this.LogFileContentsSplit = TimeRegex.Split(this.LogFileContents);
            
            // Build a temporary output list for our expressions objects here
            var BuiltExpressions = new List<PassThruExpression>();
            while (BuiltExpressions.Count == 0) BuiltExpressions.Add(null);

            // Now loop all the split log content objects and store their values in the temporary output list
            Parallel.For(0, this.LogFileContentsSplit.Length, LineSetIndex =>
            {
                // If no line content is found, then just move onto the next iteration
                var InputLineSet = this.LogFileContentsSplit[LineSetIndex];
                string[] SplitLines = InputLineSet.Split('\n').ToArray();
                if (InputLineSet.Contains("16:BUFFER_EMPTY") || SplitLines.Length == 1) return;

                try
                {
                    // Take the split content values and get our ExpressionType as long as content was found
                    var ExpressionType = SplitLines.GetPtTypeFromLines();

                    // Build expression class object and tick our progress
                    var NextClassObject = ExpressionType.GetRegexClassFromCommand(InputLineSet);
                    lock (BuiltExpressions) BuiltExpressions[LineSetIndex] = NextClassObject;
                }
                catch (Exception ParseEx)
                {
                    // Log failures out and find out why the fails happen
                    this._expressionsLogger.WriteLog($"FAILED TO PARSE A COMMAND ENTRY! FIRST LINE OF COMMAND SET: {SplitLines[0]}", LogType.ErrorLog);
                    this._expressionsLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", ParseEx, new[] { LogType.DebugLog, LogType.DebugLog });
                }

                // Build a new progress value and then store it on the view model for our log review if needed
                if (!UpdateParseProgress) return;
                lock (BuiltExpressions)
                {
                    int NumberOfValues = BuiltExpressions.Count(OutputObj => OutputObj != null);
                    double CurrentProgress = ((double)NumberOfValues / (double)BuiltExpressions.Count) * 100.00;
                    FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = (int)CurrentProgress;
                }
            });

            // Return the built expressions objects here
            this.ExpressionsBuilt = BuiltExpressions.ToArray();
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
