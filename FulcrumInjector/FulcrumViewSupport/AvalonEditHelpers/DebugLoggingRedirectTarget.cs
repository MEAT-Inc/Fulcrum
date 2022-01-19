using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters;
using ICSharpCode.AvalonEdit;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace FulcrumInjector.FulcrumViewContent.Models
{
    /// <summary>
    /// Target for redirecting logging configuration on our output
    /// </summary>
    [Target("DebugToAvEditRedirect")]
    public sealed class DebugLoggingRedirectTarget : TargetWithLayout
    {
        // Edit Object which we will be using to write into.
        [RequiredParameter]
        public TextEditor DebugEditor { get; set; }
        [RequiredParameter]
        public UserControl ParentUserControl { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of our redirecting target object.
        /// </summary>
        public DebugLoggingRedirectTarget(UserControl UserControlParent, TextEditor EditorObject)
        {
            // Store UserControl and Exit box
            this.DebugEditor = EditorObject;
            this.ParentUserControl = UserControlParent;

            // Setup our Layout
            this.Layout = new SimpleLayout(
                "[${date:format=hh\\:mm\\:ss}][${level:uppercase=true}][${mdc:custom-name}][${mdc:item=calling-class-short}] ::: ${message}"
            );

            // Build document transforming helper objects now. Each one colors in a segment of the output line
            this.DebugEditor.TextArea.TextView.LineTransformers.Add(new DebugLogTimeColorFormatter());
            this.DebugEditor.TextArea.TextView.LineTransformers.Add(new DebugLogLevelColorFormatter());
            this.DebugEditor.TextArea.TextView.LineTransformers.Add(new DebugLogCallStackColorFormatter());
            this.DebugEditor.TextArea.TextView.LineTransformers.Add(new DebugLogLoggerNameColorFormatter()); 
        }

        /// <summary>
        /// Writes the message out to our Logging box.
        /// </summary>
        /// <param name="LogEvent"></param>
        protected override void Write(LogEventInfo LogEvent)
        {
            // Write output using dispatcher to avoid threading issues.
            string RenderedText = this.Layout.Render(LogEvent);
            this.ParentUserControl.Dispatcher.Invoke(() => DebugEditor.Text += RenderedText + "\n");
        }
    }
}
