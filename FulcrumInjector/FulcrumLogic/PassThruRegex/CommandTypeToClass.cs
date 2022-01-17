using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;

namespace FulcrumInjector.FulcrumLogic.PassThruRegex
{
    /// <summary>
    /// Extensions for parsing out commands into new types of output for PT Regex Classes
    /// </summary>
    public static class CommandTypeToClass
    {
        /// <summary>
        /// Converts an input Regex command type enum into a type output
        /// </summary>
        /// <param name="InputType">Enum Regex Typ</param>
        /// <returns>Type of regex for the class output</returns>
        public static PassThruExpression ToRegexClass(this PassThruCommandType InputType, string InputLines) 
        {
            // Pull the description string and get type of regex class.
            string ClassType = $"FulcrumInjector.FulcrumLogic.PassThruRegex.{InputType.ToDescriptionString()}";
            return (PassThruExpression)(Type.GetType(ClassType) == null ?
                new PassThruExpression(InputLines, InputType) :
                Activator.CreateInstance(Type.GetType(ClassType)));
        }
    }
}
