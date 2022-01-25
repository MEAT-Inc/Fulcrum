using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using FulcrumInjector.FulcrumLogic.PassThruExpressions;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using NLog.Targets;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.ExtensionClasses
{
    /// <summary>
    /// Extensions for parsing out commands into new types of output for PT Regex Classes
    /// </summary>
    public static class ExpressionExtensions
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
            string ClassType = $"{typeof(PassThruExpression).Namespace}.{InputType.ToDescriptionString()}";
            if (Type.GetType(ClassType) == null) return new PassThruExpression(InputLines, InputType);

            // Find our output type value here.
            Type OutputType = Type.GetType(ClassType);
            var RegexConstructor = OutputType.GetConstructor(new[] { typeof(string) });
            return (PassThruExpression)RegexConstructor.Invoke(new[] { InputLines });
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
                .Select(PtEnumValue => PtEnumValue.ToString())
                .ToArray();

            // Find the return type here based on the first instance of a PTCommand type object on the array.
            string JoinedLines = string.Join("\n", InputLines);
            var EnumStringSelected = EnumTypesArray.FirstOrDefault(EnumObj => JoinedLines.Contains(EnumObj));
            return (PassThruCommandType)(string.IsNullOrWhiteSpace(EnumStringSelected) ? 
                PassThruCommandType.NONE : Enum.Parse(typeof(PassThruCommandType), EnumStringSelected));
        }
        /// <summary>
        /// Takes an input set of PTExpressions and writes them to a file object desired.
        /// </summary>
        /// <param name="InputExpressions">Expression input objects</param>
        /// <returns>Path of our built expression file</returns>
        public static string SaveExpressionsFile(this PassThruExpression[] InputExpressions, string BaseFileName = "")
        {
            // Get a logger object for saving expression sets.
            var ExpressionLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("ExpressionLogger")) ?? new SubServiceLogger("ExpressionLogger");

            // First build our output location for our file.
            string OutputFolder = Path.Combine(LogBroker.BaseOutputPath, "FulcrumExpressions");
            string FinalOutputPath =
                BaseFileName.Contains(Path.DirectorySeparatorChar) ?
                    Path.ChangeExtension(Path.Combine(
                        Path.GetDirectoryName(BaseFileName), $"FulcrumExpressions_{Path.GetFileName(BaseFileName)}"),
                "ptExp") :
                    BaseFileName.Length == 0 ? 
                        Path.Combine(OutputFolder, $"FulcrumExpressions_{DateTime.Now:MMddyyyy-HHmmss}.ptExp") : 
                        Path.Combine(OutputFolder, $"FulcrumExpressions_{Path.GetFileNameWithoutExtension(BaseFileName)}.ptExp");

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
                    .SelectMany(InputObj => (InputObj + "\n").Split('\n'))
                    .ToList();

                // Log information and write output.
                ExpressionLogger.WriteLog($"CONVERTED INPUT OBJECTS INTO A TOTAL OF {OutputExpressionStrings.Count} LINES OF TEXT!", LogType.WarnLog);
                ExpressionLogger.WriteLog("WRITING OUTPUT CONTENTS NOW...", LogType.WarnLog);
                File.WriteAllText(FinalOutputPath, string.Join("\n", OutputExpressionStrings));
                
                // Remove the Expressions Logger. Log done and return
                ExpressionLogger.WriteLog("DONE LOGGING OUTPUT CONTENT! RETURNING OUTPUT VALUES NOW");
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

        // -----------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Build a new regex model object from a given name value for a regex.
        /// </summary>
        /// <param name="RegexName"></param>
        /// <returns></returns>
        public static PassThruRegexModel GetRegexByName(this PassThruRegexModel[] RegexModelSet, string RegexName) {
            return RegexModelSet.FirstOrDefault(RegexObj => RegexObj.ExpressionName.ToUpper().Contains(RegexName.ToUpper()));
        }
        /// <summary>
        /// Build a new regex model object from a given name value for a regex.
        /// </summary>
        /// <param name="RegexName"></param>
        /// <returns></returns>
        public static PassThruRegexModel GetRegexByName(this ObservableCollection<PassThruRegexModel> RegexModelSet, string RegexName) {
            return RegexModelSet.FirstOrDefault(RegexObj => RegexObj.ExpressionName.ToUpper().Contains(RegexName.ToUpper()));
        }

        // -----------------------------------------------------------------------------------------------------------------

    }
}
