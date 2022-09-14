using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SharpWrapper;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace FulcrumInjector.FulcrumViewContent.Models.PassThruModels
{
    /// <summary>
    /// Action object which allows us to invoke any PT command and a given SharpSession with arguments
    /// </summary>
    public class PassThruExecutionAction
    {
        // SharpSession object to invoke our routine onto
        public readonly Sharp2534Session SessionToInvoke;
        private readonly MethodInfo _jCommandMethodInfo;
        private readonly ParameterInfo[] _jCommandParamsInfos;

        // Command name and the arguments to invoke on our command
        public readonly string J2534CommandName;
        public readonly object[] J2534CommandArguments;

        // Command results and output from execution
        public bool WasExecuted = false;
        public bool ExecutionPassed = false;
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Argument Information as a list object and string object
        public string CommandArgumentsString
        {
            get
            {
                // If the args list NULL, return "No Arguments!"
                if (this.J2534CommandArguments == null || this.J2534CommandArguments.Length == 0)
                    return "No Command Arguments!";


                // Build a list of objects of strings to store
                List<string> AllArgsAsStrings = new List<string>();
                foreach (var ArgObject in this.J2534CommandArguments)
                {
                    // If it's a string, just add to our string output
                    if (ArgObject == null) AllArgsAsStrings.Add("NULL");
                    AllArgsAsStrings.Add(ArgObject.ToString());
                }

                // Build a formatted arg string set and print it out to the log
                string FormattedArgsList =
                    $"J2534 Command: {J2534CommandName}\n" +
                    string.Join(string.Empty, AllArgsAsStrings.Select(ArgString => $"--> {ArgString}\n"));

                // Return the built list of arguments
                return FormattedArgsList;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new command execution object and stores all needed args/parameters on this instance
        /// </summary>
        /// <param name="InputSession">Session to invoke our command onto</param>
        /// <param name="CommandName">The command name to execute</param>
        /// <param name="CommandArguments">The arguments of our command object</param>
        public PassThruExecutionAction(Sharp2534Session InputSession, string CommandName, object[] CommandArguments = null)
        {
            // Store values passed in onto our instance.
            this.SessionToInvoke = InputSession;
            this.J2534CommandName = CommandName;
            this.J2534CommandArguments = CommandArguments;

            // Get all the methods we can support first and then find the one for our command instance
            MethodInfo[] SharpSessionMethods = typeof(Sharp2534Session)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();

            // Store the methodinfo for the command object we wish to execute
            this._jCommandMethodInfo = SharpSessionMethods
                .Where(MethodObject => MethodObject.GetParameters()
                    .All(ParamObj =>
                        ParamObj.ParameterType != typeof(J2534Filter) &&
                        ParamObj.ParameterType != typeof(J2534PeriodicMessage) &&
                        ParamObj.ParameterType != typeof(PassThruStructs.PassThruMsg[])))
                .FirstOrDefault(MethodObj => MethodObj.Name.ToUpper() == CommandName.ToUpper());
            this._jCommandParamsInfos = this._jCommandMethodInfo?.GetParameters();

            // Validate the parameter count matches the argument count and that this info object is not null
            if (this._jCommandMethodInfo == null)
                throw new InvalidOperationException($"FAILED TO FIND COMMAND METHOD INFO FOR COMMAND {CommandName}!");
            if (this._jCommandParamsInfos.Length < this.J2534CommandArguments.Length)
                throw new ArgumentOutOfRangeException("ERROR! ARGUMENT COUNT OF THIS METHOD IS LESS THAN OUR INPUT ARGUMENT LIST LENGTH!");
        }

        /// <summary>
        /// Invokes a new command action on our command instance and stores the result of the execution
        /// </summary>
        /// <returns>True if our command execution passed. False if it fails for any reason</returns>
        public bool ExecuteCommandAction()
        {
            // First validate that our argument list matches the signature of the method parameters
            for (int ArgIndex = 0; ArgIndex < this._jCommandParamsInfos.Length; ArgIndex++)
            {
                // Validate the index value first


                // Get the parameter object and check if the type matches/if we have a value at all.
                Type DesiredType = this._jCommandParamsInfos[ArgIndex].ParameterType;
                Type ProvidedType = this.J2534CommandArguments[ArgIndex].GetType();
                if ()
            }

            // Now execute the command object routine and store the results of it.
            this._jCommandMethodInfo.Invoke(this.SessionToInvoke, this.J2534CommandArguments);
        }
    }
}
