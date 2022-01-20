using System.Text.RegularExpressions;

namespace FulcrumInjector.FulcrumLogic.PassThruRegex
{
    /// <summary>
    /// Set of Regular Expressions for the PTConnect Command
    /// </summary>
    public class PassThruConnectRegex : PassThruExpression
    {
        // Command for the open command it self
        public readonly Regex ChannelIdRegex = new Regex(@"returning ChannelID: (\d+)");
        public readonly Regex PtConnectRegex = new Regex(@"(PTConnect)\((\d+),\s+([^,]+),\s([^,]+),\s+([^,]+),\s+([^)]+)\)");

        // Strings of the command and results from the command output.
        [PassThruRegexResult("PTConnect")] public readonly string PtCommand;
        [PassThruRegexResult("DeviceId")] public readonly string DeviceId;
        [PassThruRegexResult("ProtocolId")] public readonly string ProtocolId;
        [PassThruRegexResult("ConnectFlags")] public readonly string ConnectFlags;
        [PassThruRegexResult("BaudRate")] public readonly string BaudRate;
        [PassThruRegexResult("ChannelPointer")] public readonly string ChannelPointer;
        [PassThruRegexResult("ChannelId", "-1", new[] { "Channel Opened", "Invalid Channel!"}, true)] 
        public readonly string ChannelId;

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Regex helper to search for our PTConnect Command
        /// </summary>
        /// <param name="CommandInput">Input text for the command to find.</param>
        public PassThruConnectRegex(string CommandInput) : base(CommandInput, PassThruCommandType.PTConnect)
        {
            // Find the PTOpen Command Results.
            var CommandMatch = this.PtConnectRegex.Match(CommandInput);
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
