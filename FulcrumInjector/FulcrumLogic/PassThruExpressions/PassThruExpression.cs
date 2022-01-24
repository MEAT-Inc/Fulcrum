using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.PassThruExpressions
{
    /// <summary>
    /// The names of the command types.
    /// Matches a type for the PT Command to a regex class type.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PassThruCommandType
    {
        // Command Types for PassThru Regex. Pulled values from settings parse into here.
        [EnumMember(Value = "NONE")] [Description("PassThruExpresssion")]                    NONE,
        [EnumMember(Value = "PTOpen")] [Description("PassThruOpenExpression")]               PTOpen,
        [EnumMember(Value = "PTClose")] [Description("PassThruCloseExpression")]             PTClose,
        [EnumMember(Value = "PTConnect")] [Description("PassThruConnectExpression")]         PTConnect,
        [EnumMember(Value = "PTDisconnect")] [Description("PassThruDisconnectExpression")]   PTDisconnect,
        [EnumMember(Value = "PTReadMsgs")] [Description("PassThruReadMessagesExpression")]   PTReadMsgs,
        [EnumMember(Value = "PTWriteMsgs")] [Description("PassThruWriteMessagesExpression")] PTWriteMsgs,
    }

    // --------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// This class instance is used to help configure the Regex tools and commands needed to perform highlighting on output from
    /// the shim DLL.
    /// </summary>
    public class PassThruExpression
    {
        // Logger Object
        protected internal SubServiceLogger ExpressionLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith($"{this.GetType().Name}Logger")) ?? new SubServiceLogger($"{this.GetType().Name}Logger");

        // String Values for Command content
        public readonly string CommandLines;
        public readonly string[] SplitCommandLines;

        // Time values for the Regex on the command.
        public readonly PassThruCommandType TypeOfExpression;
        public readonly PassThruRegexModel TimeRegex = PassThruRegexModelShare.PassThruTime;
        public readonly PassThruRegexModel StatusCodeRegex = PassThruRegexModelShare.PassThruStatus;

        // Input command time and result values for regex searching.
        [PtExpressionProperty("Time Issued", "", new[] { "Timestamp Valid", "Invalid Timestamp" })]
        public readonly string ExecutionTime;     
        [PtExpressionProperty("J2534 Status", "0:STATUS_NOERROR", new[] { "Command Passed", "Command Failed" })] 
        public readonly string JStatusCode;

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
                .Where(MemberObj => MemberObj.GetCustomAttribute(typeof(PtExpressionProperty)) != null)
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
                var ResultValue = (PtExpressionProperty)MemberObj
                    .GetCustomAttributes(typeof(PtExpressionProperty))
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
                var ResultAttribute = (PtExpressionProperty)FieldObj.GetCustomAttributes(typeof(PtExpressionProperty)).FirstOrDefault();

                // Now compare value to the passed/failed setup.
                return ResultAttribute.ResultState(CurrentValue) == ResultAttribute.ResultValue;
            });

            // Now see if all the values in the Results array passed.
            return ResultsPassed.All(ValueObj => ValueObj);
        }

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

            // Find command issue request values. (Pull using Base Class)
            var FieldsToSet = this.GetExpressionProperties(true);
            bool ExecutionTimeResult = this.TimeRegex.Evaluate(CommandInput, out var TimeStrings);
            bool StatusCodeResult = this.StatusCodeRegex.Evaluate(CommandInput, out var StatusCodeStrings);
            if (!ExecutionTimeResult || !StatusCodeResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string>();
            StringsToApply.AddRange(from NextIndex in this.TimeRegex.ExpressionValueGroups where NextIndex <= TimeStrings.Length select TimeStrings[NextIndex]);
            StringsToApply.AddRange(from NextIndex in this.StatusCodeRegex.ExpressionValueGroups where NextIndex <= StatusCodeStrings.Length select StatusCodeStrings[NextIndex]);

            // Now apply values using base method and exit out of this routine
            bool StorePassed = this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()); 
            if (!StorePassed) throw new InvalidOperationException("FAILED TO SET BASE CLASS VALUES FOR EXPRESSION OBJECT!"); 
            this.ExpressionLogger.WriteLog($"BUILT NEW EXPRESSION OBJECT WITH TYPE OF {this.GetType().Name}", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Gets the list of properties linked to a regex group and returns them in order of decleration
        /// </summary>
        protected internal FieldInfo[] GetExpressionProperties(bool BaseClassValues = false)
        {
            // Determine the type of base property to use
            var DeclaredTypeExpected = BaseClassValues ? 
                typeof(PassThruExpression) : this.GetType();

            // Pull our property values here.
            var PropertiesLocated = this.GetType()
                .GetFields().Where(FieldObj => FieldObj.DeclaringType == DeclaredTypeExpected)
                .Where(PropObj => Attribute.IsDefined(PropObj, typeof(PtExpressionProperty)))
                .OrderBy(PropObj => ((PtExpressionProperty)PropObj.GetCustomAttributes(typeof(PtExpressionProperty), false).Single()).LineNumber)
                .ToArray();

            // Return them here.
            return PropertiesLocated;
        }
        /// <summary>
        /// Sets the values of the output regex strings onto this class object ptExpression values
        /// </summary>
        /// <param name="FieldValueStrings">Strings to store</param>
        /// <param name="FieldObjects">Property infos</param>
        /// <returns>True if set. False if not</returns>
        protected internal bool SetExpressionProperties(FieldInfo[] FieldObjects, string[] FieldValueStrings)
        {
            // Make sure the count of properties matches the count of lines.
            if (FieldValueStrings.Length != FieldObjects.Length) {
                this.ExpressionLogger.WriteLog("EXPRESSIONS FOR FIELDS AND VALUES ARE NOT EQUAL SIZES! THIS IS FATAL!", LogType.FatalLog);
                return false;
            }

            // Loop the field objects and apply a new value one by one.
            for (int FieldIndex = 0; FieldIndex < FieldObjects.Length; FieldIndex++)
            {
                // Pull field value. Try and set it.
                var CurrentField = FieldObjects[FieldIndex];
                try { CurrentField.SetValue(this, FieldValueStrings[FieldIndex]); }
                catch (Exception SetEx)
                {
                    // Throw an exception output for this error type.
                    this.ExpressionLogger.WriteLog($"EXCEPTION THROWN DURING EXPRESSION VALUE STORE FOR COMMAND TYPE {this.GetType().Name}!", LogType.ErrorLog);
                    this.ExpressionLogger.WriteLog("EXCEPTION IS BEING LOGGED BELOW", SetEx);
                    return false;
                }
            }

            // Log passed, return output.
            this.ExpressionLogger.WriteLog($"UPDATED EXPRESSION VALUES FOR A TYPE OF {this.GetType().Name} OK!");
            return true;
        }

        // --------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls out all of our message content values and stores them into a list with details.
        /// </summary>
        protected internal void FindMessageContents()
        {

        }
    }
}
