using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;

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
        public static Tuple<int, List<PassThruExpression>>[] GroupByChannelIds(this PassThruExpression[] Expressions)
        {
            // Now Split out our list in a for loop.
            var PairedExpressions = new List<Tuple<int, List<PassThruExpression>>>();
            foreach (var ExpressionObject in Expressions)
            {
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
                int ChannelIdProperty = int.Parse(DesiredProperty.GetValue(ExpressionObject).ToString());
                int IndexOfChannelId = PairedExpressions.FindIndex(ExpSet => ExpSet.Item1 == ChannelIdProperty);
                
                // Find our current ChannelID object and append if possible
                if (IndexOfChannelId != -1) PairedExpressions[IndexOfChannelId].Item2.Add(ExpressionObject);
                else PairedExpressions.Add(new Tuple<int, List<PassThruExpression>>(ChannelIdProperty, new List<PassThruExpression> { ExpressionObject }));
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
        public static Tuple<int, SimulationChannel> BuildChannelsFromExpressions(this PassThruExpression[] GroupedExpression, int ChannelId)
        {
            // Find all the PTFilter commands first and invert them.
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

            // Log information about the built out command objects.
            SimExtensionLogger.WriteLog(
                $"PULLED OUT THE FOLLOWING INFO FROM OUR COMMANDS:" +
                $"\n--> {PTFilterCommands.Length} FILTERS" +
                $"\n--> {PTReadCommands.Length} READ COMMANDS" +
                $"\n--> {PTWriteCommands} WRITE COMMANDS", 
            LogType.InfoLog
            );

            // List of our built J2534 Filters
            var NextChannel = new SimulationChannel(ChannelId);
            NextChannel.StoreMessageFilters(PTFilterCommands);

            // Temp null return
            return new Tuple<int, SimulationChannel>(ChannelId, NextChannel);
        }
    }
}
