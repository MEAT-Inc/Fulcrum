using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace FulcrumInjector.FulcrumLogic.PassThruRegex
{
    /// <summary>
    /// The names of the command types.
    /// Matches a type for the PT Command to a regex class type.
    /// </summary>
    public enum PassThruCommandType
    {
        // Command Types for PassThru Regex
        [Description("PassThruExpresssion")]   NONE,
        [Description("PassThruOpenRegex")]     PTOpen,
        [Description("PassThruCloseRegex")]    PTClose,
    }
    
    // --------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// This class instance is used to help configure the Regex tools and commands needed to perform highlighting on output from
    /// the shim DLL.
    /// </summary>
    public class PassThruExpression
    {
        // Time values for the Regex on the command.
        public readonly PassThruCommandType TypeOfExpression;
        public readonly Regex TimeRegex = new Regex(@"(\d+\.\d+s)\s+(\+\+|--|!!|\*\*)\s+");
        public readonly Regex PtErrorRegex = new Regex(@"(\d+\.\d+s)\s+(\d+:[^\n]+)");

        // String Values for Command
        public readonly string CommandLines;
        public readonly string[] SplitCommandLines;

        // Input command time and result values for regex searching.
        [ResultAttribute("Time")]   public readonly string ExecutionTime;
        [ResultAttribute("Error", "0:STATUS_NOERROR")]  public readonly string JErrorResult;

        // --------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new set of PassThruCommand Regex Operations
        /// </summary>
        /// <param name="CommandInput">Input command string</param>
        public PassThruExpression(string CommandInput, PassThruCommandType ExpressionType)
        {
            // Store input lines
            this.CommandLines = CommandInput;
            this.TypeOfExpression = ExpressionType;
            this.SplitCommandLines = CommandInput.Split('\n');

            // Match values here with regex values.
            var TimeMatch = this.TimeRegex.Match(this.CommandLines);
            var ErrorMatch = this.PtErrorRegex.Match(this.CommandLines);

            // Store values based on results.
            this.ExecutionTime = TimeMatch.Success ? TimeMatch.Value : "REGEX_FAILED";
            this.JErrorResult = ErrorMatch.Success ? ErrorMatch.Value : "REGEX_FAILED";
        }

        // --------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// ToString override to show the strings of the command here.
        /// </summary>
        /// <returns>String formatted table of the output.</returns>
        public override string ToString()
        {
            // Find Field object values here.
            var ResultFieldInfos = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var RegexResultTuples = ResultFieldInfos.Select(FieldObj =>
            {
                // Pull the ResultAttribute object.
                var CurrentValue = FieldObj.GetValue(this).ToString();
                var ResultValue = (ResultAttribute)FieldObj.GetCustomAttributes(typeof(ResultAttribute)).FirstOrDefault();
                return new Tuple<string, string, string>(
                    ResultValue.ResultName,
                    CurrentValue,
                    ResultValue.CheckValue(CurrentValue) ? "Result Valid" : "Invalid Result!"
                );
            }).ToArray();

            // Prepend a new Tuple with the type of command the regex name set
            RegexResultTuples = (Tuple<string, string, string>[])RegexResultTuples.Prepend(
                new Tuple<string, string, string>(
                    "Command Type",
                    this.TypeOfExpression.ToString(),
                    this.ExpressionPassed() ? "Regex Valid" : "Regex Failed"));

            // Build a text table object here.
            string RegexValuesOutputString = RegexResultTuples.ToStringTable(
                new[] { "Value Name", "Current Value", "Value Status" },
                RegexObj => RegexObj.Item1,
                RegexObj => RegexObj.Item2,
                RegexObj => RegexObj.Item3
            );

            // Now return the output string values here.
            return RegexValuesOutputString;
        }
        /// <summary>
        /// Expression evaluation computed result based on output values.
        /// </summary>
        /// <returns>True or false based on expression eval results.</returns>
        public bool ExpressionPassed()
        {
            // Returns true if passing matched for time
            var ResultFieldInfos = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var ResultsPassed = ResultFieldInfos.Select(FieldObj =>
            {
                // Pull the ResultAttribute object.
                var CurrentValue = FieldObj.GetValue(this).ToString();
                var ResultValue = (ResultAttribute)FieldObj.GetCustomAttributes(typeof(ResultAttribute)).FirstOrDefault();

                // Now compare value to the passed/failed setup.
                return ResultValue.CheckValue(CurrentValue);
            });

            // Now see if all the values in the Results array passed.
            return ResultsPassed.All(ValueObj => ValueObj);
        }

        // ---------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Splits an input content string into a set fo PT Command objects which are split into objects.
        /// </summary>
        /// <param name="FileContents">Input file object content</param>
        /// <returns>Returns a set of file objects which contain the PT commands from a file.</returns>
        public static string[] SplitFileIntoCommands(string FileContents)
        {
            // Build regex objects to help split input content into sets.
            var TimeRegex = new Regex(@"(\d+\.\d+s)\s+(\+\+|--|!!|\*\*)\s+");
            var PtErrorRegex = new Regex(@"(\d+\.\d+s)\s+(\d+:[^\n]+)");

            // Make an empty array of strings and then begin splitting.
            List<string> OutputLines = new List<string>();
            for (int CharIndex = 0; CharIndex < FileContents.Length;)
            {
                // Find the first index of a time entry and the close command index.
                int TimeStartIndex = TimeRegex.Match(FileContents, CharIndex).Index;
                int ErrorCloseIndex = PtErrorRegex.Match(FileContents, CharIndex, TimeStartIndex).Index;
                OutputLines.Add(FileContents.Substring(TimeStartIndex, ErrorCloseIndex));

                // Tick the current index value to our last closed index.
                CharIndex = ErrorCloseIndex;
            }

            // Return the built set of commands.
            return OutputLines.ToArray();
        }
    }
}
