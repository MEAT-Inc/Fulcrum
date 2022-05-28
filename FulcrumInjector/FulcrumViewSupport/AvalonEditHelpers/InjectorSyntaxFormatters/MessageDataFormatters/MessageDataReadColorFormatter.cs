using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using ICSharpCode.AvalonEdit.Document;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters.MessageDataFormatters
{
    /// <summary>
    /// Color output data read from a PTRead messages command
    /// </summary>
    public class MessageDataReadColorFormatter : InjectorDocFormatterBase
    {
        /// <summary>
        /// Builds a new PTRead messages data format helper
        /// </summary>
        /// <param name="FormatBase"></param>
        public MessageDataReadColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Convert input regex into a multiline ready expression
            List<Regex> BuiltLineExpressions = new List<Regex>();
            string MessageDataRegexString = PassThruRegexModelShare.MessageFilterInfo.ExpressionPattern;
            MatchCollection RegexStrings = Regex.Matches(MessageDataRegexString, @"\(\?<[^\)]+\)");
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
            if (CurrentLine.Contains("Msg"))
                foreach (var MatchValue in MatchesFound.Take(5).ToArray()) this.ColorNewMatches(InputLine, MatchValue);
            if (CurrentLine.Contains("RxStatus"))
                foreach (var MatchValue in MatchesFound.Skip(1).Take(2).ToArray()) this.ColorNewMatches(InputLine, MatchValue);
            if (CurrentLine.Contains("\\__"))
                foreach (var MatchValue in MatchesFound.Skip(6).ToArray()) this.ColorNewMatches(InputLine, MatchValue);
        }
    }
}
