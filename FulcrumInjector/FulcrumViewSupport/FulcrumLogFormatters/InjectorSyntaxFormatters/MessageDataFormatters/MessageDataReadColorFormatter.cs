using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using Newtonsoft.Json.Linq;
using SharpExpressions;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.InjectorSyntaxFormatters.MessageDataFormatters
{
    /// <summary>
    /// Color output data read from a PTRead messages command
    /// </summary>
    internal class MessageDataReadColorFormatter : InjectorDocFormatterBase
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
        /// Builds a new PTRead messages data format helper
        /// </summary>
        public MessageDataReadColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) 
        {
            // Convert input regex into a multiline ready expression
            string MessageDataRegexString = PassThruExpressionRegex
                .LoadedExpressions[PassThruExpressionTypes.MessageReadInfo]
                .ExpressionPattern;

            // Configure formatting regular expressions for output helpers
            this._builtLineExpressions = this.GenerateColorExpressions(MessageDataRegexString);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Search for our matches here and then loop our doc lines to apply coloring
            string CurrentLine = CurrentContext.Document.GetText(InputLine);
            Match[] MatchesFound = this._builtLineExpressions
                .Select(RegexPattern => RegexPattern.Match(CurrentLine))
                .ToArray();

            // See if anything matched up
            if (!MatchesFound.Any(MatchSet => MatchSet.Success)) return;

            try
            {
                // Now color output values based on what we see here 
                if (CurrentLine.Contains("Msg")) this._colorForMatchSet(InputLine, MatchesFound, MatchesFound.Take(5).ToArray());
                if (CurrentLine.Contains("RxStatus")) this._colorForMatchSet(InputLine, MatchesFound, MatchesFound.Skip(5).Take(1).ToArray());
                if (CurrentLine.Contains("\\__")) this._colorForMatchSet(InputLine, MatchesFound, MatchesFound.Skip(6).ToArray());
            }
            catch 
            {
                // Do nothing here since we don't want to fail on any color issues
            }
        }
        /// <summary>
        /// Colors a part of a match array for the given input matches found
        /// </summary>
        /// <param name="InputLine"></param>
        /// <param name="AllMatches"></param>
        /// <param name="SelectedMatchArray"></param>
        private void _colorForMatchSet(DocumentLine InputLine, Match[] AllMatches, Match[] SelectedMatchArray)
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
