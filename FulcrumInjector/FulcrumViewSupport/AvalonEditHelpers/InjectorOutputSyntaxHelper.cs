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
    public sealed class InjectorOutputSyntaxHelper : OutputFormatHelperBase
    {
        /// <summary>
        /// Builds a new instance of our redirecting target object.
        /// </summary>
        public InjectorOutputSyntaxHelper(TextEditor EditorObject)
        {
            // Build document transforming helper objects now and build formatters.
            base.OutputEditor = EditorObject;
            FormatLogger.WriteLog("STORED NEW CONTENT VALUES FOR USER CONTROL AND EDITOR INPUT OK!", LogType.InfoLog);

            // Build color objects here.
            base.BuildColorFormatValues(FulcrumSettingsShare.InjectorDllSyntaxSettings.SettingsEntries);
            FormatLogger.WriteLog("PULLED COLOR VALUES IN CORRECTLY AND BEGAN OUTPUT FORMATTING ON THIS EDITOR!", LogType.InfoLog);

            // Kickoff color formatting here.
            this.StartColorHighlighting();
            FormatLogger.WriteLog("FORMAT OUTPUT HAS BEEN BUILT AND KICKED OFF OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Turns on coloring output for highlighting on the output view
        /// </summary>
        public override void StartColorHighlighting()
        {
            // Configure new outputs here.  
            this.StopColorHighlighting(); 
            FormatLogger.WriteLog("BUILDING NEW HIGHLIGHT HELPER OUTPUT NOW...", LogType.WarnLog);
            this.OutputEditor.TextArea.TextView.LineTransformers.Add(new TypeAndTimeColorFormatter(this));

            // TODO: Finish the next output format helper objects.
            // First up is CommandNameColorFormatter.
        }
        /// <summary>
        /// Clears out the color helpers from the main input doc object.
        /// </summary>
        public override void StopColorHighlighting()
        {
            // Remove all previous transformers and return out.
            FormatLogger.WriteLog("STOPPING OUTPUT FORMAT!", LogType.WarnLog);
            this.OutputEditor.TextArea.TextView.LineTransformers.Clear();
        }
    }
}
