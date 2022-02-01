using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

// Color Brushes
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters
{
    /// <summary>
    /// Base helper class instance for injector coloring configuration
    /// </summary>
    public class InjectorDocColorizerBase : DocumentColorizingTransformer
    {
        // Logger object and color brushes for formatting output.
        protected internal SubServiceLogger FormatLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith($"{this.GetType().Name}Logger")) ?? new SubServiceLogger($"{this.GetType().Name}Logger");
        protected internal Tuple<MediaBrush, MediaBrush>[] _coloringBrushes;

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        protected InjectorDocColorizerBase(OutputFormatHelperBase FormatBase)
        {
            // Pull in our color values. Store format helper.
            this._coloringBrushes = FormatBase.PullColorForCommand(this.GetType());
            if (this._coloringBrushes == null) this.FormatLogger.WriteLog($"WARNING! NO BRUSH VALUES FOUND FOR TYPE {this.GetType().Name}!", LogType.ErrorLog);
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Appends a new set of brushes for the number of matches if they are not equal numbers.
        /// </summary>
        /// <param name="MatchesFound"></param>
        /// <returns>New output brush set.</returns>
        protected internal bool UpdateBrushesForMatches(int MatchesFound)
        {
            // Validate our match count and brush values match correctly.
            if (MatchesFound == this._coloringBrushes.Length) return true;

            // Now build new values here.
            this.FormatLogger.WriteLog($"BUILDING {MatchesFound - this._coloringBrushes.Length} EXTRA BRUSH SETS FOR MATCHES!", LogType.WarnLog);
            while (this._coloringBrushes.Length < MatchesFound)
            {
                // Append in a new brush set of White FG and no background.
                var NewSet = new Tuple<MediaBrush, MediaBrush>(MediaBrushes.White, MediaBrushes.Transparent);
                this._coloringBrushes = this._coloringBrushes.Append(NewSet).ToArray();
            }

            // Return new built set (Also the private brush set)
            return false;
        }
        /// <summary>
        /// Applies new output formatting to line object based on input match objects.
        /// </summary>
        /// <param name="InputLine"></param>
        /// <param name="MatchesFound"></param>
        protected internal void ColorNewMatches(DocumentLine InputLine, MatchCollection MatchesFound)
        {
            // First fix our coloring count if needed.
            if (!this.UpdateBrushesForMatches(MatchesFound.Count))
                this.FormatLogger.WriteLog($"WARNING: APPENDING BLANK BRUSH VALUES FOR MATCHES ON TYPE {this.GetType().Name}!", LogType.WarnLog);

            // Now from all our matches made, loop and apply color values.
            int LineStartOffset = InputLine.Offset;
            string LineText = CurrentContext.Document.GetText(InputLine);
            for (int MatchIndex = 1; MatchIndex < MatchesFound.Count; MatchIndex++)
            {
                // Find the index start, stop, and then the whole range.
                string MatchFound = MatchesFound[MatchIndex].Value;
                int MatchIndexStart = LineStartOffset + LineText.IndexOf(MatchFound);
                int MatchIndexClose = MatchIndexStart + MatchFound.Length;

                // Now apply a color value based on the type of contents provided for it.
                base.ChangeLinePart(MatchIndexStart, MatchIndexClose, (NextMatchElement) =>
                {
                    // Colorize our logger name here.
                    NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[MatchIndex].Item1);
                    NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[MatchIndex].Item2);
                });
            }
        }

        // -----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // The base definition will just print the output using our base command
            this.FormatLogger.WriteLog("BASE FORMATTER TYPE HIT! THIS SHOULDN'T BE POSSIBLE!", LogType.TraceLog);
            throw new InvalidOperationException("CAN NOT ACCESS THE OVERRIDE FOR WRITING OBJECTS ON THE BASE FORMAT HELPER COMMAND CLASS!");
        }
    }
}
