using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpSimulator;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumLogic.ExtensionClasses
{
    /// <summary>
    /// Class object which contains messages and filters for a simulation channel object
    /// </summary>
    public static class SimChannelExtensions
    {
        // Logger object for this extension class
        private static SubServiceLogger _simExtensionLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SimExtensionLogger")) ?? new SubServiceLogger("SimExtensionLogger");

        /// <summary>
        /// Stores a set of Expressions into messages on the given channel object
        /// </summary>
        /// <param name="ExpressionsToStore">Expressions to extract and store</param>
        /// <returns>The Filters built</returns>
        public static J2534Filter[] StoreMessageFilters(this SharpSimulator.SimulationChannel InputChannel, PassThruStartMessageFilterExpression[] ExpressionsToStore)
        {
            // Loop each of these filter objects in parallel and update contents.
            _simExtensionLogger.WriteLog("BUILDING NEW CHANNEL FILTER ARRAY FROM EXPRESSION SET NOW...", LogType.InfoLog);
            List<J2534Filter> BuiltFilters = new List<J2534Filter>();
            Parallel.ForEach(ExpressionsToStore, (FilterExpression) => BuiltFilters.Add(ExpressionToJObject.ConvertFilterExpression(FilterExpression, true)));
            
            // Return the built filter objects here.
            InputChannel.MessageFilters = BuiltFilters.ToArray();
            return BuiltFilters.ToArray();
        }
        /// <summary>
        /// Stores a set of PTWrite Message commands into the current sim channel as messages to READ IN
        /// </summary>
        /// <returns>List of messages stored</returns>
        public static PassThruStructs.PassThruMsg[] StoreMessagesWritten(this SimulationChannel InputChannel, PassThruWriteMessagesExpression[] ExpressionsToStore)
        {
            // Loop each of these filter objects in parallel and update contents.
            _simExtensionLogger.WriteLog("BUILDING NEW MESSAGES WRITTEN (TO BE READ) ARRAY FROM EXPRESSION SET NOW...", LogType.InfoLog);
            List<PassThruStructs.PassThruMsg> BuiltMessages = new List<PassThruStructs.PassThruMsg>();
            Parallel.ForEach(ExpressionsToStore, (MessageExpression) => BuiltMessages.Add(ExpressionToJObject.ConvertWriteExpression(MessageExpression)));

            // Return the built filter objects here.
            var CombinedMessagesSet = (InputChannel.MessagesSent ?? Array.Empty<PassThruStructs.PassThruMsg>()).ToList();
            CombinedMessagesSet.AddRange(BuiltMessages);
            CombinedMessagesSet = CombinedMessagesSet
                .Distinct()
                .ToList();

            // Return the distinct combinations
            InputChannel.MessagesSent = CombinedMessagesSet.ToArray();
            return CombinedMessagesSet.ToArray();
        }
        /// <summary>
        /// Stores a set of PTWrite Message commands into the current sim channel as messages to READ IN
        /// </summary>
        /// <returns>List of messages stored</returns>
        public static PassThruStructs.PassThruMsg[] StoreMessagesRead(this SimulationChannel InputChannel, PassThruReadMessagesExpression[] ExpressionsToStore)
        {
            // Loop each of these filter objects in parallel and update contents.
            _simExtensionLogger.WriteLog("BUILDING NEW MESSAGES READ (TO BE WRITTEN) ARRAY FROM EXPRESSION SET NOW...", LogType.InfoLog);
            List<PassThruStructs.PassThruMsg> BuiltMessages = new List<PassThruStructs.PassThruMsg>();
            Parallel.ForEach(ExpressionsToStore, (MessageExpression) => BuiltMessages.Add(ExpressionToJObject.ConvertReadExpression(MessageExpression)));

            // Return the built filter objects here.
            var CombinedMessagesSet = (InputChannel.MessagesRead ?? Array.Empty<PassThruStructs.PassThruMsg>()).ToList();
            CombinedMessagesSet.AddRange(BuiltMessages.ToList());
            CombinedMessagesSet = CombinedMessagesSet
                .Distinct()
                .ToList();

            // Return the distinct combinations
            InputChannel.MessagesRead = CombinedMessagesSet.ToArray();
            return CombinedMessagesSet.ToArray();
        }
        /// <summary>
        /// Pairs off a set of input Expressions to find their pairings
        /// </summary>
        /// <param name="GroupedExpression">Expressions to search thru</param>
        public static SimulationMessagePair[] StorePassThruPairs(this SharpSimulator.SimulationChannel InputChannel, PassThruExpression[] GroupedExpression)
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
            var TempPairsCast = new List<SimulationMessagePair>();
            foreach (var PairedMessageSet in MessagesPaired)
            {
                // Convert the Send command into a message here
                var SendExpressionAsMessage = ExpressionToJObject.ConvertWriteExpression(PairedMessageSet.Item1);
                var ReadExpressionsAsMessages = PairedMessageSet.Item2
                    .Select(ExpressionToJObject.ConvertReadExpression)
                    .ToArray();

                // Build a new Tuple and append it
                TempPairsCast.Add(new SimulationMessagePair(SendExpressionAsMessage, ReadExpressionsAsMessages));
            }

            // Store onto the class, return built values
            InputChannel.MessagePairs = TempPairsCast.ToArray();
            return InputChannel.MessagePairs;
        }
    }
}
