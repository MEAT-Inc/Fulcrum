using FulcrumInjector.FulcrumViewContent;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using SharpLogging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Class instance used to help us configure command line argument routines
    /// </summary>
    public class FulcrumCommandLine
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private logger instance for our command line helper class 
        private readonly SharpLogger _commandLineLogger;

        #endregion // Fields

        #region Properties

        // Public facing properties holding our input command line arguments and the built startup actions
        public string ParsedArguments { get; private set; }
        public List<FulcrumStartupAction> StartupActions { get; private set; }
        public bool ShouldLaunchInjector => this.StartupActions.Any(ActionObj => ActionObj.ArgumentType == StartupArguments.LAUNCH_INJECTOR);

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration used to configure different types of startup arguments
        /// </summary>
        [Flags] public enum StartupArguments
        {
            // Default values are no arguments or launch injector. If launch is not provided, we exit after invoking actions
            [Description("")] NO_ARGUMENTS = 0x00000000,
            [Description("--LAUNCH_INJECTOR")] LAUNCH_INJECTOR = 0x00000001,

            // Watchdog configuration arguments. Base value is 0x00001000. Invoke is 0x00001003
            [Description("--WATCHDOG")] WATCHDOG = 0x00001000,
            [Description("--WATCHDOG_INITALIZE")] INIT_WATCHDOG = WATCHDOG | 0x00000001,
            [Description("--WATCHDOG_INVOKE")] INVOKE_WATCHDOG = WATCHDOG | INIT_WATCHDOG | 0x00000002,

            // Upload to drive configuration arguments. Base value is 0x00002000. Invoke is 0x00002003
            [Description("--DRIVE")] DRIVE = 0x00002000,
            [Description("--DRIVE_INITIALIZE")] INIT_DRIVE = DRIVE | 0x00000001,
            [Description("--DRIVE_INVOKE")] INVOKE_DRIVE = DRIVE | INIT_DRIVE | 0x00000002,
        }

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for a new fulcrum command line argument parser
        /// </summary>
        public FulcrumCommandLine()
        {
            // Spawn a new logger, configure backing properties, and store input arguments
            this.StartupActions = new List<FulcrumStartupAction>();
            this._commandLineLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ParsedArguments = string.Join(",", Environment.GetCommandLineArgs());

            // Log out the arguments provided to the CLI for the injector application here
            this._commandLineLogger.WriteLog("PROCESSED COMMAND LINE ARGUMENTS FOR INJECTOR APPLICATION!");
            this._commandLineLogger.WriteLog($"COMMAND LINE ARGS: {this.ParsedArguments}");
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Parses the input command line arguments at the time of construction for this class and returns the arguments built from them
        /// </summary>
        /// <returns>The list of startup actions for the injector application to run</returns>
        /// <exception cref="ArgumentException">Thrown when arguments provided are invalid</exception>
        public List<FulcrumStartupAction> ParseCommandLineArgs()
        {
            // Check our arguments and invoke actions accordingly
            MatchCollection ArgumentMatches = Regex.Matches(this.ParsedArguments, @"(--\w+)");
            if (ArgumentMatches.Count == 0)
            {
                // If no arguments could be found/parsed, throw an exception and exit out
                this._commandLineLogger.WriteLog("ERROR! NO ARGUMENTS FOR STARTUP COULD BE PARSED FROM THE INPUT STRING!", LogType.ErrorLog);
                return this.StartupActions;
            }

            // Now look at the matches and find our action types
            foreach (Match ArgMatch in ArgumentMatches)
            {
                // Try and parse our arguments in to our list of actions here
                try
                {
                    // Check if we've got a parameter for the argument or not
                    string ArgString = ArgMatch.Value;
                    Match ParameterArgMatch = Regex.Match(ArgString, @"(--\w+)\((\d+)\)");
                    if (!ParameterArgMatch.Success) this.StartupActions.Add(new FulcrumStartupAction(ArgString.ToEnumValue<StartupArguments>()));
                    else
                    {
                        // If we've got a parameterized argument, store the arguments for it here
                        StartupArguments ArgType = ParameterArgMatch.Groups[1].Value.ToEnumValue<StartupArguments>();
                        string[] ArgumentParameters = ParameterArgMatch.Groups[2].Value.Split(',');

                        // Build and store the next parameterized argument object
                        this.StartupActions.Add(new FulcrumStartupAction(ArgType, ArgumentParameters));
                    }

                    // Log out the argument object parsed in here
                    var NewestArg = this.StartupActions.Last();
                    this._commandLineLogger.WriteLog(NewestArg.ArgumentParameters.Length == 0
                        ? $"--> PARSED ARGUMENT: {NewestArg.ArgumentType}"
                        : $"--> PARSED ARGUMENT: {NewestArg.ArgumentType} | PARAMETERS: {string.Join(",", NewestArg.ArgumentParameters)}");
                }
                catch (Exception ArgParseEx)
                {
                    // Log out the exception thrown during the parse routine
                    this._commandLineLogger.WriteLog($"ERROR! FAILED TO PARSE ARGUMENT: {ArgMatch.Value}!", LogType.ErrorLog);
                    this._commandLineLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW", ArgParseEx);
                }
            }

            // Return our list of built startup actions 
            return this.StartupActions;
        }
    }
}
