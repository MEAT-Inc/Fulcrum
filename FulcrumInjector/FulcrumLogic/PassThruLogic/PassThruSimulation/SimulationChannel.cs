using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation
{
    /// <summary>
    /// Class object which contains messages and filters for a simulation channel object
    /// </summary>
    public class SimulationChannel
    {
        // Channel ID Built and Logger
        public readonly uint ChannelId;
        public readonly ProtocolId ChannelProtocol;
        private readonly SubServiceLogger SimChannelLogger;

        // Class Values for a channel to simulate
        public J2534Filter[] MessageFilters;
        public PassThruStructs.PassThruMsg[] MessagesSent;
        public PassThruStructs.PassThruMsg[] MessagesRead;

        /// <summary>
        /// Builds a new Channel Simulation object from the given channel ID
        /// </summary>
        /// <param name="ChannelId"></param>
        public SimulationChannel(int ChannelId, ProtocolId ProtocolInUse)
        {
            // Store the Channel ID
            this.ChannelId = (uint)ChannelId;
            this.ChannelProtocol = ProtocolInUse;
            this.SimChannelLogger = new SubServiceLogger($"SimChannelLogger_ID-{this.ChannelId}");
            this.SimChannelLogger.WriteLog($"BUILT NEW SIM CHANNEL OBJECT FOR CHANNEL ID {this.ChannelId}!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Stores a set of Expressions into messages on the given channel object
        /// </summary>
        /// <param name="ExpressionsToStore">Expressions to extract and store</param>
        /// <returns>The Filters built</returns>
        public J2534Filter[] StoreMessageFilters(PassThruStartMessageFilterExpression[] ExpressionsToStore)
        {
            // Loop each of these filter objects in parallel and update contents.
            this.SimChannelLogger.WriteLog("BUILDING NEW CHANNEL FILTER ARRAY FROM EXPRESSION SET NOW...", LogType.InfoLog);
            List<J2534Filter> BuiltFilters = new List<J2534Filter>();
            Parallel.ForEach(ExpressionsToStore, (FilterExpression) =>
            {
                // Store the Pattern, Mask, and Flow Ctl objects if they exist.
                FilterExpression.FindFilterContents(out List<string[]> FilterContent);
                if (FilterContent.Count == 0) {
                    FilterExpression.ExpressionLogger.WriteLog("FILTER CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                    FilterExpression.ExpressionLogger.WriteLog($"FILTER COMMAND LINES ARE SHOWN BELOW:\n{FilterExpression.CommandLines}", LogType.TraceLog);
                    return;
                }

                // Build filter output contents
                var FilterType = FilterExpression.FilterType;
                var FilterFlags = uint.Parse(FilterContent[0][4]);
                var FilterPatten = FilterContent[0].Last().Replace("0x ", string.Empty);
                var FilterMask = FilterContent[1].Last().Replace("0x ", string.Empty);
                var FilterFlow = FilterContent.Count != 3 ? "" : FilterContent[2].Last().Replace("0x ", string.Empty);

                // Now convert our information into string values.
                BuiltFilters.Add(new J2534Filter()
                {
                    // Build a new filter object form the given values and return it.
                    FilterType = FilterType,
                    FilterMask = FilterPatten,
                    FilterPattern = FilterMask,
                    FilterFlowCtl = FilterFlow,
                    FilterFlags = FilterFlags,
                });
            });

            // Return the built filter objects here.
            this.MessageFilters = BuiltFilters.ToArray();
            return this.MessageFilters;
        }

        /// <summary>
        /// Stores a set of PTWrite Message commands into the current sim channel as messages to READ IN
        /// </summary>
        /// <returns>List of messages stored</returns>
        public PassThruStructs.PassThruMsg[] StoreMessagesWritten(PassThruWriteMessagesExpression[] ExpressionsToStore)
        {
            // Loop each of these filter objects in parallel and update contents.
            this.SimChannelLogger.WriteLog("BUILDING NEW MESSAGES WRITTEN (TO BE READ) ARRAY FROM EXPRESSION SET NOW...", LogType.InfoLog);
            List<PassThruStructs.PassThruMsg> BuiltMessages = new List<PassThruStructs.PassThruMsg>();
            Parallel.ForEach(ExpressionsToStore, (MessageExpression) =>
            {
                // Store the Message Data and the values of the message params.
                MessageExpression.FindMessageContents(out List<string[]> MessageContents);
                if (MessageContents.Count == 0) {
                    MessageExpression.ExpressionLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                    MessageExpression.ExpressionLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
                    return;
                }

                // Loop all the message values located and append them into the list of output
                foreach (var MessageSet in MessageContents)
                {
                    // Store message values here.
                    var MessageData = MessageSet.Last();
                    var MessageFlags = uint.Parse(MessageSet[3]);
                    var ProtocolId = (ProtocolId)Enum.Parse(typeof(ProtocolId), MessageSet[4].Split(':')[0]);

                    // Build a message and then return it.
                    MessageData = MessageData.Replace("0x", string.Empty);
                    var MsgFromBytes = J2534Device.CreatePTMsgFromString(ProtocolId, MessageFlags, MessageData);
                    BuiltMessages.Add(MsgFromBytes);
                }
            });

            // Return the built filter objects here.
            this.MessagesSent = BuiltMessages.ToArray();
            return this.MessagesSent;
        }

        /// <summary>
        /// Stores a set of PTWrite Message commands into the current sim channel as messages to READ IN
        /// </summary>
        /// <returns>List of messages stored</returns>
        public PassThruStructs.PassThruMsg[] StoreMessagesRead(PassThruReadMessagesExpression[] ExpressionsToStore)
        {
            // Loop each of these filter objects in parallel and update contents.
            this.SimChannelLogger.WriteLog("BUILDING NEW MESSAGES READ (TO BE WRITTEN) ARRAY FROM EXPRESSION SET NOW...", LogType.InfoLog);
            List<PassThruStructs.PassThruMsg> BuiltMessages = new List<PassThruStructs.PassThruMsg>();
            Parallel.ForEach(ExpressionsToStore, (MessageExpression) =>
            {
                // Store the Message Data and the values of the message params.
                MessageExpression.FindMessageContents(out List<string[]> MessageContents);
                if (MessageContents.Count == 0) {
                    MessageExpression.ExpressionLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                    MessageExpression.ExpressionLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
                    return;
                }

                // Loop all the message values located and append them into the list of output
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
                    var MsgFromBytes = J2534Device.CreatePTMsgFromString(ProtocolId, MessageFlags, MessageData);
                    BuiltMessages.Add(MsgFromBytes);
                }
            });

            // Return the built filter objects here.
            this.MessagesRead = BuiltMessages.ToArray();
            return this.MessagesRead;
        }
    }
}
