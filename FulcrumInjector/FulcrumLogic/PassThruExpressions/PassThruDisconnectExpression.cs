using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;

namespace FulcrumInjector.FulcrumLogic.PassThruExpressions
{
    /// <summary>
    /// Class object used to find a PTDisconnect command instance from an input line set.
    /// </summary>
    public class PassThruDisconnectExpression : PassThruExpression
    {
        // Command for the open command it self
        public readonly PassThruRegexModel PTDisconnectRegex = PassThruRegexModelShare.PassThruDisconnect;

        // Strings of the command and results from the command output.
        [PtExpressionProperty("Command Line")] public readonly string PtCommand;
        [PtExpressionProperty("Channel ID", "-1", new[] { "Channel Closed", "Invalid Channel!" }, true)]
        public readonly string ChannelId;

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a PTDisconnect Regex Helper 
        /// </summary>
        /// <param name="CommandInput">Lines to filter out of.</param>
        public PassThruDisconnectExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTDisconnect)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtDisconnectResult = this.PTDisconnectRegex.Evaluate(CommandInput, out var PassThruDisconnectStrings);
            if (!PtDisconnectResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruDisconnectStrings[0] };
            StringsToApply.AddRange(from NextIndex in this.PTDisconnectRegex.ExpressionValueGroups where NextIndex <= PassThruDisconnectStrings.Length select PassThruDisconnectStrings[NextIndex]);

            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
