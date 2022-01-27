using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using FulcrumInjector.FulcrumLogic.PassThruExpressions.ExpressionObjects;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters
{
    /// <summary>
    /// Colors the stacktrace on the log line
    /// </summary>
    public class PassThruCommandFormatter : DocumentColorizingTransformer
    {
        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Find the command type for our input object here. If none, drop out
            string LineText = CurrentContext.Document.GetText(InputLine);
            PassThruCommandType CommandType = LineText.GetTypeFromLines();
            if (CommandType == PassThruCommandType.NONE) return;

            // If we don't have a type of none, then we get the regex for that type object.
            var LineExpression = PassThruRegexModelShare.GetRegexForCommand(CommandType); 
            if (!LineExpression.Evaluate(LineText, out var ExpressionMatches)) return;

            // Loop all the matched values here. If none, this method ends here.
            // Eventually we might include special conditions for different types of commands.
            for (int MatchIndex = 1; MatchIndex < ExpressionMatches.Length - 1; MatchIndex++)
            {
                // Find the index start, stop, and then the whole range.
                string MatchFound = ExpressionMatches[MatchIndex];
                int MatchIndexStart = LineText.IndexOf(MatchFound);
                int MatchIndexClose = MatchIndexStart + MatchFound.Length;

                // Now apply a color value based on the type of contents provided for it.
                base.ChangeLinePart(MatchIndexStart, MatchIndexClose, (NextMatchElement) =>
                {
                    // Colorize our logger name here.
                    NextMatchElement.TextRunProperties.SetBackgroundBrush(Brushes.Gold);
                    NextMatchElement.TextRunProperties.SetForegroundBrush(Brushes.DarkCyan);
                });
            }
        }
    }
}
