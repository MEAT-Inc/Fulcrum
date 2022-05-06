using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation
{
    /// <summary>
    /// Takes a set of PT Expression objects and converts them into simulation ready commands.
    /// </summary>
    public class SimulationGenerator
    {
        // Logger object.
        private SubServiceLogger SimLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SimGeneratorLogger")) ?? new SubServiceLogger("SimGeneratorLogger");

        // Input objects for this class instance to build simulations
        public readonly string SimulationName;
        public readonly string SimulationFile;
        public readonly PassThruExpression[] InputExpressions;

        // Grouping Objects built out.
        public Tuple<int, PassThruExpression[]>[] GroupedChannelExpressions { get; private set; }
        public Tuple<int, SimulationChannel>[] BuiltSimulationChannels { get; private set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Builds a new simulation object generator from the given input expressions
        /// </summary>
        public SimulationGenerator(string SimName, PassThruExpression[] Expressions)
        {
            // Build our new file name here.
            string InputFilePath = Path.GetDirectoryName(SimName);
            string InputFileName = Path.ChangeExtension(Path.GetFileName(SimName), ".ptSim");

            // Store name of simulation and the input expressions here.
            this.SimulationName = InputFileName;
            this.InputExpressions = Expressions;
            this.SimulationFile = Path.Combine(InputFilePath, InputFileName);
            this.SimLogger.WriteLog($"READY TO BUILD NEW SIMULATION NAMED {SimulationName} WITH {Expressions.Length} INPUT EXPRESSIONS...", LogType.WarnLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------
 
        /// <summary>
        /// Generates our grouped ID values for out input expression objects
        /// </summary>
        /// <returns>Build ID groupings</returns>
        public Tuple<int, PassThruExpression[]>[] GenerateGroupedIds()
        {
            // List for return output objects
            List<Tuple<int, PassThruExpression[]>> BuiltExpressions = new List<Tuple<int, PassThruExpression[]>>();

            // Store the ID Values here
            this.SimLogger.WriteLog("GROUPING COMMANDS BY CHANNEL ID VALUES NOW...", LogType.WarnLog);
            var GroupedAsLists = this.InputExpressions.GroupByChannelIds();
            Parallel.ForEach(GroupedAsLists, (GroupList) => {
                BuiltExpressions.Add(new Tuple<int, PassThruExpression[]>(GroupList.Item1, GroupList.Item2.ToArray()));
                this.SimLogger.WriteLog($"--> BUILT NEW LIST GROPUING FOR CHANNEL ID {GroupList.Item1}", LogType.TraceLog);
            });

            // Log done grouping, return the built ID values here.
            this.SimLogger.WriteLog("BUILT GROUPED SIMULATION COMMANDS OK!", LogType.InfoLog);
            this.GroupedChannelExpressions = BuiltExpressions.ToArray();
            return this.GroupedChannelExpressions;
        }
        /// <summary>
        /// Builds our simulation channel objects for our input expressions on this object
        /// </summary>
        /// <returns>The set of built expression objects</returns>
        public Tuple<int, SimulationChannel>[] GenerateSimulationChannels()
        {
            // Start by building return list object. Then build our data
            List<Tuple<int, SimulationChannel>> BuiltChannelsList = new List<Tuple<int, SimulationChannel>>();
            this.SimLogger.WriteLog("BUILDING CHANNEL OBJECTS FROM CHANNEL ID VALUES NOW...", LogType.WarnLog);

            // Make sure the channel objects exist here first. 
            this.GroupedChannelExpressions ??= Array.Empty<Tuple<int, PassThruExpression[]>>();
            Parallel.ForEach(this.GroupedChannelExpressions, (ChannelObjectExpressions) =>
            {
                // Pull the Channel ID, build our output contents
                int ChannelId = ChannelObjectExpressions.Item1;
                var BuiltChannel = ChannelObjectExpressions.Item2.BuildChannelsFromExpressions(ChannelId).Item2;

                // Append it into our list of output here
                BuiltChannelsList.Add(new Tuple<int, SimulationChannel>(ChannelId, BuiltChannel));
                this.SimLogger.WriteLog($"--> BUILT EXPRESSION SET FOR CHANNEL {ChannelId}", LogType.TraceLog);
            });

            // Log information and exit out of this routine
            this.SimLogger.WriteLog("BUILT CHANNEL SIMULATION OBJECTS OK!", LogType.InfoLog);
            this.BuiltSimulationChannels = BuiltChannelsList.ToArray();
            return this.BuiltSimulationChannels;
        }
    }
}
