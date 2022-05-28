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
            if (!MatchesFound.All(MatchSet => MatchSet.Success)) return;

            // Now color output values based on what we see here 
            if (new[] { "Mask", "Pattern", "Flow" }.Any(CurrentLine.Contains)) 
                foreach (var MatchValue in MatchesFound.Take(4).ToArray()) this.ColorNewMatches(InputLine, MatchValue);
            if (CurrentLine.Contains("TxFlags"))
                foreach (var MatchValue in MatchesFound.Skip(4).Take(2).ToArray()) this.ColorNewMatches(InputLine, MatchValue);
            if (CurrentLine.Contains("\\__"))
                foreach (var MatchValue in MatchesFound.Skip(6).ToArray()) this.ColorNewMatches(InputLine, MatchValue);
        }
    }
}
