using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

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
        /// <returns></returns>
        public static Tuple<int, PassThruExpression[]>[] ExtractChannelIds(this PassThruExpression[] Expressions, int ChannelId = -1)
        {
            // Now Split out our list in a for loop.
            var PairedExpressions = new List<Tuple<int, PassThruExpression[]>>();
            foreach (var ExpressionObject in Expressions)
            {
                // Now store said channel ID value if we can pull it.
                var ChannelIdProperty = ExpressionObject.GetPropertyValue<int>("ChannelID");
                if (ChannelIdProperty == 0) continue;

                // Find our current ChannelID
                int IndexOfChannelId = PairedExpressions.FindIndex(ExpSet => ExpSet.Item1 == ChannelIdProperty);
                if (IndexOfChannelId != -1) PairedExpressions[IndexOfChannelId].Item2.Append(ExpressionObject); 
                else PairedExpressions.Add(new Tuple<int, PassThruExpression[]>(ChannelIdProperty, new[] { ExpressionObject }));
            }

            // Return the built list of object values here.
            SimExtensionLogger.WriteLog($"BUILT A TOTAL OF {PairedExpressions.Count} EXPRESSION CHANNEL SETS OK!", LogType.WarnLog);
            return PairedExpressions.ToArray();
        }
    }
}
