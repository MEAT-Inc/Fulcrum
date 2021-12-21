using System;
using System.Globalization;

namespace FulcrumInjector.FulcrumViewSupport.DataConverters
{
    public static class CustomColorConverter
    {
        /// <summary>
        /// Convert Media Color (WPF) to Drawing Color (WinForm)
        /// </summary>
        /// <param name="mediaColor"></param>
        /// <returns></returns>
        public static System.Drawing.Color ToDrawingColor(this System.Windows.Media.Color mediaColor)
        {
            return System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
        }
        /// <summary>
        /// Convert Drawing Color (WPF) to Media Color (WinForm)
        /// </summary>
        /// <param name="drawingColor"></param>
        /// <returns></returns>
        public static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color drawingColor)
        {
            return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }
        /// <summary>
        /// Generates a color from an input string.
        /// </summary>
        /// <param name="colorString">The string to convert out</param>
        /// <returns>A color from the string given</returns>
        public static System.Drawing.Color FromStringToDrawing(this string colorString)
        {
            // Convert the color
            int ColorUint = int.Parse(colorString.Replace("0x", String.Empty), NumberStyles.HexNumber);
            var OutputColor = System.Drawing.Color.FromArgb(ColorUint);

            // Return the color
            return OutputColor;
        }
        /// <summary>
        /// Generates a color from an input string.
        /// </summary>
        /// <param name="colorString">The string to convert out</param>
        /// <returns>A color from the string given</returns>
        public static System.Windows.Media.Color FromStringToMedia(this string colorString)
        {
            // Convert the color
            var ColorObj = colorString.FromStringToDrawing();
            var OutputColor = ColorObj.ToMediaColor();

            // Return the color
            return OutputColor;
        }

        // ---------------------------------------------------------------------------------------------


        /// <summary>
        /// Convert Drawing Color a String
        /// </summary>
        /// <param name="InputColor">Color to convert</param>
        /// <returns>string converted color value</returns>
        public static string HexConverter(System.Drawing.Color InputColor)
        {
            return (InputColor.R.ToString("X2") + InputColor.G.ToString("X2") + InputColor.B.ToString("X2")).ToUpper();
        }
        /// <summary>
        /// Convert Media Color a String
        /// </summary>
        /// <param name="InputColor">Color to convert</param>
        /// <returns>string converted color value</returns>
        public static string RGBConverter(System.Drawing.Color InputColor)
        {
            return (InputColor.R.ToString() + "," + InputColor.G.ToString() + "," + InputColor.B.ToString()).ToUpper();
        }
    }
}
