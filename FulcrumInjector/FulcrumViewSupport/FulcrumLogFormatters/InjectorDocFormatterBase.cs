using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Newtonsoft.Json.Linq;
using SharpLogging;

// Color Brushes
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters
{
    /// <summary>
    /// Base helper class instance for injector coloring configuration
    /// </summary>
    internal class InjectorDocFormatterBase : DocumentColorizingTransformer
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger object and color brushes for formatting output.
        protected static SharpLogger _formatLogger;
        protected Tuple<MediaBrush, MediaBrush>[] _coloringBrushes;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        protected InjectorDocFormatterBase(OutputFormatHelperBase FormatBase)
        {
            // Configure our logger instance if needed
            _formatLogger ??= new SharpLogger(LoggerActions.UniversalLogger);

            // Pull in our color values. Store format helper.
            if (this.GetType() == typeof(InjectorDocFormatterBase)) return;
            this._coloringBrushes = FormatBase.PullColorForCommand(this.GetType());
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds all the filtering regex values needed based on a given input regex value
        /// </summary>
        /// <param name="InputRegex">The input regex to parse</param>
        /// <returns>A list of all the built regex string from the input</returns>
        protected internal List<Regex> GenerateColorExpressions(string InputRegex)
        {
            // Setup an output list and match our input regex string
            List<Regex> BuiltLineExpressions = new List<Regex>();
            MatchCollection RegexStrings = Regex.Matches(InputRegex, @"\(\?<[^\)]+\)");

            // Build a new set of colorizing regular expressions using the loaded regex values here
            for (int StringIndex = 0; StringIndex < RegexStrings.Count; StringIndex++)
            {
                // Store our next string value and try to build a regex from it
                string NextRegexString = RegexStrings[StringIndex].Value;

                // Try and build a new regex for this match. If it fails, build the string out more
                try { BuiltLineExpressions.Add(new Regex(NextRegexString)); }
                catch
                {
                    // If the regex fails to build, substring over our input until we get a valid string
                    int StartingIndex = RegexStrings[StringIndex].Index;
                    int EndingIndex = StringIndex == RegexStrings.Count - 1
                        ? InputRegex.Length
                        : RegexStrings[StringIndex + 1].Index;

                    // If we're at the end of the regex, store the value and compute it
                    if (StringIndex == RegexStrings.Count - 1)
                    {
                        // Build our new regex string and store it for this value
                        NextRegexString = InputRegex.Substring(StartingIndex);
                        BuiltLineExpressions.Add(new Regex(NextRegexString));
                        continue;
                    }

                    // Build a substring to search for our ending
                    int SearchSize = EndingIndex - StartingIndex;
                    string SearchString = InputRegex.Substring(StartingIndex, SearchSize);

                    // Find all closing brace locations and try each one
                    bool IsAdded = false;
                    for (int NextIndex = 0; ; NextIndex += ")".Length)
                    {
                        // Build a new substring value here
                        NextIndex = SearchString.IndexOf(")", NextIndex);
                        NextRegexString = SearchString.Substring(0, NextIndex + 1);

                        // Wrap our new attempts in another try/catch block
                        try
                        {
                            // Try and build a regex from the new substring value
                            BuiltLineExpressions.Add(new Regex(NextRegexString));
                            IsAdded = true;
                            break;
                        }
                        catch
                        {
                            // TODO: Figure out a better way to try and validate regex objects
                            // Ignored since we're able to hopefully retry at the next index
                        }
                    }

                    // Throw an exception if we're at this point since we couldn't close the regex
                    if (!IsAdded) throw new IndexOutOfRangeException("Error! Could not close regex for filtering!");
                }
            }

            // Return out the built list of regex values here if any are built
            if (BuiltLineExpressions.Count != 0) return BuiltLineExpressions;
 
            // If no list values are found, try and reconfigure the input regex once more
            try { return new List<Regex>() { new Regex(InputRegex) }; }
            catch
            {
                // Return out an empty list if we're unable to build any regex values
                return new List<Regex>();
            }
        }
        /// <summary>
        /// Appends a new set of brushes for the number of matches if they are not equal numbers.
        /// </summary>
        /// <param name="MatchesFound"></param>
        /// <returns>New output brush set.</returns>
        protected internal bool UpdateBrushesForMatches(int MatchesFound)
        {
            // Validate our match count and brush values match correctly.
            if (MatchesFound == this._coloringBrushes.Length) return true;
            while (this._coloringBrushes.Length < MatchesFound)
            {
                // Append in a new brush set of White FG and no background.
                var NewSet = new Tuple<MediaBrush, MediaBrush>(MediaBrushes.White, MediaBrushes.Transparent);
                this._coloringBrushes = this._coloringBrushes.Append(NewSet).ToArray();
            }

            // Return new built set (Also the private brush set)
            return false;
        }
        /// <summary>
        /// Applies new output formatting to line object based on input match objects.
        /// </summary>
        /// <param name="InputLine"></param>
        /// <param name="MatchesFound"></param>
        protected internal void ColorNewMatches(DocumentLine InputLine, Match MatchFound)
        {
            // First fix our coloring count if needed.
            this.UpdateBrushesForMatches(MatchFound.Groups.Count);

            // Now from all our matches made, loop and apply color values.
            int LineStartOffset = InputLine.Offset;
            string LineText = CurrentContext.Document.GetText(InputLine);
            for (int GroupIndex = 1; GroupIndex < MatchFound.Groups.Count; GroupIndex++)
            {
                // Pull the current group object value
                string GroupFound = MatchFound.Groups[GroupIndex].Value;
                int GroupPositionStart = LineStartOffset + LineText.IndexOf(GroupFound);
                int GroupPositionEnd = GroupPositionStart + GroupFound.Length;

                // Now apply a color value based on the type of contents provided for it.
                base.ChangeLinePart(GroupPositionStart, GroupPositionEnd, (NextMatchElement) =>
                {
                    // Colorize our logger name here.
                    NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[GroupIndex - 1].Item1);
                    NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[GroupIndex - 1].Item2);
                });
            }
        }
        /// <summary>
        /// Colors an index based set of values by the brushes given at index values specified.
        /// </summary>
        /// <param name="BrushSet">Brushes to color</param>
        /// <param name="IndexBounds">Index to set from</param>
        protected internal void ColorMatch(DocumentLine InputLine, MediaBrush[] BrushSet, int[] IndexBounds = null)
        {
            // See if the index values and Brushes exist or not.
            IndexBounds ??= new[] { InputLine.Offset, InputLine.EndOffset };
            BrushSet ??= new[] { MediaBrushes.White, MediaBrushes.Transparent };

            // Now apply a color value based on the type of contents provided for it.
            base.ChangeLinePart(IndexBounds[0], IndexBounds[1], (NextLineObject) =>
            {
                // Colorize our logger name here.
                NextLineObject.TextRunProperties.SetForegroundBrush(BrushSet.FirstOrDefault());
                NextLineObject.TextRunProperties.SetBackgroundBrush(BrushSet.LastOrDefault());
            });
        }
        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // The base definition will just print the output using our base command
            _formatLogger.WriteLog("BASE FORMATTER TYPE HIT! THIS SHOULDN'T BE POSSIBLE!", LogType.TraceLog);
            throw new InvalidOperationException("CAN NOT ACCESS THE OVERRIDE FOR WRITING OBJECTS ON THE BASE FORMAT HELPER COMMAND CLASS!");
        }
    }
}
