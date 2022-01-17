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
    public static class CommandTypeHelpers
    {
        /// <summary>
        /// Splits an input content string into a set fo PT Command objects which are split into objects.
        /// </summary>
        /// <param name="FileContents">Input file object content</param>
        /// <returns>Returns a set of file objects which contain the PT commands from a file.</returns>
        public static string[] SplitFileIntoCommands(string FileContents)
        {
            // Build regex objects to help split input content into sets.
            var TimeRegex = new Regex(@"(\d+\.\d+s)\s+(\+\+|--|!!|\*\*)\s+PT");
            var PtErrorRegex = new Regex(@"(\d+\.\d+s)\s+(\d+:[^\n]+)");

            // Make an empty array of strings and then begin splitting.
            List<string> OutputLines = new List<string>();
            for (int CharIndex = 0; CharIndex < FileContents.Length;)
            {
                // Find the first index of a time entry and the close command index.
                int TimeStartIndex = TimeRegex.Match(FileContents, CharIndex).Index;
                var ErrorCloseMatch = PtErrorRegex.Match(FileContents, TimeStartIndex);
                int ErrorCloseIndex = ErrorCloseMatch.Index + ErrorCloseMatch.Length;

                // Take the difference in End/Start as our string length value.
                string NextCommand = FileContents.Substring(TimeStartIndex, ErrorCloseIndex - TimeStartIndex);
                if (OutputLines.Contains(NextCommand)) break;

                // If it was found in the list already, then we break out of this loop to stop adding dupes.
                if (ErrorCloseIndex < CharIndex) break; 
                CharIndex = ErrorCloseIndex; OutputLines.Add(NextCommand);
            }

            // Return the built set of commands.
            return OutputLines.ToArray();
        }


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
