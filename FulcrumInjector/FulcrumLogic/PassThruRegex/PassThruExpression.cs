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
        [Description("PassThruExpresssion")]        NONE,
        [Description("PassThruOpenRegex")]          PTOpen,
        [Description("PassThruCloseRegex")]         PTClose,
        [Description("PassThruConnectRegex")]       PTConnect,
        [Description("PassThruDisconnectRegex")]    PTDisconnect,
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
        public readonly Regex TimeRegex = new Regex(@"(\d+\.\d+s)\s+(\+\+|--|!!|\*\*)\s+PT");
        public readonly Regex PtErrorRegex = new Regex(@"(\d+\.\d+s)\s+(\d+:[^\n]+)");

        // String Values for Command
        public readonly string CommandLines;
        public readonly string[] SplitCommandLines;

        // Input command time and result values for regex searching.
        [PtRegexResult("Time Issued", "", new[] { "Timestamp Valid", "Invalid Timestamp" })]
        public readonly string ExecutionTime;       // Execution time of the command.
        
        [PtRegexResult("J2534 Error", "0:STATUS_NOERROR", new[] { "Command Passed", "Command Failed" })] 
        public readonly string JErrorResult;        // J2534 Result Error

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
            this.ExecutionTime = TimeMatch.Success ? TimeMatch.Groups[1].Value : "REGEX_FAILED";
            this.JErrorResult = ErrorMatch.Success ? ErrorMatch.Groups[1].Value : "REGEX_FAILED";
        }

        // --------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// ToString override to show the strings of the command here.
        /// </summary>
        /// <returns>String formatted table of the output.</returns>
        public override string ToString()
        {
            // Find Field object values here.
            var ResultFieldInfos = this.GetType()
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(MemberObj => MemberObj.GetCustomAttribute(typeof(PtRegexResult)) != null)
                .ToArray();

            // Build default Tuple LIst value set and apply new values into it from property attributes
            var RegexResultTuples = new List<Tuple<string, string, string>>() {
                new("Command Type", this.TypeOfExpression.ToString(), this.ExpressionPassed() ? "Parse Passed" : "Parse Failed")
            };

            // Now find ones with the attribute and pull value
            RegexResultTuples.AddRange(ResultFieldInfos.Select(MemberObj =>
            {
                // Pull the ResultAttribute object.
                FieldInfo InvokerField = (FieldInfo)MemberObj;
                string CurrentValue = InvokerField.GetValue(this).ToString();

                // Now cast the result attribute of the member and store the value of it.
                var ResultValue = (PtRegexResult)MemberObj
                    .GetCustomAttributes(typeof(PtRegexResult))
                    .FirstOrDefault();

                // Build our output tuple object here. Compare current value to the desired one and return a state value.
                return new Tuple<string, string, string>(ResultValue.ResultName, CurrentValue, ResultValue.ResultState(CurrentValue));
            }).ToArray());

            // Build a text table object here.
            string RegexValuesOutputString = RegexResultTuples.ToStringTable(
                new[] { "Value Name", "Determined Value", "Value Status" },
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
                var ResultAttribute = (PtRegexResult)FieldObj.GetCustomAttributes(typeof(PtRegexResult)).FirstOrDefault();

                // Now compare value to the passed/failed setup.
                return ResultAttribute.ResultState(CurrentValue) == ResultAttribute.ResultValue;
            });

            // Now see if all the values in the Results array passed.
            return ResultsPassed.All(ValueObj => ValueObj);
        }
    }
}
