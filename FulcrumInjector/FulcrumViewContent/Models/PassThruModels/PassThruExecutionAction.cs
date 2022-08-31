using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpWrap2534;

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

        // String values used to bind onto our UI
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
                    if (ArgObject.GetType() == typeof(string)) AllArgsAsStrings.Add(ArgObject.ToString());
                    if (ArgObject.GetType() == typeof(string[]))
                    {
                        // Cast the list to string array and build a formatted arg string from it
                        string[] CastArgStrings = (string[])ArgObject;
                        string FormattedArgStrings = string.Join(", ", CastArgStrings);
                        AllArgsAsStrings.Add(FormattedArgStrings);
                    }
                }

                // Build a formatted arg string set and print it out to the log
                string FormattedArgsList = string.Join(string.Empty, AllArgsAsStrings.Select(ArgString => $"--> {ArgString}\n"));
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
            // Store values passed in onto our instance. Parse out the argument names and types then store them.

        }
    }
}
