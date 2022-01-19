using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumLogic.PassThruRegex.CommandRegex
{
    // Regex Values for Different Command Types.
    public class PassThruOpenRegex : PassThruExpression
    {
        // Command for the open command it self
        public readonly Regex PtOpenCommandRegex = new Regex(@"(PTOpen)\(([^,]+),\s+([^,)]+)\)");
        public readonly Regex DeviceIdRegex = new Regex(@"returning DeviceID: (\d+)");

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
            // Find the PTOpen Command Results.
            var DeviceIdMatch = this.DeviceIdRegex.Match(CommandInput);
            var CommandMatch = this.PtOpenCommandRegex.Match(CommandInput);
            var DeviceNameMatch = this.PtOpenCommandRegex.Match(CommandInput).Groups[2];
            var DevicePointerMatch = this.PtOpenCommandRegex.Match(CommandInput).Groups[3];

            // Store values based on results.v
            this.DeviceId = DeviceIdMatch.Success ? DeviceIdMatch.Value : "-1";
            this.PtCommand = CommandMatch.Success ? CommandMatch.Value : "REGEX_FAILED";
            this.DeviceName = DeviceNameMatch.Success ? DeviceNameMatch.Value : "REGEX_FAILED";
            this.DevicePointer = DevicePointerMatch.Success ? DevicePointerMatch.Value : "REGEX_FAILED";
        }
    }
}
