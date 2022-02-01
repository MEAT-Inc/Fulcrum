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
        public CommandParameterColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase)
        {
            // Log the type of object built on our helper instance and then return out.
            this.FormatLogger.WriteLog($"BUILT NEW {this.GetType().Name} FORMAT HELPER!", LogType.TraceLog);
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Find the command type for our input object here. If none, drop out
            Regex TimeMatchRegex = new(PassThruRegexModelShare.PassThruParameters.ExpressionPattern);
            MatchCollection MatchesFound = TimeMatchRegex.Matches(CurrentContext.Document.GetText(InputLine));

            // Now run our coloring definitions and return out.
            int LineStartOffset = InputLine.Offset;
            this.UpdateBrushesForMatches(MatchesFound.Count);
            string LineText = CurrentContext.Document.GetText(InputLine);
            for (int MatchIndex = 1; MatchIndex < MatchesFound.Count; MatchIndex++)
            {
                // Find the index start, stop, and then the whole range.
                string MatchFound = MatchesFound[MatchIndex].Value;
                int MatchIndexStart = LineStartOffset + LineText.IndexOf(MatchFound);
                int MatchIndexClose = MatchIndexStart + MatchFound.Length;

                // Check to see what type of value we've pulled in. 
                bool IsInt = int.TryParse(MatchFound, out _);
                bool IsProtocolId = Regex.Match(MatchFound, @"\d+:\S+").Success;
                bool IsHexValue = int.TryParse(MatchFound, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);

                // Now set an index value for our int to pull from.
                int IndexOfBrush = IsInt ? 0 : IsProtocolId ? 1 : IsHexValue ? 2 : 3;
                this.FormatLogger.WriteLog($"PULLING FORMAT INDEX VALUE OF {IndexOfBrush} FOR NEW OUTPUT {MatchesFound}", LogType.TraceLog);

                // Now apply a color value based on the type of contents provided for it.
                base.ChangeLinePart(MatchIndexStart, MatchIndexClose, (NextMatchElement) =>
                {
                    // Colorize our logger name here.
                    NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[IndexOfBrush].Item1);
                    NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[IndexOfBrush].Item2);
                });
            }

            // Log done building values here.
            this.FormatLogger.WriteLog($"FORMATTED NEW OUTPUT FOR TYPE {this.GetType().Name} CORRECTLY!", LogType.TraceLog);
        }
    }
}
