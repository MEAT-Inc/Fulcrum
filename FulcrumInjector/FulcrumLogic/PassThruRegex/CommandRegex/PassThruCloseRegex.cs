using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumLogic.PassThruRegex.CommandRegex
{
    /// <summary>
    /// PTClose Command Regex Operations
    /// </summary>
    public class PassThruCloseRegex : PassThruExpression
    {
        // Command for the open command it self
        public readonly Regex PtCloseCommandRegex = new Regex(@"(PTClose)\((\d+)\)");

        // -----------------------------------------------------------------------------------------

        // Strings of the command and results from the command output.
        [PassThruRegexResult("PTClose")]          // PassThru Close command Result
        public readonly string PtCommand;       // 1.714s -- PTClose(1)

        [PassThruRegexResult("DeviceId", "-1", new[] { "Device Valid", "Device Invalid!" })] 
        public readonly string DeviceId;        // Device Id Result

        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a PTClose Regex type output.
        /// </summary>
        /// <param name="CommandInput">InputLines for the command object strings.</param>
        public PassThruCloseRegex(string CommandInput) : base(CommandInput, PassThruCommandType.PTClose)
        {
            // Find the PTOpen Command Results.
            var CommandMatch = this.PtCloseCommandRegex.Match(CommandInput);
            var DeviceIdMatch = this.PtCloseCommandRegex.Match(CommandInput).Groups[2];

            // Store values based on results.v
            this.DeviceId = DeviceIdMatch.Success ? DeviceIdMatch.Value : "-1";
            this.PtCommand = CommandMatch.Success ? CommandMatch.Value : "REGEX_FAILED";
        }
    }
}
