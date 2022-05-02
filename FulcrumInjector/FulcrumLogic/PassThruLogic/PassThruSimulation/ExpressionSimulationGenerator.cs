using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation
{
    /// <summary>
    /// Takes a set of PT Expression objects and converts them into simulation ready commands.
    /// </summary>
    public class ExpressionSimulationGenerator
    {
        // Logger object.
        private SubServiceLogger SimLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SimGeneratorLogger")) ?? new SubServiceLogger("SimGeneratorLogger");

        // Input objects for this class instance to build simulations
        public readonly string SimulationName;
        public readonly string SimulationFile;
        public readonly PassThruExpression[] InputExpressions;

        // Grouping Objects built out.
        public Tuple<int, PassThruExpression[]>[] GroupedChannelExpressions { get; }

        // ------------------------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Builds a new simulation object generator from the given input expressions
        /// </summary>
        public ExpressionSimulationGenerator(string SimName, PassThruExpression[] Expressions)
        {
            // Build our new file name here.
            string InputFilePath = Path.GetDirectoryName(SimName);
            string InputFileName = Path.ChangeExtension(Path.GetFileName(SimName), ".ptSim");

            // Store name of simulation and the input expressions here.
            this.SimulationName = InputFileName;
            this.InputExpressions = Expressions;
            this.SimulationFile = Path.Combine(InputFilePath, InputFileName);
            this.SimLogger.WriteLog($"BUILDING NEW SIMULATION FOR FILE NAMED {SimulationName} WITH A TOTAL OF {Expressions.Length} INPUT EXPRESSIONS...", LogType.WarnLog);

            // Run the Grouping command on the input expressions here.
            Expressions.ExtractChannelIds();
            this.SimLogger.WriteLog("BUILT GROUPED SIMULATION COMMANDS OK!", LogType.InfoLog);
        }
    }
}
