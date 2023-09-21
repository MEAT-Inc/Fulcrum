using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using SharpExpressions;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.InjectorSyntaxFormatters.CommandParamFormatters
{
    /// <summary>
    /// Colors the stacktrace on the log line
    /// </summary>
    internal class TypeAndTimeColorFormatter : InjectorDocFormatterBase
    {
        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public TypeAndTimeColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Search for our matches here and then loop our doc lines to apply coloring
            string TimeMatchRegex = PassThruExpressionRegex
                .LoadedExpressions[PassThruExpressionTypes.CommandTime]
                .ExpressionPattern;

            // Search for our matches here and then loop our doc lines to apply coloring
            List<Regex> BuiltLineExpressions = this.GenerateColorExpressions(TimeMatchRegex);
            string CurrentLine = CurrentContext.Document.GetText(InputLine);
            Match[] MatchesFound = BuiltLineExpressions
                .Select(RegexPattern => RegexPattern.Match(CurrentLine))
                .ToArray();

            // See if anything matched up
            if (!MatchesFound.Any(MatchSet => MatchSet.Success)) return;
            foreach (var MatchFound in MatchesFound) this.ColorNewMatches(InputLine, MatchFound);
        }
    }
}
