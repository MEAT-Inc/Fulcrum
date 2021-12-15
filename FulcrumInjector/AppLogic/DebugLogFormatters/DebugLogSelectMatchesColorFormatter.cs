using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FulcrumInjector.AppLogic.DebugLogFormatters
{
    /// <summary>
    /// Selects the text matching the given input pattern.
    /// </summary>
    public class DebugLogSelectMatchesColorFormatter : DocumentColorizingTransformer
    {
        // String to use for searching and Regex Toggle
        public readonly bool UseRegex;
        public readonly string MatchString;

        /// <summary>
        /// Builds a new coloring helper using the search params provided
        /// </summary>
        /// <param name="MatchingString">String to find</param>
        /// <param name="UseRegex">Sets if this is a regex or not.</param>
        public DebugLogSelectMatchesColorFormatter(string MatchingString, bool UseRegex = false)
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
                string LoggerName = LineText.Split(']')[3].Trim('[', ']');
                while ((CurrentIndex = LineText.IndexOf(LoggerName, StartIndex)) >= 0)
                {
                    // Change line part call here.
                    int StartOffset = LineStartOffset + CurrentIndex;
                    int EndOffset = StartOffset + LoggerName.Length;
                    base.ChangeLinePart(StartOffset, EndOffset, (NextMatchElement) =>
                    {
                        // Colorize our logger name here.
                        NextMatchElement.TextRunProperties.SetBackgroundBrush(Brushes.Transparent);
                        NextMatchElement.TextRunProperties.SetForegroundBrush(Brushes.DarkCyan);
                    });

                    // Tick our index and move on
                    StartIndex = CurrentIndex + 1;
                }
            }
            catch { return; }
        }
    }
}
