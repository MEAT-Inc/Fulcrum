using System;

namespace FulcrumInjector.AppLogic.ExtensionClasses
{
    /// <summary>
    /// Class used to convert a long value into a string.
    /// </summary>
    public static class SizeFormat
    {
        // File format size values.
        private static readonly string[] _sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

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
            return $"{(InputSize < 0 ? "-" : string.Empty, NormalizedSize)} {_sizeSuffixes[IUint]}";
        }
    }
}
