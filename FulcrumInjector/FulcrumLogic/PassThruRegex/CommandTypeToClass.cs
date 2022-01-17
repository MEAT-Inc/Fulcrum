using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Finds a PTCommand type from the given input line set
        /// </summary>
        /// <param name="InputLines">Lines to find the PTCommand Type for.</param>
        /// <returns>The type of PTCommand regex to search with.</returns>
        public static PassThruCommandType GetTypeFromLines(string InputLines)
        {
            // Find the type of command by converting all enums to string array and searching for the type.
            string[] EnumTypesArray = (string[])Enum.GetValues(typeof(PassThruCommandType));
            if (EnumTypesArray.All(EnumString => !InputLines.Contains(EnumString))) return PassThruCommandType.NONE;

            // Find the return type here based on the first instance of a PTCommand type object on the array.
            return (PassThruCommandType)Enum.Parse(typeof(PassThruCommandType), EnumTypesArray.FirstOrDefault(EnumObj => InputLines.Contains(EnumObj)));
        }
    }
}
