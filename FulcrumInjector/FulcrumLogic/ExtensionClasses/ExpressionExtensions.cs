using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
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
        /// Pulls out all of our message content values and stores them into a list with details.
        /// </summary>
        public static string FindMessageContents(this PassThruExpression ExpressionObject, out List<string[]> MessageProperties)
        {
            // Check if not read or write types. 
            if (ExpressionObject.GetType() != typeof(PassThruReadMessagesExpression) && ExpressionObject.GetType() != typeof(PassThruWriteMessagesExpression)) {
                ExpressionObject.ExpressionLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON READ OR WRITE COMMAND TYPE!", LogType.ErrorLog);
                MessageProperties = new List<string[]>();
                return string.Empty;
            }

            // Pull the object, find our matches based on our type object value.
            var MessageContentRegex = ExpressionObject.GetType() == typeof(PassThruReadMessagesExpression) ?
                PassThruRegexModelShare.MessageReadInfo : PassThruRegexModelShare.MessageSentInfo;

            // Make our value lookup table here and output tuples
            var RegexResultTuples = new List<Tuple<string, string>>();
            bool IsReadExpression = ExpressionObject.GetType() == typeof(PassThruReadMessagesExpression);
            List<string> ResultStringTable = new List<string>() { "Message Number" };

            // Fill in strings for property type values here.
            if (IsReadExpression) ResultStringTable.AddRange(new[] { "TimeStamp", "Protocol ID", "Data Count", "RX Flags", "Flag Value", "Message Data" });
            else ResultStringTable.AddRange(new[] { "Protocol ID", "Data Count", "TX Flags", "Flag Value", "Message Data" });

            // Split input command lines by the "Msg[x]" identifier and then regex match all of the outputs.
            string[] SplitMessageLines = ExpressionObject.CommandLines.Split(new[] { "Msg" }, StringSplitOptions.None)
                .Where(LineObj => LineObj.StartsWith("["))
                .Select(LineObj => "Msg" + LineObj)
                .ToArray();

            // If no messages are found during the split process, then we need to return out.
            if (SplitMessageLines.Length == 0) {
                ExpressionObject.ExpressionLogger.WriteLog($"WARNING! NO MESSAGES FOUND FOR MESSAGE COMMAND! TYPE OF MESSAGE COMMAND WAS {ExpressionObject.GetType().Name}!");
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
                    ExpressionObject.ExpressionLogger.WriteLog("NO MATCH FOUND FOR MESSAGES! MOVING ON", LogType.WarnLog);
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

                // Format our message data content to include a 0x before the data byte and caps lock message bytes.
                int LastStringIndex = SelectedStrings.Length - 1;
                SelectedStrings[LastStringIndex] = string.Join(" ",
                    SelectedStrings[LastStringIndex]
                        .Split(' ')
                        .Select(StringPart => $"0x{StringPart.Trim().ToUpper()}")
                        .ToArray()
                    );

                // Now loop each part of the matched content and add values into our output tuple set.
                RegexResultTuples.AddRange(SelectedStrings
                    .Select((T, StringIndex) => new Tuple<string, string>(ResultStringTable[StringIndex], T)));

                // Build our output table once all our values have been appended in here.
                string RegexValuesOutputString = RegexResultTuples.ToStringTable(
                    new[] { "Message Property", "Message Value" },
                    RegexObj => RegexObj.Item1,
                    RegexObj => RegexObj.Item2
                );

                // Add this string to our list of messages.
                OutputMessages.Add(RegexValuesOutputString);
                MessageProperties.Add(RegexResultTuples.Select(TupleObj => TupleObj.Item2).ToArray());
                ExpressionObject.ExpressionLogger.WriteLog("ADDED NEW MESSAGE OBJECT FOR COMMAND OK!", LogType.InfoLog);
            }

            // Return built table string object.
            ExpressionObject.ExpressionLogger.WriteLog("BUILT OUTPUT EXPRESSIONS FOR MESSAGE CONTENTS OK!", LogType.InfoLog);
            return string.Join("\n", OutputMessages);
        }
        /// <summary>
        /// Pulls out the filter contents of this command as messages and pulls them back. One entry per filter property
        /// If we have a Flow filter it's 3 lines. All others would be 2 line .
        /// </summary>
        /// <param name="FilterProperties">Properties of filter pulled</param>
        /// <returns>Text String table for filter messages.</returns>
        public static string FindFilterContents(this PassThruExpression ExpressionObject, out List<string[]> FilterProperties)
        {
            // Check if we can use this method or not.
            if (ExpressionObject.GetType() != typeof(PassThruStartMessageFilterExpression)) {
                ExpressionObject.ExpressionLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON PTSTART FILTER COMMAND TYPE!", LogType.ErrorLog);
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
                "TX Flags",         // Tx Flags
                "Flag Value",       // String Flag Value
                "Message Content"   // Content of the filter message
            };

            // Split input command lines by the "Msg[x]" identifier and then regex match all of the outputs.
            List<string> CombinedOutputs = new List<string>();
            string[] SplitMessageLines = Regex.Split(ExpressionObject.CommandLines, @"\s+(Mask|Pattern|FlowControl)").Skip(1).ToArray();
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
                ExpressionObject.ExpressionLogger.WriteLog($"WARNING! NO MESSAGES FOUND FOR MESSAGE COMMAND! TYPE OF MESSAGE COMMAND WAS {ExpressionObject.GetType().Name}!");
                FilterProperties = new List<string[]>();
                return "No Filter Content Found!";
            }

            // Setup Loop constants for parsing operations
            FilterProperties = new List<string[]>();
            List<string> OutputMessages = new List<string>();
            var MessageContentRegex = PassThruRegexModelShare.MessageFilterInfo;

            // Now parse out our content matches. Add a trailing newline to force matches.
            SplitMessageLines = CombinedOutputs.Select(LineSet => LineSet + "\n").ToArray();
            foreach (var MsgLineSet in SplitMessageLines)
            {
                // RegexMatch output here.
                var OutputMessageTuple = new List<Tuple<string, string>>();
                bool MatchedContent = MessageContentRegex.Evaluate(MsgLineSet, out var MatchedMessageStrings);
                if (!MatchedContent)
                {
                    // Check if this is a null flow control instance
                    if (MsgLineSet.Trim() != "FlowControl is NULL") {
                        ExpressionObject.ExpressionLogger.WriteLog("NO MATCH FOUND FOR MESSAGES! MOVING ON", LogType.WarnLog);
                        continue;
                    }

                    // Add null flow control here.
                    OutputMessageTuple.Add(new Tuple<string, string>(ResultStringTable[1], "FlowControl"));
                    OutputMessageTuple.Add(new Tuple<string, string>(ResultStringTable[2], "-1"));
                    for (int TupleIndex = 3; TupleIndex < ResultStringTable.Count; TupleIndex++)
                        OutputMessageTuple.Add(new Tuple<string, string>(ResultStringTable[TupleIndex], "NULL"));

                    // Log Expression found and continue.
                    ExpressionObject.ExpressionLogger.WriteLog("FOUND NULL FLOW CONTROL! PARSING AND MOVING ON...", LogType.InfoLog);
                }

                // Make sure the value for Flags is not zero. If it is, then we need to insert a "No Value" object
                var TempList = MatchedMessageStrings.ToList();
                int IndexOfZeroFlags = TempList.IndexOf("0x00000000");
                if (IndexOfZeroFlags != -1) { TempList.Insert(IndexOfZeroFlags + 1, "No Value"); }
                MatchedMessageStrings = TempList.ToArray();

                // Knock out any of the whitespace values.
                MatchedMessageStrings = MatchedMessageStrings
                    .Skip(1)
                    .Where(StringObj => !string.IsNullOrEmpty(StringObj))
                    .ToArray();

                // Format our message data content to include a 0x before the data byte and caps lock message bytes.
                int LastStringIndex = MatchedMessageStrings.Length - 1;
                MatchedMessageStrings[LastStringIndex] = string.Join(" ",
                    MatchedMessageStrings[LastStringIndex]
                        .Split(' ')
                        .Select(StringPart => $"0x{StringPart.Trim().ToUpper()}")
                        .ToArray()
                );

                // Now loop each part of the matched content and add values into our output tuple set.
                OutputMessageTuple.AddRange(MatchedMessageStrings
                    .Select((T, StringIndex) => new Tuple<string, string>(ResultStringTable[StringIndex], T)));

                // Build our output table once all our values have been appended in here.
                string RegexValuesOutputString = OutputMessageTuple.ToStringTable(
                    new[] { "Filter Message Property", "Filter Message Value" },
                    RegexObj => RegexObj.Item1,
                    RegexObj => RegexObj.Item2
                );

                // Add this string to our list of messages.
                OutputMessages.Add(RegexValuesOutputString + "\n");
                FilterProperties.Add(OutputMessageTuple.Select(TupleObj => TupleObj.Item2).ToArray());
                ExpressionObject.ExpressionLogger.WriteLog("ADDED NEW MESSAGE OBJECT FOR FILTER COMMAND OK!", LogType.InfoLog);
            }

            // Return built table string object.
            ExpressionObject.ExpressionLogger.WriteLog("BUILT OUTPUT EXPRESSIONS FOR MESSAGE FILTER CONTENTS OK!", LogType.InfoLog);
            return string.Join("\n", OutputMessages);
        }
        /// <summary>
        /// Finds all the parameters of the IOCTL command output from the input content
        /// </summary>
        /// <param name="ParameterProperties">Properties to return out.</param>
        /// <returns>The properties of the IOCTL as a string table.</returns>
        public static string FindIoctlParameters(this PassThruExpression ExpressionObject, out Tuple<string, string, string>[] ParameterProperties)
        {
            // Check if we can run this type for the given object.
            if (ExpressionObject.GetType() != typeof(PassThruIoctlExpression)) {
                ExpressionObject.ExpressionLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON IOCTL COMMAND TYPE!", LogType.ErrorLog);
                ParameterProperties = Array.Empty<Tuple<string, string, string>>();
                return string.Empty;
            }

            // Try and parse out the IOCTL Command objects from the input strings here.
            var IoctlRegex = PassThruRegexModelShare.IoctlParameterValue;
            bool IoctlResults = IoctlRegex.Evaluate(ExpressionObject.CommandLines, out var IoctlResultStrings);
            if (!IoctlResults) {
                ExpressionObject.ExpressionLogger.WriteLog("NO IOCTL COMMAND OBJECTS FOUND! RETURNING NO VALUES OUTPUT NOW...", LogType.WarnLog);
                ParameterProperties = Array.Empty<Tuple<string, string, string>>();
                return "No Parameters";
            }

            // Now pull out the IOCTL command objects
            ParameterProperties = IoctlResultStrings
                .Last().Split('\r').Select(StringObj =>
                {
                    // Get base values for name and value output.
                    string[] SplitValueAndName = StringObj.Split('=');
                    string[] SplitIdAndNameValue = SplitValueAndName[0].Split(':');

                    // Store values for content here for output tuple set.
                    string NameValue = SplitIdAndNameValue[1].Trim();
                    string ValueString = SplitValueAndName[1].Trim();
                    string IdValue = int.TryParse(SplitIdAndNameValue[0], out int OutputInt) ?
                        $"0x{OutputInt:x8}".Trim() :
                        $"{SplitIdAndNameValue[0]} (ERROR!)".Trim();

                    // Build new output tuple object here and return it.
                    return new Tuple<string, string, string>(IdValue, NameValue, ValueString);
                })
                .ToArray();

            // Build our output table object here.
            string IoctlTableOutput = ParameterProperties.ToStringTable(
                new[] { "Ioctl ID", "Ioctl Name", "Set Value" },
                IoctlPair => IoctlPair.Item1,
                IoctlPair => IoctlPair.Item2
            );

            // Throw new exception since not yet built.
            ExpressionObject.ExpressionLogger.WriteLog($"BUILT OUT A TOTAL OF {ParameterProperties.Length} NEW PT IOCTL COMMAND OBJECTS!", LogType.InfoLog);
            return IoctlTableOutput;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Splits an input content string into a set fo PT Command objects which are split into objects.
        /// </summary>
        /// <param name="FileContents">Input file object content</param>
        /// <returns>Returns a set of file objects which contain the PT commands from a file.</returns>
        public static string[] SplitLogToCommands(string FileContents)
        {
            // Build regex objects to help split input content into sets.
            var TimeRegex = new Regex(PassThruRegexModelShare.PassThruTime.ExpressionPattern);
            var StatusRegex = new Regex(PassThruRegexModelShare.PassThruStatus.ExpressionPattern);

            // Make an empty array of strings and then begin splitting.
            List<string> OutputLines = new List<string>();
            for (int CharIndex = 0; CharIndex < FileContents.Length;)
            {
                // Find the first index of a time entry and the close command index.
                int TimeStartIndex = TimeRegex.Match(FileContents, CharIndex).Index;
                var ErrorCloseMatch = StatusRegex.Match(FileContents, TimeStartIndex);
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
        public static string SaveExpressionsFile(this PassThruExpression[] InputExpressions, string BaseFileName = "")
        {
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

            // Get a logger object for saving expression sets.
            string LoggerName = $"{Path.GetFileNameWithoutExtension(BaseFileName)}_ExpressionsLogger";
            var ExpressionLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith(LoggerName)) ?? new SubServiceLogger(LoggerName);

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

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts an input Regex command type enum into a type output
        /// </summary>
        /// <param name="InputType">Enum Regex Typ</param>
        /// <returns>Type of regex for the class output</returns>
        public static PassThruExpression GetRegexClassFromCommand(this PassThruCommandType InputType, string[] InputLines)
        {
            // Pull the description string and get type of regex class.
            string ClassType = $"{typeof(PassThruExpression).Namespace}.{InputType.ToDescriptionString()}";
            if (Type.GetType(ClassType) == null) return new PassThruExpression(string.Join(string.Empty, InputLines), InputType);

            // Find our output type value here.
            Type OutputType = Type.GetType(ClassType);
            var RegexConstructor = OutputType.GetConstructor(new[] { typeof(string) });
            return (PassThruExpression)RegexConstructor.Invoke(new[] { string.Join(string.Empty, InputLines) });
        }
    }
}
