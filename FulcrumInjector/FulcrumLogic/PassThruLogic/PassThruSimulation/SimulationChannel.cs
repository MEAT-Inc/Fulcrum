using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
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
        private readonly SubServiceLogger SimChannelLogger;

        // Class Values for a channel to simulate
        public ProtocolId ChannelProtocol;
        public J2534Filter[] MessageFilters;
        public PassThruStructs.PassThruMsg[] MessagesSent;
        public PassThruStructs.PassThruMsg[] MessagesRead;

        /// <summary>
        /// Builds a new Channel Simulation object from the given channel ID
        /// </summary>
        /// <param name="ChannelId"></param>
        public SimulationChannel(int ChannelId)
        {
            // Store the Channel ID
            this.ChannelId = (uint)ChannelId;
            this.SimChannelLogger = new SubServiceLogger($"SimChannelLogger_ID-{this.ChannelId}");
            this.SimChannelLogger.WriteLog($"BUILT NEW SIM CHANNEL OBJECT FOR CHANNEL ID {this.ChannelId}!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Stores a set of Expressions into messages on the given channel object
        /// </summary>
        /// <param name="">Expressions to extract and store</param>
        /// <returns>The Filters built</returns>
        public J2534Filter StoreMessageFilter(PassThruStartMessageFilterExpression ExpressionToStore)
        {
            // Store the Pattern, Mask, and Flow Ctl objects if they exist.
            var FilterType = ExpressionToStore.FilterType;
            var FilterPatten = ExpressionToStore.MessageFilterContents[0];
            var FilterMask = ExpressionToStore.MessageFilterContents[1];
            var FilterFlow = ExpressionToStore.MessageFilterContents.Count == 3 ?
                ExpressionToStore.MessageFilterContents[2] : Array.Empty<string>();

            // Now convert our information into string values.
            ExpressionToStore.ExpressionLogger.WriteLog($"--> FILTER {ExpressionToStore.FilterID}: CONVERTING TEXT TO MESSAGES NOW...", LogType.TraceLog);
            return null;
        }
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
            Parallel.ForEach(ExpressionsToStore, (FilterExpression) => {
                BuiltFilters.Add(this.StoreMessageFilter(FilterExpression));
            });

            // Reorder the list and return it out
            BuiltFilters = BuiltFilters
                .OrderBy(FilterObj => FilterObj.FilterId)
                .ToList();

            // Return the built filter objects here.
            this.MessageFilters = BuiltFilters.ToArray();
            return this.MessageFilters;
        }
    }
}
