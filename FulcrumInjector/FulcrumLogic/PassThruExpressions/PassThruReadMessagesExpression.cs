using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;

namespace FulcrumInjector.FulcrumLogic.PassThruExpressions
{
    /// <summary>
    /// Regex object class for a PTReadMessages command
    /// </summary>
    public class PassThruReadMessagesExpression : PassThruExpression
    {
        // Command for the open command it self
        public readonly PassThruRegexModel MessagesReadRegex = PassThruRegexModelShare.NumberOfMessages;
        public readonly PassThruRegexModel PtReadMessagesRegex = PassThruRegexModelShare.PassThruReadMessages;

        // Strings of the command and results from the command output.
        [PtExpressionProperty("Command Line")] public readonly string PtCommand;
        [PtExpressionProperty("Channel ID")] public readonly string ChannelId;
        [PtExpressionProperty("Channel Pointer")] public readonly string ChannelPointer;
        [PtExpressionProperty("Message Pointer")] public readonly string MessagePointer;
        [PtExpressionProperty("Timeout")] public readonly string TimeoutTime;
        [PtExpressionProperty("Read Count")] public readonly string MessageCountRead;
        [PtExpressionProperty("Expected Count")] public readonly string MessageCountTotal;

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Regex helper to search for our PTRead Messages Command
        /// </summary>
        /// <param name="CommandInput">Input text for the command to find.</param>
        public PassThruReadMessagesExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTConnect)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtConnectResult = this.PtReadMessagesRegex.Evaluate(CommandInput, out var PassThruReadMsgsStrings);
            bool MessagesReadResult = this.MessagesReadRegex.Evaluate(CommandInput, out var MessagesReadStrings);
            if (!PtConnectResult || !MessagesReadResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruReadMsgsStrings[0] };
            StringsToApply.AddRange(from NextIndex in this.PtReadMessagesRegex.ExpressionValueGroups where NextIndex <= PassThruReadMsgsStrings.Length select PassThruReadMsgsStrings[NextIndex]);
            StringsToApply.AddRange(from NextIndex in this.MessagesReadRegex.ExpressionValueGroups where NextIndex <= MessagesReadStrings.Length select MessagesReadStrings[NextIndex]);

            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
