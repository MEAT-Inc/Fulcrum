using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;

namespace FulcrumInjector.FulcrumLogic.PassThruExpressions
{
    /// <summary>
    /// Class object used for our PTWrite Message command parsing output
    /// </summary>
    public class PassThruWriteMessagesExpression : PassThruExpression
    {
        // Command for the write command it self
        public readonly PassThruRegexModel MessagesWrittenRegex = PassThruRegexModelShare.NumberOfMessages;
        public readonly PassThruRegexModel PtWriteMessagesRegex = PassThruRegexModelShare.PassThruWriteMessages;

        // Strings of the command and results from the command output.
        [PtExpressionProperty("Command Line")] public readonly string PtCommand;
        [PtExpressionProperty("Channel ID")] public readonly string ChannelId;
        [PtExpressionProperty("Channel Pointer")] public readonly string ChannelPointer;
        [PtExpressionProperty("Message Pointer")] public readonly string MessagePointer;
        [PtExpressionProperty("Timeout")] public readonly string TimeoutTime;
        [PtExpressionProperty("Sent Count")] public readonly string MessageCountSent;
        [PtExpressionProperty("Expected Count")] public readonly string MessageCountTotal;

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new PTWrite Messages Command instance.
        /// </summary>
        /// <param name="CommandInput">Input Command Lines</param>
        public PassThruWriteMessagesExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTWriteMsgs)
        { 
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtConnectResult = this.PtWriteMessagesRegex.Evaluate(CommandInput, out var PassThruWriteMsgsStrings);
            bool MessagesReadResult = this.MessagesWrittenRegex.Evaluate(CommandInput, out var MessagesSentStrings);
            if (!PtConnectResult || !MessagesReadResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruWriteMsgsStrings[0] };
            StringsToApply.AddRange(from NextIndex in this.PtWriteMessagesRegex.ExpressionValueGroups where NextIndex <= PassThruWriteMsgsStrings.Length select PassThruWriteMsgsStrings[NextIndex]);
            StringsToApply.AddRange(from NextIndex in this.MessagesWrittenRegex.ExpressionValueGroups where NextIndex <= MessagesSentStrings.Length select MessagesSentStrings[NextIndex]);

            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
