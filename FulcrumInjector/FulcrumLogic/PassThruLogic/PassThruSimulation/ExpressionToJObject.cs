using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation
{
    /// <summary>
    /// Static class used to convert expressions into J2534 objects
    /// </summary>
    public class ExpressionToJObject
    {
        /// <summary>
        /// Converts a J2534 Filter expression into a filter object
        /// </summary>
        /// <param name="FilterExpression"></param>
        /// <returns></returns>
        public static J2534Filter ConvertFilterExpression(PassThruStartMessageFilterExpression FilterExpression, bool Inverted = false)
        {
            // Store the Pattern, Mask, and Flow Ctl objects if they exist.
            FilterExpression.FindFilterContents(out List<string[]> FilterContent);
            if (FilterContent.Count == 0)
            {
                FilterExpression.ExpressionLogger.WriteLog("FILTER CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                FilterExpression.ExpressionLogger.WriteLog($"FILTER COMMAND LINES ARE SHOWN BELOW:\n{FilterExpression.CommandLines}", LogType.TraceLog);
                return null;
            }

            // Build filter output contents
            var FilterType = FilterExpression.FilterType;
            var FilterFlags = uint.Parse(FilterContent[0][4]);
            var FilterProtocol = (ProtocolId)uint.Parse(FilterContent[0][2].Split(':')[0]);
            var FilterPatten = FilterContent[0].Last().Replace("0x ", string.Empty);
            var FilterMask = FilterContent[1].Last().Replace("0x ", string.Empty);
            var FilterFlow = FilterContent.Count != 3 ? "" : FilterContent[2].Last().Replace("0x ", string.Empty);

            // Now convert our information into string values.
            return new J2534Filter()
            {
                // Build a new filter object form the given values and return it.
                FilterMask = FilterMask,
                FilterFlags = FilterFlags,
                FilterProtocol = FilterProtocol,
                FilterPattern = Inverted ? FilterFlow : FilterPatten,
                FilterFlowCtl = Inverted ? FilterPatten : FilterFlow,
                FilterType = (FilterDef)Enum.Parse(typeof(FilterDef), FilterType)
            };
        }
        /// <summary>
        /// Converts a PTWrite Message Expression into a PTMessage
        /// </summary>
        /// <param name="MessageExpression"></param>
        /// <returns></returns>
        public static PassThruStructs.PassThruMsg ConvertWriteExpression(PassThruWriteMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0)
            {
                MessageExpression.ExpressionLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                MessageExpression.ExpressionLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
                return default;
            }

            // Loop all the message values located and append them into the list of output
            PassThruStructs.PassThruMsg MessageBuilt = default;
            foreach (var MessageSet in MessageContents)
            {
                // Store message values here.
                var MessageData = MessageSet.Last();
                var MessageFlags = uint.Parse(MessageSet[3]);
                var ProtocolId = (ProtocolId)Enum.Parse(typeof(ProtocolId), MessageSet[4].Split(':')[0]);

                // Build a message and then return it.
                MessageData = MessageData.Replace("0x", string.Empty);
                MessageBuilt = J2534Device.CreatePTMsgFromString(ProtocolId, MessageFlags, MessageData);
            }

            // Return the built message
            return MessageBuilt;
        }
        /// <summary>
        /// Converts an input PTRead Expression to a PTMessage
        /// </summary>
        /// <param name="MessageExpression"></param>
        /// <returns></returns>
        public static PassThruStructs.PassThruMsg ConvertReadExpression(PassThruReadMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0)
            {
                MessageExpression.ExpressionLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                MessageExpression.ExpressionLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
                return default;
            }

            // Loop all the message values located and append them into the list of output
            PassThruStructs.PassThruMsg BuiltMessage = default;
            foreach (var MessageSet in MessageContents)
            {
                // Store message values here.
                var MessageData = MessageSet.Last();
                if (MessageData.Contains("[") || MessageData.Contains("]"))
                {
                    // Format for framepad output
                    MessageData = MessageData.Replace("0x", string.Empty);
                    string[] SplitData = MessageData
                        .Split(']')
                        .Select(SplitPart => SplitPart.Replace("[", string.Empty))
                        .Where(SplitPart => SplitPart.Length != 0)
                        .ToArray();

                    // Now restore message values
                    MessageData = string.Join(" ", SplitData);
                }

                // If it's not a frame pad message, add to our simulation
                var MessageFlags = uint.Parse(MessageSet[4]);
                var ProtocolId = (ProtocolId)Enum.Parse(typeof(ProtocolId), MessageSet[2].Split(':')[0]);

                // Build a message and then return it.
                MessageData = MessageData.Replace("0x", string.Empty);
                BuiltMessage = J2534Device.CreatePTMsgFromString(ProtocolId, MessageFlags, MessageData);
            }

            // Return the message
            return BuiltMessage;
        }
    }
}
