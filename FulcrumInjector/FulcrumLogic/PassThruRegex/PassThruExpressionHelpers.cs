using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.PassThruRegex
{
    /// <summary>
    /// Extensions for parsing out commands into new types of output for PT Regex Classes
    /// </summary>
    public static class PassThruExpressionHelpers
    {
        /// <summary>
        /// Converts an input Regex command type enum into a type output
        /// </summary>
        /// <param name="InputType">Enum Regex Typ</param>
        /// <returns>Type of regex for the class output</returns>
        public static PassThruExpression ToRegexClass(this PassThruCommandType InputType, string[] InputLines)
        {
            // Pull the description string and get type of regex class.
            return ToRegexClass(InputType, string.Join("\n", InputLines));
        }
        /// <summary>
        /// Converts an input Regex command type enum into a type output
        /// </summary>
        /// <param name="InputType">Enum Regex Typ</param>
        /// <returns>Type of regex for the class output</returns>
        public static PassThruExpression ToRegexClass(this PassThruCommandType InputType, string InputLines)
        {
            // Pull the description string and get type of regex class.
            string ClassType = $"FulcrumInjector.FulcrumLogic.PassThruRegex.{InputType.ToDescriptionString()}";
            return (PassThruExpression)(Type.GetType(ClassType) == null ?
                new PassThruExpression(InputLines, InputType) :
                Activator.CreateInstance(Type.GetType(ClassType)));
        }
        /// <summary>
        /// Finds a PTCommand type from the given input line set
        /// </summary>
        /// <param name="InputLines">Lines to find the PTCommand Type for.</param>
        /// <returns>The type of PTCommand regex to search with.</returns>
        public static PassThruCommandType GetTypeFromLines(string[] InputLines)
        {
            // Find the type of command by converting all enums to string array and searching for the type.
            var EnumTypesArray = Enum.GetValues(typeof(PassThruCommandType))
                .Cast<PassThruCommandType>()
                .Select(v => v.ToString())
                .ToArray();

            // Find the return type here based on the first instance of a PTCommand type object on the array.
            if (EnumTypesArray.All(EnumString => !InputLines.Contains(EnumString))) return PassThruCommandType.NONE;
            return (PassThruCommandType)Enum.Parse(typeof(PassThruCommandType), EnumTypesArray.FirstOrDefault(EnumObj => InputLines.Contains(EnumObj)));
        }


        /// <summary>
        /// Splits an input content string into a set fo PT Command objects which are split into objects.
        /// </summary>
        /// <param name="FileContents">Input file object content</param>
        /// <returns>Returns a set of file objects which contain the PT commands from a file.</returns>
        public static string[] SplitLogToCommands(string FileContents)
        {
            // Build regex objects to help split input content into sets.
            var TimeRegex = new Regex(@"(\d+\.\d+s)\s+(\+\+|--|!!|\*\*)\s+PT");
            var PtErrorRegex = new Regex(@"(\d+\.\d+s)\s+(\d+:[^\n]+)");

            // Make an empty array of strings and then begin splitting.
            List<string> OutputLines = new List<string>();
            for (int CharIndex = 0; CharIndex < FileContents.Length;)
            {
                // Find the first index of a time entry and the close command index.
                int TimeStartIndex = TimeRegex.Match(FileContents, CharIndex).Index;
                var ErrorCloseMatch = PtErrorRegex.Match(FileContents, TimeStartIndex);
                int ErrorCloseIndex = ErrorCloseMatch.Index + ErrorCloseMatch.Length;

                // Take the difference in End/Start as our string length value.
                string NextCommand = FileContents.Substring(TimeStartIndex, ErrorCloseIndex - TimeStartIndex);
                if (OutputLines.Contains(NextCommand)) break;

                // If it was found in the list already, then we break out of this loop to stop adding dupes.
                if (ErrorCloseIndex < CharIndex) break;
                CharIndex = ErrorCloseIndex; OutputLines.Add(NextCommand);
            }

            // Return the built set of commands.
            return OutputLines.ToArray();
        }
        /// <summary>
        /// Takes an input set of PTExpressions and writes them to a file object desired.
        /// </summary>
        /// <param name="InputExpressions">Expression input objects</param>
        /// <returns>Path of our built expression file</returns>
        public static string SaveExpressionsToFile(this PassThruExpression[] InputExpressions, string BaseFileName = "")
        {
            // Get a logger object for saving expression sets.
            var ExpressionLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("ExpressionLogger")) ?? new SubServiceLogger("ExpressionLogger");

            // First build our output location for our file.
            string OutputFolder = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorResources.FulcrumExpressionsPath");
            string FinalOutputPath = 
                BaseFileName.Contains(Path.DirectorySeparatorChar) ? 
                    Path.ChangeExtension(BaseFileName, "ptExp") : 
                    BaseFileName.Length == 0 ? 
                        Path.Combine(OutputFolder, $"FulcrumExpressions_{DateTime.Now:MMddyyyy-HHmmss}.ptExp") : 
                        Path.Combine(OutputFolder, $"{Path.GetFileNameWithoutExtension(BaseFileName)}_{DateTime.Now:MMddyyyy-HHmmss}.ptExp");

            // Find output path and then build final path value.             
            Directory.CreateDirectory(Path.Combine(LogBroker.BaseOutputPath, "FulcrumExpressions"));
            if (!Directory.Exists(Path.GetDirectoryName(FinalOutputPath))) { Directory.CreateDirectory(Path.GetDirectoryName(FinalOutputPath)); }
            ExpressionLogger.WriteLog($"BASE OUTPUT LOCATION FOR EXPRESSIONS IS SEEN TO BE {Path.GetDirectoryName(FinalOutputPath)}", LogType.InfoLog);

            // Log information about the expression set and output location
            ExpressionLogger.WriteLog($"SAVING A TOTAL OF {InputExpressions.Length} EXPRESSION OBJECTS NOW...", LogType.InfoLog);
            ExpressionLogger.WriteLog($"EXPRESSION SET IS BEING SAVED TO OUTPUT FILE: {FinalOutputPath}", LogType.InfoLog);

            try
            {
                // Now Build output string content from each expression object.
                ExpressionLogger.WriteLog("CONVERTING TO STRINGS NOW...", LogType.WarnLog);
                List<string> OutputExpressionStrings = InputExpressions
                    .SelectMany(InputObj => (InputObj + "ENDTABLE\n").Split('\n'))
                    .ToList();

                // Find size of our largest string object here then populate split lines in place of ENDTABLE
                int MaxSizeString = OutputExpressionStrings.OrderByDescending(StringObj => StringObj.Length).First().Length;
                OutputExpressionStrings = OutputExpressionStrings
                    .Select(StringObj => StringObj == "ENDTABLE" ? Enumerable.Repeat("=", MaxSizeString).ToString() : StringObj)
                    .ToList();

                // Log information and write output.
                ExpressionLogger.WriteLog($"CONVERTED INPUT OBJECTS INTO A TOTAL OF {OutputExpressionStrings.Count} LINES OF TEXT!", LogType.WarnLog);
                ExpressionLogger.WriteLog("WRITING OUTPUT CONTENTS NOW...", LogType.WarnLog);
                File.WriteAllLines(FinalOutputPath, OutputExpressionStrings.ToArray());

                // Copy into our log file directory now
                File.Copy(FinalOutputPath, Path.Combine(LogBroker.BaseOutputPath, "FulcrumExpressions"));
                ExpressionLogger.WriteLog("CLONED EXPRESSIONS INTO OUR LOG DIRECTORY OK!", LogType.InfoLog);

                // Write completed info to the log and return our new output path value.
                ExpressionLogger.WriteLog("DONE LOGGING OUTPUT CONTENT! RETURNING OUTPUT VALUES NOW");
                return FinalOutputPath;
            }
            catch (Exception WriteEx)
            {
                // Log failures. Return an empty string.
                ExpressionLogger.WriteLog("FAILED TO SAVE OUR OUTPUT EXPRESSION SETS! THIS IS FATAL!", LogType.FatalLog);
                ExpressionLogger.WriteLog("EXCEPTION FOR THIS INSTANCE IS BEING LOGGED BELOW", WriteEx);
                return string.Empty;
            }
        }
    }
}
