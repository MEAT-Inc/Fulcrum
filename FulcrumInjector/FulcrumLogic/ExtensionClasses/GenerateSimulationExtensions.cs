using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation;
using FulcrumInjector.FulcrumViewContent;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpSimulator.SimulationObjects;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumLogic.ExtensionClasses
{
    /// <summary>
    /// Extensions for building simulations from a set of expressions
    /// </summary>
    public static class GenerateSimulationExtensions
    {
        // Logger object.
        private static SubServiceLogger SimExtensionLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SimExtensionLogger")) ?? new SubServiceLogger("SimExtensionLogger");

        // ------------------------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Pulls out channel id values from a set of input content expressions
        /// </summary>
        /// <param name="Expressions">Input Expressions</param>
        /// <returns>All Grouped output sets</returns>
        public static Tuple<uint, List<PassThruExpression>>[] GroupByChannelIds(this PassThruExpression[] Expressions)
        {
            // Now Split out our list in a for loop.
            var PairedExpressions = new List<Tuple<uint, List<PassThruExpression>>>();
            foreach (var ExpressionObject in Expressions)
            {
                // Store progress value
                double CurrentProgress = (Expressions.ToList().IndexOf(ExpressionObject) / (double)Expressions.Length) * 100.00;
                FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = (int)CurrentProgress;

                // Now store said channel ID value if we can pull it.
                if (ExpressionObject.TypeOfExpression == PassThruCommandType.NONE) continue;
                    
                // Find all properties of the expression set.
                var ExpressionValues = ExpressionObject.GetExpressionProperties();
                var DesiredProperty = ExpressionValues
                    .FirstOrDefault(FieldObj => FieldObj.Name
                        .Replace(" ", string.Empty).ToUpper()
                        .Contains("ChannelID".ToUpper()));

                // Check out for the null value possibility
                if (DesiredProperty == null) continue;
                uint ChannelIdProperty = uint.Parse(DesiredProperty.GetValue(ExpressionObject).ToString());
                int IndexOfChannelId = PairedExpressions.FindIndex(ExpSet => ExpSet.Item1 == ChannelIdProperty);
                
                // Find our current ChannelID object and append if possible
                if (IndexOfChannelId != -1) PairedExpressions[IndexOfChannelId].Item2.Add(ExpressionObject);
                else PairedExpressions.Add(new Tuple<uint, List<PassThruExpression>>(ChannelIdProperty, new List<PassThruExpression> { ExpressionObject }));
            }

            // Return the built list of object values here.
            SimExtensionLogger.WriteLog($"BUILT A TOTAL OF {PairedExpressions.Count} EXPRESSION CHANNEL SETS OK!", LogType.WarnLog);
            return PairedExpressions.ToArray();
        }

        /// <summary>
        /// Builds a Channel object from a set of input expressions
        /// </summary>
        /// <param name="GroupedExpression">Expression set to convert</param>
        /// <returns>Builds a channel session object to simulate (converted to JSON)</returns>
        public static Tuple<uint, SimulationChannel> BuildChannelsFromExpressions(this PassThruExpression[] GroupedExpression, uint ChannelId)
        {
            // Find all the PTFilter commands first and invert them.
            var PTConnectCommands = GroupedExpression
                .Where(ExpObj => ExpObj.TypeOfExpression == PassThruCommandType.PTConnect)
                .Cast<PassThruConnectExpression>()
                .ToArray();
            var PTFilterCommands = GroupedExpression
                .Where(ExpObj => ExpObj.TypeOfExpression == PassThruCommandType.PTStartMsgFilter)
                .Cast<PassThruStartMessageFilterExpression>()
                .ToArray();
            var PTReadCommands = GroupedExpression
                .Where(ExpObj => ExpObj.TypeOfExpression == PassThruCommandType.PTReadMsgs)
                .Cast<PassThruReadMessagesExpression>()
                .ToArray();
            var PTWriteCommands = GroupedExpression
                .Where(ExpObj => ExpObj.TypeOfExpression == PassThruCommandType.PTWriteMsgs)
                .Cast<PassThruWriteMessagesExpression>()
                .ToArray();
            
            // Find the ProtocolID and Current Channel ID. Then build a sim channel
            if (PTConnectCommands.Length == 0) return null;
            var ConnectCommand = PTConnectCommands.FirstOrDefault();
            var ChannelFlags = (PassThroughConnect)uint.Parse(ConnectCommand.ConnectFlags);
            var ProtocolInUse = (ProtocolId)Enum.Parse(typeof(ProtocolId), ConnectCommand.ProtocolId.Split(':')[1]);
            var BaudRateInUse = (BaudRate)Enum.Parse(typeof(BaudRate), Enum.GetNames(typeof(BaudRate))
                .FirstOrDefault(BaudObj => BaudObj.Contains(ProtocolInUse.ToString()) && BaudObj.Contains(ConnectCommand.BaudRate)));
            
            // Build simulation channel here and return it out
            var NextChannel = new SimulationChannel(ChannelId, ProtocolInUse, ChannelFlags, BaudRateInUse);
            NextChannel.StoreMessageFilters(PTFilterCommands);
            NextChannel.StoreMessagesRead(PTReadCommands);
            NextChannel.StoreMessagesWritten(PTWriteCommands);
            NextChannel.StorePassThruPairs(GroupedExpression);
            
            // Log information about the built out command objects.
            SimExtensionLogger.WriteLog(
                $"PULLED OUT THE FOLLOWING INFO FROM OUR COMMANDS (CHANNEL ID {ChannelId}):" +
                $"\n--> {PTConnectCommands.Length} PT CONNECTS" +
                $"\n--> {PTFilterCommands.Length} FILTERS" +
                $"\n--> {PTReadCommands.Length} READ COMMANDS" +
                $"\n--> {PTWriteCommands.Length} WRITE COMMANDS" + 
                $"\n--> {NextChannel.MessagePairs.Length} MESSAGE PAIRS TOTAL",
                LogType.InfoLog
            );

            // Return a new tuple of our object for the command output
            return new Tuple<uint, SimulationChannel>(ChannelId, NextChannel);
        }
    }
}
