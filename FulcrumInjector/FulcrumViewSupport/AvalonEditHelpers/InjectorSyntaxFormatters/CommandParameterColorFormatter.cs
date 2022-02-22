﻿using System;
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
            if (!FoundMatch.Success) return;

            // Now run our coloring definitions and return out.
            int LineStartOffset = InputLine.Offset;
            string LineText = CurrentContext.Document.GetText(InputLine);
            for (int MatchGroupIndex = 1; MatchGroupIndex < FoundMatch.Groups.Count; MatchGroupIndex++)
            {
                // Pull the current group object value and split it into group values.
                string[] ParameterValuesFound = FoundMatch.Groups[MatchGroupIndex].Value
                    .Split(',')
                    .Select(ParamPart => ParamPart.Trim())
                    .Select(ParamPart => ParamPart.Trim('(', ')'))
                    .ToArray();
                
                // Loop the values and pull out the desired output
                foreach (var ParamFound in ParameterValuesFound)
                {
                    // Grab the index of our current group value first
                    int GroupPositionStart = LineStartOffset + LineText.IndexOf(ParamFound);
                    int GroupPositionEnd = GroupPositionStart + ParamFound.Length;

                    // Check to see what type of value we've pulled in. 
                    bool IsInt = int.TryParse(ParamFound, out _);
                    bool IsProtocolId = Regex.Match(ParamFound, @"\d+:\S+").Success;
                    bool IsHexValue = Regex.Match(ParamFound, @"0x[0-9A-F]+").Success;

                    // Now apply a color value based on the type of contents provided for it.
                    int IndexOfBrush = IsInt ? 0 : IsProtocolId ? 1 : IsHexValue ? 2 : 3;
                    base.ChangeLinePart(GroupPositionStart, GroupPositionEnd, (NextMatchElement) =>
                    {
                        // Colorize our logger name here.
                        NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[IndexOfBrush].Item1);
                        NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[IndexOfBrush].Item2);
                    });
                }
            }
        }
    }
}