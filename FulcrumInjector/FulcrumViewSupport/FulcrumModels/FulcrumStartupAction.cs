using System.Linq;
using FulcrumSupport;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels
{
    /// <summary>
    /// Class object holding information about a parsed argument object
    /// </summary>
    public class FulcrumStartupAction
    {
        // Public properties holding information about the requested action
        public string[] ArgumentParameters { get; set; }
        public FulcrumCommandLine.StartupArguments ArgumentType { get; set; }
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Override for the ToString call which will return out the startup action as a string object
        /// </summary>
        /// <returns>A string holding our startup argument as a string</returns>
        public override string ToString()
        {
            // Build an argument string and return it out
            if (this.ArgumentParameters == null || this.ArgumentParameters.Length == 0)
                return $"{ArgumentType.ToDescriptionString()}";

            // Join the arguments together and return them wrapped in parens
            string OutputString = $"{ArgumentType.ToDescriptionString()}";
            OutputString += $"({string.Join(", ", this.ArgumentParameters.Select(ArgValue => $"\"{ArgValue}\""))}";
            return OutputString;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a FulcrumStartupAction
        /// </summary>
        /// <param name="ArgType">The type of action being invoked</param>
        /// <param name="ArgParameters">Parameters passed along with the command</param>
        public FulcrumStartupAction(FulcrumCommandLine.StartupArguments ArgType, params string[] ArgParameters)
        {
            // Store object properties and exit out 
            this.ArgumentType = ArgType;
            this.ArgumentParameters = ArgParameters;
        }
    }
}
