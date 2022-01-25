using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using FulcrumInjector.FulcrumLogic.PassThruExpressions;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using NLog.Targets;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.ExtensionClasses
{
    /// <summary>
    /// Extensions for parsing out commands into new types of output for PT Regex Classes
    /// </summary>
    public static class CommandTypeExtensions
    {
        /// <summary>
        /// Converts an input Regex command type enum into a type output
        /// </summary>
        /// <param name="InputType">Enum Regex Typ</param>
        /// <returns>Type of regex for the class output</returns>
        public static PassThruExpression ToRegexClass(this PassThruCommandType InputType, string[] InputLines)
        {
            // Pull the description string and get type of regex class.
            return ToRegexClass(InputType, string.Join("\n", InputLines));
        }
        /// <summary>
        /// Converts an input Regex command type enum into a type output
        /// </summary>
        /// <param name="InputType">Enum Regex Typ</param>
        /// <returns>Type of regex for the class output</returns>
        public static PassThruExpression ToRegexClass(this PassThruCommandType InputType, string InputLines)
        {
            // Pull the description string and get type of regex class.
            string ClassType = $"{typeof(PassThruExpression).Namespace}.{InputType.ToDescriptionString()}";
            if (Type.GetType(ClassType) == null) return new PassThruExpression(InputLines, InputType);

            // Find our output type value here.
            Type OutputType = Type.GetType(ClassType);
            var RegexConstructor = OutputType.GetConstructor(new[] { typeof(string) });
            return (PassThruExpression)RegexConstructor.Invoke(new[] { InputLines });
        }

        // -----------------------------------------------------------------------------------------------------------------

        
    }
}
