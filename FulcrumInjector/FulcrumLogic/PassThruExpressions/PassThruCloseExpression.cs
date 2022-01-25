using System;
using System.Collections.Generic;
using System.Linq;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;

namespace FulcrumInjector.FulcrumLogic.PassThruExpressions
{
    /// <summary>
    /// PTClose Command Regex Operations
    /// </summary>
    public class PassThruCloseExpression : PassThruExpression
    {
        // Command for the open command it self
        public readonly PassThruRegexModel PtCloseRegex = PassThruRegexModelShare.PassThruClose;

        // -----------------------------------------------------------------------------------------

        // Strings of the command and results from the command output.
        [PtExpressionProperty("Command Line")] public readonly string PtCommand;
        [PtExpressionProperty("Device ID", "-1", new[] { "Device Closed", "Device Invalid!" }, true)] 
        public readonly string DeviceId;    

        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a PTClose Regex type output.
        /// </summary>
        /// <param name="CommandInput">InputLines for the command object strings.</param>
        public PassThruCloseExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTClose)
        {
            // Find the PTClose Command Results.
            var FieldsToSet = this.GetExpressionProperties();
            bool PtCloseResult = this.PtCloseRegex.Evaluate(CommandInput, out var PassThruCloseStrings);
            if (!PtCloseResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruCloseStrings[0] };
            StringsToApply.AddRange(from NextIndex in this.PtCloseRegex.ExpressionValueGroups where NextIndex <= PassThruCloseStrings.Length select PassThruCloseStrings[NextIndex]);

            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
