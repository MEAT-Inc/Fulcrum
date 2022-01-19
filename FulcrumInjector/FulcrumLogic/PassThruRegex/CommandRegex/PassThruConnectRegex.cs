using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumLogic.PassThruRegex.CommandRegex
{
    /// <summary>
    /// Set of Regular Expressions for the PTConnect Command
    /// </summary>
    public class PassThruConnectRegex : PassThruExpression
    {
        // Command for the open command it self
        public readonly Regex PtConnectCommandRegex = new Regex(@"(PTConnect)\((\d+),\s+([^,]+),\s([^,]+),\s+([^,]+),\s+([^)]+)\)");
        public readonly Regex ChannelIdRegex = new Regex(@"returning ChannelID: (\d+)");

        // Strings of the command and results from the command output.
        [PtRegexResult("PTConnect")] public readonly string PtCommand;
        [PtRegexResult("DeviceId")] public readonly string DeviceId;
        [PtRegexResult("ProtocolId")] public readonly string ProtocolId;
        [PtRegexResult("ConnectFlags")] public readonly string ConnectFlags;
        [PtRegexResult("BaudRate")] public readonly string BaudRate;
        [PtRegexResult("ChannelPointer")] public readonly string ChannelPointer;
        [PtRegexResult("ChannelId", "-1", new[] { "Channel Opened", "Invalid Channel!"}, true)] 
        public readonly string ChannelId;

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Regex helper to search for our PTConnect Command
        /// </summary>
        /// <param name="CommandInput">Input text for the command to find.</param>
        public PassThruConnectRegex(string CommandInput) : base(CommandInput, PassThruCommandType.PTConnect)
        {
            // Find the PTOpen Command Results.
            var CommandMatch = this.PtConnectCommandRegex.Match(CommandInput);
            var ChannelIdMatch = this.ChannelIdRegex.Match(CommandInput);

            // Store values based on results.
            this.PtCommand = CommandMatch.Success ? CommandMatch.Value : "REGEX_FAILED";
            this.DeviceId = CommandMatch.Success ? CommandMatch.Groups[2].Value : "REGEX_FAILED";
            this.ProtocolId = CommandMatch.Success ? CommandMatch.Groups[3].Value : "REGEX_FAILED";
            this.ConnectFlags = CommandMatch.Success ? CommandMatch.Groups[4].Value : "REGEX_FAILED";
            this.BaudRate = CommandMatch.Success ? CommandMatch.Groups[5].Value : "REGEX_FAILED";
            this.ChannelPointer = CommandMatch.Success ? CommandMatch.Groups[6].Value : "REGEX_FAILED";
            this.ChannelId = ChannelIdMatch.Success ? ChannelIdMatch.Groups[2].Value : "-1";
        }
    }
}
