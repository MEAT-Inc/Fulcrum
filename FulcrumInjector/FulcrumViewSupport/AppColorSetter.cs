using System.Linq;
using System.Windows;
using System.Windows.Media;
using ControlzEx.Theming;
using FulcrumInjector.FulcrumViewSupport.DataConverters;
using FulcrumInjector.FulcrumViewSupport.StyleModels;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Class used to help configure coloring setup for themes
    /// </summary>
    public static class AppColorSetter
    {        
        /// <summary>
        /// Sets the application theme based ona theme object.
        /// </summary>
        /// <param name="ThemeToSet">Object used for resources to the theme</param>
        public static void SetAppColorScheme(AppTheme ThemeToSet)
        {
            // Get Color Values and set.
            var PrimaryColor = ThemeToSet.PrimaryColor.ToMediaColor();
            var SecondaryColor = ThemeToSet.PrimaryColor.ToMediaColor();

            // Get the resource dictionary
            var CurrentMerged = Application.Current.Resources.MergedDictionaries;
            var ColorResources = CurrentMerged.FirstOrDefault(Dict =>
                Dict.Source.ToString().Contains("AppColorTheme")
            );

            // Set Primary and Secondary Colors
            ColorResources["PrimaryColor"] = new SolidColorBrush(CustomColorShader.GenerateShadeColor(PrimaryColor, Colors.Black, 0));
            ColorResources["SecondaryColor"] = new SolidColorBrush(CustomColorShader.GenerateShadeColor(SecondaryColor, Colors.Black, 0));
            ColorResources["TextColorBase"] = ThemeToSet.TypeOfTheme switch
            {
                // Set the text base color
                ThemeType.DARK_COLORS => new SolidColorBrush(Colors.White),
                ThemeType.LIGHT_COLORS => new SolidColorBrush(Colors.Black),
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
            SharpWrap2534_UI.SharpWrapUi.RegisterApplicationThemes(ColorResources);
            ThemeManager.Current.ChangeTheme(Application.Current, RuntimeThemeGenerator.Current.GenerateRuntimeTheme(
                    (ThemeToSet.TypeOfTheme == ThemeType.DARK_COLORS ? "Dark" : "Light"), 
                    ThemeToSet.PrimaryColor.ToMediaColor()
            ));
        }
    }
}
