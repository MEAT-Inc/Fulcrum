using System;
using System.Collections.Generic;
using System.Linq;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;

namespace FulcrumInjector.FulcrumLogic.PassThruExpressions
{
    // Regex Values for Different Command Types.
    public class PassThruOpenExpression : PassThruExpression
    {
        // Command for the open command it self
        public readonly PassThruRegexModel PtOpenRegex = PassThruRegexModelShare.PassThruOpen;
        public readonly PassThruRegexModel DeviceIdRegex = PassThruRegexModelShare.DeviceIdReturned;

        // Strings of the command and results from the command output.
        [PtExpressionProperty("Command")] public readonly string PtCommand;
        [PtExpressionProperty("DeviceName")] public readonly string DeviceName;
        [PtExpressionProperty("DevicePointer")] public readonly string DevicePointer;
        [PtExpressionProperty("DeviceId", "-1", new[] { "Device Opened", "Invalid Device ID!" }, true)]
        public readonly string DeviceId;

        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new PTOpen Regex command type.
        /// </summary>
        /// <param name="CommandInput">Input expression lines to store.</param>
        public PassThruOpenExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTOpen)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtOpenResult = this.PtOpenRegex.Evaluate(CommandInput, out var PassThruOpenStrings);
            bool DeviceIdResult = this.DeviceIdRegex.Evaluate(CommandInput, out var DeviceIdStrings);
            if (!PtOpenResult || !DeviceIdResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruOpenStrings[0] };
            StringsToApply.AddRange(from NextIndex in this.PtOpenRegex.ExpressionValueGroups where NextIndex <= PassThruOpenStrings.Length select PassThruOpenStrings[NextIndex]);
            StringsToApply.AddRange(from NextIndex in this.DeviceIdRegex.ExpressionValueGroups where NextIndex <= DeviceIdStrings.Length select DeviceIdStrings[NextIndex]);

            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
