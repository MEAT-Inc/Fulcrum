using System;
using System.Windows.Media;
using ControlzEx.Theming;
using FulcrumInjector.FulcrumViewSupport.DataContentHelpers;
using Newtonsoft.Json;
using Color = System.Drawing.Color;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumStyles.AppStyleModels
{
    /// <summary>
    /// Color Theme for this application
    /// </summary>
    internal class AppStyleModel
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Base Theme Values used to help configure color values
        public string ThemeName;                        // Name of the theme
        public StyleType TypeOfStyle;                   // Is Dark Or light or both
        public AppStyleColorSet StyleColors;            // Custom app theme color set
        [JsonIgnore] public Theme MahThemeObject;       // Mahapps Theme object built from this theme.

        #endregion //Fields

        #region Properties

        // Color objects.
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        public Color PrimaryColor
        {
            get => StyleColors.GetDrawingColor(ColorTypes.PRIMARY_COLOR_BASE);
            set => StyleColors.SetDrawingColor(ColorTypes.PRIMARY_COLOR_BASE, value);
        }
        [JsonConverter(typeof(CustomColorValueJsonConverter))]
        public Color SecondaryColor
        {
            get => StyleColors.GetDrawingColor(ColorTypes.SECONDARY_COLOR_BASE);
            set => StyleColors.SetDrawingColor(ColorTypes.SECONDARY_COLOR_BASE, value);
        }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns a string ID of this theme object split by -
        /// </summary>
        /// <returns>Theme name, and base/secondary colors</returns>
        public override string ToString()
        {
            // Build theme string. Example: 'Test Theme - 0x65646 - 0x56838' would be a base return type
            return $"{ThemeName} - {CustomColorConverter.HexConverter(PrimaryColor)} - {CustomColorConverter.HexConverter(SecondaryColor)}";
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Json Constructor for this object
        /// </summary>
        [JsonConstructor]
        public AppStyleModel()
        {
            // Still build the mahapp theme object
            // BuildMahTheme();
        }
        /// <summary>
        /// Setup basic theme colors
        /// </summary>
        public AppStyleModel(string ThemeName, string Primary, string Secondary, StyleType typeOfStyle)
        {
            // Set color string values
            this.TypeOfStyle = typeOfStyle;
            this.ThemeName = ThemeName + (typeOfStyle == StyleType.DARK_COLORS ? " (Dark)" : " (Light)");

            // Build the new Theme object
            StyleColors = new AppStyleColorSet(
                Primary, Secondary,
                typeOfStyle == StyleType.DARK_COLORS
            );

            // Generate the mah theme and the color setup
            MahThemeObject = BuildMahTheme();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a MahApps Theme object to associate with this app theme
        /// </summary>
        public Theme BuildMahTheme()
        {
            // Build the new theme here
            switch (TypeOfStyle)
            {
                // For Dark colors
                case StyleType.DARK_COLORS:
                    // Make a new theme
                    var CustomDarkColors = new Theme(
                        "FulcrumInjector." + PrimaryColor.Name.ToUpper() + ".Dark",
                        ThemeName,
                        "Dark (FulcrumInjector)",
                        PrimaryColor.Name.ToUpper() + " - " + SecondaryColor.Name.ToUpper(),
                        PrimaryColor.ToMediaColor(),
                        new SolidColorBrush(SecondaryColor.ToMediaColor()),
                        false,
                        false
                    );

                    // Break out and set
                    return CustomDarkColors;

                // For light colors
                case StyleType.LIGHT_COLORS:
                    // Make a new theme.
                    var CustomLightColors = new Theme(
                        "FulcrumInjector." + PrimaryColor.Name.ToUpper() + "_" + SecondaryColor.Name.ToUpper() + ".Light",
                        ThemeName,
                        "Light (FulcrumInjector)",
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
