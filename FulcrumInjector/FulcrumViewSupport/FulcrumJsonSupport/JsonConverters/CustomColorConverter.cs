using System;
using System.Globalization;
// Static using statements for colors and brushes
using DrawingColor = System.Drawing.Color;
using DrawingBrush = System.Drawing.Brush;
using MediaColor = System.Windows.Media.Color;
using MediaBrush = System.Windows.Media.Brush;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters
{
    /// <summary>
    /// Helper methods for converting our various types of colors.
    /// </summary>
    internal static class CustomColorConverter
    {
        /// <summary>
        /// Convert Media Color (WPF) to Drawing Color (WinForm)
        /// </summary>
        /// <param name="MediaColor"></param>
        /// <returns></returns>
        public static DrawingColor ToDrawingColor(this MediaColor MediaColor)
        {
            return DrawingColor.FromArgb(MediaColor.A, MediaColor.R, MediaColor.G, MediaColor.B);
        }
        /// <summary>
        /// Convert Drawing Color (WPF) to Media Color (WinForm)
        /// </summary>
        /// <param name="DrawingColor"></param>
        /// <returns></returns>
        public static MediaColor ToMediaColor(this DrawingColor DrawingColor)
        {
            return MediaColor.FromArgb(DrawingColor.A, DrawingColor.R, DrawingColor.G, DrawingColor.B);
        }
        /// <summary>
        /// Generates a color from an input string.
        /// </summary>
        /// <param name="ColorString">The string to convert out</param>
        /// <returns>A color from the string given</returns>
        public static DrawingColor ToDrawingColor(this string ColorString)
        {
            // Convert the color
            int ColorUint = int.Parse(ColorString.Replace("0x", String.Empty), NumberStyles.HexNumber);
            var OutputColor = DrawingColor.FromArgb(ColorUint);

            // Return the color
            return OutputColor;
        }
        /// <summary>
        /// Generates a color from an input string.
        /// </summary>
        /// <param name="ColorString">The string to convert out</param>
        /// <returns>A color from the string given</returns>
        public static MediaColor ToMediaColor(this string ColorString)
        {
            // Convert the color
            var ColorObj = ColorString.ToDrawingColor();
            var OutputColor = ColorObj.ToMediaColor();

            // Return the color
            return OutputColor;
        }

        /// <summary>
        /// Converts an input string into a media brush
        /// </summary>
        /// <param name="ColorString"></param>
        /// <returns></returns>
        public static MediaBrush ToMediaBrush(this string ColorString)
        {
            // Build new color from converted values to avoid parse string input issues.
            var MediaColor = ColorString.ToMediaColor();
            var OutputColor = MediaColor.FromRgb(
                MediaColor.R,   // Red Channel
                MediaColor.G,   // Green Channel
                MediaColor.B    // Blue Channel
            );

            // Cast color into a brush object and return output.
            return new System.Windows.Media.SolidColorBrush(OutputColor);
        }
        /// <summary>
        /// Converts an input string into a media brush
        /// </summary>
        /// <param name="ColorString"></param>
        /// <returns></returns>
        public static DrawingBrush ToColorBrush(this string ColorString)
        {
            // Convert and return the color value here.
            var DrawingColor = ColorString.ToDrawingColor();
            return new System.Drawing.SolidBrush(DrawingColor);
        }
        
        /// <summary>
        /// Convert Drawing Color a String
        /// </summary>
        /// <param name="InputColor">Color to convert</param>
        /// <returns>string converted color value</returns>
        public static string ToHexString(this DrawingColor InputColor)
        {
            return (InputColor.R.ToString("X2") + InputColor.G.ToString("X2") + InputColor.B.ToString("X2")).ToUpper();
        }
        /// <summary>
        /// Convert Media Color a String
        /// </summary>
        /// <param name="InputColor">Color to convert</param>
        /// <returns>string converted color value</returns>
        public static string ToRgbString(this DrawingColor InputColor)
        {
            return (InputColor.R.ToString() + "," + InputColor.G.ToString() + "," + InputColor.B.ToString()).ToUpper();
        }
    }
}
