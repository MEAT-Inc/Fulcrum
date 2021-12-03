using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumConsoleGui;
using FulcrumInjector.FulcrumConsoleGui.ConsoleSupport;
using FulcrumInjector.FulcrumJsonHelpers;
using FulcrumInjector.FulcrumPipeLogic;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector
{
    /// <summary>
    /// A Set of static methods used for sending pipe data around and configuring this application
    /// </summary>
    public static class FulcrumInjectorInit
    {
        // Logger object for the pipe injection application
        private static SubServiceLogger InjectorConfigLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorConfigLogger")) ?? new SubServiceLogger("InjectorConfigLogger");

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Checks for an existing fulcrum process object and kill all but the running one.
        /// </summary>
        private static bool InitCheckForExisting_1()
        {
            // Find all the fulcrum process objects now.
            InjectorConfigLogger.WriteLog("KILLING EXISTING FULCRUM INSTANCES NOW!", LogType.WarnLog);
            var CurrentInjector = Process.GetCurrentProcess();
            InjectorConfigLogger.WriteLog($"CURRENT FULCRUM PROCESS IS SEEN TO HAVE A PID OF {CurrentInjector.Id}", LogType.InfoLog);

            // Find the process values here.
            string CurrentInstanceName = ValueLoaders.GetConfigValue<string>("AppInstanceName");
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
        /// Builds a new console configuration based on values provided
        /// </summary>
        private static bool InitConfigureConsoleOutput_2()
        {
            // Setup Console Output and lock window location to what is set.
            var ConsoleSizes = ValueLoaders.GetConfigValue<int[]>("FulcrumConsole.ConsoleWindowSize");
            var RectShape = ConsoleShapeSetup.InitializeConsole(ConsoleSizes[0], ConsoleSizes[1]);

            // Check if we want to show the console GUI
            if (ValueLoaders.GetConfigValue<bool>("FulcrumConsole.EnableGuiConsole")) new FulcrumGuiConstructor().ToggleConsoleGuiView();
            else InjectorConfigLogger.WriteLog("NOT SETTING CONSOLE STATE TO A VALUE! THIS IS BECAUSE OUR BOOLEAN TO ALLOW GUI IS FALSE!", LogType.WarnLog);

            // Lock console window location
            new ConsoleLocker(RectShape, IntPtr.Zero).LockWindowLocation();
            InjectorConfigLogger.WriteLog($"CONSOLE WINDOW LOCKING HAS BEEN STARTED OK!", LogType.WarnLog);
            InjectorConfigLogger.WriteLog("BUILT NEW CONSOLE CONFIGURATION AND OUTPUT CORRECTLY!", LogType.WarnLog);
            InjectorConfigLogger.WriteLog("CONSOLE GUI IS SHOWING UP ON TOP OF THE CONSOLE NOW...", LogType.WarnLog);
            return true;
        }
        /// <summary>
        /// Builds and sets up a new set of fulcrum pipe objects
        /// </summary>
        /// <returns>True if pipes are built. False if not.</returns>
        private static bool InitConfigureFulcrumPipes_3()
        {
            // Main pipes for the fulcrum application
            InjectorConfigLogger.WriteLog("BUILDING NEW PIPE OBJECTS NOW...", LogType.InfoLog);
            FulcrumInjectorMain.AlphaPipe = new FulcrumPipeReader();
            FulcrumInjectorMain.BravoPipe = new FulcrumPipeWriter();

            // Output Pipe objects built.
            var OutputPipes = new[] { FulcrumInjectorMain.AlphaPipe, FulcrumInjectorMain.BravoPipe };
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
