using System.Windows.Media;

namespace FulcrumInjector.FulcrumViewSupport.AppStyleSupport
{
    public static class CustomColorShader
    {
        /// <summary>
        /// Color shader.
        /// </summary>
        /// <param name="InputColor">Color to shade from</param>
        /// <param name="Comparison">Color to shade into</param>
        /// <param name="ChangeBy">FLoat value to change the color by.</param>
        /// <returns>String of hex about the color made.</returns>
        public static string GenerateShadeString(Color InputColor, Color Comparison, float ChangeBy)
        {
            var NewColor = GetShade(InputColor, Comparison, ChangeBy);
            return GenerateColorString(NewColor);
        }
        /// <summary>
        /// Color shader.
        /// </summary>
        /// <param name="InputColor">Color to shade from</param>
        /// <param name="Comparison">Color to shade into</param>
        /// <param name="ChangeBy">FLoat value to change the color by.</param>
        /// <returns>Color object of the shaded result.</returns>
        public static Color GenerateShadeColor(Color InputColor, Color Comparison, float ChangeBy)
        {
            return GetShade(InputColor, Comparison, ChangeBy);
        }
        /// <summary>
        /// Color shader.
        /// </summary>
        /// <param name="InputColor">Color to shade from</param>
        /// <param name="Comparison">Color to shade into</param>
        /// <param name="ChangeBy">FLoat value to change the color by.</param>
        /// <returns>Color object of the shaded result.</returns>
        public static SolidColorBrush GenerateShadeColorBrush(Color InputColor, Color Comparison, float ChangeBy)
        {
            return new SolidColorBrush(GetShade(InputColor, Comparison, ChangeBy));
        }
        /// <summary>
        /// Shade of color generation
        /// </summary>
        /// <param name="InputColor">Color to shade</param>
        /// <param name="CompareColor">Shade into</param>
        /// <param name="amount">Amount to shade</param>
        /// <returns></returns>
        private static Color GetShade(this Color InputColor, Color CompareColor, float amount)
        {
            // Start with RBG values.
            float sr = InputColor.R, sg = InputColor.G, sb = InputColor.B;

            // End colors in comparison to the compare color.
            float er = CompareColor.R, eg = CompareColor.G, eb = CompareColor.B;

            // Modify the colors CompareColor get the difference
            byte r = (byte)sr.GetShadeFloats(er, amount),
                g = (byte)sg.GetShadeFloats(eg, amount),
                b = (byte)sb.GetShadeFloats(eb, amount);

            // Return the new InputColor
            return Color.FromRgb(r, g, b);
        }
        private static float GetShadeFloats(this float start, float end, float amount)
        {
            float difference = end - start;
            float adjusted = difference * amount;
            return start + adjusted;
        }
        private static string GenerateColorString(Color BaseColor)
        {
            string AlphaHex = BaseColor.A.ToString("X2");
            string RedHex = BaseColor.R.ToString("X2");
            string GreenHex = BaseColor.G.ToString("X2");
            string BlueHex = BaseColor.B.ToString("X2");

            var HexString = $"#{AlphaHex}{RedHex}{GreenHex}{BlueHex}";

            return HexString;
        }
    }
}