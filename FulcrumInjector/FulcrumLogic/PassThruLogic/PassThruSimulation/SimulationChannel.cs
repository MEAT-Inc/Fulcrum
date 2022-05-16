using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        public Tuple<PassThruStructs.PassThruMsg, PassThruStructs.PassThruMsg[]>[] PairedMessageArray;

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
                    FilterMask = FilterMask,
                    FilterPattern = FilterPatten,
                    FilterFlowCtl = FilterFlow,
                    FilterFlags = FilterFlags,
                });
            });

            // Return the built filter objects here.
            this.MessageFilters = BuiltFilters.ToArray();
            return BuiltFilters.ToArray();
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
            Parallel.ForEach(ExpressionsToStore, (MessageExpression) => BuiltMessages.Add(ConvertWriteExpression(MessageExpression)));

            // Return the built filter objects here.
            var CombinedMessagesSet = (this.MessagesSent ?? Array.Empty<PassThruStructs.PassThruMsg>()).ToList();
            CombinedMessagesSet.AddRange(BuiltMessages);
            CombinedMessagesSet = CombinedMessagesSet
                .Distinct()
                .ToList();

            // Return the distinct combinations
            this.MessagesSent = CombinedMessagesSet.ToArray();
            return CombinedMessagesSet.ToArray();
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
            Parallel.ForEach(ExpressionsToStore, (MessageExpression) => BuiltMessages.Add(ConvertReadExpression(MessageExpression)));

            // Return the built filter objects here.
            var CombinedMessagesSet = (this.MessagesRead ?? Array.Empty<PassThruStructs.PassThruMsg>()).ToList();
            CombinedMessagesSet.AddRange(BuiltMessages.ToList());
            CombinedMessagesSet = CombinedMessagesSet
                .Distinct()
                .ToList();

            // Return the distinct combinations
            this.MessagesRead = CombinedMessagesSet.ToArray();
            return CombinedMessagesSet.ToArray();
        }
        /// <summary>
        /// Pairs off a set of input Expressions to find their pairings
        /// </summary>
        /// <param name="GroupedExpression">Expressions to search thru</param>
        public Tuple<PassThruStructs.PassThruMsg, PassThruStructs.PassThruMsg[]>[] StorePassThruPairs(PassThruExpression[] GroupedExpression)
        {
            // Pull out our pairs
            var MessagesPaired = new List<Tuple<PassThruWriteMessagesExpression, PassThruReadMessagesExpression[]>>();
            foreach (var ExpressionObject in GroupedExpression)
            {
                // Find if the expression is a PTWrite command then find all the next ones that are 
                if (ExpressionObject.TypeOfExpression != PassThruCommandType.PTWriteMsgs) { continue; }

                // Store the next expression
                int IndexOfExpression = GroupedExpression.ToList().IndexOf(ExpressionObject);
                if ((IndexOfExpression + 1) > GroupedExpression.Length) continue;

                // Find the next expression and get all future ones
                IndexOfExpression += 1;
                var ReadExpressions = new List<PassThruReadMessagesExpression>();
                var NextExpression = GroupedExpression[IndexOfExpression];
                while (NextExpression.TypeOfExpression != PassThruCommandType.PTWriteMsgs)
                {
                    // Check if it's a PTRead Messages
                    if (NextExpression.TypeOfExpression != PassThruCommandType.PTReadMsgs) {
                        IndexOfExpression += 1;
                        if ((IndexOfExpression + 1) > GroupedExpression.Length) break;
                        continue;
                    }

                    // Add and check if the value is configured
                    IndexOfExpression += 1;
                    if ((IndexOfExpression + 1) > GroupedExpression.Length) break;
                    ReadExpressions.Add((PassThruReadMessagesExpression)NextExpression);
                    NextExpression = GroupedExpression[IndexOfExpression];
                }

                // Now add it into our list of messages paired with our original write command
                MessagesPaired.Add(new Tuple<PassThruWriteMessagesExpression, PassThruReadMessagesExpression[]>(
                    (PassThruWriteMessagesExpression)ExpressionObject, ReadExpressions.ToArray()
                ));
            }

            // Using each item in the built array, convert them all into 
            var TempPairsCast = new List<Tuple<PassThruStructs.PassThruMsg, PassThruStructs.PassThruMsg[]>>();
            foreach (var PairedMessageSet in MessagesPaired)
            {
                // Convert the Send command into a message here
                var SendExpressionAsMessage = ConvertWriteExpression(PairedMessageSet.Item1);
                var ReadExpressionsAsMessages = PairedMessageSet.Item2
                    .Select(ConvertReadExpression)
                    .ToArray();

                // Build a new Tuple and append it
                TempPairsCast.Add(new Tuple<PassThruStructs.PassThruMsg, PassThruStructs.PassThruMsg[]>(
                    SendExpressionAsMessage,
                    ReadExpressionsAsMessages
                ));
            }

            // Store onto the class, return built values
            this.PairedMessageArray = TempPairsCast.ToArray();
            return this.PairedMessageArray;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts a PTWrite Message Expression into a PTMessage
        /// </summary>
        /// <param name="MessageExpression"></param>
        /// <returns></returns>
        public static PassThruStructs.PassThruMsg ConvertWriteExpression(PassThruWriteMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0) {
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
            if (MessageContents.Count == 0) {
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
