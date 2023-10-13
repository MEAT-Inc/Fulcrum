using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels
{
    /// <summary>
    /// Class object holding information about a parsed argument object
    /// </summary>
    internal class FulcrumStartupAction
    {
        // Public properties holding information about the requested action
        public string[] ArgumentParameters { get; set; }
        public FulcrumCommandLine.StartupArguments ArgumentType { get; set; }

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
