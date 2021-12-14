using System.Diagnostics;
using System.Linq;
using FulcrumInjector.AppLogic.InjectorPipes;
using FulcrumInjector.JsonHelpers;
using FulcrumInjector.ViewControl;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.AppLogic
{
    /// <summary>
    /// A Set of static methods used for sending pipe data around and configuring this application
    /// </summary>
    public static class InjectorPipeSetup
    {
        // Logger object for the pipe injection application
        private static SubServiceLogger InjectorConfigLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorConfigLogger")) ?? new SubServiceLogger("InjectorConfigLogger");

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Checks for an existing fulcrum process object and kill all but the running one.
        /// </summary>
        public static bool KillExistingFulcrumInstances()
        {
            // Find all the fulcrum process objects now.
            InjectorConfigLogger.WriteLog("KILLING EXISTING FULCRUM INSTANCES NOW!", LogType.WarnLog);
            var CurrentInjector = Process.GetCurrentProcess();
            InjectorConfigLogger.WriteLog($"CURRENT FULCRUM PROCESS IS SEEN TO HAVE A PID OF {CurrentInjector.Id}", LogType.InfoLog);

            // Find the process values here.
            string CurrentInstanceName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorSettings.AppInstanceName");
            InjectorConfigLogger.WriteLog($"CURRENT INJECTOR PROCESS NAME FILTERS ARE: {CurrentInstanceName} AND {CurrentInjector.ProcessName}");
            var InjectorsTotal = Process.GetProcesses()
                .Where(ProcObj => ProcObj.Id != CurrentInjector.Id)
                .Where(ProcObj => ProcObj.ProcessName.ToUpper().Contains(CurrentInstanceName)
                                  || ProcObj.ProcessName.Contains(CurrentInjector.ProcessName))
                .ToList();

            // THIS IS A POTENTIAL ISSUE!
            // BUG: KILLING NEW CAN DROP COMMANDS TO OUR PIPE! WE NEED TO BUILD THIS SO THAT THE OLDEST INSTANCE REMAINS ALIVE!

            // Now kill any existing instances
            InjectorConfigLogger.WriteLog($"FOUND A TOTAL OF {InjectorsTotal.Count} INJECTORS ON OUR MACHINE");
            InjectorConfigLogger.WriteLog("KILLING THESE PROCESS OBJECTS NOW...", LogType.InfoLog);
            foreach (var InjectorProc in InjectorsTotal)
            {
                InjectorProc.Kill();
                InjectorConfigLogger.WriteLog($"--> KILLED PROCESS {InjectorProc.Id} OK!", LogType.TraceLog);
            }

            // Return passed output.
            return true;
        }
        /// <summary>
        /// Builds and sets up a new set of fulcrum pipe objects
        /// </summary>
        /// <returns>True if pipes are built. False if not.</returns>
        public static bool ValidateFulcrumPipeConfiguration()
        {
            // Main pipes for the fulcrum application
            InjectorConfigLogger.WriteLog("BUILDING NEW PIPE OBJECTS NOW...", LogType.InfoLog);
            InjectorConstants.AlphaPipe = new FulcrumPipeReader();
            InjectorConstants.BravoPipe = new FulcrumPipeWriter();

            // Output Pipe objects built.
            var OutputPipes = new FulcrumPipe[] { InjectorConstants.AlphaPipe, InjectorConstants.BravoPipe };
            bool OutputResult = OutputPipes.All(PipeObj => PipeObj.PipeState == FulcrumPipeState.Connected);
            if (OutputResult)
            {
                // Store pipes from our connection routine
                InjectorConfigLogger.WriteLog("BUILT NEW PIPE SERVERS FOR BOTH ALPHA AND BRAVO WITHOUT ISSUE!", LogType.InfoLog);
                InjectorConfigLogger.WriteLog("PIPES ARE OPEN AND STORED CORRECTLY! READY TO PROCESS OR SEND DATA THROUGH THEM!", LogType.InfoLog);
                return true;
            }

            // Log this failure then exit the application
            InjectorConfigLogger.WriteLog("FAILED TO BUILD ONE OR BOTH PIPE SERVER READING CLIENTS!", LogType.FatalLog);
            InjectorConfigLogger.WriteLog("FAILED TO CONFIGURE ONE OR MORE OF THE PIPE OBJECTS FOR THIS SESSION!", LogType.FatalLog);
            return false;
        }
    }
}
