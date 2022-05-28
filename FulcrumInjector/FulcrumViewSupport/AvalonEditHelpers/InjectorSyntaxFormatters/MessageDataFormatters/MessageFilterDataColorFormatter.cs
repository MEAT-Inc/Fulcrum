using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using ICSharpCode.AvalonEdit.Document;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters.MessageDataFormatters
{
    /// <summary>
    /// Used to color and format message data contents out inside our injector log file outputs
    /// </summary>
    public class MessageFilterDataColorFormatter : InjectorDocFormatterBase
    {
        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public MessageFilterDataColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) { }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Convert input regex into a multiline ready expression
            List<Regex> BuiltLineExpressions = new List<Regex>();
            string FilterRegexString = PassThruRegexModelShare.MessageFilterInfo.ExpressionPattern;
            MatchCollection RegexStrings = Regex.Matches(FilterRegexString, @"\(\?<[^\)]+\)");
            for (int StringIndex = 0; StringIndex < RegexStrings.Count; StringIndex++) 
                BuiltLineExpressions.Add(new Regex(RegexStrings[StringIndex].Value));
            
            // Search for our matches here and then loop our doc lines to apply coloring
            string CurrentLine = CurrentContext.Document.GetText(InputLine);
            Match[] MatchesFound = BuiltLineExpressions
                .Select(RegexPattern => RegexPattern.Match(CurrentLine))
                .ToArray();

            // See if anything matched up
            if (!MatchesFound.Any(MatchSet => MatchSet.Success)) return;

            // Now color output values based on what we see here 
            if (new[] { "Mask", "Pattern", "Flow"}.Any(CurrentLine.Contains)) this.ColorForMatchSet(InputLine, MatchesFound, MatchesFound.Take(4).ToArray());
            if (CurrentLine.Contains("TxFlags")) this.ColorForMatchSet(InputLine, MatchesFound, MatchesFound.Skip(4).Take(2).ToArray());
            if (CurrentLine.Contains("\\__")) this.ColorForMatchSet(InputLine, MatchesFound, MatchesFound.Skip(6).ToArray());
        }


        /// <summary>
        /// Colors a part of a match array for the given input matches found
        /// </summary>
        /// <param name="InputLine"></param>
        /// <param name="AllMatches"></param>
        /// <param name="SelectedMatchArray"></param>
        private void ColorForMatchSet(DocumentLine InputLine, Match[] AllMatches, Match[] SelectedMatchArray)
        {
            // Store current line text value
            string CurrentLine = CurrentContext.Document.GetText(InputLine);
            foreach (var MatchValue in SelectedMatchArray)
            {
                // Set Index and get brushes. Then color output
                int CurrentIndex = AllMatches.ToList().IndexOf(MatchValue);
                var BrushSet = this._coloringBrushes[CurrentIndex];
                int GroupPositionStart = InputLine.Offset + CurrentLine.IndexOf(MatchValue.Value);
                int GroupPositionEnd = GroupPositionStart + MatchValue.Value.Length;

                // Color line output here
                this.ColorMatch(
                    InputLine,
                    new[] { BrushSet.Item1, BrushSet.Item2 },
                    new[] { GroupPositionStart, GroupPositionEnd }
                );
            }
        }
    }
}
