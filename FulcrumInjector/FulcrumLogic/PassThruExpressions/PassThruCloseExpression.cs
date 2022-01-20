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
        [PtExpressionProperty("PTClose")]    // PassThru Close Command
        public readonly string PtCommand;      

        [PtExpressionProperty("DeviceId", "-1", new[] { "Device Closed", "Device Invalid!" }, true)] 
        public readonly string DeviceId;        // Device Id Result

        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a PTClose Regex type output.
        /// </summary>
        /// <param name="CommandInput">InputLines for the command object strings.</param>
        public PassThruCloseExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTClose)
        {
            // Find the PTClose Command Results.
            bool PtCloseResult = this.PtCloseRegex.Evaluate(CommandInput, out var PassThruCloseStrings);
            this.PtCommand = PtCloseResult ? PassThruCloseStrings[0] : "REGEX_FAILED";
            this.DeviceId = PtCloseResult ? PassThruCloseStrings[this.PtCloseRegex.ExpressionValueGroups[0]] : "REGEX_FAILED";
        }
    }
}
