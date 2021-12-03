using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Main class for fulcrum injector configuration application
    /// </summary>
    public class FulcrumInjectorMain
    {
        // Logger object for the pipe injection application
        private static SubServiceLogger InjectorMainLogger;

        // Pipe objects used for building connections to our DLL
        public static FulcrumPipe AlphaPipe;
        public static FulcrumPipe BravoPipe;

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Main entry point for the Fulcrum Injector configuration application
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Build our logging configurations
            string AppName = ValueLoaders.GetConfigValue<string>("AppInstanceName");
            string LoggingPath = ValueLoaders.GetConfigValue<string>("FulcrumLogging.DefaultLoggingPath");
            var ConfigObj = ValueLoaders.GetConfigValue<dynamic>("FulcrumLogging.LogArchiveSetup");
            LoggingSetup LoggerInit = new LoggingSetup(AppName, LoggingPath);

            // Configure loggers and their outputs here
            LoggerInit.ConfigureLogging();                  // Make loggers
            LoggerInit.ConfigureLogCleanup(ConfigObj);      // Build log cleanup routines
            InjectorMainLogger = new SubServiceLogger("InjectorMainLogger");
            InjectorMainLogger.WriteLog("BUILT NEW LOGGING INSTANCE CORRECTLY!", LogType.InfoLog);

            // Run the Setup Methods for Logging and console output locking
            InjectorMainLogger.WriteLog("RUNNING OUR FULCRUM INJECTOR INIT LOGIC METHODS NOW...", LogType.InfoLog);
            if (!MethodSetInvoker(typeof(FulcrumInjectorInit), BindingFlags.Static | BindingFlags.NonPublic, false, "Init"))
                throw new InvalidOperationException("FAILED TO SETUP ONE OR MORE ACTIONS FOR FULCRUM!");

            // Now log info and build out new logger object for main instance.
            InjectorMainLogger.WriteLog(string.Concat(Enumerable.Repeat("=", 95)), LogType.WarnLog);
            InjectorMainLogger.WriteLog("INVOKED ALL CONFIG METHODS FOR THE NEW FULCRUM INSTANCE OK! READY TO PROCESS NEW PT COMMANDS!", LogType.InfoLog);
            InjectorMainLogger.WriteLog(string.Concat(Enumerable.Repeat("=", 95)), LogType.WarnLog);
        }

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Runs a set of methods pulled using reflection for the type requested.
        /// </summary>
        /// <param name="ClassTypeToCall">Type to reflect</param>
        /// <param name="FlagsToCheck">Flags for searching</param>
        /// <param name="InvokeOnInstance">Set if the methods need to be run on an instance or not.</param>
        /// <param name="MethodFilter">Name of methods to find. (Start of name)</param>
        /// <returns></returns>
        private static bool MethodSetInvoker(Type ClassTypeToCall, BindingFlags FlagsToCheck = default, bool InvokeOnInstance = false, string MethodFilter = "")
        {
            // List of methods to init with.
            MethodInfo[] InitMethods = ClassTypeToCall.GetMethods(FlagsToCheck).ToArray();
            if (InitMethods.Length == 0) { InitMethods = ClassTypeToCall.GetMethods(); }
            if (MethodFilter != "") { InitMethods = InitMethods.Where(MethodObj => MethodObj.Name.StartsWith(MethodFilter)).ToArray(); }

            // Get method count.
            int CurrentStep = 1;
            int TotalMethods = InitMethods.Length;

            // Build object to invoke onto if needed.
            object InvokeOnThis = null;
            if (!InvokeOnInstance) { InjectorMainLogger.WriteLog("NOT MAKING AN INSTANCE FOR METHOD INVOCATION!"); }
            else
            {
                // Build and log
                InvokeOnThis = Activator.CreateInstance(ClassTypeToCall);
                InjectorMainLogger.WriteLog($"BUILD NEW INSTANCE OF A {ClassTypeToCall.Name} OBJECT OK! INVOKING METHODS ONTO THIS NOW...", LogType.InfoLog);
            }

            // Loop all the method infos and run them
            var OrderedInitMethods = InitMethods.OrderBy(MethodObj => int.Parse(MethodObj.Name.Split('_').Last())).ToArray();
            InjectorMainLogger.WriteLog($"SETUP METHOD ORDER OF EXECUTION: {string.Join(",", OrderedInitMethods.Select(MethodObj => MethodObj.Name))}", LogType.InfoLog);
            foreach (var MethodToRun in OrderedInitMethods)
            {
                // Run the method.
                try
                {
                    // Log info and count up.
                    InjectorMainLogger.WriteLog($"TRYING TO EXECUTE METHOD {MethodToRun.Name} ({CurrentStep} of {TotalMethods})", LogType.DebugLog);
                    MethodToRun.Invoke(InvokeOnThis, null);
                    InjectorMainLogger.WriteLog($"METHOD {MethodToRun.Name} HAS BEEN EXECUTED OK!", LogType.InfoLog);

                    // Count up
                    CurrentStep += 1;
                }
                catch (Exception MethodEx)
                {
                    // Log failure and break
                    InjectorMainLogger.WriteLog($"ERROR! FAILED TO EXECUTE METHOD: {MethodToRun.Name}!", LogType.FatalLog);
                    InjectorMainLogger.WriteLog("EXCEPTION THROWN CAUSED SETUP TO FAIL!", MethodEx, new[] { LogType.FatalLog });

                    // Return failed
                    return false;
                }
            }

            // Return passed.
            InjectorMainLogger.WriteLog("DONE CONFIGURING METHODS FOR SETUP TYPE OK!", LogType.WarnLog);
            return true;
        }
    }
}
