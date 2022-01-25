using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
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
        [EnumMember(Value = "NONE")] [Description("PassThruExpresssion")]                               NONE,
        [EnumMember(Value = "PTOpen")] [Description("PassThruOpenExpression")]                          PTOpen,
        [EnumMember(Value = "PTClose")] [Description("PassThruCloseExpression")]                        PTClose,
        [EnumMember(Value = "PTConnect")] [Description("PassThruConnectExpression")]                    PTConnect,
        [EnumMember(Value = "PTDisconnect")] [Description("PassThruDisconnectExpression")]              PTDisconnect,
        [EnumMember(Value = "PTReadMsgs")] [Description("PassThruReadMessagesExpression")]              PTReadMsgs,
        [EnumMember(Value = "PTWriteMsgs")] [Description("PassThruWriteMessagesExpression")]            PTWriteMsgs,
        // TODO: Write PTStartPeriodic
        // TODO: Write PTStopPeriodic
        [EnumMember(Value = "PTStartMsgFilter")] [Description("PassThruStartMessageFilterExpression")]  PTStartMsgFilter,
        [EnumMember(Value = "PTStartMsgFilter")] [Description("PassThruStopMessageFilterExpression")]   PTStopMsgFilter,
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
            NewLines.AddRange(this.SplitCommandLines.Select(LineObj => "   " + LineObj));
            NewLines.Add("\n");

            // Add our breakdown contents here.
            NewLines.Add(SplitTable[0]);
            NewLines.AddRange(SplitTable.Skip(1).Take(SplitTable.Length - 2));
            NewLines.Add(SplitTable.FirstOrDefault()); NewLines.Add("\n");

            // Append in contents for message values if needed. 
            if (this.GetType() == typeof(PassThruReadMessagesExpression) || this.GetType() == typeof(PassThruWriteMessagesExpression))
            {
                // Log information, pull in new split table contents
                this.ExpressionLogger.WriteLog("APPENDING MESSAGE CONTENT VALUES NOW...", LogType.WarnLog);
                string MessagesTable = this.FindMessageContents(out _);

                // Append the new table of messages into the current output.
                NewLines.AddRange(MessagesTable.Split('\n').Select(LineObj => "   " + LineObj).ToArray());
                this.ExpressionLogger.WriteLog("PULLED IN NEW MESSAGES CONTENTS CORRECTLY!", LogType.InfoLog);

                // Splitting end line.
                NewLines.Add("\n");
            }

            // Append in contents for a filter message set if needed.
            if (this.GetType() == typeof(PassThruStartMessageFilterExpression))
            {
                // Log information, pull in split table contents.
                this.ExpressionLogger.WriteLog("APPENDING FILTER CONTENT VALUES NOW...", LogType.WarnLog);
                string FilterMessageTable = this.FindFilterContents(out _);

                // Append the new values for the messages into our output strings now.
                NewLines.AddRange(FilterMessageTable.Split('\n').Select(LineObj => "   " + LineObj).ToArray());
                this.ExpressionLogger.WriteLog("PULLED IN NEW MESSAGES FOR FILTER CONTENTS CORRECTLY!", LogType.InfoLog);

                // Splitting end line.
                NewLines.Add("\n");
            }

            // Remove double newlines. Command lines are split with \r so this doesn't apply.
            NewLines.Add(SplitString);
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
        protected internal string FindMessageContents(out List<string[]> MessageProperties)
        {
            // Check if not read or write types. 
            if (this.GetType() != typeof(PassThruReadMessagesExpression) && this.GetType() != typeof(PassThruWriteMessagesExpression)) {
                this.ExpressionLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON READ OR WRITE COMMAND TYPE!", LogType.ErrorLog);
                MessageProperties = new List<string[]>();
                return string.Empty;
            }

            // Pull the object, find our matches based on our type object value.
            var MessageContentRegex = this.GetType() == typeof(PassThruReadMessagesExpression) ?
                PassThruRegexModelShare.MessageReadInfo : PassThruRegexModelShare.MessageSentInfo;

            // Make our value lookup table here and output tuples
            var RegexResultTuples = new List<Tuple<string, string>>();
            bool IsReadExpression = this.GetType() == typeof(PassThruReadMessagesExpression);
            List<string> ResultStringTable = new List<string>() { "Message Number" };

            // Fill in strings for property type values here.
            if (IsReadExpression) ResultStringTable.AddRange(new[] { "TimeStamp", "Protocol ID", "Data Count", "Rx Flags", "Flag Value", "Message Data" });
            else ResultStringTable.AddRange(new[] { "Protocol ID", "Data Count", "Tx Flags", "Flag Value", "Message Data" });

            // Split input command lines by the "Msg[x]" identifier and then regex match all of the outputs.
            string[] SplitMessageLines = this.CommandLines.Split(new[] { "Msg" }, StringSplitOptions.None)
                .Where(LineObj => LineObj.StartsWith("["))
                .Select(LineObj => "Msg" + LineObj)
                .ToArray();

            // If no messages are found during the split process, then we need to return out.
            if (SplitMessageLines.Length == 0) {
                this.ExpressionLogger.WriteLog($"WARNING! NO MESSAGES FOUND FOR MESSAGE COMMAND! TYPE OF MESSAGE COMMAND WAS {this.GetType().Name}!");
                MessageProperties = new List<string[]>();
                return "No Messages Found!";
            }

            // Now run each of them thru here.
            MessageProperties = new List<string[]>();
            List<string> OutputMessages = new List<string>();
            foreach (var MsgLineSet in SplitMessageLines)
            {
                // RegexMatch output here.
                bool MatchedContent = MessageContentRegex.Evaluate(MsgLineSet, out var MatchedMessageStrings);
                if (!MatchedContent) {
                    this.ExpressionLogger.WriteLog("NO MATCH FOUND FOR MESSAGES! MOVING ON", LogType.WarnLog);
                    continue;
                }

                // Make sure the value for Flags is not zero. If it is, then we need to insert a "No Value" object
                var TempList = MatchedMessageStrings.ToList();
                int IndexOfZeroFlags = TempList.IndexOf("0x00000000");
                if (IndexOfZeroFlags != -1) { TempList.Insert(IndexOfZeroFlags + 1, "No Value"); }
                MatchedMessageStrings = TempList.ToArray();

                // Remove any and all whitespace values from our output content here.
                string[] SelectedStrings = MatchedMessageStrings
                    .Skip(1)
                    .Where(StringObj => !string.IsNullOrEmpty(StringObj))
                    .ToArray();

                // Now loop each part of the matched content and add values into our output tuple set.
                RegexResultTuples.AddRange(SelectedStrings
                    .Select((T, StringIndex) => new Tuple<string, string>(ResultStringTable[StringIndex], T)));

                // Build our output table once all our values have been appended in here.
                string RegexValuesOutputString = RegexResultTuples.ToStringTable(
                    new[] { "Filter Message Property", "Filter Message Value" },
                    RegexObj => RegexObj.Item1,
                    RegexObj => RegexObj.Item2
                );

                // Add this string to our list of messages.
                OutputMessages.Add(RegexValuesOutputString);
                MessageProperties.Add(RegexResultTuples.Select(TupleObj => TupleObj.Item2).ToArray());
                this.ExpressionLogger.WriteLog("ADDED NEW MESSAGE OBJECT FOR COMMAND OK!", LogType.InfoLog);
            }

            // Return built table string object.
            this.ExpressionLogger.WriteLog("BUILT OUTPUT EXPRESSIONS FOR MESSAGE CONTENTS OK!", LogType.InfoLog);
            return string.Join("\n", OutputMessages);
        }
        /// <summary>
        /// Pulls out the filter contents of this command as messages and pulls them back. One entry per filter property
        /// If we have a Flow filter it's 3 lines. All others would be 2 line .
        /// </summary>
        /// <param name="FilterProperties">Properties of filter pulled</param>
        /// <returns>Text String table for filter messages.</returns>
        protected internal string FindFilterContents(out List<string[]> FilterProperties)
        {
            // Check if we can use this method or not.
            if (this.GetType() != typeof(PassThruStartMessageFilterExpression)) {
                this.ExpressionLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON PTSTART FILTER COMMAND TYPE!", LogType.ErrorLog);
                FilterProperties = new List<string[]>();
                return string.Empty;
            }

            // Make our value lookup table here and output tuples.
            List<string> ResultStringTable = new List<string>()
            {
                "Message Type",     // Mask Pattern or Flow
                "Message Number",   // Always 0
                "Protocol ID",      // Protocol Of Message
                "Message Size",     // Size of message
                "Tx Flags",         // Tx Flags
                "Flag Value",       // String Flag Value
                "Message Content"   // Content of the filter message
            };

            // Split input command lines by the "Msg[x]" identifier and then regex match all of the outputs.
            List<string> CombinedOutputs = new List<string>();
            string[] SplitMessageLines = Regex.Split(this.CommandLines, @"\s+(Mask|Pattern|FlowControl)").Skip(1).ToArray();
            for (int LineIndex = 0; LineIndex < SplitMessageLines.Length; LineIndex++)
            {
                // Append based on line value input here.
                CombinedOutputs.Add(LineIndex + 1 >= SplitMessageLines.Length
                    ? SplitMessageLines[LineIndex]
                    : string.Join(string.Empty, SplitMessageLines.Skip(LineIndex).Take(2)));

                // Check index value.
                if (LineIndex + 1 >= SplitMessageLines.Length) break;
                LineIndex += 1;
            }

            // Check if no values were pulled. If this is the case then dump out.
            if (SplitMessageLines.Length == 0) {
                this.ExpressionLogger.WriteLog($"WARNING! NO MESSAGES FOUND FOR MESSAGE COMMAND! TYPE OF MESSAGE COMMAND WAS {this.GetType().Name}!");
                FilterProperties = new List<string[]>();
                return "No Filter Content Found!";
            }

            // Setup Loop constants for parsing operations
            FilterProperties = new List<string[]>();
            List<string> OutputMessages = new List<string>();
            var MessageContentRegex = PassThruRegexModelShare.MessageFilterInfo;

            // Now parse out our content matches
            SplitMessageLines = CombinedOutputs.ToArray();
            foreach (var MsgLineSet in SplitMessageLines)
            {
                // RegexMatch output here.
                var OutputMessageTuple = new List<Tuple<string, string>>();
                bool MatchedContent = MessageContentRegex.Evaluate(MsgLineSet, out var MatchedMessageStrings);
                if (!MatchedContent) 
                {
                    // Check if this is a null flow control instance
                    if (MsgLineSet.Trim() != "FlowControl is NULL") {
                        this.ExpressionLogger.WriteLog("NO MATCH FOUND FOR MESSAGES! MOVING ON", LogType.WarnLog);
                        continue;
                    }

                    // Add null flow control here.
                    OutputMessageTuple.Add(new Tuple<string, string>(ResultStringTable[1], "FlowControl"));
                    OutputMessageTuple.Add(new Tuple<string, string>(ResultStringTable[2], "-1"));
                    for (int TupleIndex = 3; TupleIndex < ResultStringTable.Count; TupleIndex++)
                        OutputMessageTuple.Add(new Tuple<string, string>(ResultStringTable[TupleIndex], "NULL"));

                    // Log Expression found and continue.
                    this.ExpressionLogger.WriteLog("FOUND NULL FLOW CONTROL! PARSING AND MOVING ON...", LogType.InfoLog);
                }

                // Ensure we fill in gaps for filter content if flags are not an option
                if (MatchedMessageStrings.Any(StringObj => StringObj == "0x00000000")) {
                    int IndexOfNoFlag = MatchedMessageStrings.ToList().IndexOf("0x00000000");
                    var TempList = MatchedMessageStrings.ToList();
                    TempList.Insert(IndexOfNoFlag, "No Flags");
                    MatchedMessageStrings = TempList.ToArray();
                }

                // Knock out any of the whitespace values.
                MatchedMessageStrings = MatchedMessageStrings
                    .Skip(1)
                    .Where(StringObj => !string.IsNullOrEmpty(StringObj))
                    .ToArray();

                // Now loop each part of the matched content and add values into our output tuple set.
                OutputMessageTuple.AddRange(MatchedMessageStrings
                    .Select((T, StringIndex) => new Tuple<string, string>(ResultStringTable[StringIndex], T)));

                // Build our output table once all our values have been appended in here.
                string RegexValuesOutputString = OutputMessageTuple.ToStringTable(
                    new[] { "Message Property", "Message Value" },
                    RegexObj => RegexObj.Item1,
                    RegexObj => RegexObj.Item2
                );

                // Add this string to our list of messages.
                OutputMessages.Add(RegexValuesOutputString + "\n");
                FilterProperties.Add(OutputMessageTuple.Select(TupleObj => TupleObj.Item2).ToArray());
                this.ExpressionLogger.WriteLog("ADDED NEW MESSAGE OBJECT FOR FILTER COMMAND OK!", LogType.InfoLog);
            }

            // Return built table string object.
            this.ExpressionLogger.WriteLog("BUILT OUTPUT EXPRESSIONS FOR MESSAGE FILTER CONTENTS OK!", LogType.InfoLog);
            return string.Join("\n", OutputMessages);
        }
    }
}
