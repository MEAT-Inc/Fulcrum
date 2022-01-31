using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters
{
    /// <summary>
    /// Colorizing object helper to format our built log lines.
    /// </summary>
    public class TimeColorFormatter : DocumentColorizingTransformer
    {
        // Color format brushes for this format instance.
        private readonly OutputFormatHelperBase _formatBase;
        private readonly Tuple<Brush, Brush>[] _coloringBrushes;

        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public TimeColorFormatter(OutputFormatHelperBase FormatBase)
        {
            // Pull in our color values. Store format helper.
            this._formatBase = FormatBase;
            this._coloringBrushes = this._formatBase.PullColorForCommand(this.GetType());
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Coloring Line command override
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Setup base index values.
            int LineStartOffset = InputLine.Offset;
            int StartIndex = 0; int CurrentIndex;
            string LineText = CurrentContext.Document.GetText(InputLine);

            // See if we match the time string value.
            var MatchResult = Regex.Match(LineText, @"\d{2}:\d{2}:\d{2}");
            if (!MatchResult.Success) return;

            // If we got a match, then find it on the line.
            while ((CurrentIndex = LineText.IndexOf(MatchResult.Value, StartIndex)) >= 0)
            {
                // Change line part call here.
                int StartOffset = LineStartOffset + CurrentIndex;
                int EndOffset = StartOffset + MatchResult.Length;
                base.ChangeLinePart(StartOffset, EndOffset, (NextMatchElement) =>
                {
                    // Set our current color scheme we want.
                    NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[0].Item1);
                    NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[0].Item2);

                    // Pull current typeface, update it with new value
                    Typeface CurrentTypeFace = NextMatchElement.TextRunProperties.Typeface;
                    NextMatchElement.TextRunProperties.SetTypeface(new Typeface(
                        CurrentTypeFace.FontFamily,
                        FontStyles.Italic,
                        CurrentTypeFace.Weight,
                        CurrentTypeFace.Stretch
                    ));
                });

                // Tick our index and move on
                StartIndex = CurrentIndex + 1;
            }
        }
    }
}
