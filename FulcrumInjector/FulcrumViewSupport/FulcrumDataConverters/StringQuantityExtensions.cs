using System;
using System.Linq;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Class used to convert a long value into a string and to convert a word into a plural form if needed
    /// </summary>
    internal static class StringQuantityExtensions
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // File format size values and pluralization conditions
        private static readonly string[] _esCharacters = { "s", "sh", "ch", "x", "z" };
        private static readonly string[] _sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        
        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts a byte value in long into a size with a suffix.
        /// </summary>
        /// <param name="InputSize"></param>
        /// <returns></returns>
        public static string ToFileSize(this long InputSize)
        {
            // Build format template and setup output.
            var OutputTemplate = "{0}{1:0.#} {2}";
            if (InputSize == 0) { return string.Format(OutputTemplate, null, 0, _sizeSuffixes[0]); }

            // Build decimal value size here.
            var AbsSize = Math.Abs((double)InputSize);
            var FpPower = Math.Log(AbsSize, 1000);
            var IntPower = (int)FpPower;
            var IUint = IntPower >= _sizeSuffixes.Length ? _sizeSuffixes.Length - 1 : IntPower;
            var NormalizedSize = AbsSize / Math.Pow(1000, IUint);

            // Build output value string.
            return $"{(InputSize < 0 ? "-" : string.Empty)}{NormalizedSize} {_sizeSuffixes[IUint]}";
        }

        /// <summary>
        /// Takes in a given input string and will append a 's' to the end of it if needed
        /// </summary>
        /// <param name="InputString">The string we want to control</param>
        /// <param name="ObjectCount">The number used to determine changes</param>
        /// <returns>The quantity aware string value built based on the ObjectCount</returns>
        public static string ToPluralString(this string InputString, int ObjectCount)
        {
            // If our object count is one, then return the string with the count as is
            int AbsObjectCount = Math.Abs(ObjectCount);
            if (AbsObjectCount == 1) return $"{ObjectCount} {InputString}";

            // Find if we've got an upper case string and get the last character
            bool IsUpperString = InputString.All(char.IsUpper);
            string LastCharacter = InputString[InputString.Length - 1].ToString();
            string PluralString = _esCharacters.Contains(LastCharacter) ? "es" : "s";
            if (IsUpperString) PluralString = PluralString.ToUpper();

            // Return our newly built string value using the found pluralization value
            string OutputString = $"{ObjectCount} {InputString}{PluralString}";
            return OutputString;
        }
        /// <summary>
        /// Takes in a given input string and will append a 's' to the end of it if needed
        /// </summary>
        /// <param name="InputString">The string we want to control</param>
        /// <param name="ObjectCount">The number used to determine changes</param>
        /// <param name="Precision">The number of decimal places to hold out for our value</param>
        /// <returns>The quantity aware string value built based on the ObjectCount</returns>
        public static string ToPluralString(this string InputString, double ObjectCount, int Precision = 3)
        {
            // If our object count is one, then return the string with the count as is
            double AbsObjectCount = ObjectCount < 0 ? ObjectCount * -1 : ObjectCount;
            if (AbsObjectCount == 1.0) return $"{ObjectCount} {InputString}";

            // Find if we've got an upper case string and get the last character
            bool IsUpperString = InputString.All(char.IsUpper);
            string LastCharacter = InputString[InputString.Length - 1].ToString();
            string PluralString = _esCharacters.Contains(LastCharacter) ? "es" : "s";
            if (IsUpperString) PluralString = PluralString.ToUpper();

            // Return our newly built string value using the found pluralization value
            string PrecisionFormat = "{0:F" + (Precision < 0 ? "0" : Precision) + "}";
            string OutputString = $"{string.Format(PrecisionFormat, ObjectCount)} {InputString}{PluralString}";
            return OutputString;
        }
    }
}