using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters;
using ICSharpCode.AvalonEdit;
using NLog.Targets;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters
{
    /// <summary>
    /// Base output object for formatting output log configurations
    /// </summary>
    internal class OutputFormatHelperBase : TargetWithLayout
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger object used to log exceptions thrown during formatting
        protected static SharpLogger _formatLogger;

        #endregion //Fields

        #region Properties

        // Editor object and color helper objects.
        public TextEditor OutputEditor { get; protected set; }
        public Tuple<string, Tuple<string, string>[]>[] ColorConfigurationValues { get; protected set; }
        public bool IsHighlighting => (this.OutputEditor?.TextArea.TextView.LineTransformers)!
            .Count(TransObj => TransObj.GetType().BaseType == typeof(InjectorDocFormatterBase)) != 0;

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        protected OutputFormatHelperBase()
        {
            // Spawn our logger instance if needed and move on
            _formatLogger ??= new SharpLogger(LoggerActions.UniversalLogger);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Turns on coloring output for highlighting on the output view
        /// </summary>
        public virtual void StartColorHighlighting()
        {
            // Throw not created exception
            throw new MissingMethodException($"START FORMAT METHOD WAS NOT DEFINED FOR TYPE INSTANCE {this.GetType().Name}");
        }
        /// <summary>
        /// Clears out the color helpers from the main input doc object.
        /// </summary>
        public virtual void StopColorHighlighting()
        {
            // Throw not created exception
            throw new MissingMethodException($"STOP FORMAT METHOD WAS NOT DEFINED FOR TYPE INSTANCE {this.GetType().Name}");
        }

        /// <summary>
        /// Pulls in all settings newly from the JSON Config file and stores them on this instance object.
        /// </summary>
        /// <returns></returns>
        public Tuple<string, Tuple<string, string>[]>[] BuildColorFormatValues(FulcrumSettingsEntryModel[] SettingsInputValues)
        {
            // Load value from the config file.
            _formatLogger?.WriteLog("[INJECTOR SYNTAX] ::: IMPORTING CONFIGURATION VALUES FROM SETTINGS STORE NOW...", LogType.WarnLog);
            List<Tuple<string, Tuple<string, string>[]>> OutputValues = new List<Tuple<string, Tuple<string, string>[]>>();

            // Loop our settings values here and build entries.
            _formatLogger?.WriteLog($"[INJECTOR SYNTAX] ::: COMBINED TOTAL OF {SettingsInputValues.Length} SETTING VALUES TO PARSE", LogType.TraceLog);
            foreach (var SettingEntry in SettingsInputValues)
            {
                // Get current setting value string.
                string SettingColorValues = SettingEntry.SettingValue.ToString();
                Regex FormatParse = new Regex(@"([0-9A-F]+)|\(([0-9A-F]+,\s[0-9A-F]+)\)");

                // Find matches on this string output here.
                List<Tuple<string, string>> PulledColorSetList = new List<Tuple<string, string>>();
                var MatchSetBuilt = FormatParse.Matches(SettingColorValues);

                // If Failed, log it and move on.
                if (MatchSetBuilt.Count == 0) {
                    _formatLogger?.WriteLog($"[INJECTOR SYNTAX] ::: FAILED TO PULL COLOR VALUES FOR STRING ENTRY {SettingColorValues}!", LogType.ErrorLog);
                    continue;
                }

                // Find the values and build a foreground background pair value.
                foreach (Match RegexMatch in MatchSetBuilt)
                { 
                    // Check if we have a set of values or not. If not, then we make a new pair value.
                    if (!RegexMatch.Value.Contains(",")) PulledColorSetList.Add(new(RegexMatch.Value.Trim('(', ')'), "000000"));
                    else
                    {
                        // Split content into two parts and add a value set here.
                        string[] SplitColorValues = RegexMatch.Value.Split(',');
                        PulledColorSetList.Add(new(
                            SplitColorValues[0].Trim('(', ')'),
                            SplitColorValues[1].Trim('(', ')'))
                        );
                    }
                }

                // Append our new value set onto our outputs
                OutputValues.Add(new Tuple<string, Tuple<string, string>[]>(SettingEntry.SettingName, PulledColorSetList.ToArray()));
            }

            // Return built list of tuple output here.
            _formatLogger?.WriteLog("[INJECTOR SYNTAX] ::: CONFIGURED NEW COLOR VALUE SETS OK! RETURNING BUILT OUTPUT CONTENTS NOW", LogType.InfoLog);
            this.ColorConfigurationValues = OutputValues.ToArray();
            return OutputValues.ToArray();
        }
        /// <summary>
        /// Pulls in settings values and locates a desired color format type as a set of strings.
        /// </summary>
        /// <param name="TypeOfFormatter">Type of formatter to use.</param>
        /// <returns>String set of color values or just White if none found.</returns>
        public Tuple<string, string>[] PullColorsForCommand(Type TypeOfFormatter)
        {
            // Make sure type is a doc formatter. If not return null. Then try to pull value.
            if (TypeOfFormatter.BaseType != typeof(InjectorDocFormatterBase)) {
                _formatLogger.WriteLog("CAN NOT FORMAT OUTPUT FOR INJECTOR LOGS THAT ARE NOT OF DOC COLORIZING TYPE!");
                return null;
            }

            // Now find our output matches.
            var MatchedType = this.ColorConfigurationValues.FirstOrDefault(ColorSet =>
            {
                // Sample Name conversion
                // Setting name: "Call Stack Colors"
                // Type Name:    "CallStackColorFormatter"
                // 
                // Convert the Setting name to "CallStackColorFormatter"
                //      - Take off the S from colors
                //      - Tack on "Formatter"

                // Name of the set value to find. Then find the type name value.
                string NameToFind = ColorSet.Item1
                    .Replace("Colors", "Color")
                    .Replace(" ", string.Empty)
                    + "Formatter";

                // Compare to the input Type and check if we've got a match.
                return TypeOfFormatter.Name == NameToFind;
            });
            
            // Log the type pulled and values.
            _formatLogger.WriteLog($"PULLED TYPE COLOR VALUE SET {MatchedType.Item1} OK!", LogType.InfoLog);
            _formatLogger.WriteLog($"TOTAL OF {MatchedType.Item2.Length} COLOR SETS FOR THIS TYPE!", LogType.InfoLog);

            // Return the values pulled in.
            return MatchedType.Item2;
        }
        /// <summary>
        /// Pulls in settings values and locates a desired color format type as a set of strings.
        /// </summary>
        /// <param name="TypeOfFormatter">Type of formatter to use.</param>
        /// <returns>String set of color values or just White if none found. (But as media brush objects)</returns>
        public Tuple<Brush, Brush>[] PullColorForCommand(Type TypeOfFormatter)
        {
            // Pull the media string brush set first and then convert our output.
            Tuple<string, string>[] OutputStrings = this.PullColorsForCommand(TypeOfFormatter);
            var CastBrushSet = OutputStrings.Select(StringSet => new Tuple<Brush, Brush>(
                StringSet.Item1.ToMediaBrush(),
                StringSet.Item2.ToMediaBrush()))
            .ToArray();

            // Return built output set.
            return CastBrushSet;
        }
    }
}
