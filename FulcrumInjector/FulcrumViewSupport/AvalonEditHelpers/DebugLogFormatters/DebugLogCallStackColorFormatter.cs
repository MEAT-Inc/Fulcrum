using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters
{
    /// <summary>
    /// Colors the stacktrace on the log line
    /// </summary>
    public class DebugLogCallStackColorFormatter : DocumentColorizingTransformer
    {
        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Setup base index values.
            int LineStartOffset = InputLine.Offset;
            int StartIndex = 0; int CurrentIndex;
            string LineText = CurrentContext.Document.GetText(InputLine);

            // Find our logger name segment.
            try
            {
                string LoggerName = LineText.Split(']')[3].Trim('[', ']');
                while ((CurrentIndex = LineText.IndexOf(LoggerName, StartIndex)) >= 0)
                {
                    // Change line part call here.
                    int StartOffset = LineStartOffset + CurrentIndex;
                    int EndOffset = StartOffset + LoggerName.Length;
                    base.ChangeLinePart(StartOffset, EndOffset, (NextMatchElement) =>
                    {
                    // Colorize our logger name here.
                    NextMatchElement.TextRunProperties.SetBackgroundBrush(Brushes.Transparent);
                        NextMatchElement.TextRunProperties.SetForegroundBrush(Brushes.DarkCyan);
                    });

                    // Tick our index and move on
                    StartIndex = CurrentIndex + 1;
                }
            }
            catch { return; }
        }
    }
}