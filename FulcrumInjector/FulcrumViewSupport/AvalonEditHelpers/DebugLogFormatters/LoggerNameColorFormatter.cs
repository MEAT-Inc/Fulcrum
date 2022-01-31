using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters
{
    /// <summary>
    /// Colorizes the logger names on the log output view
    /// </summary>
    public class LoggerNameColorFormatter : DocumentColorizingTransformer
    {
        // Color format brushes for this format instance.
        private readonly OutputFormatHelperBase _formatBase;
        private readonly Tuple<Brush, Brush>[] _coloringBrushes;

        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public LoggerNameColorFormatter(OutputFormatHelperBase FormatBase)
        {
            // Pull in our color values. Store format helper.
            this._formatBase = FormatBase;
            this._coloringBrushes = this._formatBase.PullColorForCommand(this.GetType());
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color logger names here
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
                string LoggerName = LineText.Split(']')[2].Trim('[', ']');
                while ((CurrentIndex = LineText.IndexOf(LoggerName, StartIndex)) >= 0)
                {
                    // Change line part call here.
                    int StartOffset = LineStartOffset + CurrentIndex;
                    int EndOffset = StartOffset + LoggerName.Length;
                    base.ChangeLinePart(StartOffset, EndOffset, (NextMatchElement) =>
                    {
                        // Colorize our logger name here.
                        NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[0].Item1);
                        NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[0].Item2);
                    });

                    // Tick our index and move on
                    StartIndex = CurrentIndex + 1;
                }
            }
            catch { return; }
        }
    }
}
