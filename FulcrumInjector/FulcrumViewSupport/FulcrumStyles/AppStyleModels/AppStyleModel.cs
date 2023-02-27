using System;
using System.Windows.Media;
using ControlzEx.Theming;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters;
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

        // Public fields for our theme instance and color configuration set
        [JsonIgnore] public ThemeTypes ThemeType;             // The type of theme being built. Light or dark                           
        [JsonIgnore] public Theme MahThemeObject;             // Mahapps Theme object built from this theme.
        [JsonIgnore] public AppStyleColorSet StyleColors;     // Custom app theme color set

        #endregion //Fields

        #region Properties

        // Public name and type properties for our theme instance
        public string ThemeName { get; set; }
        
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
            return $"{this.ThemeName} - {this.PrimaryColor.ToHexString()} - {this.SecondaryColor.ToHexString()}";
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Json Constructor for this object
        /// </summary>
        [JsonConstructor]
        public AppStyleModel() { }
        /// <summary>
        /// Setup basic theme colors
        /// </summary>
        public AppStyleModel(string ThemeName, string Primary, string Secondary, ThemeTypes ThemeType)
        {
            // Set color string values
            this.ThemeType = ThemeType;
            this.ThemeName = ThemeName + (ThemeType == ThemeTypes.DARK_COLORS ? " (Dark)" : " (Light)");

            // Build the new Theme object and an app color set for this theme configuration model
            StyleColors = new AppStyleColorSet(Primary, Secondary, ThemeType == ThemeTypes.DARK_COLORS);
            MahThemeObject = BuildMahTheme();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a MahApps Theme object to associate with this app theme
        /// </summary>
        public Theme BuildMahTheme()
        {
            // Build the new theme here
            switch (this.ThemeType)
            {
                // For Dark colors
                case ThemeTypes.DARK_COLORS:
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
                case ThemeTypes.LIGHT_COLORS:
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

                // Default case will always throw an exception here
                default: throw new InvalidOperationException("FAILED TO GENERATE MAH COLOR OBJECT!");
            }
        }
    }
}
