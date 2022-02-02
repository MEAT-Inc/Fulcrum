using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using ICSharpCode.AvalonEdit.Document;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters
{
    /// <summary>
    /// Class instance used to help format color values for the command parameter values when pulled from the log lines.
    /// </summary>
    public class CommandParameterColorFormatter : InjectorDocFormatterBase
    {
        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public CommandParameterColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) { }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Find the command type for our input object here. If none, drop out
            Regex TimeMatchRegex = new(PassThruRegexModelShare.PassThruParameters.ExpressionPattern);
            Match FoundMatch = TimeMatchRegex.Match(CurrentContext.Document.GetText(InputLine));
            if (FoundMatch.Groups.Count == 0) return;

            // Now run our coloring definitions and return out.
            int LineStartOffset = InputLine.Offset;
            this.UpdateBrushesForMatches(FoundMatch.Groups.Count);
            string LineText = CurrentContext.Document.GetText(InputLine);
            for (int MatchGroupIndex = 0; MatchGroupIndex < FoundMatch.Groups.Count; MatchGroupIndex++)
            {
                // Pull the current group object value
                string GroupFound = FoundMatch.Groups[MatchGroupIndex].Value;
                int GroupPositionStart = LineStartOffset + LineText.IndexOf(GroupFound);
                int GroupPositionEnd = GroupPositionStart + GroupFound.Length;

                // Check to see what type of value we've pulled in. 
                bool IsInt = int.TryParse(GroupFound, out _);
                bool IsProtocolId = Regex.Match(GroupFound, @"\d+:\S+").Success;
                bool IsHexValue = int.TryParse(GroupFound, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);

                // Now apply a color value based on the type of contents provided for it.
                int IndexOfBrush = IsInt ? 0 : IsProtocolId ? 1 : IsHexValue ? 2 : 3;
                base.ChangeLinePart(GroupPositionStart, GroupPositionEnd, (NextMatchElement) =>
                {
                    // Colorize our logger name here.
                    NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[IndexOfBrush].Item1);
                    NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[IndexOfBrush].Item2);
                });
            }
        }
    }
}
