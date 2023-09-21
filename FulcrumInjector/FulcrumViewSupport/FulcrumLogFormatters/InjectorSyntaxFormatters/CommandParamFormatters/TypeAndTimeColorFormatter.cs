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
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private list of regular expressions for formatting
        private readonly List<Regex> _builtLineExpressions;

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public TypeAndTimeColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) 
        {
            // Search for our matches here and then loop our doc lines to apply coloring
            string TimeMatchRegex = PassThruExpressionRegex
                .LoadedExpressions[PassThruExpressionTypes.CommandTime]
                .ExpressionPattern;

            // Search for our matches here and then loop our doc lines to apply coloring
            this._builtLineExpressions = this.GenerateColorExpressions(TimeMatchRegex);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Find the command type for our input object here. If none, drop out
            string CurrentLine = CurrentContext.Document.GetText(InputLine);
            Match[] MatchesFound = this._builtLineExpressions
                .Select(RegexPattern => RegexPattern.Match(CurrentLine))
                .ToArray();

            // See if anything matched up
            if (!MatchesFound.Any(MatchSet => MatchSet.Success)) return;
            foreach (var MatchFound in MatchesFound) this.ColorNewMatches(InputLine, MatchFound);
        }
    }
}
