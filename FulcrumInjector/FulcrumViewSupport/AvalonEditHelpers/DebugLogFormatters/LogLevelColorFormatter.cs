using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters
{
    /// <summary>
    /// Colorizing object helper to format our built log lines.
    /// </summary>
    public class LogLevelColorFormatter : InjectorDocFormatterBase
    {
        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public LogLevelColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

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
            var LogTypes = Enum.GetNames(typeof(LogType))
                .Select(StringObj => StringObj.Replace("Log", string.Empty).ToUpper())
                .ToList();

            // If we got a match, then find it on the line.
            if (!LogTypes.Any(LineText.Contains)) return;
            foreach (var LogTypeObj in LogTypes)
            {
                // Now using the match of our line output
                string NextLogType = LogTypeObj.ToUpper();
                if (!LineText.Contains(NextLogType)) { continue; }
                while ((CurrentIndex = LineText.IndexOf(NextLogType, StartIndex)) >= 0)
                {
                    // Change line part call here.
                    int StartOffset = LineStartOffset + CurrentIndex;
                    int EndOffset = StartOffset + NextLogType.Length;
                    base.ChangeLinePart(StartOffset, EndOffset, (NextMatchElement) =>
                    {
                        // Set our current color scheme we want.
                        switch (LogTypeObj)
                        {
                            case "TRACE":
                                NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[0].Item1);
                                NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[0].Item2);
                                break;

                            case "DEBUG":
                                NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[1].Item1);
                                NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[1].Item2);
                                break;

                            case "INFO":
                                NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[2].Item1);
                                NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[2].Item2);
                                break;

                            case "WARN":
                                NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[3].Item1);
                                NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[3].Item2);

                                // Pull current typeface, update it with new value
                                Typeface WarnTypeFace = NextMatchElement.TextRunProperties.Typeface;
                                NextMatchElement.TextRunProperties.SetTypeface(new Typeface(
                                    WarnTypeFace.FontFamily,
                                    FontStyles.Italic,
                                    FontWeights.DemiBold,
                                    WarnTypeFace.Stretch
                                ));
                                break;

                            case "ERROR":
                            case "FATAL":
                                int IndexOfBrush = LogTypeObj == "ERROR" ? 4 : 5;
                                NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[IndexOfBrush].Item1);
                                NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[IndexOfBrush].Item2);

                                // Pull current typeface, update it with new value
                                Typeface ErrorTypeFace = NextMatchElement.TextRunProperties.Typeface;
                                NextMatchElement.TextRunProperties.SetTypeface(new Typeface(
                                    ErrorTypeFace.FontFamily,
                                    FontStyles.Normal,
                                    FontWeights.Bold,
                                    ErrorTypeFace.Stretch
                                ));
                                break;
                        }
                    });

                    // Tick our index and move on
                    StartIndex = CurrentIndex + 1;
                }
            }
        }
    }
}