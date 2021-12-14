using System;
using System.Reflection;
using System.Windows.Media;
using FulcrumInjector.AppStyles.AppStyleLogic;
using FulcrumInjector.AppStyles.DataConverters;
using Newtonsoft.Json;
using Color = System.Drawing.Color;

namespace FulcrumInjector.AppStyles.StyleModels
{
    /// <summary>
    /// Type of color options
    /// </summary>
    [Flags]
    public enum ColorTypes : uint
    {
        // Color Type Bases
        PRIMARY_COLOR =         0x001000,
        SECONDARY_COLOR =       0x002000,
        BASE_TEXT_COLOR =       0x003000,

        // Shade operation status
        DARKER_COLOR =          0x000100,
        SHADE_35_PCT =          0x000035,
        SHADE_65_PCT =          0x000065,

        // Predefined Colors
        PREDEFINED_COLOR =      0x100000,
        PRIMARY_COLOR_BASE =    0x101000,
        PRIMARY_DARKER_35 =     0x101135,
        PRIMARY_DARKER_65 =     0x101165,
        PRIMARY_LIGHTER_35 =    0x101035,
        PRIMARY_LIGHTER_65 =    0x101065,
        SECONDARY_COLOR_BASE =  0x102000,
        SECONDARY_DARKER_35 =   0x102135,
        SECONDARY_DARKER_65 =   0x102165,
        SECONDARY_LIGHTER_35 =  0x102035,
        SECONDARY_LIGHTER_65 =  0x102065,
    }

    /// <summary>
    /// Color Set for an app theme.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class AppThemeColorSet
    {
        // Main Color Values
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color PrimaryColor;       // Primary Color       
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color SecondaryColor;     // Accent/Showcase Color
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color TextColorBase;      // Main Text Color

        // Shades For Primary
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color PrimaryColor_Darker_35;      // 35% Darker Primary Color
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color PrimaryColor_Darker_65;      // 65% Darker Primary Color
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color PrimaryColor_Lighter_35;     // 35% Lighter Primary Color
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color PrimaryColor_Lighter_65;     // 65% Lighter Primary Color

        // Shades for Secondary
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color SecondaryColor_Darker_35;     // 35% Darker Secondary Color
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color SecondaryColor_Darker_65;     // 65% Darker Secondary Color
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color SecondaryColor_Lighter_35;    // 35% Lighter Secondary Color
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        private Color SecondaryColor_Lighter_65;    // 65% Lighter Secondary Color

        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Generates a new color set for a theme.
        /// </summary>
        /// <param name="PrimaryColor">Base color to use</param>
        /// <param name="SecondaryColor">Showcase color</param>
        /// <param name="TextColorBase">Main text color</param>
        public AppThemeColorSet(Color PrimaryColor, Color SecondaryColor, Color TextColorBase)
        {
            // Set the color Values
            this.SetColors(PrimaryColor, SecondaryColor, TextColorBase);
        }
        /// <summary>
        /// Generates a new color set for a theme.
        /// </summary>
        /// <param name="PrimaryColorString">Base color to use</param>
        /// <param name="SecondaryColorString">Showcase color</param>
        /// <param name="IsDark">Sets if the theme is dark or light</param>
        public AppThemeColorSet(string PrimaryColorString, string SecondaryColorString, bool IsDark)
        {
            // Setup main values
            Color TextBase = IsDark ? Color.White : Color.Black;
            Color PrimaryFromString = PrimaryColorString.FromStringToDrawing();
            Color SecondaryFromString = SecondaryColorString.FromStringToDrawing();

            // Return the new instance
            this.SetColors(PrimaryFromString, SecondaryFromString, TextBase);
        }


        /// <summary>
        /// Generates a new color set for a theme.
        /// </summary>
        /// <param name="PrimaryColor">Base color to use</param>
        /// <param name="SecondaryColor">Showcase color</param>
        /// <param name="TextColorBase">Main text color</param>
        private void SetColors(Color PrimaryColor, Color SecondaryColor, Color TextColorBase)
        {
            // Set Main Values then shade
            this.PrimaryColor = PrimaryColor;
            this.SecondaryColor = SecondaryColor;
            this.TextColorBase = TextColorBase;

            // Run the shading operations
            var FieldList = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance);
            foreach (var ClassField in FieldList)
            {
                // Make sure we can actually use this
                if (!ClassField.Name.Contains("_")) { continue; }

                // Check the type of field being requested
                string[] SplitName = ClassField.Name.Split('_');
                bool IsDarker = SplitName[1].ToUpper() == "DARKER";
                float PctToChange = (float)((double)int.Parse(SplitName[2]));

                // Write shade values for primary
                if (SplitName[0].Contains("PrimaryColor") && IsDarker && PctToChange == 35.0)
                    this.PrimaryColor_Darker_35 = GenerateConvertedShade(ColorTypes.PRIMARY_DARKER_35);
                if (SplitName[0].Contains("PrimaryColor") && IsDarker && PctToChange == 65.0)
                    this.PrimaryColor_Darker_65 = GenerateConvertedShade(ColorTypes.PRIMARY_DARKER_35);
                if (SplitName[0].Contains("PrimaryColor") && !IsDarker && PctToChange == 35.0)
                    this.PrimaryColor_Lighter_35 = GenerateConvertedShade(ColorTypes.PRIMARY_LIGHTER_35);
                if (SplitName[0].Contains("PrimaryColor") && !IsDarker && PctToChange == 65.0)
                    this.PrimaryColor_Lighter_65 = GenerateConvertedShade(ColorTypes.PRIMARY_LIGHTER_65);

                // Write shade values for secondary
                if (SplitName[0].Contains("SecondaryColor") && IsDarker && PctToChange == 35.0)
                    this.SecondaryColor_Darker_35 = GenerateConvertedShade(ColorTypes.SECONDARY_DARKER_35);
                if (SplitName[0].Contains("SecondaryColor") && IsDarker && PctToChange == 65.0)
                    this.SecondaryColor_Darker_65 = GenerateConvertedShade(ColorTypes.SECONDARY_DARKER_65);
                if (SplitName[0].Contains("SecondaryColor") && !IsDarker && PctToChange == 35.0)
                    this.SecondaryColor_Lighter_35 = GenerateConvertedShade(ColorTypes.SECONDARY_LIGHTER_35);
                if (SplitName[0].Contains("SecondaryColor") && !IsDarker && PctToChange == 65.0)
                    this.SecondaryColor_Lighter_65 = GenerateConvertedShade(ColorTypes.SECONDARY_LIGHTER_65);
            }
        }
        /// <summary>
        /// Takes an input color and shades it.
        /// </summary>
        /// <param name="Type">Type of Color To Shade</param>
        /// <param name="PctToChange">Percent to shade by</param>
        /// <returns>Color object value</returns>
        private Color GenerateConvertedShade(ColorTypes TypeOfColor)
        {
            // Make sure we're not using text color
            if (TypeOfColor.HasFlag(ColorTypes.BASE_TEXT_COLOR)) return TextColorBase;

            // Make brush then get color
            var ShadeBrush = TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR) ? Colors.Black : Colors.White;
            var ColorToShade = TypeOfColor.HasFlag(ColorTypes.PRIMARY_COLOR) ? PrimaryColor.ToMediaColor() : SecondaryColor.ToMediaColor();
            var FloatPct = TypeOfColor.HasFlag(ColorTypes.SHADE_35_PCT) ? 35.0f : 65.0f;
            var SolidColor = new SolidColorBrush(CustomColorShader.GenerateShadeColor(ColorToShade, ShadeBrush, FloatPct));

            // Return the color of the brush
            return SolidColor.Color.ToDrawingColor();
        }


        /// <summary>
        /// Returns a color object based on the params
        /// </summary>
        /// <param name="Type">Type of color (Primary/Secondary)</param>
        /// <param name="Direction">Lighter Darker or no shade</param>
        /// <param name="ShadePct">Pct of shading</param>
        /// <returns>SolidColorBrush of theme brush</returns>
        public SolidColorBrush GetBrushColor(ColorTypes TypeOfColor)
        {
            // Get a drawing color first.
            var DrawColor = GetDrawingColor(TypeOfColor);
            return new SolidColorBrush(DrawColor.ToMediaColor());
        }
        /// <summary>
        /// Returns a color object based on the params
        /// </summary>
        /// <param name="Type">Type of color (Primary/Secondary)</param>
        /// <param name="Direction">Lighter Darker or no shade</param>
        /// <param name="ShadePct">Pct of shading</param>
        /// <returns></returns>
        public Color GetDrawingColor(ColorTypes TypeOfColor)
        {
            // No Shading done so return base color
            if (TypeOfColor.HasFlag(ColorTypes.BASE_TEXT_COLOR)) return TextColorBase;

            // Check the type of color 
            switch (TypeOfColor)
            {
                // Return a Predefined Color Set
                case ColorTypes TypePredefined when TypeOfColor.HasFlag(ColorTypes.PREDEFINED_COLOR):
                    switch (TypePredefined)
                    {
                        // Check for predefined types here
                        case ColorTypes.PRIMARY_COLOR_BASE: return PrimaryColor;
                        case ColorTypes.PRIMARY_DARKER_35: return PrimaryColor_Darker_35;
                        case ColorTypes.PRIMARY_DARKER_65: return PrimaryColor_Darker_65;
                        case ColorTypes.PRIMARY_LIGHTER_35: return PrimaryColor_Lighter_35;
                        case ColorTypes.PRIMARY_LIGHTER_65: return PrimaryColor_Lighter_65;
                        case ColorTypes.SECONDARY_COLOR_BASE: return SecondaryColor;
                        case ColorTypes.SECONDARY_DARKER_35: return SecondaryColor_Darker_35;
                        case ColorTypes.SECONDARY_DARKER_65: return SecondaryColor_Darker_65;
                        case ColorTypes.SECONDARY_LIGHTER_35: return SecondaryColor_Lighter_35;
                        case ColorTypes.SECONDARY_LIGHTER_65: return SecondaryColor_Lighter_65;

                        // Default returns base color
                        default:
                            return TypePredefined.HasFlag(ColorTypes.PRIMARY_COLOR) ?
                                PrimaryColor :
                                SecondaryColor;
                    }

                // Primary Flag Combos
                case ColorTypes TypePrimary when TypeOfColor.HasFlag(ColorTypes.PRIMARY_COLOR):
                    if (TypePrimary == ColorTypes.PRIMARY_COLOR) return PrimaryColor;
                    if (TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR))
                        return TypeOfColor.HasFlag(ColorTypes.SHADE_35_PCT) ?
                            PrimaryColor_Darker_35 :
                            PrimaryColor_Darker_65;
                    else
                        return TypeOfColor.HasFlag(ColorTypes.SHADE_35_PCT) ?
                            PrimaryColor_Lighter_35 :
                            PrimaryColor_Lighter_65;

                // Secondary Flag Combos
                case ColorTypes TypeSecondary when TypeOfColor.HasFlag(ColorTypes.SECONDARY_COLOR):
                    if (TypeSecondary == ColorTypes.SECONDARY_COLOR) return SecondaryColor;
                    if (TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR))
                        return TypeOfColor.HasFlag(ColorTypes.SHADE_35_PCT) ?
                            SecondaryColor_Darker_35 :
                            SecondaryColor_Darker_65;
                    else
                        return TypeOfColor.HasFlag(ColorTypes.SHADE_35_PCT) ?
                            SecondaryColor_Lighter_35 :
                            SecondaryColor_Lighter_65;

                // Default case throws an invalid exception
                default: throw new ArgumentOutOfRangeException(nameof(TypeOfColor), TypeOfColor, null);
            }
        }


        /// <summary>
        /// Sets the value of a new theme color property
        /// </summary>
        /// <param name="TypeOfColor">Type of color to set</param>
        /// <param name="ValueToSet">Value to set in the theme</param>
        public void SetBrushColor(ColorTypes TypeOfColor, SolidColorBrush ValueToSet)
        {
            // Convert to a Color object then set value
            Color ConvertedBrush = ValueToSet.Color.ToDrawingColor();
            SetDrawingColor(TypeOfColor, ConvertedBrush);
        }
        public void SetDrawingColor(ColorTypes TypeOfColor, Color ValueToSet)
        {
            // No Shading done so return base color
            if (TypeOfColor.HasFlag(ColorTypes.BASE_TEXT_COLOR)) this.TextColorBase = ValueToSet;

            // Check the type of color 
            switch (TypeOfColor)
            {
                // Return a Predefined Color Set
                case ColorTypes TypePredefined when TypeOfColor.HasFlag(ColorTypes.PREDEFINED_COLOR):
                    switch (TypePredefined)
                    {
                        // Check for predefined types here
                        case ColorTypes.PRIMARY_COLOR_BASE: this.PrimaryColor = ValueToSet; break;
                        case ColorTypes.PRIMARY_DARKER_35: this.PrimaryColor_Darker_35 = ValueToSet; break;
                        case ColorTypes.PRIMARY_DARKER_65: this.PrimaryColor_Darker_65 = ValueToSet; break;
                        case ColorTypes.PRIMARY_LIGHTER_35: this.PrimaryColor_Lighter_35 = ValueToSet; break;
                        case ColorTypes.PRIMARY_LIGHTER_65: this.PrimaryColor_Lighter_65 = ValueToSet; break;
                        case ColorTypes.SECONDARY_COLOR_BASE: this.SecondaryColor = ValueToSet; break;
                        case ColorTypes.SECONDARY_DARKER_35: this.SecondaryColor_Darker_35 = ValueToSet; break;
                        case ColorTypes.SECONDARY_DARKER_65: this.SecondaryColor_Darker_65 = ValueToSet; break;
                        case ColorTypes.SECONDARY_LIGHTER_35: this.SecondaryColor_Lighter_35 = ValueToSet; break;
                        case ColorTypes.SECONDARY_LIGHTER_65: this.SecondaryColor_Lighter_65 = ValueToSet; break;
                    }
                    return;

                // Primary Flag Combos
                case ColorTypes TypePrimary when TypeOfColor.HasFlag(ColorTypes.PRIMARY_COLOR):
                    if (TypePrimary == ColorTypes.PRIMARY_COLOR) { PrimaryColor = ValueToSet; break; }
                    if (TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR) && TypeOfColor.HasFlag(ColorTypes.SHADE_35_PCT))
                        PrimaryColor_Darker_35 = ValueToSet;
                    if (TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR) && TypeOfColor.HasFlag(ColorTypes.SHADE_65_PCT))
                        PrimaryColor_Darker_65 = ValueToSet;
                    if (!TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR) && TypeOfColor.HasFlag(ColorTypes.SHADE_35_PCT))
                        PrimaryColor_Lighter_35 = ValueToSet;
                    if (!TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR) && TypeOfColor.HasFlag(ColorTypes.SHADE_65_PCT))
                        PrimaryColor_Lighter_65 = ValueToSet;
                    break;

                // Secondary Flag Combos
                case ColorTypes TypeSecondary when TypeOfColor.HasFlag(ColorTypes.SECONDARY_COLOR):
                    if (TypeSecondary == ColorTypes.SECONDARY_COLOR) { SecondaryColor = ValueToSet; break; }
                    if (TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR) && TypeOfColor.HasFlag(ColorTypes.SHADE_35_PCT))
                        SecondaryColor_Darker_35 = ValueToSet;
                    if (TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR) && TypeOfColor.HasFlag(ColorTypes.SHADE_65_PCT))
                        SecondaryColor_Darker_65 = ValueToSet;
                    if (!TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR) && TypeOfColor.HasFlag(ColorTypes.SHADE_35_PCT))
                        SecondaryColor_Lighter_35 = ValueToSet;
                    if (!TypeOfColor.HasFlag(ColorTypes.DARKER_COLOR) && TypeOfColor.HasFlag(ColorTypes.SHADE_65_PCT))
                        SecondaryColor_Lighter_65 = ValueToSet;
                    break;

                // Default case throws an invalid exception
                default: throw new ArgumentOutOfRangeException(nameof(TypeOfColor), TypeOfColor, null);
            }
        }
    }
}
