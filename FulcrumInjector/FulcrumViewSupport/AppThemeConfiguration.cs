using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ControlzEx.Theming;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewSupport.DataConverters;
using FulcrumInjector.FulcrumViewSupport.StyleModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Class reserved to configuring theme object values on the application
    /// </summary>
    public class AppThemeConfiguration
    {
        // Logger Object
        private static SubServiceLogger ThemeLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("AppThemeLogger")) ?? new SubServiceLogger("AppThemeLogger");

        // Themes that exist regardless of the current operations
        public AppTheme[] PresetThemes { get; private set; }

        // The Current theme in use
        private AppTheme _currentAppTheme;
        public AppTheme CurrentAppTheme
        {
            get => _currentAppTheme;
            set
            {
                // Set new theme value here.
                _currentAppTheme = value;
                AppColorSetter.SetAppColorScheme(value);
                ThemeLogger.WriteLog("SETTING NEW THEME VALUE BASED ON CURRENT SET VALUE NOW!", LogType.InfoLog);

                // Add into the list of themes if needed
                if (PresetThemes.All(ThemeObj => ThemeObj.ThemeName != value.ThemeName))
                    PresetThemes = PresetThemes.Append(_currentAppTheme).ToArray();

                // Write it out here
                ValueSetters.SetValue("FulcrumInjectorAppThemes.AppliedAppTheme", value);
                ThemeLogger.WriteLog("APPLIED NEW THEME OUTPUT FOR THIS APPLICATION OK!", LogType.InfoLog);
            }
        }

        // ---------------------------------------------------------------------------------------------       

        /// <summary>
        /// Builds preset theme objects and appends all values into the app theme config.
        /// </summary>
        public AppThemeConfiguration()
        {
            // Store theme values.
            ThemeLogger.WriteLog("SETTING UP NEW THEME CONTENT SETS FOR APPLICATION NOW...", LogType.InfoLog);

            // List of colors to setup as themes
            this.PresetThemes ??= Array.Empty<AppTheme>();
            string AppName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorSettings.AppInstanceName");
            var ThemeDefines = new List<(string, string, string, ThemeType)>() { };

            // Try and add forced user themes here.
            var PulledUserThemes = ValueLoaders.GetConfigValue<dynamic[]>("FulcrumInjectorAppThemes.UserEnteredThemes");
            if (PulledUserThemes.Length != 0)
            {
                ThemeLogger.WriteLog($"LOCATED A TOTAL OF {PulledUserThemes.Length} USER DEFINED THEMES. IMPORTING THEM NOW...", LogType.InfoLog);
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
                        ThemeLogger.WriteLog("FAILED TO BUILD NEW THEME! ONE OR MORE ENTRIES WAS FOUND TO BE NULL!", LogType.ErrorLog);
                        ThemeLogger.WriteLog($"VALUES LOCATED:", LogType.ErrorLog);
                        ThemeLogger.WriteLog($"Theme Name:      {NewThemeObject.ThemeName ?? "NULL"}", LogType.ErrorLog);
                        ThemeLogger.WriteLog($"Base Color:      {ThemeBaseColor ?? "NULL"}", LogType.ErrorLog);
                        ThemeLogger.WriteLog($"Secondary Color: {ThemeSecondaryColor ?? "NULL"}", LogType.ErrorLog);
                        continue;
                    }

                    // Add theme object
                    ThemeType TypeOfTheme = NewThemeObject.IsDarkTheme == true ? ThemeType.DARK_COLORS : ThemeType.LIGHT_COLORS;
                    ThemeDefines.Add(new(ThemeName, ThemeBaseColor, ThemeSecondaryColor, TypeOfTheme));
                    ThemeLogger.WriteLog($"BUILT NEW THEME NAMED {ThemeName} FOR APP {AppName} CORRECTLY!", LogType.InfoLog);
                }
            }

            // Log information and the themes built.
            ThemeLogger.WriteLog("BUILT NEW PRESETS OK! STORING THEM INTO OUR CONFIG OBJECT NOW...");
            ThemeLogger.WriteLog($"THEMES BUILT ARE BEING SHOWN BELOW:\n{string.Join("\n", this.PresetThemes.Select(ThemeObj => ThemeObj.ToString()))}");

            // Add these themes into the whole setup now.
            this.PresetThemes = ThemeDefines
                .SelectMany(ThemeObj =>
                {
                    // Build themes here and store a dark/light configuration for each one of them
                    var DarkTheme = GenerateNewTheme(ThemeObj.Item1, ThemeObj.Item2, ThemeObj.Item3, ThemeType.DARK_COLORS);
                    var LightTheme = GenerateNewTheme(ThemeObj.Item1, ThemeObj.Item2, ThemeObj.Item3, ThemeType.LIGHT_COLORS);
                    ThemeLogger.WriteLog($"STORED NEW THEME TITLED {ThemeObj.Item1} TO THE THEME MANGER OK!");

                    // Return our theme objects
                    return new[] { DarkTheme, LightTheme };
                })
                .ToArray();

            // Log done and ready.
            ValueSetters.SetValue("FulcrumInjectorAppThemes.GeneratedAppPresets", this.PresetThemes);
            ThemeLogger.WriteLog("STORED PRESET VALUES ON THE MAIN INSTANCE THEME OBJECT OK!", LogType.InfoLog);
            ThemeLogger.WriteLog("READY TO RUN CUSTOM THEME CONFIGURATIONS FROM HERE ON.", LogType.InfoLog);
        }


        /// <summary>
        /// Adds a new theme object to this settings model
        /// </summary>
        /// <param name="ThemeName">Name of theme</param>
        /// <param name="BaseColor">Main color</param>
        /// <param name="ShowcaseColor">Background Color</param>
        public AppTheme GenerateNewTheme(string ThemeName, string BaseColor, string ShowcaseColor, ThemeType TypeOfTheme)
        {
            // Get Color Uints
            int BaseUint = int.Parse(BaseColor.Replace("0x", string.Empty), NumberStyles.HexNumber);
            int ShowcaseUint = int.Parse(ShowcaseColor.Replace("0x", string.Empty), NumberStyles.HexNumber);

            // Convert the Uints into colors.
            return GenerateNewTheme(ThemeName, System.Drawing.Color.FromArgb(BaseUint), System.Drawing.Color.FromArgb(ShowcaseUint), TypeOfTheme);
        }
        /// <summary>
        /// Adds a new theme object to this settings model
        /// </summary>
        /// <param name="ThemeName">Name of theme</param>
        /// <param name="BaseColor">Main color</param>
        /// <param name="ShowcaseColor">Background Color</param>
        public AppTheme GenerateNewTheme(string ThemeName, System.Drawing.Color BaseColor, System.Drawing.Color ShowcaseColor, ThemeType TypeOfTheme)
        {
            // Build the new theme here
            switch (TypeOfTheme)
            {
                // Generate the custom schemes here.
                case ThemeType.DARK_COLORS:
                    string DarkBaseHexString = CustomColorConverter.HexConverter(BaseColor);
                    string DarkShowcaseHexString = CustomColorConverter.HexConverter(ShowcaseColor);
                    var DarkNewTheme = new AppTheme(ThemeName, DarkBaseHexString, DarkShowcaseHexString, TypeOfTheme);

                    // Return the new theme object if in the list.
                    if (this.PresetThemes.ToList().TrueForAll(ThemeObj => ThemeObj.ThemeName == DarkNewTheme.ThemeName))
                        return DarkNewTheme;

                    this.PresetThemes = this.PresetThemes.Append(DarkNewTheme).ToArray();
                    ThemeManager.Current.AddTheme(DarkNewTheme.MahThemeObject);
                    return DarkNewTheme;

                // Light color theme
                case ThemeType.LIGHT_COLORS:
                    string LightBaseHexString = CustomColorConverter.HexConverter(BaseColor);
                    string LightShowcaseHexString = CustomColorConverter.HexConverter(ShowcaseColor);
                    var LightNewTheme = new AppTheme(ThemeName, LightBaseHexString, LightShowcaseHexString, TypeOfTheme);

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
    }
}