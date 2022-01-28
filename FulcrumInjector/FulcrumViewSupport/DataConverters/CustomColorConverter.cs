using System;
using System.Globalization;

namespace FulcrumInjector.FulcrumViewSupport.DataConverters
{
    /// <summary>
    /// Helper methods for converting our various types of colors.
    /// </summary>
    public static class CustomColorConverter
    {
        /// <summary>
        /// Convert Media Color (WPF) to Drawing Color (WinForm)
        /// </summary>
        /// <param name="MediaColor"></param>
        /// <returns></returns>
        public static System.Drawing.Color ToDrawingColor(this System.Windows.Media.Color MediaColor)
        {
            return System.Drawing.Color.FromArgb(MediaColor.A, MediaColor.R, MediaColor.G, MediaColor.B);
        }
        /// <summary>
        /// Convert Drawing Color (WPF) to Media Color (WinForm)
        /// </summary>
        /// <param name="DrawingColor"></param>
        /// <returns></returns>
        public static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color DrawingColor)
        {
            return System.Windows.Media.Color.FromArgb(DrawingColor.A, DrawingColor.R, DrawingColor.G, DrawingColor.B);
        }
        /// <summary>
        /// Generates a color from an input string.
        /// </summary>
        /// <param name="ColorString">The string to convert out</param>
        /// <returns>A color from the string given</returns>
        public static System.Drawing.Color ToDrawingColor(this string ColorString)
        {
            // Convert the color
            int ColorUint = int.Parse(ColorString.Replace("0x", String.Empty), NumberStyles.HexNumber);
            var OutputColor = System.Drawing.Color.FromArgb(ColorUint);

            // Return the color
            return OutputColor;
        }
        /// <summary>
        /// Generates a color from an input string.
        /// </summary>
        /// <param name="ColorString">The string to convert out</param>
        /// <returns>A color from the string given</returns>
        public static System.Windows.Media.Color ToMediaColor(this string ColorString)
        {
            // Convert the color
            var ColorObj = ColorString.ToDrawingColor();
            var OutputColor = ColorObj.ToMediaColor();

            // Return the color
            return OutputColor;
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts an input string into a media brush
        /// </summary>
        /// <param name="ColorString"></param>
        /// <returns></returns>
        public static System.Windows.Media.Brush ToMediaBrush(this string ColorString)
        {
            // Convert and return the color value here.
            var MediaColor = ColorString.ToMediaColor();
            var BrushConverter = new System.Windows.Media.BrushConverter();
            return (System.Windows.Media.Brush)BrushConverter.ConvertFromString(MediaColor.ToString());
        }
        /// <summary>
        /// Converts an input string into a media brush
        /// </summary>
        /// <param name="ColorString"></param>
        /// <returns></returns>
        public static System.Drawing.Brush ToColorBrush(this string ColorString)
        {
            // Convert and return the color value here.
            var DrawingColor = ColorString.ToDrawingColor();
            return new System.Drawing.SolidBrush(DrawingColor);
        }

        // --------------------------------------------------------------------------------------------------------------------------

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
