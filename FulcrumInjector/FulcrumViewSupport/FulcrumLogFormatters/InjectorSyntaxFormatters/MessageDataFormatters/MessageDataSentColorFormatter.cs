﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using SharpExpressions;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.InjectorSyntaxFormatters.MessageDataFormatters
{
    /// <summary>
    /// Color output data read from a PTRead messages command
    /// </summary>
    internal class MessageDataSentColorFormatter : InjectorDocFormatterBase
    {
        /// <summary>
        /// Builds a new PTRead messages data format helper
        /// </summary>
        /// <param name="FormatBase"></param>
        public MessageDataSentColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Convert input regex into a multiline ready expression
            string MessageDataRegexString = PassThruExpressionRegex
                .LoadedExpressions[PassThruExpressionTypes.MessageSentInfo]
                .ExpressionPattern;

            // Search for our matches here and then loop our doc lines to apply coloring
            List<Regex> BuiltLineExpressions = this.GenerateColorExpressions(MessageDataRegexString);
            string CurrentLine = CurrentContext.Document.GetText(InputLine);
            Match[] MatchesFound = BuiltLineExpressions
                .Select(RegexPattern => RegexPattern.Match(CurrentLine))
                .ToArray();

            // See if anything matched up or not.
            if (!MatchesFound.Any(MatchSet => MatchSet.Success)) return;

            try
            {
                // Now color output values based on what we see here 
                if (CurrentLine.Contains("Msg")) this._colorForMatchSet(InputLine, MatchesFound, MatchesFound.Take(4).ToArray());
                if (CurrentLine.Contains("TxFlags")) this._colorForMatchSet(InputLine, MatchesFound, MatchesFound.Skip(4).Take(2).ToArray());
                if (CurrentLine.Contains("\\__")) this._colorForMatchSet(InputLine, MatchesFound, MatchesFound.Skip(6).ToArray());
            }
            catch {
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