using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumConsoleGui;
using FulcrumInjector.FulcrumConsoleGui.ConsoleSupport;
using FulcrumInjector.FulcrumJsonHelpers;
using FulcrumInjector.FulcrumLogging;
using FulcrumInjector.FulcrumLogging.LoggerObjects;
using FulcrumInjector.FulcrumLogging.LoggerSupport;
using FulcrumInjector.FulcrumLogic;

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
        public static FulcrumPipeReader AlphaPipe;
        public static FulcrumPipeReader BravoPipe;

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Main entry point for the Fulcrum Injector configuration application
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Build logging and console configurations
            ConfigureLogging();
            InjectorMainLogger = new SubServiceLogger("InjectorMainLogger");
            InjectorMainLogger.WriteLog("LOGGER CONFIGURED OK FOR MAIN FULCRUM INJECTOR!", LogType.InfoLog);

            // Configure console output view contents
            ConfigureConsoleOutput();
            InjectorMainLogger.WriteLog("LOGGERS AND CONSOLE OUTPUT BUILT OK! GENERATING LOGGER FOR MAIN NOW...", LogType.InfoLog);

            // Build out new Pipe Servers.
            if (!ConfigurePipes(out FulcrumPipeReader[] BuiltPipeReaders))
            {
                // Log this failure then exit the application
                InjectorMainLogger.WriteLog("FAILED TO CONFIGURE ONE OR MORE OF THE PIPE OBJECTS FOR THIS SESSION!", LogType.FatalLog);
                Console.ReadLine();

                // Exit application. -100 means failed to configure pipes
                Environment.Exit(-100);
            }

            // Store pipes from our connection routine
            AlphaPipe = BuiltPipeReaders[0]; BravoPipe = BuiltPipeReaders[1];
            InjectorMainLogger.WriteLog("PIPES ARE OPEN AND STORED CORRECTLY! READY TO PROCESS OR SEND DATA THROUGH THEM!", LogType.InfoLog);
            Console.ReadLine();
        }


        /// <summary>
        /// Builds a new console configuration based on values provided
        /// </summary>
        private static void ConfigureConsoleOutput()
        {
            // Setup Console Output and lock window location to what is set.
            var ConsoleSizes = ValueLoaders.GetConfigValue<int[]>("FulcrumConsole.ConsoleWindowSize");
            var RectShape = ConsoleShapeSetup.InitializeConsole(ConsoleSizes[0], ConsoleSizes[1]);

            // Lock console location here and GUI builder
            new FulcrumGuiConstructor().ToggleConsoleGuiView();
            new ConsoleLocker(RectShape, IntPtr.Zero).LockWindowLocation();
            InjectorMainLogger.WriteLog($"CONSOLE WINDOW LOCKING HAS BEEN STARTED OK!", LogType.WarnLog);
            InjectorMainLogger.WriteLog("BUILT NEW CONSOLE CONFIGURATION AND OUTPUT CORRECTLY! GUI IS SHOWING UP ON TOP OF THE CONSOLE NOW", LogType.WarnLog);
        }
        /// <summary>
        /// Builds new logging information and instances for fulcrum logging output.
        /// </summary>
        /// <returns>True if done ok, false if not.</returns>
        private static void ConfigureLogging()
        {
            // Build our logging configurations
            string AppName = ValueLoaders.GetConfigValue<string>("AppInstanceName");
            string LoggingPath = ValueLoaders.GetConfigValue<string>("FulcrumLogging.DefaultLoggingPath");
            FulcrumLoggingSetup LoggerInit = new FulcrumLoggingSetup(AppName, LoggingPath);

            // Configure loggers and their outputs here
            LoggerInit.ConfigureLogging();              // Make loggers
            LoggerInit.ConfigureLogCleanup();           // Build log cleanup routines
            FulcrumLogBroker.Logger?.WriteLog("BUILT NEW LOGGING INSTANCE CORRECTLY!", LogType.InfoLog);
        }
        /// <summary>
        /// Builds two new pipe server objects for us to configure during execution of this application
        /// </summary>
        /// <returns></returns>
        private static bool ConfigurePipes(out FulcrumPipeReader[] OutputPipes)
        {
            // First up, configure our new pipe servers for reading information.
            var PipeAlpha = new FulcrumPipeReader(FulcrumPipeType.FulcrumPipeAlpha);
            var PipeBravo = new FulcrumPipeReader(FulcrumPipeType.FulcrumPipeBravo);
            InjectorMainLogger.WriteLog("BUILT NEW PIPE SERVERS FOR BOTH ALPHA AND BRAVO WITHOUT ISSUE!", LogType.InfoLog);

            // Return passed output/
            OutputPipes = new[] { PipeAlpha, PipeBravo };
            return OutputPipes.All(PipeObj => PipeObj.PipeState == FulcrumPipeState.Connected);
        }
    }
}
