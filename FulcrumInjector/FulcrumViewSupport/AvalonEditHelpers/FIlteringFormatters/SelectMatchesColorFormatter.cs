using System;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.FIlteringFormatters
{
    /// <summary>
    /// Selects the text matching the given input pattern.
    /// </summary>
    public class SelectMatchesColorFormatter : DocumentColorizingTransformer
    {
        // No matches bool value
        public bool NoMatches { get; private set; }

        // String to use for searching and Regex Toggle
        public readonly bool UseRegex;
        public readonly string MatchString;

        /// <summary>
        /// Builds a new coloring helper using the search params provided
        /// </summary>
        /// <param name="MatchingString">String to find</param>
        /// <param name="UseRegex">Sets if this is a regex or not.</param>
        public SelectMatchesColorFormatter(string MatchingString, bool UseRegex = false)
        {
            // Store the matching string. Remove Regex call if needed.
            this.MatchString = MatchingString.ToLower().StartsWith("@r") ? 
                    MatchingString.Substring(2) :
                    MatchingString;

            // Sets if we need to use Regex or not.
            this.UseRegex = UseRegex || MatchingString.ToLower().StartsWith("@r");
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color the line containing the text where we need to select
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Setup base index values.
            int LineStartOffset = InputLine.Offset;
            int StartIndex = 0; int CurrentIndex;
            string LineText = CurrentContext.Document.GetText(InputLine);

            // Find our logger name segment.
            try
            {
                // Temp values for search passed and the found value
                bool RegexPassed = true;
                string FindThis = this.MatchString;

                // If using regex, find values
                if (this.UseRegex)
                { 
                    // Run our regex matching process here
                    var RegexOutput = Regex.Match(LineText, this.MatchString);

                    // Set if the regex passed and the value found.
                    FindThis = RegexOutput.Value;
                    RegexPassed = RegexOutput.Success;
                }

                // Search the string value
                while ((CurrentIndex = UseRegex ? 
                    LineText.IndexOf(FindThis, StartIndex) : 
                    LineText.IndexOf(FindThis, StartIndex, StringComparison.CurrentCultureIgnoreCase)) >= 0 && RegexPassed)
                {
                    // Change line part call here.
                    int StartOffset = LineStartOffset + CurrentIndex;
                    int EndOffset = StartOffset + FindThis.Length;
                    base.ChangeLinePart(StartOffset, EndOffset, (NextMatchElement) =>
                    {
                        // Colorize our logger name here.
                        NextMatchElement.TextRunProperties.SetBackgroundBrush(Brushes.Yellow);
                        NextMatchElement.TextRunProperties.SetForegroundBrush(Brushes.Black);
                    });

                    // Tick our index and move on
                    StartIndex = CurrentIndex + 1;
                    NoMatches = false;
                }
            }
            catch { return; }
        }
    }
}
