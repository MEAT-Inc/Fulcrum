using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ControlzEx.Theming;
using FulcrumInjector.FulcrumViewSupport.DataContentHelpers;
using FulcrumInjector.FulcrumViewSupport.FulcrumJson.JsonHelpers;
using FulcrumInjector.FulcrumViewSupport.FulcrumStyles.AppStyleModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumStyles
{
    /// <summary>
    /// Class reserved to configuring theme object values on the application
    /// </summary>
    internal class AppThemeConfiguration
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Backing fields used to help setup theme configurations and logging
        private readonly SharpLogger _themeLogger;        // Logging object used to help keep track of them updates
        private AppStyleModel _currentAppStyleModel;      // Currently applied theme object for the injector instance

        #endregion //Fields

        #region Properties

        // Public facing collections of themes we can pick from and the currently applied theme value
        public AppStyleModel[] PresetThemes { get; private set; }
        public AppStyleModel CurrentAppStyleModel
        {
            get => _currentAppStyleModel;
            set
            {
                // Set new theme value here and exit out
                _currentAppStyleModel = value;
                ApplyAppTheme(value);
            }
        }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds preset theme objects and appends all values into the app theme config.
        /// </summary>
        public AppThemeConfiguration()
        {
            // Store theme values.
            this._themeLogger = new SharpLogger(LoggerActions.FileLogger);
            this._themeLogger.WriteLog("SETTING UP NEW THEME CONTENT SETS FOR APPLICATION NOW...", LogType.InfoLog);

            // List of colors to setup as themes
            this.PresetThemes ??= Array.Empty<AppStyleModel>();
            string AppName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.AppInstanceName");
            var ThemeDefines = new List<(string, string, string, StyleType)>() { };

            // Try and add forced user themes here.
            var PulledUserThemes = ValueLoaders.GetConfigValue<dynamic[]>("FulcrumInjectorAppThemes.UserEnteredThemes");
            if (PulledUserThemes.Length != 0)
            {
                this._themeLogger.WriteLog($"LOCATED A TOTAL OF {PulledUserThemes.Length} USER DEFINED THEMES. IMPORTING THEM NOW...", LogType.InfoLog);
                foreach (var NewThemeObject in PulledUserThemes)
                {
                    // Store new values and build themes
                    string ThemeBaseColor = NewThemeObject.BaseColor;
                    string ThemeSecondaryColor = NewThemeObject.SecondaryColor;
                    string ThemeName = $"{AppName} - {NewThemeObject.ThemeName}";

                    // Make sure this is a valid theme object
                    if (NewThemeObject.ThemeName == null || ThemeBaseColor == null || ThemeSecondaryColor == null)
                    {
                        // Log failed to build theme and continue.
                        this._themeLogger.WriteLog("FAILED TO BUILD NEW THEME! ONE OR MORE ENTRIES WAS FOUND TO BE NULL!", LogType.ErrorLog);
                        this._themeLogger.WriteLog($"VALUES LOCATED:", LogType.ErrorLog);
                        this._themeLogger.WriteLog($"Theme Name:      {NewThemeObject.ThemeName ?? "NULL"}", LogType.ErrorLog);
                        this._themeLogger.WriteLog($"Base Color:      {ThemeBaseColor ?? "NULL"}", LogType.ErrorLog);
                        this._themeLogger.WriteLog($"Secondary Color: {ThemeSecondaryColor ?? "NULL"}", LogType.ErrorLog);
                        continue;
                    }

                    // Add theme object
                    StyleType typeOfStyle = NewThemeObject.IsDarkTheme == true ? StyleType.DARK_COLORS : StyleType.LIGHT_COLORS;
                    ThemeDefines.Add(new(ThemeName, ThemeBaseColor, ThemeSecondaryColor, typeOfStyle));
                    this._themeLogger.WriteLog($"BUILT NEW THEME NAMED {ThemeName} FOR APP {AppName} CORRECTLY!", LogType.InfoLog);
                }
            }

            // Add these themes into the whole setup now.
            this.PresetThemes = ThemeDefines
                .SelectMany(ThemeObj =>
                {
                    // Build themes here and store a dark/light configuration for each one of them
                    var DarkTheme = GenerateAppTheme(ThemeObj.Item1, ThemeObj.Item2, ThemeObj.Item3, StyleType.DARK_COLORS);
                    var LightTheme = GenerateAppTheme(ThemeObj.Item1, ThemeObj.Item2, ThemeObj.Item3, StyleType.LIGHT_COLORS);
                    this._themeLogger.WriteLog($"STORED NEW THEME TITLED {ThemeObj.Item1} TO THE THEME MANGER OK!");

                    // Return our theme objects
                    return new[] { DarkTheme, LightTheme };
                })
                .ToArray();

            // Log done and ready.
            ValueSetters.SetValue("FulcrumInjectorAppThemes.GeneratedAppPresets", this.PresetThemes);
            this._themeLogger.WriteLog("STORED PRESET VALUES ON THE MAIN INSTANCE THEME OBJECT OK!", LogType.InfoLog);
            this._themeLogger.WriteLog("READY TO RUN CUSTOM THEME CONFIGURATIONS FROM HERE ON.", LogType.InfoLog);
        }

        /// <summary>
        /// Adds a new theme object to this settings model
        /// </summary>
        /// <param name="ThemeName">Name of theme</param>
        /// <param name="BaseColor">Main color</param>
        /// <param name="ShowcaseColor">Background Color</param>
        public AppStyleModel GenerateAppTheme(string ThemeName, string BaseColor, string ShowcaseColor, StyleType typeOfStyle)
        {
            // Get Color Uints
            int BaseUint = int.Parse(BaseColor.Replace("0x", string.Empty), NumberStyles.HexNumber);
            int ShowcaseUint = int.Parse(ShowcaseColor.Replace("0x", string.Empty), NumberStyles.HexNumber);

            // Convert the Uints into colors.
            return GenerateAppTheme(ThemeName, System.Drawing.Color.FromArgb(BaseUint), System.Drawing.Color.FromArgb(ShowcaseUint), typeOfStyle);
        }
        /// <summary>
        /// Adds a new theme object to this settings model
        /// </summary>
        /// <param name="ThemeName">Name of theme</param>
        /// <param name="BaseColor">Main color</param>
        /// <param name="ShowcaseColor">Background Color</param>
        public AppStyleModel GenerateAppTheme(string ThemeName, System.Drawing.Color BaseColor, System.Drawing.Color ShowcaseColor, StyleType typeOfStyle)
        {
            // Build the new theme here
            switch (typeOfStyle)
            {
                // Generate the custom schemes here.
                case StyleType.DARK_COLORS:
                    string DarkBaseHexString = CustomColorConverter.HexConverter(BaseColor);
                    string DarkShowcaseHexString = CustomColorConverter.HexConverter(ShowcaseColor);
                    var DarkNewTheme = new AppStyleModel(ThemeName, DarkBaseHexString, DarkShowcaseHexString, typeOfStyle);

                    // Return the new theme object if in the list.
                    if (this.PresetThemes.ToList().TrueForAll(ThemeObj => ThemeObj.ThemeName == DarkNewTheme.ThemeName))
                        return DarkNewTheme;

                    this.PresetThemes = this.PresetThemes.Append(DarkNewTheme).ToArray();
                    ThemeManager.Current.AddTheme(DarkNewTheme.MahThemeObject);
                    return DarkNewTheme;

                // Light color theme
                case StyleType.LIGHT_COLORS:
                    string LightBaseHexString = CustomColorConverter.HexConverter(BaseColor);
                    string LightShowcaseHexString = CustomColorConverter.HexConverter(ShowcaseColor);
                    var LightNewTheme = new AppStyleModel(ThemeName, LightBaseHexString, LightShowcaseHexString, typeOfStyle);

                    // Return the new theme object if in the list.
                    if (this.PresetThemes.ToList().TrueForAll(ThemeObj => ThemeObj.ThemeName == LightNewTheme.ThemeName))
                        return LightNewTheme;

                    this.PresetThemes = this.PresetThemes.Append(LightNewTheme).ToArray();
                    ThemeManager.Current.AddTheme(LightNewTheme.MahThemeObject);
                    return LightNewTheme;
            }

            // Throw an ex if we get he1re.
            throw new InvalidOperationException("THEME COULD NOT BE ADDED SINCE COLOR PRESET WAS INVALID!");
        }

        /// <summary>
        /// Sets the application theme based ona theme object.
        /// </summary>
        /// <param name="styleModelToSet">Object used for resources to the theme</param>
        public void ApplyAppTheme(AppStyleModel styleModelToSet)
        {
            // Get Color Values and set.
            var PrimaryColor = styleModelToSet.PrimaryColor.ToMediaColor();
            var SecondaryColor = styleModelToSet.PrimaryColor.ToMediaColor();

            // Get the resource dictionary
            var CurrentMerged = Application.Current.Resources.MergedDictionaries;
            var ColorResources = CurrentMerged.FirstOrDefault(Dict =>
                Dict.Source.ToString().Contains("AppColorTheme")
            );

            // Set Primary and Secondary Colors
            ColorResources["PrimaryColor"] = new SolidColorBrush(CustomColorShader.GenerateShadeColor(PrimaryColor, Colors.Black, 0));
            ColorResources["SecondaryColor"] = new SolidColorBrush(CustomColorShader.GenerateShadeColor(SecondaryColor, Colors.Black, 0));
            ColorResources["TextColorBase"] = styleModelToSet.TypeOfStyle switch
            {
                // Set the text base color
                StyleType.DARK_COLORS => new SolidColorBrush(Colors.White),
                StyleType.LIGHT_COLORS => new SolidColorBrush(Colors.Black),
                _ => ColorResources["TextColorBase"]
            };

            // Setup All The Values
            foreach (var ColorKey in ColorResources.Keys)
            {
                // If we have something we can't change move on.
                if (!ColorKey.ToString().Contains("_")) { continue; }

                // Split into three parts. Lighter/darker, base or secondary, and pct.
                string[] SplitName = ColorKey.ToString().Split('_');
                bool IsDarker = SplitName[1].ToUpper() == "DARKER";
                float PctToChange = (float)((double)int.Parse(SplitName[2]) / 100.0);

                // Create our colors now.
                if (SplitName[0].Contains("PrimaryColor") && IsDarker)
                    ColorResources[ColorKey] = new SolidColorBrush(CustomColorShader.GenerateShadeColor(PrimaryColor, Colors.Black, PctToChange));
                if (SplitName[0].Contains("PrimaryColor") && !IsDarker)
                    ColorResources[ColorKey] = new SolidColorBrush(CustomColorShader.GenerateShadeColor(PrimaryColor, Colors.White, PctToChange));
                if (SplitName[0].Contains("SecondaryColor") && IsDarker)
                    ColorResources[ColorKey] = new SolidColorBrush(CustomColorShader.GenerateShadeColor(SecondaryColor, Colors.Black, PctToChange));
                if (SplitName[0].Contains("SecondaryColor") && !IsDarker)
                    ColorResources[ColorKey] = new SolidColorBrush(CustomColorShader.GenerateShadeColor(SecondaryColor, Colors.White, PctToChange));
            }

            // Set the resource back here.
            Application.Current.Resources["AppColorTheme"] = ColorResources;
            ValueSetters.SetValue("FulcrumInjectorAppThemes.AppliedAppTheme", styleModelToSet);
            ThemeManager.Current.ChangeTheme(Application.Current, RuntimeThemeGenerator.Current.GenerateRuntimeTheme(
                    (styleModelToSet.TypeOfStyle == StyleType.DARK_COLORS ? "Dark" : "Light"),
                    styleModelToSet.PrimaryColor.ToMediaColor()
            ));

            // Add into the list of themes if needed
            if (PresetThemes.All(ThemeObj => ThemeObj.ThemeName != styleModelToSet.ThemeName)) 
                PresetThemes = PresetThemes.Append(_currentAppStyleModel).ToArray();

            // Log theme applied without issues and exit out
            this._themeLogger.WriteLog("APPLIED NEW THEME OUTPUT FOR THIS APPLICATION OK!", LogType.InfoLog);
        }
    }
}