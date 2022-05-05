using System;
using System.Collections.Generic;
using System.Linq;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions
{
    /// <summary>
    /// Class object for our PTStart filter regex methods.
    /// </summary>
    public class PassThruStartMessageFilterExpression : PassThruExpression
    {
        // Command for the open command it self
        public readonly PassThruRegexModel PtStartMsgFilterRegex = PassThruRegexModelShare.PassThruStartMsgFilter;
        public readonly PassThruRegexModel FilterIdReturnedRegex = PassThruRegexModelShare.FilterIdReturned;

        // Strings of the command and results from the command output.
        [PtExpressionProperty("Command Line")] public readonly string PtCommand;
        [PtExpressionProperty("Channel ID")] public readonly string ChannelID;
        [PtExpressionProperty("Filter Type")] public readonly string FilterType;
        [PtExpressionProperty("Mask Pointer")] public readonly string MaskPointer;
        [PtExpressionProperty("Pattern Pointer")] public readonly string PatternPointer;
        [PtExpressionProperty("Flow Control Pointer")] public readonly string FlowCtlPointer;
        [PtExpressionProperty("Filter Pointer (Struct)")] public readonly string FilterPointer;
        [PtExpressionProperty("Filter ID")] public readonly string FilterID;

        // Contents for our message objects here.
        public readonly List<string[]> MessageFilterContents;

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a PTStartMsgFilter expression 
        /// </summary>
        /// <param name="CommandInput"></param>
        public PassThruStartMessageFilterExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTStartMsgFilter)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtStartFilterResult = this.PtStartMsgFilterRegex.Evaluate(CommandInput, out var PassThruFilterStrings);
            bool FilterIdResult = this.FilterIdReturnedRegex.Evaluate(CommandInput, out var FilterIdResultStrings);
            if (!PtStartFilterResult || !FilterIdResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruFilterStrings[0] };
            StringsToApply.AddRange(from NextIndex in this.PtStartMsgFilterRegex.ExpressionValueGroups where NextIndex <= PassThruFilterStrings.Length select PassThruFilterStrings[NextIndex]);
            StringsToApply.AddRange(from NextIndex in this.FilterIdReturnedRegex.ExpressionValueGroups where NextIndex <= FilterIdResultStrings.Length select FilterIdResultStrings[NextIndex]);

            // Now apply our message filter contents
            string MessageTable = this.FindMessageContents(out this.MessageFilterContents);
            if (MessageTable is "" or "No Filter Content Found!")
                this.ExpressionLogger.WriteLog($"WARNING! NO MESSAGES FOR FILTER FOUND FOR EXPRESSION TYPE {this.GetType().Name}!", LogType.WarnLog);

            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
