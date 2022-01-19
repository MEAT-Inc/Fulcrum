using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using FulcrumInjector.FulcrumViewContent.Models.ModelShares;

namespace FulcrumInjector.FulcrumLogic.PassThruRegex
{
    /// <summary>
    /// The names of the command types.
    /// Matches a type for the PT Command to a regex class type.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PassThruCommandType
    {
        // Command Types for PassThru Regex
        [EnumMember(Value = "NONE")] [Description("PassThruExpresssion")]               NONE,
        [EnumMember(Value = "PTOpen")] [Description("PassThruOpenRegex")]               PTOpen,
        [EnumMember(Value = "PTClose")] [Description("PassThruCloseRegex")]             PTClose,
        [EnumMember(Value = "PTConnect")] [Description("PassThruConnectRegex")]         PTConnect,
        [EnumMember(Value = "PTDisconnect")] [Description("PassThruDisconnectRegex")]   PTDisconnect,
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
        public readonly PassThruRegexModel TimeRegex = PassThruExpressionShare.PassThruTime;
        public readonly PassThruRegexModel StatusCodeRegex = PassThruExpressionShare.PassThruStatus;

        // String Values for Command
        public readonly string CommandLines;
        public readonly string[] SplitCommandLines;

        // Input command time and result values for regex searching.
        [PassThruRegexResult("Time Issued", "", new[] { "Timestamp Valid", "Invalid Timestamp" })]
        public readonly string ExecutionTime;       // Execution time of the command.
        
        [PassThruRegexResult("J2534 Status", "0:STATUS_NOERROR", new[] { "Command Passed", "Command Failed" })] 
        public readonly string JStatusCode;        // J2534 Result Error

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
            this.TimeRegex.Evaluate(this.CommandLines, out this.ExecutionTime);
            this.StatusCodeRegex.Evaluate(this.CommandLines, out this.JStatusCode);
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
                .Where(MemberObj => MemberObj.GetCustomAttribute(typeof(PassThruRegexResult)) != null)
                .ToArray();

            // Build default Tuple LIst value set and apply new values into it from property attributes
            var RegexResultTuples = new List<Tuple<string, string, string>>() {
                new("J2534 Command", this.TypeOfExpression.ToString(), this.ExpressionPassed() ? "Parse Passed" : "Parse Failed")
            };

            // Now find ones with the attribute and pull value
            RegexResultTuples.AddRange(ResultFieldInfos.Select(MemberObj =>
            {
                // Pull the ResultAttribute object.
                FieldInfo InvokerField = (FieldInfo)MemberObj;
                string CurrentValue = InvokerField.GetValue(this).ToString().Trim();

                // Now cast the result attribute of the member and store the value of it.
                var ResultValue = (PassThruRegexResult)MemberObj
                    .GetCustomAttributes(typeof(PassThruRegexResult))
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

            // Split lines, build some splitting strings, and return output.
            string SplitString = string.Join("", Enumerable.Repeat("=", 100));
            string[] SplitTable = RegexValuesOutputString.Split('\n')
                .Select(StringObj => "   " + StringObj.Trim())
                .ToArray();

            // Store string to replace and build new list of strings
            var NewLines = new List<string>() { SplitString }; NewLines.Add("\r");
            NewLines.AddRange(this.SplitCommandLines.Select(CmdLine => $"   {CmdLine}"));
            NewLines.Add("\n");

            // Add our breakdown contents here.
            NewLines.Add(SplitTable[0]); NewLines.AddRange(SplitTable.Skip(1).Take(SplitTable.Length - 2)); 
            NewLines.Add(SplitTable.FirstOrDefault()); NewLines.Add("\n"); NewLines.Add(SplitString); 

            // Remove double newlines. Command lines are split with \r so this doesn't apply.
            RegexValuesOutputString = string.Join("\n", NewLines).Replace("\n\n", "\n");
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
                var CurrentValue = FieldObj.GetValue(this).ToString().Trim();
                var ResultAttribute = (PassThruRegexResult)FieldObj.GetCustomAttributes(typeof(PassThruRegexResult)).FirstOrDefault();

                // Now compare value to the passed/failed setup.
                return ResultAttribute.ResultState(CurrentValue) == ResultAttribute.ResultValue;
            });

            // Now see if all the values in the Results array passed.
            return ResultsPassed.All(ValueObj => ValueObj);
        }
    }
}
