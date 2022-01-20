using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;

namespace FulcrumInjector.FulcrumLogic.PassThruRegex
{
    // Regex Values for Different Command Types.
    public class PassThruOpenRegex : PassThruExpression
    {
        // Command for the open command it self
        public readonly PassThruRegexModel PtOpenRegex = PassThruExpressionShare.PassThruOpen;
        public readonly PassThruRegexModel DeviceIdRegex = PassThruExpressionShare.DeviceIdReturned;

        // Strings of the command and results from the command output.
        [PassThruRegexResult("Command")] public readonly string PtCommand;
        [PassThruRegexResult("DeviceName")] public readonly string DeviceName;
        [PassThruRegexResult("DevicePointer")] public readonly string DevicePointer;
        [PassThruRegexResult("DeviceId", "-1", new[] { "Device Opened", "Invalid Device ID!" }, true)]
        public readonly string DeviceId;

        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new PTOpen Regex command type.
        /// </summary>
        /// <param name="commandInput"></param>
        public PassThruOpenRegex(string CommandInput) : base(CommandInput, PassThruCommandType.PTOpen)
        {
            // Find the PTOpen Command Send instance.
            bool PtOpenResult = this.PtOpenRegex.Evaluate(CommandInput, out var PassThruOpenStrings);
            this.PtCommand = PtOpenResult ? PassThruOpenStrings[0] : "REGEX_FAILED";
            this.DeviceName = PtOpenResult ? PassThruOpenStrings[this.PtOpenRegex.ExpressionValueGroups[0]] : "REGEX_FAILED";
            this.DevicePointer = PtOpenResult ? PassThruOpenStrings[this.PtOpenRegex.ExpressionValueGroups[1]] : "REGEX_FAILED";

            // Find the PTOpen Command Results (Device ID)
            bool DeviceIdResult = this.DeviceIdRegex.Evaluate(CommandInput, out var DeviceIdStrings);
            this.DeviceId =  DeviceIdResult ? DeviceIdStrings[this.DeviceIdRegex.ExpressionValueGroups[0]] : "-1";
        }
    }
}
