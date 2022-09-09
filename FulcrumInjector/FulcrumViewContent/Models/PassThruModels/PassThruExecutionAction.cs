using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpWrapper;

namespace FulcrumInjector.FulcrumViewContent.Models.PassThruModels
{
    /// <summary>
    /// Action object which allows us to invoke any PT command and a given SharpSession with arguments
    /// </summary>
    public class PassThruExecutionAction
    {
        // SharpSession object to invoke our routine onto
        public readonly Sharp2534Session SessionToInvoke;

        // Command name and the arguments to invoke on our command
        public readonly string J2534CommandName;
        public readonly object[] J2534CommandArguments;

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
        }
    }
}
