using System;
using System.Collections.Generic;
using System.Linq;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions
{
    /// <summary>
    /// Set of Regular Expressions for the PTConnect Command
    /// </summary>
    public class PassThruConnectExpression : PassThruExpression
    {
        // Command for the open command it self
        public readonly PassThruRegexModel PtConnectRegex = PassThruRegexModelShare.PassThruConnect;
        public readonly PassThruRegexModel ChannelIdRegex = PassThruRegexModelShare.ChannelIdReturned;

        // Strings of the command and results from the command output.
        [PtExpressionProperty("Command Line")] public readonly string PtCommand;
        [PtExpressionProperty("Device ID")] public readonly string DeviceId;
        [PtExpressionProperty("Protocol ID")] public readonly string ProtocolId;
        [PtExpressionProperty("Connect Flags")] public readonly string ConnectFlags;
        [PtExpressionProperty("BaudRate")] public readonly string BaudRate;
        [PtExpressionProperty("Channel Pointer")] public readonly string ChannelPointer;
        [PtExpressionProperty("Channel ID", "-1", new[] { "Channel Opened", "Invalid Channel!"}, true)] 
        public readonly string ChannelId;

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Regex helper to search for our PTConnect Command
        /// </summary>
        /// <param name="CommandInput">Input text for the command to find.</param>
        public PassThruConnectExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTConnect)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtConnectResult = this.PtConnectRegex.Evaluate(CommandInput, out var PassThruConnectStrings);
            bool ChannelIdResult = this.ChannelIdRegex.Evaluate(CommandInput, out var ChannelIdStrings);
            if (!PtConnectResult || !ChannelIdResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruConnectStrings[0] };
            StringsToApply.AddRange(from NextIndex in this.PtConnectRegex.ExpressionValueGroups where NextIndex <= PassThruConnectStrings.Length select PassThruConnectStrings[NextIndex]);
            StringsToApply.AddRange(from NextIndex in this.ChannelIdRegex.ExpressionValueGroups where NextIndex <= ChannelIdStrings.Length select ChannelIdStrings[NextIndex]);
          
            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
