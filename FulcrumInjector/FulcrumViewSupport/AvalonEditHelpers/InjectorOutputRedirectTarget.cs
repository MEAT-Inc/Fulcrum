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
    [Target("InjectorOutputRedirectTarget")]
    public sealed class InjectorOutputRedirectTarget : TargetWithLayout
    {
        // Edit Object which we will be using to write into.
        [RequiredParameter]
        public TextEditor OutputEditor { get; set; }
        [RequiredParameter]
        public UserControl ParentUserControl { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of our redirecting target object.
        /// </summary>
        public InjectorOutputRedirectTarget(UserControl UserControlParent, TextEditor EditorObject)
        {
            // Store UserControl and Exit box
            this.OutputEditor = EditorObject;
            this.ParentUserControl = UserControlParent;
            
            // Build document transforming helper objects now. Each one colors in a segment of the output line
            // TODO: Build an include new logger target redirect coloring helpers for fulcrum outputs.
        }

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
