using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SharpWrapper;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace FulcrumInjector.FulcrumViewContent.Models.PassThruModels
{
    /// <summary>
    /// Enumerable used for listing the different types of PassThru commands we can execute
    ///
    /// Hex values are set by the type of command and the DLL version supported
    /// 0x00VVTTCC (See below for letter meanings)
    /// - V is the Version supported.
    ///  - 00 - None specified
    ///  - 01 - Version 0404
    ///  - 10 - Version 0500
    ///  - 11 - Versions 0404 and 0500
    /// - T is the Type of command being used
    ///  - 10 - PTOpen/Close
    ///  - 20 - PTConnect/Disconnect
    ///  - 21 - PTLogicalConnect/Disconnect
    ///  - 30 - Generic IOCTL Command
    ///  - 31 - Buffer IOCTL Command
    ///  - 32 - Set/Get Pins Command
    ///  - 33 - Init Commands
    ///  - 40 - Write Message Commands
    ///  - 41 - PTQueue Commands
    ///  - 50 - Read Message Commands
    ///  - 51 - PTSelect Commands
    ///  - 60 - Start Filter Commands
    ///  - 61 - Stop Filter Commands
    ///  - 70 - Start Periodic Commands
    ///  - 71 - Stop Periodic Commands
    /// - C is the command number. This counts up by one for each command entry
    /// </summary>
    public enum SharpSessionCommandType
    {
        // Command type entries go here with their hex value for identification
        [Description("PassThruOpen")] PTOpen = 0x00111001,
        [Description("PassThruClose")] PTClose = 0x0011102,
        [Description("PassThruConnect")] PTConnect = 0x00112001,
        [Description("PassThruDisconnect")] PTDisconnect = 0x00112002,
        [Description("PassThruLogicalConnect")] PTLogicalConnect =  0x00102101,
        [Description("PassThruLogicalDisconnect")] PTLogicalDisconnect = 0x00102102,
        [Description("PassThruReadVoltage")] PTReadVoltage = 0x00113001,
        [Description("PassThruClearTxBuffer")] PTClearTxBuffer = 0x00113101,
        [Description("PassThruClearRxBuffer")] PTClearRxBuffer = 0x00113102,
        [Description("PassThruSetPins")] PTSetPins = 0x00113201,
        [Description("PassThruSetConfig")] PTSetConfig = 0x00113202,
        [Description("PassThruGetConfig")] PTGetConfig = 0x00113203,
        [Description("PassThruFiveBaudInit")] PTFiveBaudInit = 0x00013301,
        [Description("PassThruFastInit")] PTFastInit = 0x00013302,
        [Description("PassThruWriteMessages")] PTWriteMessages = 0x00114001,
        [Description("PassThruQueueMessages")] PTQueueMessages = 0x00104102,
        [Description("PassThruReadMessages")] PTReadMessages = 0x00115001,
        [Description("PassThruSelect")] PTSelect = 0x00105101,
        [Description("PassThruStartMessageFilter")] PTStartMessageFilter = 0x00116001,
        [Description("PassThruStopMessageFilter")] PTStopMessageFilter = 0x00116101,
        [Description("PassThruStartPeriodicMessage")] PTStartPeriodicMessage = 0x00117001,
        [Description("PassThruStopPeriodicMessage")] PTStopPeriodicMessage = 0x00117101,
    }

    // ------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Action object which allows us to invoke any PT command and a given SharpSession with arguments
    /// </summary>
    public class PassThruExecutionAction : INotifyPropertyChanged
    {
        // Property Changed Events
        #region Property Changed Event Handler

        // Event object to fire when properties are changed
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            // Invoke the event if it's not null
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        #endregion

        // SharpSession object to invoke our routine onto
        public readonly Sharp2534Session SessionToInvoke;
        
        // Command name and the arguments to invoke on our command
        public readonly IEnumerable<object> CommandArguments;
        public readonly SharpSessionCommandType CommandName;

        // Method information and parameter information for our method object
        public readonly MethodInfo CommandMethodInfo;
        public readonly ParameterInfo[] CommandParamsInfos;

        // Command results and output from execution
        public bool WasExecuted { get; private set; }
        public bool ExecutionPassed { get; private set; } 
        public object ExecutionResult { get; private set; }
        public Exception ExecutionException { get; private set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Argument Information as a list object and string object
        public string CommandArgumentsString
        {
            get
            {
                // If the args list NULL, return "No Arguments!"
                if (this.CommandArguments == null || !this.CommandArguments.Any())
                    return "No Command Arguments!";


                // Build a list of objects of strings to store
                List<string> AllArgsAsStrings = new List<string>();
                foreach (var ArgObject in this.CommandArguments)
                {
                    // If it's a string, just add to our string output
                    if (ArgObject == null) AllArgsAsStrings.Add("NULL");
                    AllArgsAsStrings.Add(ArgObject.ToString());
                }

                // Build a formatted arg string set and print it out to the log
                string FormattedArgsList =
                    $"J2534 Command: {CommandName}\n" +
                    string.Join(string.Empty, AllArgsAsStrings.Select(ArgString => $"--> {ArgString}\n"));

                // Return the built list of arguments
                return FormattedArgsList;
            }
        }
        /// <summary>
        /// Override for printing this execution action out to a string
        /// </summary>
        /// <returns>A String containing the name of the command, the arguments in use, the Device in use, and the results</returns>
        public override string ToString()
        {
            // Generate a splitting log string
            string SplitLine = string.Join(string.Empty, Enumerable.Repeat("=", 50));

            // Build information strings here
            StringBuilder InformationStringBuilder = new StringBuilder();
            InformationStringBuilder.AppendLine(SplitLine);
            InformationStringBuilder.AppendLine($"--> Command:       {this.CommandName}");
            InformationStringBuilder.AppendLine($"--> Device:        {this.SessionToInvoke.DeviceName}");
            InformationStringBuilder.AppendLine($"--> Executed:      {(this.WasExecuted ? "Yes" : "No")}");
            InformationStringBuilder.AppendLine($"--> Passed:        {(this.ExecutionPassed ? "Yes" : "No")}");
            InformationStringBuilder.AppendLine(SplitLine);
            InformationStringBuilder.AppendLine($"--> Args Count:    {this.CommandParamsInfos.Length}");
            InformationStringBuilder.AppendLine($"--> Return Type:   {this.CommandMethodInfo.ReturnType.Name}");
            if (this.CommandArguments?.Count() > 0) {
                InformationStringBuilder.AppendLine(SplitLine);
                InformationStringBuilder.AppendLine("Argument Objects Included (Pretty Printed Below)");
                InformationStringBuilder.AppendLine(this.CommandArgumentsString);
            }

            // TODO: Build logic to include execution states and results in this output

            // Add a trailing pad line at the end of this string and return the built output
            InformationStringBuilder.AppendLine(SplitLine);
            return InformationStringBuilder.ToString();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new command execution object and stores all needed args/parameters on this instance
        /// </summary>
        /// <param name="InputSession">Session to invoke our command onto</param>
        /// <param name="CommandName">The command name to execute</param>
        /// <param name="CommandArguments">The arguments of our command object</param>
        public PassThruExecutionAction(Sharp2534Session InputSession, string CommandName, IEnumerable<object> CommandArguments = null)
        {
            // Store values passed in onto our instance.
            this.SessionToInvoke = InputSession;
            this.CommandArguments = (List<object>)CommandArguments;
            this.CommandName = (SharpSessionCommandType)Enum.Parse(typeof(SharpSessionCommandType), CommandName);

            // Get all the methods we can support first and then find the one for our command instance
            MethodInfo[] SharpSessionMethods = typeof(Sharp2534Session)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();

            // Store the methodinfo for the command object we wish to execute
            this.CommandMethodInfo = SharpSessionMethods
                .Where(MethodObject => MethodObject.GetParameters()
                    .All(ParamObj =>
                        ParamObj.ParameterType != typeof(J2534Filter) &&
                        ParamObj.ParameterType != typeof(J2534PeriodicMessage) &&
                        ParamObj.ParameterType != typeof(PassThruStructs.PassThruMsg[])))
                .FirstOrDefault(MethodObj => MethodObj.Name.ToUpper() == CommandName.ToUpper());
            this.CommandParamsInfos = this.CommandMethodInfo?.GetParameters();

            // Validate the parameter count matches the argument count and that this info object is not null
            if (this.CommandMethodInfo == null)
                throw new InvalidOperationException($"FAILED TO FIND COMMAND METHOD INFO FOR COMMAND {CommandName}!");
            if (this.CommandParamsInfos?.Length < this.CommandArguments?.Count())
                throw new ArgumentOutOfRangeException("ERROR! ARGUMENT COUNT OF THIS METHOD IS LESS THAN OUR INPUT ARGUMENT LIST LENGTH!");
        }

        /// <summary>
        /// Invokes a new command action on our command instance and stores the result of the execution
        /// </summary>
        /// <returns>True if our command execution passed. False if it fails for any reason</returns>
        public bool ExecuteCommandAction()
        {
            try
            {
                // First validate that our argument list matches the signature of the method parameters
                for (int ArgIndex = 0; ArgIndex < this.CommandParamsInfos.Length; ArgIndex++)
                {
                    // Validate the index value first to make sure we're still lined up
                    if (ArgIndex > this.CommandArguments.Count())
                    {
                        // If we're over the arg count, check if the next parameter is an out parameter or not.
                        ParameterInfo NextParamInfo = this.CommandParamsInfos[ArgIndex];
                        if (!NextParamInfo.IsOptional && !NextParamInfo.IsOut)
                            throw new ArgumentOutOfRangeException($"ERROR! MISSING ONE OR MORE CRITICAL ARGS FOR COMMAND: {CommandName}!");

                        // If it is an optional argument object then we can just add a new object to our list of arguments on the class
                        Type ParamType = NextParamInfo.ParameterType;
                        this.CommandArguments.Append(Activator.CreateInstance(ParamType));
                    }

                    // Get the parameter object and check if the type matches/if we have a value at all.
                    Type DesiredType = this.CommandParamsInfos[ArgIndex].ParameterType;
                    Type ProvidedType = this.CommandArguments.ElementAt(ArgIndex).GetType();
                    if (DesiredType != ProvidedType)
                        throw new TypeInitializationException(
                            $"ERROR! TYPE {ProvidedType.FullName} IS NOT THE SAME AS EXPECTED TYPE {DesiredType.FullName}!",
                            new InvalidOperationException($"FAILED TO EXECUTE ACTIONS FOR COMMAND {this.CommandName}!"));
                }
            }
            catch (Exception ConfigurationEx)
            {
                // Store the configuration exception as the execution result state and exit out
                this.ExecutionPassed = false;
                this.ExecutionResult = null;
                this.ExecutionException = ConfigurationEx;
                return false;
            }

            try
            {
                // Now execute the command object routine and store the results of it.
                this.ExecutionResult = this.CommandMethodInfo.Invoke(
                    this.SessionToInvoke, 
                    this.CommandArguments.ToArray()
                );
                
                // If the execution routine passes, return true
                return true;
            }
            catch (Exception ExecutionEx)
            {
                // Set execution failed, store the thrown exception, and return false.
                this.ExecutionPassed = false;
                this.ExecutionResult = null;
                this.ExecutionException = ExecutionEx;
                return false;
            }
        }
    }
}
