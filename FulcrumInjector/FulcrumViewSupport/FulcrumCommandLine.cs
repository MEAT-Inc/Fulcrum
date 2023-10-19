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
using FulcrumDriveService;
using FulcrumEmailService;
using FulcrumEncryption;
using FulcrumInjector.FulcrumViewContent.FulcrumControls;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using FulcrumSupport;
using FulcrumWatchdogService;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Class instance used to help us configure command line argument routines
    /// </summary>
    public sealed class FulcrumCommandLine
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private static configuration for Singleton pattern
        private static FulcrumCommandLine _commandLineHelper = null;
        private static readonly object _instanceLock = new object();

        // Private logger instance for our command line helper class 
        private readonly SharpLogger _commandLineLogger;

        #endregion // Fields

        #region Properties

        // Public static singleton instance for our email broker object
        public static FulcrumCommandLine CommandLineHelper
        {
            get
            {
                // Lock onto our instance lock for threading and pull our broker instance out
                lock (_instanceLock)
                    return _commandLineHelper ??= new FulcrumCommandLine();
            }
        }

        // Public facing properties holding our input command line arguments and the built startup actions
        public List<FulcrumStartupAction> StartupActions { get; private set; }
        public string ParsedArguments => string.Join(",", Environment.GetCommandLineArgs());
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

            // Watchdog configuration arguments. Base value is 0x00001000. Invoke is 0x00001002
            [Description("--WATCHDOG")] WATCHDOG = 0x00001000,
            [Description("--WATCHDOG_INITALIZE")] INIT_WATCHDOG = WATCHDOG | 0x00000001,
            [Description("--WATCHDOG_INVOKE")] INVOKE_WATCHDOG = WATCHDOG | INIT_WATCHDOG | 0x00000002,

            // Upload to drive configuration arguments. Base value is 0x00002000. Invoke is 0x00002001
            [Description("--DRIVE")] DRIVE = 0x00002000,
            [Description("--DRIVE_INITIALIZE")] INIT_DRIVE = DRIVE | 0x00000001,
            [Description("--DRIVE_INVOKE")] INVOKE_DRIVE = DRIVE | INIT_DRIVE | 0x00000002,

            // Encryption configuration arguments. Base value is 0x00008000. Invoke is 0x00008002
            [Description("--EMAIL")] EMAIL = 0x00008000,
            [Description("--EMAIL_INITIALIZE")] INIT_EMAIL = EMAIL | 0x00000001,
            [Description("--DRIVE_INVOKE")] INVOKE_EMAIL = EMAIL | INIT_EMAIL | 0x00000002,

            // Encryption configuration arguments. Base value is 0x00008000. Invoke is 0x00008001
            [Description("--ENCRYPTION")] ENCRYPTION = 0x00008000,
            [Description("--ENCRYPTION")] INIT_ENCRYPTION = ENCRYPTION | 0x00000001,
            [Description("--ENCRYPT_STRING")] ENCRYPT_STRING = ENCRYPTION | INIT_ENCRYPTION | 0x00000002,
            [Description("--DECRYPT_STRING")] DECRYPT_STRING = ENCRYPTION | INIT_ENCRYPTION | 0x00000004,
        }

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for a new fulcrum command line argument parser
        /// </summary>
        private FulcrumCommandLine()
        {
            // Spawn a new logger, configure backing properties, and store input arguments
            this.StartupActions = new List<FulcrumStartupAction>();
            this._commandLineLogger = new SharpLogger(LoggerActions.UniversalLogger);
            
            // Log out the arguments provided to the CLI for the injector application here
            this._commandLineLogger.WriteLog("PROCESSED COMMAND LINE ARGUMENTS FOR INJECTOR APPLICATION!");
            this._commandLineLogger.WriteLog($"COMMAND LINE ARGS: {this.ParsedArguments}");
        }
        /// <summary>
        /// Static CTOR for a command line helper instance. Simply pulls out the new singleton instance for our command line helper
        /// </summary>
        /// <returns>The instance for our CLI helper singleton</returns>
        public static FulcrumCommandLine InitializeCommandLineHelper()
        {
            // Build a new singleton instance for our CLI helper or return the current instance 
            return CommandLineHelper;
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
        /// <summary>
        /// Helper method used to invoke a requested startup action
        /// </summary>
        /// <param name="StartupAction">The startup action we're invoking</param>
        /// <returns>True if the action is invoked. False if it is not</returns>
        public bool InvokeCommandLineAction(FulcrumStartupAction StartupAction)
        {
            // Log out the action being invoked here and store the argument type
            StartupArguments ArgType = StartupAction.ArgumentType;
            this._commandLineLogger.WriteLog($"INVOKING ACTION {StartupAction.ArgumentType}...", LogType.InfoLog);

            // For no action types, just log out the type and return passed
            if (ArgType.HasFlag(StartupArguments.NO_ARGUMENTS)) {
                this._commandLineLogger.WriteLog("PROCESSED NON ACTION VALID CORRECTLY! NOT INVOKING A ROUTINE!", LogType.WarnLog);
                return true;
            }

            // For launch injector actions, just log out the type and return passed
            if (ArgType.HasFlag(StartupArguments.LAUNCH_INJECTOR)) {
                this._commandLineLogger.WriteLog("PROCESSED START INJECTOR ACTION CORRECTLY! NOT INVOKING A ROUTINE!", LogType.InfoLog);
                return true;
            }

            // For all other action types, return the result of the invoked routine
            if (ArgType.HasFlag(StartupArguments.WATCHDOG)) return this._invokeWatchdogAction(StartupAction);
            if (ArgType.HasFlag(StartupArguments.DRIVE)) return this._invokeDriveAction(StartupAction);
            if (ArgType.HasFlag(StartupArguments.EMAIL)) return this._invokeEmailAction(StartupAction);
            if (ArgType.HasFlag(StartupArguments.ENCRYPTION)) return this._invokeEncryptionAction(StartupAction);

            // If no argument types are found to be valid, log this issue and exit out 
            this._commandLineLogger.WriteLog("ERROR! NO VALID STARTUP TYPES COULD BE FOUND FOR INPUT ACTION!", LogType.ErrorLog);
            return false;
        }

        /// <summary>
        /// Private helper method used to invoke a watchdog action
        /// </summary>
        /// <param name="WatchdogAction">The watchdog action being invoked</param>
        /// <returns>True if the action is invoked. False if not</returns>
        private bool _invokeWatchdogAction(FulcrumStartupAction WatchdogAction)
        {
            // Switch based on the argument type and execute the needed action
            StartupArguments ArgType = WatchdogAction.ArgumentType;
            switch (ArgType)
            {
                // For watchdog init, build a new service and exit out
                case StartupArguments.WATCHDOG:
                case StartupArguments.INIT_WATCHDOG:
                    FulcrumWatchdog.InitializeWatchdogService(true);
                    this._commandLineLogger.WriteLog("INVOKED NEW WATCHDOG INSTANCE CORRECTLY!", LogType.InfoLog);
                    return true;

                // For watchdog invoke, build the service and invoke a new 
                case StartupArguments.INVOKE_WATCHDOG:
                    if (WatchdogAction.ArgumentParameters.Length == 0) {
                        this._commandLineLogger.WriteLog("ERROR! NO COMMAND TYPE WAS PROVIDED FOR WATCHDOG ROUTINE!", LogType.ErrorLog);
                        return false;
                    }

                    // Invoke a new watchdog service instance and run a custom command for it
                    string CommandNumberString = WatchdogAction.ArgumentParameters[0];
                    if (!int.TryParse(CommandNumberString, out int WatchdogCommand)) {
                        this._commandLineLogger.WriteLog($"ERROR! COULD NOT PARSE WATCHDOG COMMAND TYPE {CommandNumberString}!", LogType.ErrorLog);
                        return false;
                    }

                    // Once we've got a valid command, invoke it
                    this._commandLineLogger.WriteLog($"BUILDING WATCHDOG SERVICE AND INVOKING COMMAND {WatchdogCommand}...", LogType.InfoLog);
                    var WatchdogService = FulcrumWatchdog.InitializeWatchdogService().Result;
                    WatchdogService.RunCommand(WatchdogCommand);

                    // Break out once we've invoked our command
                    this._commandLineLogger.WriteLog($"EXECUTED COMMAND {WatchdogCommand} CORRECTLY!");
                    return true;

                // For all other cases, exit out failed
                default:
                    this._commandLineLogger.WriteLog($"ERROR! FAILED TO INVOKE WATCHDOG ACTION {ArgType}!", LogType.ErrorLog);
                    return false;
            }
        }
        /// <summary>
        /// Private helper method used to invoke a drive action
        /// </summary>
        /// <param name="DriveAction">The drive action being invoked</param>
        /// <returns>True if the action is invoked. False if not</returns>
        private bool _invokeDriveAction(FulcrumStartupAction DriveAction)
        {
            // Switch based on the argument type and execute the needed action
            StartupArguments ArgType = DriveAction.ArgumentType;
            switch (ArgType)
            {
                // For drive service init, build a new service and exit out
                case StartupArguments.DRIVE:
                case StartupArguments.INIT_DRIVE:
                    FulcrumDrive.InitializeDriveService(true);
                    this._commandLineLogger.WriteLog("INVOKED NEW DRIVE SERVICE INSTANCE CORRECTLY!", LogType.InfoLog);
                    return true;

                // For watchdog invoke, build the service and invoke a new command
                case StartupArguments.INVOKE_DRIVE:
                    if (DriveAction.ArgumentParameters.Length == 0) {
                        this._commandLineLogger.WriteLog("ERROR! NO COMMAND TYPE WAS PROVIDED FOR DRIVE ROUTINE!", LogType.ErrorLog);
                        return false;
                    }

                    // Invoke a new watchdog service instance and run a custom command for it
                    string CommandNumberString = DriveAction.ArgumentParameters[0];
                    if (!int.TryParse(CommandNumberString, out int DriveCommand)) {
                        this._commandLineLogger.WriteLog($"ERROR! COULD NOT PARSE DRIVE COMMAND TYPE {CommandNumberString}!", LogType.ErrorLog);
                        return false;
                    }

                    // Once we've got a valid command, invoke it
                    this._commandLineLogger.WriteLog($"BUILDING DRIVE SERVICE AND INVOKING COMMAND {DriveCommand}...", LogType.InfoLog);
                    var DriveService = FulcrumDrive.InitializeDriveService().Result;
                    DriveService.RunCommand(DriveCommand);

                    // Break out once we've invoked our command
                    this._commandLineLogger.WriteLog($"EXECUTED COMMAND {DriveCommand} CORRECTLY!");
                    return true;

                // For all other cases, exit out failed
                default:
                    this._commandLineLogger.WriteLog($"ERROR! FAILED TO INVOKE DRIVE ACTION {ArgType}!", LogType.ErrorLog);
                    return false;
            }
        }
        /// <summary>
        /// Private helper method used to invoke an email action
        /// </summary>
        /// <param name="EmailAction">The email action being invoked</param>
        /// <returns>True if the action is invoked. False if not</returns>
        private bool _invokeEmailAction(FulcrumStartupAction EmailAction)
        {
            // Switch based on the argument type and execute the needed action
            StartupArguments ArgType = EmailAction.ArgumentType;
            switch (ArgType)
            {
                // For encryption init, build a new service and exit out
                case StartupArguments.EMAIL:
                case StartupArguments.INIT_EMAIL:
                    FulcrumEmail.InitializeEmailService(true);
                    this._commandLineLogger.WriteLog("INVOKED NEW DRIVE SERVICE INSTANCE CORRECTLY!", LogType.InfoLog);
                    return true;

                case StartupArguments.INVOKE_EMAIL:
                    if (EmailAction.ArgumentParameters.Length == 0) {
                        this._commandLineLogger.WriteLog("ERROR! NO COMMAND TYPE WAS PROVIDED FOR EMAIL ROUTINE!", LogType.ErrorLog);
                        return false;
                    }

                    // Invoke a new watchdog service instance and run a custom command for it
                    string CommandNumberString = EmailAction.ArgumentParameters[0];
                    if (!int.TryParse(CommandNumberString, out int WatchdogCommand)) {
                        this._commandLineLogger.WriteLog($"ERROR! COULD NOT PARSE EMAIL COMMAND TYPE {CommandNumberString}!", LogType.ErrorLog);
                        return false;
                    }

                    // Once we've got a valid command, invoke it
                    this._commandLineLogger.WriteLog($"BUILDING EMAIL SERVICE AND INVOKING COMMAND {WatchdogCommand}...", LogType.InfoLog);
                    var WatchdogService = FulcrumEmail.InitializeEmailService().Result;
                    WatchdogService.RunCommand(WatchdogCommand);

                    // Break out once we've invoked our command
                    this._commandLineLogger.WriteLog($"EXECUTED COMMAND {WatchdogCommand} CORRECTLY!");
                    return true;

                // For all other cases, exit out failed
                default:
                    this._commandLineLogger.WriteLog($"ERROR! FAILED TO INVOKE EMAIL ACTION {ArgType}!", LogType.ErrorLog);
                    return false;
            }
        }
        /// <summary>
        /// Private helper method used to invoke an encryption action
        /// </summary>
        /// <param name="EncryptionAction">The encryption action being invoked</param>
        /// <returns>True if the action is invoked. False if not</returns>
        private bool _invokeEncryptionAction(FulcrumStartupAction EncryptionAction)
        {
            // Switch based on the argument type and execute the needed action
            StartupArguments ArgType = EncryptionAction.ArgumentType;
            switch (ArgType)
            {
                // For encryption init, build a new service and exit out
                case StartupArguments.ENCRYPTION:
                case StartupArguments.INIT_ENCRYPTION:

                    // Ensure our encryption keys are configured before trying to invoke an action
                    if (FulcrumEncryptionWindow.ConfigureEncryptionKeys()) this._commandLineLogger.WriteLog("ENCRYPTION CONFIGURATION IS OK!", LogType.InfoLog);
                    else
                    {
                        // If encryption is not configured, request the user set it up now
                        this._commandLineLogger.WriteLog("ERROR! ENCRYPTION IS NOT CONFIGURED!", LogType.WarnLog);
                        this._commandLineLogger.WriteLog("SHOWING ENCRYPTION CONFIGURATION WINDOW NOW!", LogType.WarnLog);
                        return false;
                    }

                    // Log out that we've invoked the encryption configuration routine and exit out
                    this._commandLineLogger.WriteLog("INVOKED NEW ENCRYPTION SERVICE ACTION CORRECTLY!", LogType.InfoLog);
                    return true;

                // For encrypting or decrypting a string, check configuration and decrypt the value given
                case StartupArguments.ENCRYPT_STRING:
                case StartupArguments.DECRYPT_STRING:

                    // Ensure our encryption keys are configured before trying to invoke an action
                    if (FulcrumEncryptionWindow.ConfigureEncryptionKeys()) this._commandLineLogger.WriteLog("ENCRYPTION CONFIGURATION IS OK!", LogType.InfoLog);
                    else
                    {
                        // If encryption is not configured, request the user set it up now
                        this._commandLineLogger.WriteLog("ERROR! ENCRYPTION IS NOT CONFIGURED!", LogType.WarnLog);
                        this._commandLineLogger.WriteLog("SHOWING ENCRYPTION CONFIGURATION WINDOW NOW!", LogType.WarnLog);
                        return false;
                    }

                    // Check our argument count for the encryption routine here
                    if (EncryptionAction.ArgumentParameters.Length == 0)
                    {
                        this._commandLineLogger.WriteLog("ERROR! COULD NOT FIND STRING VALUE TO ENCRYPT OR DECRYPT!", LogType.ErrorLog);
                        return false;
                    }

                    // Store if we're encrypting or decrypting here and convert our input value
                    bool IsEncrypting = ArgType == StartupArguments.ENCRYPT_STRING;
                    string InputValue = EncryptionAction.ArgumentParameters[0];
                    string ConvertedString = IsEncrypting
                        ? FulcrumEncryptor.Encrypt(InputValue)
                        : FulcrumEncryptor.Decrypt(InputValue);

                    // Log out the action has passed correctly and write the new value out to our log file
                    this._commandLineLogger.WriteLog($"{(IsEncrypting ? "ENCRYPTED" : "DECRYPTED")} STRING CORRECTLY!", LogType.InfoLog);
                    this._commandLineLogger.WriteLog($"INPUT STRING VALUE: {InputValue}", LogType.InfoLog);
                    this._commandLineLogger.WriteLog($"{(IsEncrypting ? "ENCRYPTED" : "DECRYPTED")} STRING VALUE: {ConvertedString}", LogType.InfoLog);
                    return true;

                // For all other cases, exit out failed
                default:
                    this._commandLineLogger.WriteLog($"ERROR! FAILED TO INVOKE ENCRYPTION ACTION {ArgType}!", LogType.ErrorLog);
                    return false;
            }
        }
    }
}
