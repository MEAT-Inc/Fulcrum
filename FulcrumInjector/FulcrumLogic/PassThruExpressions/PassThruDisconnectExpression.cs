using System.Text.RegularExpressions;

namespace FulcrumInjector.FulcrumLogic.PassThruExpressions
{
    /// <summary>
    /// Class object used to find a PTDisconnect command instance from an input line set.
    /// </summary>
    public class PassThruDisconnectExpression : PassThruExpression
    {
        // Command for the open command it self
        public readonly Regex PTDisconnectRegex = new Regex(@"(PTDisconnect)\((\d+)\)");

        // Strings of the command and results from the command output.
        [PtExpressionProperty("Command")] public readonly string PtCommand;
        [PtExpressionProperty("ChannelId", "-1", new[] { "Channel Closed", "Invalid Channel!" }, true)]
        public readonly string ChannelId;

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a PTDisconnect Regex Helper 
        /// </summary>
        /// <param name="CommandInput">Lines to filter out of.</param>
        public PassThruDisconnectExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTDisconnect)
        {
            // Find the PTDisconnect Command Results.
            var CommandMatch = this.PTDisconnectRegex.Match(CommandInput);
            var DeviceIdMatch = this.PTDisconnectRegex.Match(CommandInput).Groups[2];

            // Store values based on results.v
            this.ChannelId = DeviceIdMatch.Success ? DeviceIdMatch.Value : "-1";
            this.PtCommand = CommandMatch.Success ? CommandMatch.Value : "REGEX_FAILED";
        }
    }
}
