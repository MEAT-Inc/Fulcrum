using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers
{
    /// <summary>
    /// Target for redirecting logging configuration on our output
    /// </summary>
    public sealed class InjectorOutputSyntaxHelper 
    {
        // Logger instance for configuring output during setup
        private SubServiceLogger ConfigLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorOutputSyntaxHelperLogger")) ?? new SubServiceLogger("InjectorOutputSyntaxHelperLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        // Editor object and color helper objects.
        public TextEditor OutputEditor { get; }
        public Tuple<string, Tuple<string, string>[]>[] ColorConfigurationValues { get; }
        public bool IsHighlighting => this.OutputEditor?.TextArea.TextView.LineTransformers.Count != 0; 

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of our redirecting target object.
        /// </summary>
        public InjectorOutputSyntaxHelper(TextEditor EditorObject)
        {
            // Import our color values here.
            BuildColorFormatValues();

            // Build document transforming helper objects now and build formatters.
            this.OutputEditor = EditorObject;
            this.StartColorHighlighting();
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls in all settings newly from the JSON Config file and stores them on this instance object.
        /// </summary>
        /// <returns></returns>
        public static Tuple<string, Tuple<string, string>[]>[] BuildColorFormatValues()
        {
            // Load value from the config file.
            LogBroker.Logger?.WriteLog("[INJECTOR SYNTAX] ::: IMPORTING CONFIGURATION VALUES FROM SETTINGS STORE NOW...", LogType.WarnLog);
            var SettingSet = FulcrumSettingsShare.InjectorDllSyntaxSettings.SettingsEntries;
            List<Tuple<string, Tuple<string, string>[]>> OutputValues = new List<Tuple<string, Tuple<string, string>[]>>();

            // Loop our settings values here and build entries.
            LogBroker.Logger?.WriteLog($"[INJECTOR SYNTAX] ::: COMBINED TOTAL OF {SettingSet.Length} SETTING VALUES TO PARSE", LogType.TraceLog);
            foreach (var SettingEntry in SettingSet)
            {
                // Get current setting value string.
                string SettingColorValues = SettingEntry.SettingValue.ToString();
                Regex FormatParse = new Regex(@"([0-9A-F]+)|\(([0-9A-F]+,\s[0-9A-F]+)\)");

                // Find matches on this string output here.
                List<Tuple<string, string>> PulledColorSetList = new List<Tuple<string, string>>();
                var MatchSetBuilt = FormatParse.Match(SettingColorValues);

                // If Failed, log it and move on.
                if (!MatchSetBuilt.Success) {
                    LogBroker.Logger?.WriteLog($"[INJECTOR SYNTAX] ::: FAILED TO PULL COLOR VALUES FOR STRING ENTRY {SettingColorValues}!", LogType.ErrorLog);
                    continue;
                }

                // Find the values and build a foreground background pair value.
                foreach (Match RegexMatch in MatchSetBuilt.Groups)
                {
                    // Check if we have a set of values or not. If not, then we make a new pair value.
                    if (!RegexMatch.Value.Contains(",")) PulledColorSetList.Add(new(RegexMatch.Value, "000000"));
                    else
                    {
                        // Split content into two parts and add a value set here.
                        string[] SplitColorValues = RegexMatch.Value.Split(',');
                        PulledColorSetList.Add(new(SplitColorValues[0], SplitColorValues[1]));
                    }
                }

                // Append our new value set onto our outputs
                OutputValues.Add(new Tuple<string, Tuple<string, string>[]>(SettingEntry.SettingName, PulledColorSetList.ToArray()));
            }

            // Return built list of tuple output here.
            LogBroker.Logger?.WriteLog("[INJECTOR SYNTAX] ::: CONFIGURED NEW COLOR VALUE SETS OK! RETURNING BUILT OUTPUT CONTENTS NOW", LogType.InfoLog);
            return OutputValues.ToArray();
        }
        /// <summary>
        /// Pulls in settings values and locates a desired color format type as a set of strings.
        /// </summary>
        /// <param name="TypeOfFormatter">Type of formatter to use.</param>
        /// <returns>String set of color values or just White if none found.</returns>
        public static Tuple<string, string>[] PullColorsForCommand(Type TypeOfFormatter)
        {
            // Make sure type is a doc formatter. If not return null. Then try to pull value.
            if (TypeOfFormatter.BaseType != typeof(DocumentColorizingTransformer)) return null;
            return null;
        }


        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Turns on coloring output for highlighting on the output view
        /// </summary>
        public void StartColorHighlighting()
        {
            // Configure new outputs here.  
            this.StopColorHighlighting(); 
            this.OutputEditor.TextArea.TextView.LineTransformers.Add(new PassThruTypeAndTimeColors());
        }
        /// <summary>
        /// Clears out the color helpers from the main input doc object.
        /// </summary>
        public void StopColorHighlighting()
        {
            // Remove all previous transformers and return out.
            this.OutputEditor.TextArea.TextView.LineTransformers.Clear();
        }
        
    }
}
