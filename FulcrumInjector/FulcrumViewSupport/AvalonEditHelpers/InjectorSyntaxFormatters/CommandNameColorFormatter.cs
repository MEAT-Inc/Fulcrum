using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters
{
    /// <summary>
    /// Format helper for outlining the command name, and the hex properties of the input line value.
    /// </summary>
    public class CommandNameColorFormatter : DocumentColorizingTransformer
    {
        // Color format brushes for this format instance.
        private Tuple<Brush, Brush>[] _coloringBrushes;
        private readonly OutputFormatHelperBase _formatBase;

        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public CommandNameColorFormatter(OutputFormatHelperBase FormatBase)
        {
            // Pull in our color values. Store format helper.
            this._formatBase = FormatBase;
            this._coloringBrushes = this._formatBase.PullColorForCommand(this.GetType());
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Find the command type for our input object here. If none, drop out
            string LineText = CurrentContext.Document.GetText(InputLine);
            Regex TimeMatchRegex = new(PassThruRegexModelShare.PassThruTime.ExpressionPattern);
            MatchCollection MatchesFound = TimeMatchRegex.Matches(LineText);

            // Validate our match count and brush values match correctly.
            while (this._coloringBrushes.Length < MatchesFound.Count)
            {
                // Append in a new brush set of White FG and no background.
                var NewSet = new Tuple<Brush, Brush>(Brushes.White, Brushes.Transparent);
                this._coloringBrushes = this._coloringBrushes.Append(NewSet).ToArray();
                this._formatBase.FormatLogger.WriteLog("WROTE EXTRA SETS FOR MATCHES DUE TO INVALID REGEX BRUSH COUNT!");
            }

            // Now from all our matches made, loop and apply color values.
            int LineStartOffset = InputLine.Offset;
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
    }
}
