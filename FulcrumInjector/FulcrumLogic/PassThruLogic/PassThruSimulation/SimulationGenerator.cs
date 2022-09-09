using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumViewContent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpSimulator;
using SharpSimulator.SimulationObjects;

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
        public string SimulationName;
        public string SimulationFile;
        public PassThruExpression[] InputExpressions;

        // Grouping Objects built out.
        public SimulationChannel[] BuiltSimulationChannels { get; private set; }
        public Tuple<uint, PassThruExpression[]>[] GroupedChannelExpressions { get; private set; }

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
        public Tuple<uint, PassThruExpression[]>[] GenerateGroupedIds()
        {
            // List for return output objects
            List<Tuple<uint, PassThruExpression[]>> BuiltExpressions = new List<Tuple<uint, PassThruExpression[]>>();

            // Store the ID Values here
            this.SimLogger.WriteLog("GROUPING COMMANDS BY CHANNEL ID VALUES NOW...", LogType.WarnLog);
            var GroupedAsLists = this.InputExpressions.GroupByChannelIds();
            Parallel.ForEach(GroupedAsLists, (GroupList) => {

                // Build Expression
                BuiltExpressions.Add(new Tuple<uint, PassThruExpression[]>(GroupList.Item1, GroupList.Item2.ToArray()));
                this.SimLogger.WriteLog($"--> BUILT NEW LIST GROUPING FOR CHANNEL ID {GroupList.Item1}", LogType.TraceLog);

                // Store progress value
                double CurrentProgress = BuiltExpressions.Count / (double)GroupedAsLists.Length * 100.00;
                FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = (int)CurrentProgress;
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
        public Tuple<uint, SimulationChannel>[] GenerateSimulationChannels()
        {
            // Start by building return list object. Then build our data
            List<Tuple<uint, SimulationChannel>> BuiltChannelsList = new List<Tuple<uint, SimulationChannel>>();
            this.SimLogger.WriteLog("BUILDING CHANNEL OBJECTS FROM CHANNEL ID VALUES NOW...", LogType.WarnLog);

            // Make sure the channel objects exist here first. 
            this.GroupedChannelExpressions ??= Array.Empty<Tuple<uint, PassThruExpression[]>>();
            foreach (var ChannelObjectExpressions in this.GroupedChannelExpressions)
            {
                // Pull the Channel ID, build our output contents
                uint ChannelId = ChannelObjectExpressions.Item1;
                var BuiltChannel = ChannelObjectExpressions.Item2.BuildChannelsFromExpressions(ChannelId);

                // Store progress value
                double CurrentProgress = (double)(BuiltChannelsList.Count / (double)this.GroupedChannelExpressions.Length) * 100.00;
                FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = (int)CurrentProgress;

                // Append it into our list of output here
                if (BuiltChannel == null) continue;
                BuiltChannelsList.Add(new Tuple<uint, SimulationChannel>(ChannelId, BuiltChannel.Item2));
                this.SimLogger.WriteLog($"--> BUILT EXPRESSION SET FOR CHANNEL {ChannelId}", LogType.TraceLog);
            }

            // Log information and exit out of this routine
            this.SimLogger.WriteLog("BUILT CHANNEL SIMULATION OBJECTS OK!", LogType.InfoLog);
            BuiltChannelsList = BuiltChannelsList.Where(TupleOBj => TupleOBj != null).ToList();
            this.BuiltSimulationChannels = BuiltChannelsList.Select(ChannelSet => ChannelSet.Item2).ToArray();
            return BuiltChannelsList.ToArray();
        }
        /// <summary>
        /// Takes an input set of PTExpressions and writes them to a file object desired.
        /// </summary>
        /// <param name="InputExpressions">Expression input objects</param>
        /// <returns>Path of our built expression file</returns>
        public string SaveSimulationFile(string BaseFileName = "")
        {
            // First build our output location for our file.
            string OutputFolder = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorLogging.DefaultSimulationsPath");
            string FinalOutputPath =
                Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(BaseFileName)) + ".ptSim";

            // Get a logger object for saving expression sets.
            string LoggerName = $"{Path.GetFileNameWithoutExtension(BaseFileName)}_SimulationsLogger";
            var ExpressionLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith(LoggerName)) ?? new SubServiceLogger(LoggerName);

            // Find output path and then build final path value.             
            Directory.CreateDirectory(OutputFolder);
            if (!Directory.Exists(Path.GetDirectoryName(FinalOutputPath))) { Directory.CreateDirectory(Path.GetDirectoryName(FinalOutputPath)); }
            ExpressionLogger.WriteLog($"BASE OUTPUT LOCATION FOR SIMULATIONS IS SEEN TO BE {Path.GetDirectoryName(FinalOutputPath)}", LogType.InfoLog);

            // Log information about the expression set and output location
            ExpressionLogger.WriteLog($"SAVING A TOTAL OF {this.BuiltSimulationChannels.Length} SIMULATION OBJECTS NOW...", LogType.InfoLog);
            ExpressionLogger.WriteLog($"EXPRESSION SET IS BEING SAVED TO OUTPUT FILE: {FinalOutputPath}", LogType.InfoLog);

            try
            {
                // Now Build output string content from each expression object.
                ExpressionLogger.WriteLog("CONVERTING TO STRINGS NOW...", LogType.WarnLog);
                string OutputJsonValues = JsonConvert.SerializeObject(this.BuiltSimulationChannels, Formatting.Indented);

                // Log information and write output.
                ExpressionLogger.WriteLog($"CONVERTED INPUT OBJECTS INTO A JSON OUTPUT STRING OK!", LogType.WarnLog);
                ExpressionLogger.WriteLog("WRITING OUTPUT CONTENTS NOW...", LogType.WarnLog);
                File.WriteAllText(FinalOutputPath,OutputJsonValues);

                // TODO: Figure out why I had this code in here...
                // Check to see if we aren't in the default location
                // if (BaseFileName.Contains(Path.DirectorySeparatorChar) && !BaseFileName.Contains("FulcrumLogs"))
                // {
                //     // Find the base path, get the file name, and copy it into here.
                //     string LocalDirectory = Path.GetDirectoryName(BaseFileName);
                //     string CopyLocation = Path.Combine(LocalDirectory, Path.GetFileNameWithoutExtension(FinalOutputPath)) + ".ptSim";
                //     File.Copy(FinalOutputPath, CopyLocation, true);
                // 
                //     // Remove the Expressions Logger. Log done and return
                //     ExpressionLogger.WriteLog("DONE LOGGING OUTPUT CONTENT! RETURNING OUTPUT VALUES NOW");
                //     this.SimulationFile = CopyLocation;
                //     return CopyLocation;
                // }

                // Remove the Expressions Logger. Log done and return
                ExpressionLogger.WriteLog("DONE LOGGING OUTPUT CONTENT! RETURNING OUTPUT VALUES NOW");
                this.SimulationFile = FinalOutputPath;
                return FinalOutputPath;
            }
            catch (Exception WriteEx)
            {
                // Log failures. Return an empty string.
                ExpressionLogger.WriteLog("FAILED TO SAVE OUR OUTPUT EXPRESSION SETS! THIS IS FATAL!", LogType.FatalLog);
                ExpressionLogger.WriteLog("EXCEPTION FOR THIS INSTANCE IS BEING LOGGED BELOW", WriteEx);

                // Return nothing.
                return string.Empty;
            }
        }
    }
}
