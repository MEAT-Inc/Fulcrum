using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
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
        /// <param name="ExpressionToStore">Expressions to extract and store</param>
        /// <returns>The Filters built</returns>
        public J2534Filter StoreMessageFilter(PassThruStartMessageFilterExpression ExpressionToStore)
        {
            // Store the Pattern, Mask, and Flow Ctl objects if they exist.
            ExpressionToStore.FindFilterContents(out List<string[]> FilterContent);
            var FilterType = ExpressionToStore.FilterType;
            var FilterPatten = FilterContent[0].Last();
            var FilterMask = FilterContent[1].Last();
            var FilterFlow = FilterContent.Count == 3 ? FilterContent[2].Last() : "";

            // Now convert our information into string values.
            ExpressionToStore.ExpressionLogger.WriteLog($"--> FILTER {ExpressionToStore.FilterID}: CONVERTING TEXT TO MESSAGES NOW...", LogType.TraceLog);
            return new J2534Filter()
            {
                // Build a new filter object form the given values and return it.
                FilterType = FilterType,
                FilterMask = FilterPatten,
                FilterPattern = FilterMask,
                FilterFlowCtl = FilterFlow,

                // TODO FIX FILTER FLAGS BY USING CONTENTS OF THE LISTS
                FilterFlags = 0x00
            };
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
            foreach (var FilterExpression in ExpressionsToStore)
            {
                // Build the filter, log it's ID value out, and move on.
                BuiltFilters.Add(this.StoreMessageFilter(FilterExpression));
                this.SimChannelLogger.WriteLog($"--> BUILT NEW FILTER WITH ID {BuiltFilters.Last().FilterId}", LogType.TraceLog);
            }

            // TODO: FIX THIS PARALLEL BUG AT SOME POINT IF PERFORMANCE BECOMES POOR
            // Parallel.ForEach(ExpressionsToStore, (FilterExpression) => 
            // {
            //     // Build the filter, log it's ID value out, and move on.
            //     BuiltFilters.Add(this.StoreMessageFilter(FilterExpression));
            //     this.SimChannelLogger.WriteLog($"--> BUILT NEW FILTER WITH ID {BuiltFilters.Last().FilterId}", LogType.TraceLog);
            // });

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
