using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions
{
    /// <summary>
    /// The names of the command types.
    /// Matches a type for the PT Command to a regex class type.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PassThruCommandType
    {
        // Command Types for PassThru Regex. Pulled values from settings parse into here.
        [EnumMember(Value = "NONE")] [Description("PassThruExpresssion")] NONE,
        [EnumMember(Value = "PTOpen")] [Description("PassThruOpenExpression")] PTOpen,
        [EnumMember(Value = "PTClose")] [Description("PassThruCloseExpression")] PTClose,
        [EnumMember(Value = "PTConnect")] [Description("PassThruConnectExpression")] PTConnect,
        [EnumMember(Value = "PTDisconnect")] [Description("PassThruDisconnectExpression")] PTDisconnect,
        [EnumMember(Value = "PTReadMsgs")] [Description("PassThruReadMessagesExpression")] PTReadMsgs,
        [EnumMember(Value = "PTWriteMsgs")] [Description("PassThruWriteMessagesExpression")] PTWriteMsgs,
        // TODO: Write PTStartPeriodic (May be needed for Sims)
        // TODO: Write PTStopPeriodic (May be needed for Sims)
        [EnumMember(Value = "PTStartMsgFilter")] [Description("PassThruStartMessageFilterExpression")] PTStartMsgFilter,
        [EnumMember(Value = "PTStartMsgFilter")] [Description("PassThruStopMessageFilterExpression")] PTStopMsgFilter,
        // TODO: Write PassThruSetProgrammingVoltage (Not Needed for Sims)
        // TODO: Write PTReadVersion (Not Needed for Sims)
        [EnumMember(Value = "PTIoctl")] [Description("PassThruIoctlExpression")] PTIoctl,
        // TODO: Write PassThruGetLastError (Not needed for Sims)
    }

    // -------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Set of static helper methods used to pull in the PTCommand Type as an extension class.
    /// </summary>
    public static class PtTypeHelpers
    {
        /// <summary>
        /// Finds a PTCommand type from the given input line set
        /// </summary>
        /// <param name="InputLines">Lines to find the PTCommand Type for.</param>
        /// <returns>The type of PTCommand regex to search with.</returns>
        public static PassThruCommandType GetTypeFromLines(this string[] InputLines)
        {
            // Return the result from our joined line output.
            return GetTypeFromLines(string.Join("\n", InputLines.Select(Input => Input.TrimEnd())));
        }
        /// <summary>
        /// Finds a PTCommand type from the given input line set
        /// </summary>
        /// <param name="InputLines">Lines to find the PTCommand Type for.</param>
        /// <returns>The type of PTCommand regex to search with.</returns>
        public static PassThruCommandType GetTypeFromLines(this string InputLines)
        {
            // Find the type of command by converting all enums to string array and searching for the type.
            var EnumTypesArray = Enum.GetValues(typeof(PassThruCommandType))
                .Cast<PassThruCommandType>()
                .Select(PtEnumValue => PtEnumValue.ToString())
                .ToArray();

            // Find the return type here based on the first instance of a PTCommand type object on the array.
            var EnumStringSelected = EnumTypesArray.FirstOrDefault(InputLines.Contains);
            return (PassThruCommandType)(string.IsNullOrWhiteSpace(EnumStringSelected) ?
                PassThruCommandType.NONE : Enum.Parse(typeof(PassThruCommandType), EnumStringSelected));
        }
    }
}