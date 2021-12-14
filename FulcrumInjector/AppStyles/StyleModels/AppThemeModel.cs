using System;
using System.Windows.Media;
using ControlzEx.Theming;
using FulcrumInjector.AppStyles.DataConverters;
using Newtonsoft.Json;
using Color = System.Drawing.Color;

namespace FulcrumInjector.AppStyles.StyleModels
{
    /// <summary>
    /// Type of theme used.
    /// </summary>
    public enum ThemeType
    {
        DARK_COLORS = 0,
        LIGHT_COLORS = 1,
    }

    /// <summary>
    /// Color Theme for this application
    /// </summary>
    public class AppTheme
    {
        // Base Theme Values    
        public string ThemeName;                // Name of the theme
        public ThemeType TypeOfTheme;           // Is Dark Or light or both
        public AppThemeColorSet ThemeColors;    // Custom app theme color set

        [JsonIgnore]
        public Theme MahThemeObject;            // Mahapps Theme object built from this theme.

        // Color objects.
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        public Color PrimaryColor
        {
            get => ThemeColors.GetDrawingColor(ColorTypes.PRIMARY_COLOR_BASE);
            set => ThemeColors.SetDrawingColor(ColorTypes.PRIMARY_COLOR_BASE, value);
        }
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        public Color SecondaryColor
        {
            get => ThemeColors.GetDrawingColor(ColorTypes.SECONDARY_COLOR_BASE);
            set => ThemeColors.SetDrawingColor(ColorTypes.SECONDARY_COLOR_BASE, value);
        }

        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns a string ID of this theme object split by -
        /// </summary>
        /// <returns>Theme name, and base/secondary colors</returns>
        public override string ToString()
        {
            // Build theme string. Example: 'Test Theme - 0x65646 - 0x56838' would be a base return type
            return $"{ThemeName} - {CustomColorConverter.HexConverter(PrimaryColor)} - {CustomColorConverter.HexConverter(SecondaryColor)}";
        }

        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Json Constructor for this object
        /// </summary>
        [JsonConstructor]
        public AppTheme()
        {
            // Still build the mahapp theme object
            // BuildMahTheme();
        }
        /// <summary>
        /// Setup basic theme colors
        /// </summary>
        public AppTheme(string ThemeName, string Primary, string Secondary, ThemeType TypeOfTheme)
        {
            // Set color string values
            this.TypeOfTheme = TypeOfTheme;
            this.ThemeName = ThemeName + (TypeOfTheme == ThemeType.DARK_COLORS ? " (Dark)" : " (Light)");

            // Build the new Theme object
            ThemeColors = new AppThemeColorSet(
                Primary, Secondary,
                TypeOfTheme == ThemeType.DARK_COLORS
            );

            // Generate the mah theme and the color setup
            MahThemeObject = BuildMahTheme();
        }


        /// <summary>
        /// Builds a MahApps Theme object to associate with this app theme
        /// </summary>
        public Theme BuildMahTheme()
        {
            // Build the new theme here
            switch (TypeOfTheme)
            {
                // For Dark colors
                case ThemeType.DARK_COLORS:
                    // Make a new theme
                    var CustomDarkColors = new Theme(
                        "JCanaLog." + PrimaryColor.Name.ToUpper() + ".Dark",
                        ThemeName,
                        "Dark (JCanaLog)",
                        PrimaryColor.Name.ToUpper() + " - " + SecondaryColor.Name.ToUpper(),
                        PrimaryColor.ToMediaColor(),
                        new SolidColorBrush(SecondaryColor.ToMediaColor()),
                        false,
                        false
                    );

                    // Break out and set
                    return CustomDarkColors;

                // For light colors
                case ThemeType.LIGHT_COLORS:
                    // Make a new theme.
                    var CustomLightColors = new Theme(
                        "JCanaLog." + PrimaryColor.Name.ToUpper() + "_" + SecondaryColor.Name.ToUpper() + ".Light",
                        ThemeName,
                        "Light (JCanaLog)",
                        PrimaryColor.Name.ToUpper() + " - " + SecondaryColor.Name.ToUpper(),
                        PrimaryColor.ToMediaColor(),
                        new SolidColorBrush(SecondaryColor.ToMediaColor()),
                        false,
                        false
                    );

                    // Break out and set
                    return CustomLightColors;
            }

            // Fail out if we got here.
            throw new InvalidOperationException("FAILED TO GENERATE MAH COLOR OBJECT!");
        }
    }
}
