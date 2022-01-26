using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.PassThruExpressions
{
    /// <summary>
    /// Class object to help parse out the PTIoctl Control values from a PTIoctl command.
    /// </summary>
    public class PassThruIoctlExpression : PassThruExpression
    {
        // Command for the open command it self
        public readonly PassThruRegexModel PtIoctlRegex = PassThruRegexModelShare.PassThruIoctl;

        // Strings of the command and results from the command output.
        [PtExpressionProperty("Command Line")] public readonly string PtCommand;
        [PtExpressionProperty("Channel ID")] public readonly string ChannelID;
        [PtExpressionProperty("IOCTL Type")] public readonly string IoctlType;
        [PtExpressionProperty("IOCTL Input")] public readonly string IoctlInputStruct;
        [PtExpressionProperty("IOCTL Output")] public readonly string IoctlOutputStruct;
        [PtExpressionProperty("Parameter Count")] public readonly string ParameterCount;

        // Number of Parameters and values for the IOCTL command.
        public readonly Tuple<string, string, string>[] ParameterValues;

        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a PTIoctl expression for parsing a PTIoctl command.
        /// </summary>
        /// <param name="CommandInput">Input command lines.</param>
        public PassThruIoctlExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTIoctl)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtIoctlResult = this.PtIoctlRegex.Evaluate(CommandInput, out var PassThruIoctlStrings);
            if (!PtIoctlResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruIoctlStrings[0] };
            StringsToApply.AddRange(from NextIndex in this.PtIoctlRegex.ExpressionValueGroups where NextIndex <= PassThruIoctlStrings.Length select PassThruIoctlStrings[NextIndex]);

            // Now build the Ioctl Parameters from the input content if any exist.
            this.FindIoctlParameters(out this.ParameterValues);
            this.ParameterCount = this.ParameterValues.Length == 0 ? "No Parameters" : $"{this.ParameterValues.Length} Parameters";
            if (this.ParameterCount == "No Parameters") 
                this.ExpressionLogger.WriteLog($"WARNING! NO IOCTL PARAMETERS FOUND FOR TYPE {this.GetType().Name}!", LogType.WarnLog);

            // Now apply values using base method and exit out of this routine
            StringsToApply.Add(this.ParameterCount);
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}

