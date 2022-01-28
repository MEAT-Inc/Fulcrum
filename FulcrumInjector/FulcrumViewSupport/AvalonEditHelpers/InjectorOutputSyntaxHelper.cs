using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters;
using ICSharpCode.AvalonEdit;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers
{
    /// <summary>
    /// Target for redirecting logging configuration on our output
    /// </summary>
    public sealed class InjectorOutputSyntaxHelper 
    {
        // Edit Object which we will be using to write into.
        public readonly TextEditor OutputEditor;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of our redirecting target object.
        /// </summary>
        public InjectorOutputSyntaxHelper(TextEditor EditorObject)
        {
            // Build document transforming helper objects now. Each one colors in a segment of the output line
            this.OutputEditor = EditorObject;
            this.OutputEditor.TextArea.TextView.LineTransformers.Add(new PassThruCommandFormatter());       // For coloring in PTCommand values.
        }
    }
}
