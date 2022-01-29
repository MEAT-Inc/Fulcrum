using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters;
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
    [Target("DebugLoggingRedirectTarget")]
    public sealed class DebugLoggingRedirectTarget : TargetWithLayout
    {
        // Logger instance for configuring output during setup
        private SubServiceLogger ConfigLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("DebugLoggingRedirectTargetLogger")) ?? new SubServiceLogger("DebugLoggingRedirectTargetLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        // Edit Object which we will be using to write into.
        [RequiredParameter]
        public TextEditor OutputEditor { get; set; }
        [RequiredParameter]
        public UserControl ParentUserControl { get; set; }

        // Color Configuration
        public Tuple<string, Tuple<string, string>[]>[] ColorConfigurationValues { get; }
        public bool IsHighlighting => this.OutputEditor?.TextArea.TextView.LineTransformers.Count != 0;


        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of our redirecting target object.
        /// </summary>
        public DebugLoggingRedirectTarget(UserControl UserControlParent, TextEditor EditorObject)
        {
            // Store UserControl and Exit box
            this.OutputEditor = EditorObject;
            this.ParentUserControl = UserControlParent;
            this.ConfigLogger.WriteLog("STORED NEW CONTENT VALUES FOR USER CONTROL AND EDITOR INPUT OK!", LogType.InfoLog);

            // Setup our Layout
            this.Layout = new SimpleLayout(
                "[${date:format=hh\\:mm\\:ss}][${level:uppercase=true}][${mdc:custom-name}][${mdc:item=calling-class-short}] ::: ${message}"
            );
            this.ConfigLogger.WriteLog("BUILT LAYOUT FORMAT CORRECTLY! READY TO PULL COLORS", LogType.InfoLog);

            // Startup highlighting for this output.
            BuildColorFormatValues();
            this.StartColorHighlighting();
            this.ConfigLogger.WriteLog("PULLED COLOR VALUES IN CORRECTLY AND BEGAN OUTPUT FORMATTING ON THIS EDITOR!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls in all settings newly from the JSON Config file and stores them on this instance object.
        /// </summary>
        /// <returns></returns>
        public static Tuple<string, Tuple<string, string>[]>[] BuildColorFormatValues()
        {
            // Load value from the config file.
            LogBroker.Logger?.WriteLog("[DEBUG REDIRECT] ::: IMPORTING CONFIGURATION VALUES FROM SETTINGS STORE NOW...", LogType.WarnLog);
            var SettingSet = FulcrumSettingsShare.InjectorDebugSyntaxSettings.SettingsEntries;
            List<Tuple<string, Tuple<string, string>[]>> OutputValues = new List<Tuple<string, Tuple<string, string>[]>>();

            // Loop our settings values here and build entries.
            LogBroker.Logger?.WriteLog($"[DEBUG REDIRECT] ::: COMBINED TOTAL OF {SettingSet.Length} SETTING VALUES TO PARSE", LogType.TraceLog);
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
                    LogBroker.Logger?.WriteLog($"[DEBUG REDIRECT] ::: FAILED TO PULL COLOR VALUES FOR STRING ENTRY {SettingColorValues}!", LogType.ErrorLog);
                    continue;
                }

                // Find the values and build a foreground background pair value.
                foreach (Match RegexMatch in MatchSetBuilt.Groups)
                {
                    // Check if we have a set of values or not. If not, then we make a new pair value.
                    if (!RegexMatch.Value.Contains(",")) PulledColorSetList.Add(new(RegexMatch.Value, "NONE"));
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
            LogBroker.Logger?.WriteLog("[DEBUG REDIRECT] ::: CONFIGURED NEW COLOR VALUE SETS OK! RETURNING BUILT OUTPUT CONTENTS NOW", LogType.InfoLog);
            return OutputValues.ToArray();
        }
        /// <summary>
        /// Pulls in settings values and locates a desired color format type as a set of strings.
        /// </summary>
        /// <param name="TypeOfFormatter">Type of formatter to use.</param>
        /// <returns>String set of color values or just White if none found.</returns>
        public static Tuple<string,string>[] PullColorsForCommand(Type TypeOfFormatter)
        {
            // Make sure type is a doc formatter. If not return null
            if (TypeOfFormatter.BaseType != typeof(DocumentColorizingTransformer)) return null;
            return null;
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Begins writing output for highlighting syntax
        /// </summary>
        public void StartColorHighlighting()
        {
            // Build all the color formatting helpers here. Clear out existing ones first.
            this.StopColorHighlighting();

            // Now build all our new color format helpers.
            this.OutputEditor.TextArea.TextView.LineTransformers.Add(new DebugLogTimeColorFormatter());
            this.OutputEditor.TextArea.TextView.LineTransformers.Add(new DebugLogLevelColorFormatter());
            this.OutputEditor.TextArea.TextView.LineTransformers.Add(new DebugLogCallStackColorFormatter());
            this.OutputEditor.TextArea.TextView.LineTransformers.Add(new DebugLogLoggerNameColorFormatter());
        }
        /// <summary>
        /// Clears out the color helpers from the main input doc object.
        /// </summary>
        public void StopColorHighlighting() { this.OutputEditor.TextArea.TextView.LineTransformers.Clear(); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes the message out to our Logging box.
        /// </summary>
        /// <param name="LogEvent"></param>
        protected override void Write(LogEventInfo LogEvent)
        {
            // Write output using dispatcher to avoid threading issues.
            string RenderedText = this.Layout.Render(LogEvent);
            this.ParentUserControl.Dispatcher.Invoke(() => OutputEditor.Text += RenderedText + "\n");
        }
    }
}
