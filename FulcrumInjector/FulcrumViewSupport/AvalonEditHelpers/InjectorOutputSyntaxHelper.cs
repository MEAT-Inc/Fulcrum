using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters;
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

        // TODO: Build properties into here to represent our objects built and parsed during runtime operations

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of our redirecting target object.
        /// </summary>
        public InjectorOutputSyntaxHelper(TextEditor EditorObject)
        {
            // Store UserControl and Exit box
            this.OutputEditor = EditorObject;
            
            // Build document transforming helper objects now. Each one colors in a segment of the output line
            // TODO: Build an include new logger target redirect coloring helpers for fulcrum outputs.
        }

        // --------------------------------------------------------------------------------------------------------------------------

        // TODO: Determine what kind of helper methods this class object will need.
    }
}
