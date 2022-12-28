using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance and input objects for this class instance to build simulations
        private readonly SubServiceLogger _simulationLogger;
        public string SimulationName;
        public string SimulationFile;
        public PassThruExpression[] InputExpressions;

        #endregion // Fields

        #region Properties

        // Grouping Objects built out.
        public Dictionary<uint, SimulationChannel> SimulationChannels { get; private set; }
        public Dictionary<uint, PassThruExpression[]> SimulationExpressions { get; private set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

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
            string FileNameCleaned = Path.GetFileNameWithoutExtension(this.SimulationName);
            this._simulationLogger = (SubServiceLogger)LoggerQueue.SpawnLogger($"SimGeneratorLogger_{FileNameCleaned}", LoggerActions.SubServiceLogger);
            this._simulationLogger.WriteLog($"READY TO BUILD NEW SIMULATION NAMED {this.SimulationName} WITH {Expressions.Length} INPUT EXPRESSIONS...", LogType.WarnLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts an input set of expression objects into a grouped set of expressions paired off by a Channel ID Value
        /// </summary>
        /// <param name="UpdateParseProgress">When true, progress on the injector log review window will be updated</param>
        /// <returns>A collection of built expression objects paired off by channel ID values</returns>
        public Dictionary<uint, PassThruExpression[]> GenerateGroupedIds(bool UpdateParseProgress = false)
        {
            // Build a dictionary for return output objects and log we're starting to update our values
            var BuiltExpressions = new Dictionary<uint, PassThruExpression[]>();
            this._simulationLogger.WriteLog("GROUPING COMMANDS BY CHANNEL ID VALUES NOW...", LogType.WarnLog);

            // Group off all the commands by channel ID and then convert them to paired objects
            int LoopsCompleted = 0; int ExpressionsCount = this.InputExpressions.Length;
            Parallel.ForEach(this.InputExpressions, ExpressionObject =>
            {
                // Invoke a progress update here if needed
                if (UpdateParseProgress)
                {
                    // Get the new progress value and update our UI value with it
                    int OldProgress = FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress;
                    int CurrentProgress = (int)((double)LoopsCompleted++ / (double)ExpressionsCount * 100.00);
                    if (OldProgress != CurrentProgress) FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = CurrentProgress;
                }

                // If we've got a none type expression, move on to the next loop
                if (ExpressionObject.TypeOfExpression == PassThruCommandType.NONE) return;
                
                // Find our channel ID property and store it here as a uint value to pair off with
                FieldInfo DesiredProperty = ExpressionObject
                    .GetExpressionProperties()
                    .FirstOrDefault(FieldObj => FieldObj.Name
                        .Replace(" ", string.Empty).ToUpper()
                        .Contains("ChannelID".ToUpper()));

                // If the field info is not null, then store the value located
                if (DesiredProperty ==  null) return;
                uint ChannelIdValue = uint.Parse(DesiredProperty.GetValue(ExpressionObject).ToString());

                // Lock our output collection here to avoid thread issues
                lock (BuiltExpressions)
                {
                    // Now insert this expression object based on what keys are in the output collection of expressions
                    if (!BuiltExpressions.ContainsKey(ChannelIdValue))
                    {
                        // Build a new tuple object to store on the output collection
                        PassThruExpression[] ExpressionsArray = { ExpressionObject };
                        BuiltExpressions.Add(ChannelIdValue, ExpressionsArray);
                    }
                    else
                    {
                        // Pull the current tuple object set, update the list value for it, and store it
                        PassThruExpression[] ExpressionsFound = BuiltExpressions[ChannelIdValue];
                        ExpressionsFound = ExpressionsFound.Append(ExpressionObject).ToArray();
                        BuiltExpressions[ChannelIdValue] = ExpressionsFound;
                    }
                }
            });

            // Log done grouping, return the built ID values here as a dictionary with the Expressions and ID values
            this._simulationLogger.WriteLog("BUILT GROUPED SIMULATION COMMANDS OK!", LogType.InfoLog);
            this.SimulationExpressions = BuiltExpressions;
            return this.SimulationExpressions;
        }
        /// <summary>
        /// Builds our simulation channel objects for our input expressions which were paired off and generated in the routine above
        /// </summary>
        /// <param name="UpdateParseProgress">When true, progress on the injector log review window will be updated</param>
        /// <returns>The set of built simulation channels and channel ID values paired off</returns>
        public Dictionary<uint, SimulationChannel> GenerateSimulationChannels(bool UpdateParseProgress = false)
        {
            // Build a dictionary for return output objects and log we're starting to update our values
            var SimChannelsBuilt = new Dictionary<uint, SimulationChannel>();
            this._simulationLogger.WriteLog("BUILDING CHANNEL OBJECTS FROM CHANNEL ID VALUES NOW...", LogType.WarnLog);

            // Loop all the expression sets built in parallel and generate a simulation channel for them
            int LoopsCompleted = 0; int ExpressionsCount = this.SimulationExpressions.Count;
            Parallel.ForEach(this.SimulationExpressions, ExpressionSet =>
            {
                try
                {
                    // Pull the Channel ID and the expression objects here and build a channel from it
                    uint SimChannelId = ExpressionSet.Key;
                    PassThruExpression[] ChannelExpressions = ExpressionSet.Value;
                    SimulationChannel BuiltChannel = ChannelExpressions.BuildChannelsFromExpressions(SimChannelId);

                    // If our channel object is not null, then store it on our output collection now
                    if (BuiltChannel != null)
                    {
                        // Lock the output collection to avoid thread issues and store the new channel
                        lock (SimChannelsBuilt)
                        {
                            // Now insert this expression object based on what keys are in the output collection of expressions
                            if (!SimChannelsBuilt.ContainsKey(SimChannelId)) SimChannelsBuilt.Add(SimChannelId, BuiltChannel);
                            else throw new InvalidDataException($"ERROR! CAN NOT APPEND A SIM CHANNEL WITH ID {SimChannelId} SINCE IT EXISTS ALREADY!");
                        }
                    }
                }
                catch (Exception BuildChannelCommandEx)
                {
                    // Log failures out and find out why the fails happen then move to our progress routine or move to next iteration
                    this._simulationLogger.WriteLog($"FAILED TO GENERATE A SIMULATION CHANNEL FROM A SET OF EXPRESSIONS!", LogType.WarnLog);
                    this._simulationLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", BuildChannelCommandEx, new[] { LogType.WarnLog, LogType.TraceLog });
                }

                // Invoke a progress update here if needed
                if (!UpdateParseProgress) return;

                // Get the new progress value and update our UI value with it
                int OldProgress = FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress;
                int CurrentProgress = (int)((double)LoopsCompleted++ / (double)ExpressionsCount * 100.00);
                if (OldProgress != CurrentProgress) FulcrumConstants.FulcrumLogReviewViewModel.ProcessingProgress = CurrentProgress;
            });

            // Log information about the simulation generation routine and exit out
            this._simulationLogger.WriteLog($"BUILT CHANNEL SIMULATION OBJECTS OK!", LogType.InfoLog);
            this._simulationLogger.WriteLog($"A TOTAL OF {SimChannelsBuilt.Count} CHANNELS HAVE BEEN BUILT!", LogType.InfoLog);
            this.SimulationChannels = SimChannelsBuilt;
            return this.SimulationChannels;
        }
        
        /// <summary>
        /// Takes an input set of PTExpressions and writes them to a file object desired.
        /// </summary>
        /// <param name="BaseFileName">Base file name to update and use as the output file name</param>
        /// <returns>Path of our built expression file</returns>
        public string SaveSimulationFile(string BaseFileName = "")
        {
            // First build our output location for our file.
            string OutputFolder = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorLogging.DefaultSimulationsPath");
            string FinalOutputPath =
                Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(BaseFileName)) + ".ptSim";

            // Get a logger object for saving expression sets.
            string LoggerName = $"{Path.GetFileNameWithoutExtension(BaseFileName)}_SimulationsLogger";
            var ExpressionLogger = (SubServiceLogger)LoggerQueue.SpawnLogger(LoggerName, LoggerActions.SubServiceLogger);

            // Find output path and then build final path value.             
            Directory.CreateDirectory(OutputFolder);
            if (!Directory.Exists(Path.GetDirectoryName(FinalOutputPath))) { Directory.CreateDirectory(Path.GetDirectoryName(FinalOutputPath)); }
            ExpressionLogger.WriteLog($"BASE OUTPUT LOCATION FOR SIMULATIONS IS SEEN TO BE {Path.GetDirectoryName(FinalOutputPath)}", LogType.InfoLog);

            // Log information about the expression set and output location
            ExpressionLogger.WriteLog($"SAVING A TOTAL OF {this.SimulationChannels.Count} SIMULATION OBJECTS NOW...", LogType.InfoLog);
            ExpressionLogger.WriteLog($"EXPRESSION SET IS BEING SAVED TO OUTPUT FILE: {FinalOutputPath}", LogType.InfoLog);

            try
            {
                // Now Build output string content from each expression object.
                ExpressionLogger.WriteLog("CONVERTING TO STRINGS NOW...", LogType.WarnLog);
                string OutputJsonValues = JsonConvert.SerializeObject(this.SimulationChannels, Formatting.Indented);

                // Log information and write output.
                ExpressionLogger.WriteLog($"CONVERTED INPUT OBJECTS INTO A JSON OUTPUT STRING OK!", LogType.WarnLog);
                ExpressionLogger.WriteLog("WRITING OUTPUT CONTENTS NOW...", LogType.WarnLog);
                File.WriteAllText(FinalOutputPath,OutputJsonValues);

                // Check to see if we aren't in the default location. If not, store the file in both the input spot and the injector directory
                if (BaseFileName.Contains(Path.DirectorySeparatorChar) && !BaseFileName.Contains("FulcrumLogs"))
                {
                    // Find the base path, get the file name, and copy it into here.
                    string LocalDirectory = Path.GetDirectoryName(BaseFileName);
                    string CopyLocation = Path.Combine(LocalDirectory, Path.GetFileNameWithoutExtension(FinalOutputPath)) + ".ptSim";
                    File.Copy(FinalOutputPath, CopyLocation, true);

                    // Remove the Expressions Logger. Log done and return
                    ExpressionLogger.WriteLog("COPIED OUTPUT SIMULATION FILE INTO THE BASE SIMULATION FILE LOCATION!");
                }

                // Remove the Expressions Logger. Log done and return
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
